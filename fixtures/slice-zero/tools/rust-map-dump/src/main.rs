//! `rust-map-dump`: the Rust-side entity provider for Slice Zero (CONTRACT
//! §6).
//!
//! Usage:
//!   cargo run --manifest-path tools/rust-map-dump/Cargo.toml -- <crate-dir> > out.json
//!
//! Parses every `.rs` file under `<crate-dir>/src`, using `syn` to extract
//! modules, structs, enums, variants, functions, methods, impls, consts,
//! and fields as CONTRACT §6 `Entity` records, and prints one deterministic
//! (sorted, compact) JSON object to stdout.
//!
//! Module resolution is deliberately simple, not full Cargo-aware crate
//! graph resolution: `src/lib.rs` and `src/main.rs` are treated as the
//! crate root (named after the package's `Cargo.toml` `[package] name`),
//! and each `src/bin/<name>.rs` is treated as its own crate root named
//! `<name>`. Any other `.rs` file (e.g. one only reachable via `mod foo;`
//! elsewhere) is skipped — this fixture's `headscan` crate never has one,
//! and teaching this tool full `mod`-path file resolution isn't needed to
//! satisfy CONTRACT.md.

use sha2::{Digest, Sha256};
use std::fs;
use std::path::{Path, PathBuf};
use syn::spanned::Spanned;
use syn::{Fields, ImplItem, Item};

struct Entity {
    kind: &'static str,
    name: String,
    symbol_path: String,
    file: String,
    start_line: usize,
    end_line: usize,
    content_hash: String,
    parent_symbol_path: Option<String>,
}

fn main() {
    let mut args = std::env::args().skip(1);
    let crate_dir = match args.next() {
        Some(a) => a,
        None => {
            eprintln!("usage: rust-map-dump <crate-dir>");
            std::process::exit(2);
        }
    };

    let crate_dir_path = PathBuf::from(&crate_dir);
    if !crate_dir_path.join("src").is_dir() {
        eprintln!(
            "error: no such directory: {}",
            crate_dir_path.join("src").display()
        );
        std::process::exit(2);
    }

    let entities = dump_entities(&crate_dir_path);

    let provider = format!("rust-map-dump@{}", env!("CARGO_PKG_VERSION"));
    let root = to_forward_slashes(Path::new(&crate_dir));
    println!("{}", render_json(&provider, &root, &entities));
}

/// Walk `<crate_dir>/src`, parse every recognized crate-root `.rs` file,
/// and return every CONTRACT §6 entity found, sorted by (file, startLine,
/// symbolPath).
fn dump_entities(crate_dir: &Path) -> Vec<Entity> {
    let src_dir = crate_dir.join("src");
    let pkg_name = read_package_name(crate_dir).unwrap_or_else(|| "crate".to_string());

    let mut rs_files: Vec<PathBuf> = walkdir::WalkDir::new(&src_dir)
        .into_iter()
        .filter_map(|e| e.ok())
        .filter(|e| e.file_type().is_file())
        .filter(|e| e.path().extension().map(|ext| ext == "rs").unwrap_or(false))
        .map(|e| e.path().to_path_buf())
        .collect();
    rs_files.sort();

    let mut entities: Vec<Entity> = Vec::new();

    for file_path in &rs_files {
        let rel_to_src = to_forward_slashes(
            file_path
                .strip_prefix(&src_dir)
                .expect("walkdir yields paths under src_dir"),
        );
        let (root_name, is_root) = root_module_name(&rel_to_src, &pkg_name);
        if !is_root {
            continue;
        }
        let rel_file = format!("src/{}", rel_to_src);

        let content = fs::read_to_string(file_path)
            .unwrap_or_else(|e| panic!("failed to read {}: {}", file_path.display(), e));
        let parsed = syn::parse_file(&content)
            .unwrap_or_else(|e| panic!("failed to parse {}: {}", file_path.display(), e));

        let total_lines = content.lines().count().max(1);
        let root_hash = hash_span(&content, 1, total_lines);
        entities.push(Entity {
            kind: "module",
            name: root_name.clone(),
            symbol_path: root_name.clone(),
            file: rel_file.clone(),
            start_line: 1,
            end_line: total_lines,
            content_hash: root_hash,
            parent_symbol_path: None,
        });

        visit_items(&parsed.items, &root_name, &rel_file, &content, &mut entities);
    }

    entities.sort_by(|a, b| {
        (a.file.as_str(), a.start_line, a.symbol_path.as_str()).cmp(&(
            b.file.as_str(),
            b.start_line,
            b.symbol_path.as_str(),
        ))
    });

    entities
}

