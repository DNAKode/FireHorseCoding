//! `rust-map-dump`: the Rust-side entity provider for Slice Zero (CONTRACT
//! §6, extended by KodePorter.Core CONTRACT-M15.md §5 for v1.1 -- multi-
//! crate discovery + the optional `isTest`/`resolution` imperfection
//! fields).
//!
//! Usage:
//!   cargo run --manifest-path tools/rust-map-dump/Cargo.toml -- <root-dir> > out.json
//!
//! `<root-dir>` may be a single crate (a directory with its own
//! `Cargo.toml`) or a tree containing several crates (a Cargo workspace,
//! or just an arbitrary directory tree with multiple crates nested inside
//! it): every `Cargo.toml` under `<root-dir>` that has a `[package]`
//! section is treated as its own crate (a `[workspace]`-only manifest with
//! no `[package]` section is not itself a crate and contributes no
//! entities of its own -- only its member crates do, since each has its
//! own `Cargo.toml`). Any `target/` directory, wherever it occurs, is
//! pruned from every walk (both when discovering `Cargo.toml` files and
//! when discovering `.rs` files within a crate).
//!
//! For each discovered crate, `src/**/*.rs` is parsed with `syn` to
//! extract modules, structs, enums, variants, functions, methods, impls,
//! consts, and fields as CONTRACT §6 `Entity` records. `tests/*.rs`
//! (direct children only -- see the module-resolution note below) is
//! parsed the same way, with every entity from such a file marked
//! `isTest: true`; entities anywhere whose symbolPath contains
//! `::tests::` (a `#[cfg(test)] mod tests { ... }` block) are also marked
//! `isTest: true` (CONTRACT-M15 §1.1). One deterministic (sorted,
//! compact) JSON object is printed to stdout, covering every discovered
//! crate's entities together.
//!
//! Module resolution is deliberately simple, not full Cargo-aware crate
//! graph resolution: within a crate, `src/lib.rs` and `src/main.rs` are
//! treated as the crate root (named after that crate's own `Cargo.toml`
//! `[package] name`), each `src/bin/<name>.rs` is treated as its own
//! crate root named `<name>`, and each *direct child* `tests/<name>.rs` is
//! treated as its own (test) crate root named `<name>` -- matching what
//! Cargo itself would compile each of those files as. Any other `.rs`
//! file (one only reachable via `mod foo;` elsewhere, or nested under a
//! `tests/<subdir>/`) is skipped -- this fixture's crates never have one,
//! and teaching this tool full `mod`-path file resolution isn't needed to
//! satisfy CONTRACT.md.
//!
//! `symbolPath`s are already "prefixed with the crate name" (CONTRACT-M15
//! §5) by construction: a crate root's name IS that crate's own name in
//! Rust's sense (the package name for `lib.rs`/`main.rs`, the target's own
//! name for a `bin`/`tests` file -- matching what `rustc --crate-name`
//! would call it), and every descendant symbolPath is built by appending
//! onto its root. This is why a single-crate root's output is
//! byte-identical before and after v1.1's multi-crate discovery was
//! added: discovering exactly one crate (at the root itself) and walking
//! it is exactly what v1.0 already did.
//!
//! `file` in the emitted JSON is relative to `<root-dir>` (not to the
//! individual crate), so that files from different crates never collide
//! on `file` -- for a single-crate root this is identical to the old
//! crate-relative path (the crate dir IS the root dir there), so v1.0
//! output is unaffected.
//!
//! A file that fails to read or fails to `syn::parse_file` no longer
//! aborts the whole dump (v1.1, CONTRACT-M15 §5): it contributes a single
//! file-level entity (same root-naming rules as above, no descendants)
//! with `"resolution": "gap"` instead.

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
    /// CONTRACT-M15 §1.1: rust test-ness is symbolPath containing
    /// `::tests::`, or file under a `tests/` directory. Computed in one
    /// post-pass over the whole entity set (`mark_test_entities`) rather
    /// than threaded through construction, so every entity -- gap
    /// entities included -- gets the same treatment uniformly.
    is_test: bool,
    /// `"clean"` or `"gap"` (CONTRACT-M15 §1.1/§5); `"degraded"` is a
    /// C#-side-only resolution grade per §1.1, not produced here.
    resolution: &'static str,
}