/// Walk one file's top-level items, recursing into `mod { ... }` bodies and
/// `impl` blocks, appending every CONTRACT §6 entity found to `out`.
fn visit_items(
    items: &[Item],
    parent_module_path: &str,
    rel_file: &str,
    source: &str,
    out: &mut Vec<Entity>,
) {
    for item in items {
        match item {
            Item::Mod(m) => {
                let name = m.ident.to_string();
                let symbol_path = format!("{}::{}", parent_module_path, name);
                let (start_line, end_line) = line_span(item.span());
                out.push(Entity {
                    kind: "module",
                    name,
                    symbol_path: symbol_path.clone(),
                    file: rel_file.to_string(),
                    start_line,
                    end_line,
                    content_hash: hash_span(source, start_line, end_line),
                    parent_symbol_path: Some(parent_module_path.to_string()),
                });
                if let Some((_, content)) = &m.content {
                    visit_items(content, &symbol_path, rel_file, source, out);
                }
            }

            Item::Struct(s) => {
                let name = s.ident.to_string();
                let symbol_path = format!("{}::{}", parent_module_path, name);
                let (start_line, end_line) = line_span(item.span());
                out.push(Entity {
                    kind: "struct",
                    name,
                    symbol_path: symbol_path.clone(),
                    file: rel_file.to_string(),
                    start_line,
                    end_line,
                    content_hash: hash_span(source, start_line, end_line),
                    parent_symbol_path: Some(parent_module_path.to_string()),
                });
                emit_fields(&s.fields, &symbol_path, rel_file, source, out);
            }

            Item::Enum(e) => {
                let name = e.ident.to_string();
                let symbol_path = format!("{}::{}", parent_module_path, name);
                let (start_line, end_line) = line_span(item.span());
                out.push(Entity {
                    kind: "enum",
                    name,
                    symbol_path: symbol_path.clone(),
                    file: rel_file.to_string(),
                    start_line,
                    end_line,
                    content_hash: hash_span(source, start_line, end_line),
                    parent_symbol_path: Some(parent_module_path.to_string()),
                });
                for variant in &e.variants {
                    let vname = variant.ident.to_string();
                    let vsym = format!("{}::{}", symbol_path, vname);
                    let (vs, ve) = line_span(variant.span());
                    out.push(Entity {
                        kind: "variant",
                        name: vname,
                        symbol_path: vsym.clone(),
                        file: rel_file.to_string(),
                        start_line: vs,
                        end_line: ve,
                        content_hash: hash_span(source, vs, ve),
                        parent_symbol_path: Some(symbol_path.clone()),
                    });
                    emit_fields(&variant.fields, &vsym, rel_file, source, out);
                }
            }

            Item::Fn(f) => {
                let name = f.sig.ident.to_string();
                let symbol_path = format!("{}::{}", parent_module_path, name);
                let (start_line, end_line) = line_span(item.span());
                out.push(Entity {
                    kind: "fn",
                    name,
                    symbol_path,
                    file: rel_file.to_string(),
                    start_line,
                    end_line,
                    content_hash: hash_span(source, start_line, end_line),
                    parent_symbol_path: Some(parent_module_path.to_string()),
                });
            }

            Item::Const(c) => {
                let name = c.ident.to_string();
                let symbol_path = format!("{}::{}", parent_module_path, name);
                let (start_line, end_line) = line_span(item.span());
                out.push(Entity {
                    kind: "const",
                    name,
                    symbol_path,
                    file: rel_file.to_string(),
                    start_line,
                    end_line,
                    content_hash: hash_span(source, start_line, end_line),
                    parent_symbol_path: Some(parent_module_path.to_string()),
                });
            }

            Item::Impl(im) => {
                let type_name = type_to_string(&im.self_ty);
                let type_symbol_path = format!("{}::{}", parent_module_path, type_name);
                // The impl block's own symbolPath is deliberately distinct
                // from `type_symbol_path` (kept under a synthetic "::impl"
                // segment — `impl` is a reserved word, so it can never
                // collide with a real identifier) even though `kind`
                // already disambiguates entityId: two entities sharing a
                // literal symbolPath is needlessly confusing for anything
                // (e.g. ground-truth.yaml correspondences) that references
                // symbolPath alone. Method/const symbolPaths still use
                // `type_symbol_path` directly, per CONTRACT §6 ("methods
                // as path::Type::method") — only the impl entity itself,
                // and its children's *parent* pointer, use this.
                let (impl_name, impl_symbol_path) = match &im.trait_ {
                    Some((_, trait_path, _)) => {
                        let trait_name = path_to_string(trait_path);
                        (
                            format!("impl {} for {}", trait_name, type_name),
                            format!("{}::impl as {}", type_symbol_path, trait_name),
                        )
                    }
                    None => (
                        format!("impl {}", type_name),
                        format!("{}::impl", type_symbol_path),
                    ),
                };
                let (start_line, end_line) = line_span(item.span());
                out.push(Entity {
                    kind: "impl",
                    name: impl_name,
                    symbol_path: impl_symbol_path.clone(),
                    file: rel_file.to_string(),
                    start_line,
                    end_line,
                    content_hash: hash_span(source, start_line, end_line),
                    parent_symbol_path: Some(parent_module_path.to_string()),
                });

                for ii in &im.items {
                    match ii {
                        ImplItem::Fn(mf) => {
                            let mname = mf.sig.ident.to_string();
                            let msym = format!("{}::{}", type_symbol_path, mname);
                            let (ms, me) = line_span(ii.span());
                            out.push(Entity {
                                kind: "method",
                                name: mname,
                                symbol_path: msym,
                                file: rel_file.to_string(),
                                start_line: ms,
                                end_line: me,
                                content_hash: hash_span(source, ms, me),
                                parent_symbol_path: Some(impl_symbol_path.clone()),
                            });
                        }
                        ImplItem::Const(ic) => {
                            let cname = ic.ident.to_string();
                            let csym = format!("{}::{}", type_symbol_path, cname);
                            let (cs, ce) = line_span(ii.span());
                            out.push(Entity {
                                kind: "const",
                                name: cname,
                                symbol_path: csym,
                                file: rel_file.to_string(),
                                start_line: cs,
                                end_line: ce,
                                content_hash: hash_span(source, cs, ce),
                                parent_symbol_path: Some(impl_symbol_path.clone()),
                            });
                        }
                        _ => {}
                    }
                }
            }

            _ => {}
        }
    }
}

fn emit_fields(
    fields: &Fields,
    parent_symbol_path: &str,
    rel_file: &str,
    source: &str,
    out: &mut Vec<Entity>,
) {
    match fields {
        Fields::Named(named) => {
            for f in &named.named {
                let fname = f
                    .ident
                    .as_ref()
                    .expect("named field has an ident")
                    .to_string();
                let fsym = format!("{}::{}", parent_symbol_path, fname);
                let (fs, fe) = line_span(f.span());
                out.push(Entity {
                    kind: "field",
                    name: fname,
                    symbol_path: fsym,
                    file: rel_file.to_string(),
                    start_line: fs,
                    end_line: fe,
                    content_hash: hash_span(source, fs, fe),
                    parent_symbol_path: Some(parent_symbol_path.to_string()),
                });
            }
        }
        Fields::Unnamed(unnamed) => {
            for (idx, f) in unnamed.unnamed.iter().enumerate() {
                let fname = idx.to_string();
                let fsym = format!("{}::{}", parent_symbol_path, fname);
                let (fs, fe) = line_span(f.span());
                out.push(Entity {
                    kind: "field",
                    name: fname,
                    symbol_path: fsym,
                    file: rel_file.to_string(),
                    start_line: fs,
                    end_line: fe,
                    content_hash: hash_span(source, fs, fe),
                    parent_symbol_path: Some(parent_symbol_path.to_string()),
                });
            }
        }
        Fields::Unit => {}
    }
}

fn line_span(span: proc_macro2::Span) -> (usize, usize) {
    (span.start().line, span.end().line)
}

/// sha256 hex of the (1-based, inclusive) line range `[start_line,
/// end_line]` of `source`, reconstructed with `\n` line separators only
/// (CONTRACT §6: "\r\n normalized to \n").
fn hash_span(source: &str, start_line: usize, end_line: usize) -> String {
    let lines: Vec<&str> = source.split('\n').collect();
    let start_idx = start_line.saturating_sub(1);
    let end_idx = end_line
        .saturating_sub(1)
        .min(lines.len().saturating_sub(1));

    let mut buf = String::new();
    for i in start_idx..=end_idx {
        if i > start_idx {
            buf.push('\n');
        }
        let line = lines.get(i).copied().unwrap_or("");
        let line = line.strip_suffix('\r').unwrap_or(line);
        buf.push_str(line);
    }

    let mut hasher = Sha256::new();
    hasher.update(buf.as_bytes());
    hex_encode(&hasher.finalize())
}