fn main() {
    let mut args = std::env::args().skip(1);
    let root_arg = match args.next() {
        Some(a) => a,
        None => {
            eprintln!("usage: rust-map-dump <root-dir>");
            std::process::exit(2);
        }
    };

    let root_path = PathBuf::from(&root_arg);
    let crates = discover_crates(&root_path);
    if crates.is_empty() {
        eprintln!(
            "error: no Cargo.toml with a [package] section found under {} (excluding target/ dirs)",
            root_path.display()
        );
        std::process::exit(2);
    }

    let entities = build_entities(&root_path, &crates);

    let provider = format!("rust-map-dump@{}", env!("CARGO_PKG_VERSION"));
    let root = to_forward_slashes(Path::new(&root_arg));
    println!("{}", render_json(&provider, &root, &entities));
}

/// Find every `Cargo.toml` under `root` (pruning any directory literally
/// named `target` from the walk) that has a `[package]` section, returning
/// `(crate_dir, package_name)` pairs sorted by `crate_dir` for
/// determinism. A `[workspace]`-only manifest (no `[package]`) is skipped
/// -- it isn't itself a crate.
fn discover_crates(root: &Path) -> Vec<(PathBuf, String)> {
    let mut manifest_paths: Vec<PathBuf> = walkdir::WalkDir::new(root)
        .into_iter()
        .filter_entry(|e| e.file_name().to_string_lossy() != "target")
        .filter_map(|e| e.ok())
        .filter(|e| e.file_type().is_file())
        .filter(|e| e.file_name() == "Cargo.toml")
        .map(|e| e.path().to_path_buf())
        .collect();
    manifest_paths.sort();

    let mut crates = Vec::new();
    for manifest_path in manifest_paths {
        let crate_dir = manifest_path
            .parent()
            .expect("a Cargo.toml path always has a parent directory")
            .to_path_buf();
        if let Some(pkg_name) = read_package_name(&crate_dir) {
            crates.push((crate_dir, pkg_name));
        }
    }
    crates
}

/// Walk every discovered crate, collect every CONTRACT §6 entity, mark
/// test-ness (CONTRACT-M15 §1.1), and return them sorted by (file,
/// startLine, symbolPath).
fn build_entities(root: &Path, crates: &[(PathBuf, String)]) -> Vec<Entity> {
    let mut entities: Vec<Entity> = Vec::new();
    for (crate_dir, pkg_name) in crates {
        dump_crate_entities(root, crate_dir, pkg_name, &mut entities);
    }

    mark_test_entities(&mut entities);

    entities.sort_by(|a, b| {
        (a.file.as_str(), a.start_line, a.symbol_path.as_str()).cmp(&(
            b.file.as_str(),
            b.start_line,
            b.symbol_path.as_str(),
        ))
    });

    entities
}

/// Convenience wrapper combining discovery + entity-building in one call,
/// for callers (tests) that just want "every entity under this root".
#[cfg(test)]
fn dump_entities(root: &Path) -> Vec<Entity> {
    let crates = discover_crates(root);
    build_entities(root, &crates)
}

/// CONTRACT-M15 §1.1: `isTest` is true when a symbolPath contains
/// `::tests::`, or the entity's file sits under a `tests/` directory
/// (checked by path *segment*, not substring, so a crate legitimately
/// named e.g. `mytests` doesn't false-positive).
fn mark_test_entities(entities: &mut [Entity]) {
    for e in entities.iter_mut() {
        if e.symbol_path.contains("::tests::") || file_under_tests_dir(&e.file) {
            e.is_test = true;
        }
    }
}

fn file_under_tests_dir(rel_file: &str) -> bool {
    rel_file.split('/').any(|seg| seg == "tests")
}