fn hex_encode(bytes: &[u8]) -> String {
    let mut s = String::with_capacity(bytes.len() * 2);
    for b in bytes {
        s.push_str(&format!("{:02x}", b));
    }
    s
}

/// Join a `syn::Path`'s segment identifiers with `::`, ignoring any generic
/// arguments (sufficient for the simple paths this fixture's crates use).
fn path_to_string(path: &syn::Path) -> String {
    path.segments
        .iter()
        .map(|s| s.ident.to_string())
        .collect::<Vec<_>>()
        .join("::")
}

/// The "simple name" of a type for symbolPath purposes: the last path
/// segment's identifier for `syn::Type::Path` (the overwhelmingly common
/// case for `impl Self` types), falling back to a `Debug` rendering
/// (`extra-traits` feature) for anything more exotic.
fn type_to_string(ty: &syn::Type) -> String {
    match ty {
        syn::Type::Path(tp) => tp
            .path
            .segments
            .last()
            .map(|s| s.ident.to_string())
            .unwrap_or_else(|| "?".to_string()),
        other => format!("{:?}", other),
    }
}

/// Classify a file's path (relative to `<crate-dir>/src`, forward slashes)
/// as a crate root or not, and if so, the module name it roots.
fn root_module_name(rel_to_src: &str, pkg_name: &str) -> (String, bool) {
    if rel_to_src == "lib.rs" || rel_to_src == "main.rs" {
        (pkg_name.to_string(), true)
    } else if let Some(stripped) = rel_to_src.strip_prefix("bin/") {
        match stripped.strip_suffix(".rs") {
            Some(stem) if !stem.is_empty() => (stem.to_string(), true),
            _ => (String::new(), false),
        }
    } else {
        (String::new(), false)
    }
}

/// Minimal `[package] name = "..."` scrape from Cargo.toml — avoids taking
/// a `toml` crate dependency for one field.
fn read_package_name(crate_dir: &Path) -> Option<String> {
    let cargo_toml = fs::read_to_string(crate_dir.join("Cargo.toml")).ok()?;
    let mut in_package = false;
    for line in cargo_toml.lines() {
        let trimmed = line.trim();
        if trimmed.starts_with('[') {
            in_package = trimmed == "[package]";
            continue;
        }
        if !in_package {
            continue;
        }
        if let Some(rest) = trimmed.strip_prefix("name") {
            let rest = rest.trim_start();
            if let Some(rest) = rest.strip_prefix('=') {
                let value = rest.trim().trim_matches('"');
                return Some(value.to_string());
            }
        }
    }
    None
}

fn to_forward_slashes(p: &Path) -> String {
    p.components()
        .map(|c| c.as_os_str().to_string_lossy().into_owned())
        .collect::<Vec<_>>()
        .join("/")
}

// ---------------------------------------------------------------------
// Canonical JSON (CONTRACT §6). Built by hand, field by field, for the same
// byte-exact-order reason as headscan's canonical result JSON.
// ---------------------------------------------------------------------

fn render_json(provider: &str, root: &str, entities: &[Entity]) -> String {
    let mut out = String::new();
    out.push_str("{\"provider\":");
    push_json_string(&mut out, provider);
    out.push_str(",\"root\":");
    push_json_string(&mut out, root);
    out.push_str(",\"entities\":[");
    for (i, e) in entities.iter().enumerate() {
        if i > 0 {
            out.push(',');
        }
        out.push_str("{\"kind\":");
        push_json_string(&mut out, e.kind);
        out.push_str(",\"name\":");
        push_json_string(&mut out, &e.name);
        out.push_str(",\"symbolPath\":");
        push_json_string(&mut out, &e.symbol_path);
        out.push_str(",\"file\":");
        push_json_string(&mut out, &e.file);
        out.push_str(",\"startLine\":");
        out.push_str(&e.start_line.to_string());
        out.push_str(",\"endLine\":");
        out.push_str(&e.end_line.to_string());
        out.push_str(",\"contentHash\":");
        push_json_string(&mut out, &e.content_hash);
        out.push_str(",\"parentSymbolPath\":");
        match &e.parent_symbol_path {
            Some(p) => push_json_string(&mut out, p),
            None => out.push_str("null"),
        }
        out.push('}');
    }
    out.push_str("]}");
    out
}

fn push_json_string(out: &mut String, s: &str) {
    out.push('"');
    for c in s.chars() {
        match c {
            '"' => out.push_str("\\\""),
            '\\' => out.push_str("\\\\"),
            '\n' => out.push_str("\\n"),
            '\r' => out.push_str("\\r"),
            '\t' => out.push_str("\\t"),
            c if (c as u32) < 0x20 => {
                out.push_str(&format!("\\u{:04x}", c as u32));
            }
            c => out.push(c),
        }
    }
    out.push('"');
}

#[cfg(test)]
mod tests {
    use super::*;

    /// A fresh `<tmp>/rust-map-dump-test-<pid>-<name>/` directory with an
    /// empty `src/` already created, so tests just need to write
    /// Cargo.toml and source files into it.
    fn temp_crate_dir(name: &str) -> PathBuf {
        let dir =
            std::env::temp_dir().join(format!("rust-map-dump-test-{}-{}", std::process::id(), name));
        let _ = fs::remove_dir_all(&dir);
        fs::create_dir_all(dir.join("src")).unwrap();
        dir
    }

    #[test]
    fn path_to_string_joins_segments() {
        let path: syn::Path = syn::parse_str("std::fmt::Display").unwrap();
        assert_eq!(path_to_string(&path), "std::fmt::Display");
    }

    #[test]
    fn type_to_string_uses_last_segment_of_a_path_type() {
        let ty: syn::Type = syn::parse_str("std::result::Result").unwrap();
        assert_eq!(type_to_string(&ty), "Result");
    }

    #[test]
    fn type_to_string_falls_back_for_non_path_types() {
        let ty: syn::Type = syn::parse_str("(u8, u8)").unwrap();
        // The exact Debug rendering isn't part of the contract; just check
        // it doesn't panic and yields something non-empty.
        assert!(!type_to_string(&ty).is_empty());
    }

    #[test]
    fn root_module_name_lib_and_main() {
        assert_eq!(
            root_module_name("lib.rs", "foo"),
            ("foo".to_string(), true)
        );
        assert_eq!(
            root_module_name("main.rs", "foo"),
            ("foo".to_string(), true)
        );
    }

    #[test]
    fn root_module_name_bin_uses_file_stem() {
        assert_eq!(
            root_module_name("bin/headscan-cases.rs", "headscan"),
            ("headscan-cases".to_string(), true)
        );
    }

    #[test]
    fn root_module_name_unresolvable_file_is_skipped() {
        assert_eq!(root_module_name("util.rs", "foo"), (String::new(), false));
    }

    #[test]
    fn hash_span_selects_the_requested_line_range() {
        let source = "a\nb\nc\n";
        let mut hasher = Sha256::new();
        hasher.update(b"b");
        assert_eq!(hash_span(source, 2, 2), hex_encode(&hasher.finalize()));

        let mut hasher = Sha256::new();
        hasher.update(b"a\nb");
        assert_eq!(hash_span(source, 1, 2), hex_encode(&hasher.finalize()));
    }

    #[test]
    fn hash_span_normalizes_crlf_to_lf() {
        let lf_source = "a\nb\nc\n";
        let crlf_source = "a\r\nb\r\nc\r\n";
        assert_eq!(hash_span(lf_source, 1, 3), hash_span(crlf_source, 1, 3));
    }

    #[test]
    fn read_package_name_scrapes_the_package_section_only() {
        let dir = temp_crate_dir("read-package-name");
        fs::write(
            dir.join("Cargo.toml"),
            "[package]\nname = \"widgets\"\nversion = \"0.1.0\"\n\n[dependencies]\nname = \"not-this-one\"\n",
        )
        .unwrap();

        assert_eq!(read_package_name(&dir), Some("widgets".to_string()));

        let _ = fs::remove_dir_all(&dir);
    }