/// Process one discovered crate: `src/**` (recognized roots: `lib.rs`,
/// `main.rs`, `bin/<name>.rs`) and `tests/*.rs` (direct children only),
/// appending every entity found to `out`. `root` is the overall discovery
/// root (used to compute root-relative `file` paths so entities from
/// different crates never collide on `file`).
fn dump_crate_entities(root: &Path, crate_dir: &Path, pkg_name: &str, out: &mut Vec<Entity>) {
    let src_dir = crate_dir.join("src");
    if src_dir.is_dir() {
        for file_path in list_rs_files(&src_dir) {
            let rel_to_src = to_forward_slashes(
                file_path
                    .strip_prefix(&src_dir)
                    .expect("walkdir yields paths under src_dir"),
            );
            let (root_name, is_root) = root_module_name(&rel_to_src, pkg_name);
            if !is_root {
                continue;
            }
            let rel_file = root_relative_file(root, &file_path);
            process_root_file(&file_path, &root_name, &rel_file, out);
        }
    }

    let tests_dir = crate_dir.join("tests");
    if tests_dir.is_dir() {
        for file_path in list_rs_files(&tests_dir) {
            let rel_to_tests = to_forward_slashes(
                file_path
                    .strip_prefix(&tests_dir)
                    .expect("walkdir yields paths under tests_dir"),
            );
            // Only direct children of tests/ compile as their own
            // integration-test crate (Cargo's own rule); anything nested
            // under a subdirectory (e.g. tests/common/mod.rs helpers) is
            // skipped -- same simplification as unreachable non-root files
            // under src/ (see module doc comment).
            let root_name = match rel_to_tests.strip_suffix(".rs") {
                Some(stem) if !stem.is_empty() && !stem.contains('/') => stem.to_string(),
                _ => continue,
            };
            let rel_file = root_relative_file(root, &file_path);
            process_root_file(&file_path, &root_name, &rel_file, out);
        }
    }
}

/// Every `.rs` file under `dir` (recursive, `target/` pruned), sorted.
fn list_rs_files(dir: &Path) -> Vec<PathBuf> {
    let mut rs_files: Vec<PathBuf> = walkdir::WalkDir::new(dir)
        .into_iter()
        .filter_entry(|e| e.file_name().to_string_lossy() != "target")
        .filter_map(|e| e.ok())
        .filter(|e| e.file_type().is_file())
        .filter(|e| e.path().extension().map(|ext| ext == "rs").unwrap_or(false))
        .map(|e| e.path().to_path_buf())
        .collect();
    rs_files.sort();
    rs_files
}

fn root_relative_file(root: &Path, file_path: &Path) -> String {
    to_forward_slashes(
        file_path
            .strip_prefix(root)
            .expect("discovered files are always under the discovery root"),
    )
}

/// Read + parse one recognized crate-root `.rs` file and append either its
/// full entity tree (module root + descendants) or, if reading/parsing
/// fails, a single `resolution: "gap"` file-level entity (CONTRACT-M15
/// §5) -- never aborts the whole dump.
fn process_root_file(file_path: &Path, root_name: &str, rel_file: &str, out: &mut Vec<Entity>) {
    // Read as raw bytes + lossy-decode rather than `fs::read_to_string`:
    // invalid UTF-8 no longer needs its own failure branch -- the lossy
    // text will simply fail `syn::parse_file` below and fall into the gap
    // path like any other unparseable file. A true read failure (permission
    // denied, file vanished mid-walk) still can't produce any text, so it
    // gets its own (empty-content) gap entity.
    let content = match fs::read(file_path) {
        Ok(bytes) => String::from_utf8_lossy(&bytes).into_owned(),
        Err(_) => {
            push_gap_entity(root_name, rel_file, "", 1, out);
            return;
        }
    };

    let total_lines = content.lines().count().max(1);

    match syn::parse_file(&content) {
        Ok(parsed) => {
            out.push(Entity {
                kind: "module",
                name: root_name.to_string(),
                symbol_path: root_name.to_string(),
                file: rel_file.to_string(),
                start_line: 1,
                end_line: total_lines,
                content_hash: hash_span(&content, 1, total_lines),
                parent_symbol_path: None,
                is_test: false,
                resolution: "clean",
            });
            visit_items(&parsed.items, root_name, rel_file, &content, out);
        }
        Err(_) => push_gap_entity(root_name, rel_file, &content, total_lines, out),
    }
}