    #[test]
    fn dump_entities_end_to_end_over_a_tiny_crate() {
        let dir = temp_crate_dir("end-to-end");
        fs::write(dir.join("Cargo.toml"), "[package]\nname = \"widgets\"\n").unwrap();
        fs::write(
            dir.join("src").join("lib.rs"),
            "pub const LIMIT: u32 = 10;\n\npub struct Point {\n    pub x: i32,\n    pub y: i32,\n}\n\nimpl Point {\n    pub fn origin() -> Point {\n        Point { x: 0, y: 0 }\n    }\n}\n",
        )
        .unwrap();

        let entities = dump_entities(&dir);
        let by_symbol: std::collections::BTreeMap<&str, &Entity> = entities
            .iter()
            .map(|e| (e.symbol_path.as_str(), e))
            .collect();

        assert_eq!(by_symbol["widgets"].kind, "module");
        assert_eq!(by_symbol["widgets"].parent_symbol_path, None);

        assert_eq!(by_symbol["widgets::LIMIT"].kind, "const");
        assert_eq!(
            by_symbol["widgets::LIMIT"].parent_symbol_path.as_deref(),
            Some("widgets")
        );

        assert_eq!(by_symbol["widgets::Point"].kind, "struct");
        assert_eq!(by_symbol["widgets::Point::x"].kind, "field");
        assert_eq!(by_symbol["widgets::Point::y"].kind, "field");

        // The impl block gets its own symbolPath, distinct from the
        // struct's (see the comment at its construction site).
        assert_eq!(by_symbol["widgets::Point::impl"].kind, "impl");
        assert_eq!(
            by_symbol["widgets::Point::impl"].parent_symbol_path.as_deref(),
            Some("widgets")
        );

        assert_eq!(
            by_symbol["widgets::Point::origin"].kind,
            "method"
        );
        assert_eq!(
            by_symbol["widgets::Point::origin"]
                .parent_symbol_path
                .as_deref(),
            Some("widgets::Point::impl")
        );

        let _ = fs::remove_dir_all(&dir);
    }

    #[test]
    fn dump_entities_sorted_by_file_then_start_line() {
        let dir = temp_crate_dir("sorted");
        fs::write(dir.join("Cargo.toml"), "[package]\nname = \"widgets\"\n").unwrap();
        fs::write(
            dir.join("src").join("lib.rs"),
            "pub fn b() {}\npub fn a() {}\n",
        )
        .unwrap();

        let entities = dump_entities(&dir);
        let fn_names: Vec<&str> = entities
            .iter()
            .filter(|e| e.kind == "fn")
            .map(|e| e.name.as_str())
            .collect();
        // Sorted by startLine (rule: file, startLine, symbolPath), not by
        // name, so "b" (declared first) comes before "a".
        assert_eq!(fn_names, vec!["b", "a"]);

        let _ = fs::remove_dir_all(&dir);
    }

    #[test]
    fn dump_entities_places_bin_target_at_its_own_crate_root() {
        let dir = temp_crate_dir("bin-root");
        fs::write(dir.join("Cargo.toml"), "[package]\nname = \"widgets\"\n").unwrap();
        fs::create_dir_all(dir.join("src").join("bin")).unwrap();
        fs::write(dir.join("src").join("lib.rs"), "pub fn lib_fn() {}\n").unwrap();
        fs::write(
            dir.join("src").join("bin").join("tool.rs"),
            "fn main() {}\n",
        )
        .unwrap();

        let entities = dump_entities(&dir);
        let symbols: std::collections::BTreeSet<&str> =
            entities.iter().map(|e| e.symbol_path.as_str()).collect();

        assert!(symbols.contains("widgets"));
        assert!(symbols.contains("widgets::lib_fn"));
        assert!(symbols.contains("tool"));
        assert!(symbols.contains("tool::main"));

        let _ = fs::remove_dir_all(&dir);
    }

    #[test]
    fn render_json_produces_expected_field_order_and_null_parent() {
        let entities = vec![Entity {
            kind: "const",
            name: "X".to_string(),
            symbol_path: "crate::X".to_string(),
            file: "src/lib.rs".to_string(),
            start_line: 1,
            end_line: 1,
            content_hash: "deadbeef".to_string(),
            parent_symbol_path: None,
        }];
        let json = render_json("rust-map-dump@0.0.0", "rust", &entities);
        assert_eq!(
            json,
            "{\"provider\":\"rust-map-dump@0.0.0\",\"root\":\"rust\",\"entities\":\
             [{\"kind\":\"const\",\"name\":\"X\",\"symbolPath\":\"crate::X\",\
             \"file\":\"src/lib.rs\",\"startLine\":1,\"endLine\":1,\
             \"contentHash\":\"deadbeef\",\"parentSymbolPath\":null}]}"
        );
    }
}