fn push_gap_entity(
    root_name: &str,
    rel_file: &str,
    content: &str,
    total_lines: usize,
    out: &mut Vec<Entity>,
) {
    out.push(Entity {
        kind: "module",
        name: root_name.to_string(),
        symbol_path: root_name.to_string(),
        file: rel_file.to_string(),
        start_line: 1,
        end_line: total_lines,
        content_hash: hash_span(content, 1, total_lines),
        parent_symbol_path: None,
        is_test: false,
        resolution: "gap",
    });
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
                    is_test: false,
                    resolution: "clean",
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
                    is_test: false,
                    resolution: "clean",
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
                    is_test: false,
                    resolution: "clean",
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
                        is_test: false,
                        resolution: "clean",
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
                    is_test: false,
                    resolution: "clean",
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
                    is_test: false,
                    resolution: "clean",
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
                    is_test: false,
                    resolution: "clean",
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
                                is_test: false,
                                resolution: "clean",
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
                                is_test: false,
                                resolution: "clean",
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
                    is_test: false,
                    resolution: "clean",
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
                    is_test: false,
                    resolution: "clean",
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
/// a `toml` crate dependency for one field. Returns `None` for a manifest
/// with no `[package]` section (e.g. a pure `[workspace]` manifest), which
/// `discover_crates` uses to decide a `Cargo.toml` isn't its own crate.
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
// Canonical JSON (CONTRACT §6, extended CONTRACT-M15 §5). Built by hand,
// field by field, for the same byte-exact-order reason as headscan's
// canonical result JSON. `isTest`/`resolution` are appended only when
// non-default ("absent -> clean/0", CONTRACT-M15 §1.1) so a dump with no
// test files and no gaps renders byte-identically to the v1.0 shape.
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
        if e.is_test {
            out.push_str(",\"isTest\":true");
        }
        if e.resolution != "clean" {
            out.push_str(",\"resolution\":");
            push_json_string(&mut out, e.resolution);
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

    /// A fresh `<tmp>/rust-map-dump-test-<pid>-<name>/` directory (empty),
    /// so tests just need to write Cargo.toml and source files into it.
    fn temp_crate_dir(name: &str) -> PathBuf {
        let dir =
            std::env::temp_dir().join(format!("rust-map-dump-test-{}-{}", std::process::id(), name));
        let _ = fs::remove_dir_all(&dir);
        fs::create_dir_all(&dir).unwrap();
        dir
    }

    fn write_crate(dir: &Path, pkg_name: &str, lib_rs: &str) {
        fs::create_dir_all(dir.join("src")).unwrap();
        fs::write(
            dir.join("Cargo.toml"),
            format!("[package]\nname = \"{}\"\n", pkg_name),
        )
        .unwrap();
        fs::write(dir.join("src").join("lib.rs"), lib_rs).unwrap();
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
    fn read_package_name_none_for_workspace_only_manifest() {
        let dir = temp_crate_dir("workspace-only-manifest");
        fs::write(
            dir.join("Cargo.toml"),
            "[workspace]\nmembers = [\"a\", \"b\"]\nresolver = \"2\"\n",
        )
        .unwrap();

        assert_eq!(read_package_name(&dir), None);

        let _ = fs::remove_dir_all(&dir);
    }

    #[test]
    fn dump_entities_end_to_end_over_a_tiny_crate() {
        let dir = temp_crate_dir("end-to-end");
        fs::write(dir.join("Cargo.toml"), "[package]\nname = \"widgets\"\n").unwrap();
        fs::create_dir_all(dir.join("src")).unwrap();
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
        assert_eq!(by_symbol["widgets"].file, "src/lib.rs");
        assert!(!by_symbol["widgets"].is_test);
        assert_eq!(by_symbol["widgets"].resolution, "clean");

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
        fs::create_dir_all(dir.join("src")).unwrap();
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

    // ------------------------------------------------------------------
    // v1.1: multi-crate discovery (CONTRACT-M15 §5).
    // ------------------------------------------------------------------

    #[test]
    fn discover_crates_finds_every_package_manifest_and_skips_workspace_only_ones() {
        let root = temp_crate_dir("discover-multi");
        fs::write(
            root.join("Cargo.toml"),
            "[workspace]\nmembers = [\"alpha\", \"beta\"]\n",
        )
        .unwrap();
        write_crate(&root.join("alpha"), "alpha", "pub fn a_fn() {}\n");
        write_crate(&root.join("beta"), "beta", "pub fn b_fn() {}\n");

        let mut crates = discover_crates(&root);
        crates.sort_by(|a, b| a.1.cmp(&b.1));
        let names: Vec<&str> = crates.iter().map(|(_, n)| n.as_str()).collect();
        assert_eq!(names, vec!["alpha", "beta"]);

        let _ = fs::remove_dir_all(&root);
    }

    #[test]
    fn dump_entities_over_a_synthetic_two_crate_workspace() {
        let root = temp_crate_dir("two-crate-workspace");
        fs::write(
            root.join("Cargo.toml"),
            "[workspace]\nmembers = [\"alpha\", \"beta\"]\n",
        )
        .unwrap();
        write_crate(
            &root.join("alpha"),
            "alpha",
            "pub fn shared_name() {}\n",
        );
        write_crate(&root.join("beta"), "beta", "pub fn shared_name() {}\n");

        let entities = dump_entities(&root);
        let symbols: std::collections::BTreeSet<&str> =
            entities.iter().map(|e| e.symbol_path.as_str()).collect();

        // Each crate's root module is named after its own package, and
        // every descendant symbolPath is naturally disambiguated by that
        // root -- no collision between the two crates' same-named fns.
        assert!(symbols.contains("alpha"));
        assert!(symbols.contains("beta"));
        assert!(symbols.contains("alpha::shared_name"));
        assert!(symbols.contains("beta::shared_name"));

        // `file` is root-relative, so same-named files in different
        // crates don't collide either.
        let by_symbol: std::collections::BTreeMap<&str, &Entity> = entities
            .iter()
            .map(|e| (e.symbol_path.as_str(), e))
            .collect();
        assert_eq!(by_symbol["alpha"].file, "alpha/src/lib.rs");
        assert_eq!(by_symbol["beta"].file, "beta/src/lib.rs");

        let _ = fs::remove_dir_all(&root);
    }

    #[test]
    fn dump_entities_single_crate_root_matches_v1_0_shape() {
        // A root directory that IS a crate (single Cargo.toml, right at
        // the root) discovers exactly that one crate and walks it exactly
        // as v1.0 did directly -- the multi-crate discovery pass is a
        // strict generalization, not a behavior change, for this case.
        let dir = temp_crate_dir("single-crate-unchanged");
        write_crate(&dir, "widgets", "pub fn only_fn() {}\n");

        let entities = dump_entities(&dir);
        let symbols: std::collections::BTreeSet<&str> =
            entities.iter().map(|e| e.symbol_path.as_str()).collect();
        assert!(symbols.contains("widgets"));
        assert!(symbols.contains("widgets::only_fn"));

        let by_symbol: std::collections::BTreeMap<&str, &Entity> = entities
            .iter()
            .map(|e| (e.symbol_path.as_str(), e))
            .collect();
        assert_eq!(by_symbol["widgets"].file, "src/lib.rs");

        let _ = fs::remove_dir_all(&dir);
    }

    // ------------------------------------------------------------------
    // v1.1: gap entities on unparseable files (CONTRACT-M15 §5).
    // ------------------------------------------------------------------

    #[test]
    fn unparseable_file_contributes_a_single_gap_entity_instead_of_aborting() {
        let dir = temp_crate_dir("gap-file");
        write_crate(&dir, "widgets", "pub fn ok_fn( {\n");

        let entities = dump_entities(&dir);
        assert_eq!(entities.len(), 1, "unparseable file must yield exactly one entity, no descendants");

        let e = &entities[0];
        assert_eq!(e.kind, "module");
        assert_eq!(e.symbol_path, "widgets");
        assert_eq!(e.file, "src/lib.rs");
        assert_eq!(e.resolution, "gap");
        assert!(!e.is_test);

        let _ = fs::remove_dir_all(&dir);
    }

    #[test]
    fn gap_entity_in_a_tests_file_is_also_marked_is_test() {
        let dir = temp_crate_dir("gap-file-in-tests");
        write_crate(&dir, "widgets", "pub fn ok_fn() {}\n");
        fs::create_dir_all(dir.join("tests")).unwrap();
        fs::write(dir.join("tests").join("broken.rs"), "fn ( {\n").unwrap();

        let entities = dump_entities(&dir);
        let gap: Vec<&Entity> = entities.iter().filter(|e| e.resolution == "gap").collect();
        assert_eq!(gap.len(), 1);
        assert_eq!(gap[0].symbol_path, "broken");
        assert_eq!(gap[0].file, "tests/broken.rs");
        assert!(gap[0].is_test, "a gap file under tests/ is still test-ish");

        let _ = fs::remove_dir_all(&dir);
    }

    // ------------------------------------------------------------------
    // v1.1: isTest marking (CONTRACT-M15 §1.1).
    // ------------------------------------------------------------------

    #[test]
    fn unit_test_module_children_are_marked_is_test_but_the_module_itself_is_not() {
        let dir = temp_crate_dir("unit-test-module");
        write_crate(
            &dir,
            "widgets",
            "pub fn prod_fn() {}\n\n#[cfg(test)]\nmod tests {\n    #[test]\n    fn helper_check() {}\n}\n",
        );

        let entities = dump_entities(&dir);
        let by_symbol: std::collections::BTreeMap<&str, &Entity> = entities
            .iter()
            .map(|e| (e.symbol_path.as_str(), e))
            .collect();

        assert!(!by_symbol["widgets::prod_fn"].is_test);
        // Literal rule (CONTRACT-M15 §1.1): symbolPath *contains*
        // "::tests::" -- true for the fn nested inside the tests module...
        assert!(by_symbol["widgets::tests::helper_check"].is_test);
        // ...but the "tests" module's own symbolPath ("widgets::tests")
        // only *ends with* "::tests", so it does not itself qualify.
        assert!(!by_symbol["widgets::tests"].is_test);

        let _ = fs::remove_dir_all(&dir);
    }

    #[test]
    fn integration_test_file_and_its_descendants_are_all_marked_is_test() {
        let dir = temp_crate_dir("integration-test-file");
        write_crate(&dir, "widgets", "pub fn prod_fn() {}\n");
        fs::create_dir_all(dir.join("tests")).unwrap();
        fs::write(
            dir.join("tests").join("it.rs"),
            "#[test]\nfn it_works() {}\n",
        )
        .unwrap();

        let entities = dump_entities(&dir);
        let by_symbol: std::collections::BTreeMap<&str, &Entity> = entities
            .iter()
            .map(|e| (e.symbol_path.as_str(), e))
            .collect();

        assert!(!by_symbol["widgets"].is_test);
        assert!(by_symbol["it"].is_test, "the tests/*.rs root itself is test-ish");
        assert_eq!(by_symbol["it"].file, "tests/it.rs");
        assert!(by_symbol["it::it_works"].is_test);

        let _ = fs::remove_dir_all(&dir);
    }

    #[test]
    fn nested_tests_subdirectory_file_is_skipped_same_as_unreachable_src_files() {
        let dir = temp_crate_dir("nested-tests-subdir");
        write_crate(&dir, "widgets", "pub fn prod_fn() {}\n");
        fs::create_dir_all(dir.join("tests").join("common")).unwrap();
        fs::write(
            dir.join("tests").join("common").join("helpers.rs"),
            "pub fn helper() {}\n",
        )
        .unwrap();

        let entities = dump_entities(&dir);
        let symbols: std::collections::BTreeSet<&str> =
            entities.iter().map(|e| e.symbol_path.as_str()).collect();
        assert!(!symbols.contains("helper"));
        assert!(!symbols.contains("helpers"));
        assert!(!symbols.contains("common::helper"));

        let _ = fs::remove_dir_all(&dir);
    }

    // ------------------------------------------------------------------
    // JSON rendering (CONTRACT §6, CONTRACT-M15 §5).
    // ------------------------------------------------------------------

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
            is_test: false,
            resolution: "clean",
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

    #[test]
    fn render_json_appends_is_test_and_resolution_only_when_non_default() {
        let entities = vec![
            Entity {
                kind: "fn",
                name: "f".to_string(),
                symbol_path: "crate::tests::f".to_string(),
                file: "src/lib.rs".to_string(),
                start_line: 1,
                end_line: 1,
                content_hash: "aaaa".to_string(),
                parent_symbol_path: Some("crate::tests".to_string()),
                is_test: true,
                resolution: "clean",
            },
            Entity {
                kind: "module",
                name: "broken".to_string(),
                symbol_path: "broken".to_string(),
                file: "src/broken.rs".to_string(),
                start_line: 1,
                end_line: 3,
                content_hash: "bbbb".to_string(),
                parent_symbol_path: None,
                is_test: false,
                resolution: "gap",
            },
        ];
        let json = render_json("rust-map-dump@0.2.0", "rust", &entities);
        assert!(json.contains("\"parentSymbolPath\":\"crate::tests\",\"isTest\":true}"));
        assert!(json.contains("\"parentSymbolPath\":null,\"resolution\":\"gap\"}"));
        assert!(!json.contains("\"resolution\":\"clean\""));
        assert!(!json.contains("\"isTest\":false"));
    }
}
