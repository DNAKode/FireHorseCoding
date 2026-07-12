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
//! Module resolution starts the same way, not full Cargo-aware crate graph
//! resolution: within a crate, `src/lib.rs` and `src/main.rs` are treated
//! as the crate root (named after that crate's own `Cargo.toml` `[package]
//! name`), each `src/bin/<name>.rs` is treated as its own crate root named
//! `<name>`, and each *direct child* `tests/<name>.rs` is treated as its
//! own (test) crate root -- matching what Cargo itself would compile each
//! of those files as.
//!
//! From each such root, `mod foo;` (out-of-line, no inline `{ ... }` body)
//! items are now followed (v0.4.0): the target file is resolved per Rust's
//! own module-file rules, relative to the *declaring file's own module
//! directory* -- a directory owner (any crate-root file, regardless of its
//! name, or a `mod.rs` file) owns its own containing directory, while a
//! plain `<name>.rs` file owns a `<name>/` sibling directory for its own
//! children. Candidates are tried in a fixed, deterministic order --
//! `<dir>/<name>.rs` before `<dir>/<name>/mod.rs` -- so if a tree
//! (incorrectly, or via cfg-gating rustc itself would resolve differently)
//! ships both, the first one wins rather than being ambiguous. A plain
//! string-literal `#[path = "..."]` attribute overrides this search
//! entirely, resolved relative to the *declaring file's own* directory
//! (not its module directory). The resolved file's items become children
//! of the declaring `mod` entity with the exact same symbolPath shape an
//! inline `mod foo { ... }` would have produced, and its `file`/
//! `contentHash` are the submodule file's own -- so header-citation
//! matching against a submodule path works identically to a crate-root
//! path. Recursion is natural: a submodule can itself declare further
//! out-of-line `mod`s. A `mod foo;` that cannot be resolved (target not
//! found, or found but unreadable/unparseable) never aborts the dump --
//! it contributes a single `resolution: "gap"` module entity in the
//! declaring module's place (CONTRACT-M15 §5's existing gap contract,
//! extended down to individual `mod` items) and recursion simply stops
//! there.
//!
//! Any `.rs` file that is nested under a `tests/<subdir>/` and not reached
//! by a `mod` declaration from a `tests/<name>.rs` root is still skipped,
//! same as before -- Cargo doesn't compile such a file as anything on its
//! own either.
//!
//! `symbolPath`s are already "prefixed with the crate name" (CONTRACT-M15
//! §5) by construction: a `lib.rs`/`main.rs`/`bin/<name>.rs` root's name IS
//! that crate/target's own name in Rust's sense (matching what
//! `rustc --crate-name` would call it), and every descendant symbolPath is
//! built by appending onto its root. This is why a single-crate root's
//! output is byte-identical before and after v1.1's multi-crate discovery
//! was added: discovering exactly one crate (at the root itself) and
//! walking it is exactly what v1.0 already did.
//!
//! A *direct-child* `tests/<name>.rs`, however, is a special case (v1.2,
//! identity-collision fix): `rustc --crate-name` really would call it just
//! `<name>`, with no relationship to its owning package, but two
//! independently-compiled crates in a multi-crate root can ship a
//! same-named test file (e.g. two packages each with `tests/smoke.rs`) --
//! an unqualified `<name>` root symbolPath would then collide under the
//! schema's (kind, symbolPath) identity (`EntityResolution
//! .SortAndDeduplicate`), silently dropping one of the two. So a test
//! root's symbolPath (and name) is instead qualified by its *owning
//! package*: `<package>#tests/<name>` (root module), with every
//! descendant built by appending onto that, e.g.
//! `<package>#tests/<name>::some_fn`. `#` is used as the package/tests
//! separator (rather than `::`) because it cannot appear in a Rust
//! identifier or `::`-path segment, so this qualified root can never
//! collide with a real `::`-joined symbolPath from anywhere else in the
//! dump -- deterministic, collision-free across packages, and visibly
//! test-scoped at a glance. `lib.rs`/`main.rs`/`bin/<name>.rs` roots are
//! unaffected by this and keep their v1.1 unqualified names.
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
//! with `"resolution": "gap"` instead. The same tolerance applies to an
//! individual `mod foo;` include (v0.4.0, see above): the containing file
//! and every sibling item are unaffected, only that one `mod` subtree
//! becomes a gap leaf.

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
            process_root_file(&file_path, &root_name, &rel_file, root, out);
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
            let stem = match rel_to_tests.strip_suffix(".rs") {
                Some(stem) if !stem.is_empty() && !stem.contains('/') => stem,
                _ => continue,
            };
            // v1.2: qualify by owning package (see module doc comment) so
            // same-named tests/*.rs files in different packages never
            // collide under (kind, symbolPath) identity.
            let root_name = format!("{}#tests/{}", pkg_name, stem);
            let rel_file = root_relative_file(root, &file_path);
            process_root_file(&file_path, &root_name, &rel_file, root, out);
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
/// full entity tree (module root + descendants, descendants following
/// `mod foo;` includes recursively -- v0.4.0) or, if reading/parsing
/// fails, a single `resolution: "gap"` file-level entity (CONTRACT-M15
/// §5) -- never aborts the whole dump. `root` is the overall discovery
/// root, needed to compute root-relative `file` paths for any submodule
/// files pulled in along the way.
fn process_root_file(file_path: &Path, root_name: &str, rel_file: &str, root: &Path, out: &mut Vec<Entity>) {
    match read_and_parse(file_path) {
        ReadParseResult::Ok(content, parsed) => {
            let total_lines = content.lines().count().max(1);
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
            let file_dir = file_path.parent().unwrap_or_else(|| Path::new(""));
            let module_dir = module_dir_for(file_path, true);
            visit_items(
                &parsed.items,
                root_name,
                rel_file,
                &content,
                root,
                file_dir,
                &module_dir,
                out,
            );
        }
        ReadParseResult::ReadErr => push_gap_entity(root_name, rel_file, "", 1, out),
        ReadParseResult::ParseErr(content) => {
            let total_lines = content.lines().count().max(1);
            push_gap_entity(root_name, rel_file, &content, total_lines, out);
        }
    }
}

/// Read a `.rs` file as (lossy-decoded) text and attempt to `syn::parse_file`
/// it, distinguishing "couldn't even read it" from "read fine but doesn't
/// parse" -- both feed a `resolution: "gap"` entity somewhere upstream, but
/// only the latter has real content to hash.
enum ReadParseResult {
    Ok(String, syn::File),
    ReadErr,
    ParseErr(String),
}

fn read_and_parse(file_path: &Path) -> ReadParseResult {
    // Read as raw bytes + lossy-decode rather than `fs::read_to_string`:
    // invalid UTF-8 no longer needs its own failure branch -- the lossy
    // text will simply fail `syn::parse_file` below and fall into the gap
    // path like any other unparseable file. A true read failure (permission
    // denied, file vanished mid-walk) still can't produce any text.
    let content = match fs::read(file_path) {
        Ok(bytes) => String::from_utf8_lossy(&bytes).into_owned(),
        Err(_) => return ReadParseResult::ReadErr,
    };
    match syn::parse_file(&content) {
        Ok(parsed) => ReadParseResult::Ok(content, parsed),
        Err(_) => ReadParseResult::ParseErr(content),
    }
}

fn push_gap_entity(
    root_name: &str,
    rel_file: &str,
    content: &str,
    total_lines: usize,
    out: &mut Vec<Entity>,
) {
    push_gap_module(
        root_name.to_string(),
        root_name.to_string(),
        rel_file.to_string(),
        1,
        total_lines,
        hash_span(content, 1, total_lines),
        None,
        out,
    );
}

/// Append a `resolution: "gap"` module entity -- the general form used both
/// for a whole unresolvable/unreadable/unparseable crate-root file
/// (`push_gap_entity`, no parent) and for a single unresolvable `mod foo;`
/// include nested somewhere inside an otherwise-clean file (has a parent).
fn push_gap_module(
    name: String,
    symbol_path: String,
    rel_file: String,
    start_line: usize,
    end_line: usize,
    content_hash: String,
    parent_symbol_path: Option<String>,
    out: &mut Vec<Entity>,
) {
    out.push(Entity {
        kind: "module",
        name,
        symbol_path,
        file: rel_file,
        start_line,
        end_line,
        content_hash,
        parent_symbol_path,
        is_test: false,
        resolution: "gap",
    });
}

/// The directory a `.rs` file "owns" for resolving its own out-of-line
/// `mod foo;` children, per Rust's module-file rules: a crate-root file
/// (any name -- `is_root`) or a `mod.rs` file owns its own containing
/// directory; any other `<name>.rs` file owns a `<name>/` sibling
/// directory instead.
fn module_dir_for(file_path: &Path, is_root: bool) -> PathBuf {
    let parent = file_path.parent().unwrap_or_else(|| Path::new(""));
    if is_root || file_path.file_name().map(|n| n == "mod.rs").unwrap_or(false) {
        return parent.to_path_buf();
    }
    match file_path.file_stem() {
        Some(stem) => parent.join(stem),
        None => parent.to_path_buf(),
    }
}

/// A plain string-literal `#[path = "..."]` attribute's value, if present
/// (the only form this tool honors -- CONTRACT scope, see module doc
/// comment).
fn path_attr_literal(attrs: &[syn::Attribute]) -> Option<String> {
    for attr in attrs {
        if !attr.path().is_ident("path") {
            continue;
        }
        if let syn::Meta::NameValue(nv) = &attr.meta {
            if let syn::Expr::Lit(syn::ExprLit {
                lit: syn::Lit::Str(s),
                ..
            }) = &nv.value
            {
                return Some(s.value());
            }
        }
    }
    None
}

/// Resolve an out-of-line `mod name;` item to the file it names, per Rust's
/// module-file rules (module doc comment): an explicit `#[path = "..."]`
/// wins outright (resolved relative to `file_dir`, the declaring file's
/// own directory) with no fallback; otherwise `<module_dir>/name.rs` is
/// tried before `<module_dir>/name/mod.rs` (first-found-wins, deterministic
/// order -- handles a tree that ships both, e.g. via cfg-gating rustc
/// itself would pick between). `None` means unresolvable (the caller emits
/// a gap entity).
fn resolve_mod_path(
    module_dir: &Path,
    file_dir: &Path,
    name: &str,
    attrs: &[syn::Attribute],
) -> Option<PathBuf> {
    if let Some(p) = path_attr_literal(attrs) {
        let candidate = file_dir.join(p);
        return if candidate.is_file() {
            Some(candidate)
        } else {
            None
        };
    }
    let as_file = module_dir.join(format!("{}.rs", name));
    if as_file.is_file() {
        return Some(as_file);
    }
    let as_dir_mod = module_dir.join(name).join("mod.rs");
    if as_dir_mod.is_file() {
        return Some(as_dir_mod);
    }
    None
}

/// Walk one file's top-level items, recursing into `mod { ... }` bodies,
/// out-of-line `mod foo;` includes (resolved to their own file -- v0.4.0,
/// see module doc comment), and `impl` blocks, appending every CONTRACT §6
/// entity found to `out`. `root` is the overall discovery root (for
/// root-relative `file` paths on any submodule file pulled in);
/// `file_dir`/`module_dir` are the *current* file's own directory and the
/// directory it owns for resolving its out-of-line children, respectively
/// (see `module_dir_for`).
fn visit_items(
    items: &[Item],
    parent_module_path: &str,
    rel_file: &str,
    source: &str,
    root: &Path,
    file_dir: &Path,
    module_dir: &Path,
    out: &mut Vec<Entity>,
) {
    for item in items {
        match item {
            Item::Mod(m) => {
                let name = m.ident.to_string();
                let symbol_path = format!("{}::{}", parent_module_path, name);
                let (decl_start, decl_end) = line_span(item.span());

                match &m.content {
                    Some((_, content)) => {
                        out.push(Entity {
                            kind: "module",
                            name: name.clone(),
                            symbol_path: symbol_path.clone(),
                            file: rel_file.to_string(),
                            start_line: decl_start,
                            end_line: decl_end,
                            content_hash: hash_span(source, decl_start, decl_end),
                            parent_symbol_path: Some(parent_module_path.to_string()),
                            is_test: false,
                            resolution: "clean",
                        });
                        // An inline module owns a `<module_dir>/<name>/`
                        // directory for its own out-of-line children
                        // (unless overridden by its own `#[path]`), same
                        // as Rust itself resolves it.
                        let child_module_dir = match path_attr_literal(&m.attrs) {
                            Some(p) => file_dir.join(p),
                            None => module_dir.join(&name),
                        };
                        visit_items(
                            content,
                            &symbol_path,
                            rel_file,
                            source,
                            root,
                            file_dir,
                            &child_module_dir,
                            out,
                        );
                    }
                    None => match resolve_mod_path(module_dir, file_dir, &name, &m.attrs) {
                        Some(target_path) => match read_and_parse(&target_path) {
                            ReadParseResult::Ok(sub_content, parsed) => {
                                let sub_rel_file = root_relative_file(root, &target_path);
                                let total_lines = sub_content.lines().count().max(1);
                                out.push(Entity {
                                    kind: "module",
                                    name: name.clone(),
                                    symbol_path: symbol_path.clone(),
                                    file: sub_rel_file.clone(),
                                    start_line: 1,
                                    end_line: total_lines,
                                    content_hash: hash_span(&sub_content, 1, total_lines),
                                    parent_symbol_path: Some(parent_module_path.to_string()),
                                    is_test: false,
                                    resolution: "clean",
                                });
                                let sub_file_dir =
                                    target_path.parent().unwrap_or_else(|| Path::new("")).to_path_buf();
                                let sub_module_dir = module_dir_for(&target_path, false);
                                visit_items(
                                    &parsed.items,
                                    &symbol_path,
                                    &sub_rel_file,
                                    &sub_content,
                                    root,
                                    &sub_file_dir,
                                    &sub_module_dir,
                                    out,
                                );
                            }
                            ReadParseResult::ReadErr => {
                                let sub_rel_file = root_relative_file(root, &target_path);
                                push_gap_module(
                                    name,
                                    symbol_path,
                                    sub_rel_file,
                                    1,
                                    1,
                                    hash_span("", 1, 1),
                                    Some(parent_module_path.to_string()),
                                    out,
                                );
                            }
                            ReadParseResult::ParseErr(sub_content) => {
                                let sub_rel_file = root_relative_file(root, &target_path);
                                let total_lines = sub_content.lines().count().max(1);
                                push_gap_module(
                                    name,
                                    symbol_path,
                                    sub_rel_file,
                                    1,
                                    total_lines,
                                    hash_span(&sub_content, 1, total_lines),
                                    Some(parent_module_path.to_string()),
                                    out,
                                );
                            }
                        },
                        None => {
                            // Unresolvable: no target file to point at, so
                            // the gap entity is grounded in the *declaring*
                            // file's own `mod foo;` line instead (still
                            // real content, just not the submodule's).
                            push_gap_module(
                                name,
                                symbol_path,
                                rel_file.to_string(),
                                decl_start,
                                decl_end,
                                hash_span(source, decl_start, decl_end),
                                Some(parent_module_path.to_string()),
                                out,
                            );
                        }
                    },
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

    /// Writes a direct-child `tests/<file_name>` file (content verbatim)
    /// into an already-`write_crate`'d crate directory.
    fn write_test_file(crate_dir: &Path, file_name: &str, content: &str) {
        fs::create_dir_all(crate_dir.join("tests")).unwrap();
        fs::write(crate_dir.join("tests").join(file_name), content).unwrap();
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

    // ------------------------------------------------------------------
    // v1.2: per-file test-crate identity collision fix (CONTRACT-M15 §5
    // extended). PROBE-REPORT.md §7 finding #2: two independently-
    // compiled crates that happen to ship a same-named direct-child
    // `tests/<name>.rs` file used to collide on an unqualified `<name>`
    // root symbolPath, silently dropping one of the two under
    // EntityResolution.SortAndDeduplicate's (kind, symbolPath) identity.
    // ------------------------------------------------------------------

    #[test]
    fn same_named_tests_file_in_two_packages_does_not_collide() {
        let root = temp_crate_dir("two-crate-same-named-test-file");
        fs::write(
            root.join("Cargo.toml"),
            "[workspace]\nmembers = [\"alpha\", \"beta\"]\n",
        )
        .unwrap();
        write_crate(&root.join("alpha"), "alpha", "pub fn a_fn() {}\n");
        write_crate(&root.join("beta"), "beta", "pub fn b_fn() {}\n");
        // Both packages ship a `tests/smoke.rs` with the identical inner
        // fn name -- the exact shape rustc itself would compile as two
        // independent crates both literally named "smoke".
        write_test_file(&root.join("alpha"), "smoke.rs", "#[test]\nfn it_works() {}\n");
        write_test_file(&root.join("beta"), "smoke.rs", "#[test]\nfn it_works() {}\n");

        let entities = dump_entities(&root);
        let by_symbol: std::collections::BTreeMap<&str, &Entity> = entities
            .iter()
            .map(|e| (e.symbol_path.as_str(), e))
            .collect();

        // Package-qualified root symbolPaths: no collision, both present.
        assert!(by_symbol.contains_key("alpha#tests/smoke"));
        assert!(by_symbol.contains_key("beta#tests/smoke"));
        assert!(by_symbol.contains_key("alpha#tests/smoke::it_works"));
        assert!(by_symbol.contains_key("beta#tests/smoke::it_works"));

        assert!(by_symbol["alpha#tests/smoke"].is_test);
        assert!(by_symbol["beta#tests/smoke"].is_test);
        assert!(by_symbol["alpha#tests/smoke::it_works"].is_test);
        assert!(by_symbol["beta#tests/smoke::it_works"].is_test);

        // `file` is root-relative, disambiguating the two crates' identical
        // "tests/smoke.rs" leaf path too.
        assert_eq!(by_symbol["alpha#tests/smoke"].file, "alpha/tests/smoke.rs");
        assert_eq!(by_symbol["beta#tests/smoke"].file, "beta/tests/smoke.rs");

        // Nothing from either tests/smoke.rs got dropped: 2 entities each
        // (root module + the one fn) = 4 total, none deduplicated away.
        let test_related = entities
            .iter()
            .filter(|e| e.file.ends_with("tests/smoke.rs"))
            .count();
        assert_eq!(test_related, 4);

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
    // v0.4.0: out-of-line `mod foo;` file resolution.
    // ------------------------------------------------------------------

    #[test]
    fn out_of_line_mod_pulls_in_submodule_file_as_children() {
        // lib.rs declares `mod a;` -> src/a.rs; a.rs itself declares
        // `mod b;` -> src/a/b.rs (a.rs is not a directory owner, so its
        // own children live under a src/a/ sibling directory). Recursion
        // through a second out-of-line level, exactly the task's own
        // worked example.
        let dir = temp_crate_dir("out-of-line-mod");
        write_crate(&dir, "widgets", "mod a;\n");
        fs::write(dir.join("src").join("a.rs"), "pub fn a_fn() {}\n\nmod b;\n").unwrap();
        fs::create_dir_all(dir.join("src").join("a")).unwrap();
        fs::write(dir.join("src").join("a").join("b.rs"), "pub fn b_fn() {}\n").unwrap();

        let entities = dump_entities(&dir);
        let by_symbol: std::collections::BTreeMap<&str, &Entity> = entities
            .iter()
            .map(|e| (e.symbol_path.as_str(), e))
            .collect();

        assert_eq!(by_symbol["widgets::a"].kind, "module");
        assert_eq!(by_symbol["widgets::a"].file, "src/a.rs");
        assert_eq!(by_symbol["widgets::a"].resolution, "clean");
        assert_eq!(
            by_symbol["widgets::a"].parent_symbol_path.as_deref(),
            Some("widgets")
        );
        assert_eq!(by_symbol["widgets::a::a_fn"].file, "src/a.rs");

        assert_eq!(by_symbol["widgets::a::b"].kind, "module");
        assert_eq!(by_symbol["widgets::a::b"].file, "src/a/b.rs");
        assert_eq!(by_symbol["widgets::a::b"].resolution, "clean");
        assert_eq!(
            by_symbol["widgets::a::b"].parent_symbol_path.as_deref(),
            Some("widgets::a")
        );

        assert_eq!(by_symbol["widgets::a::b::b_fn"].kind, "fn");
        assert_eq!(by_symbol["widgets::a::b::b_fn"].file, "src/a/b.rs");
        assert_eq!(
            by_symbol["widgets::a::b::b_fn"]
                .parent_symbol_path
                .as_deref(),
            Some("widgets::a::b")
        );

        let _ = fs::remove_dir_all(&dir);
    }

    #[test]
    fn mod_resolves_via_name_slash_mod_rs_when_name_rs_is_absent() {
        let dir = temp_crate_dir("mod-rs-pattern");
        write_crate(&dir, "widgets", "mod a;\n");
        fs::create_dir_all(dir.join("src").join("a")).unwrap();
        fs::write(
            dir.join("src").join("a").join("mod.rs"),
            "pub fn a_fn() {}\n\nmod b;\n",
        )
        .unwrap();
        // a/mod.rs is a directory owner of src/a/ itself, so its own
        // `mod b;` finds src/a/b.rs, not src/a/a/b.rs.
        fs::write(dir.join("src").join("a").join("b.rs"), "pub fn b_fn() {}\n").unwrap();

        let entities = dump_entities(&dir);
        let by_symbol: std::collections::BTreeMap<&str, &Entity> = entities
            .iter()
            .map(|e| (e.symbol_path.as_str(), e))
            .collect();

        assert_eq!(by_symbol["widgets::a"].file, "src/a/mod.rs");
        assert_eq!(by_symbol["widgets::a::b"].file, "src/a/b.rs");

        let _ = fs::remove_dir_all(&dir);
    }

    #[test]
    fn mod_prefers_name_rs_over_name_slash_mod_rs_when_both_exist() {
        // Deterministic first-found-wins order: name.rs before
        // name/mod.rs, for a tree that (e.g. via cfg-gating rustc itself
        // would pick between) ships both.
        let dir = temp_crate_dir("first-found-wins");
        write_crate(&dir, "widgets", "mod a;\n");
        fs::write(dir.join("src").join("a.rs"), "pub fn from_flat() {}\n").unwrap();
        fs::create_dir_all(dir.join("src").join("a")).unwrap();
        fs::write(
            dir.join("src").join("a").join("mod.rs"),
            "pub fn from_dir() {}\n",
        )
        .unwrap();

        let entities = dump_entities(&dir);
        let symbols: std::collections::BTreeSet<&str> =
            entities.iter().map(|e| e.symbol_path.as_str()).collect();
        assert!(symbols.contains("widgets::a::from_flat"));
        assert!(!symbols.contains("widgets::a::from_dir"));

        let by_symbol: std::collections::BTreeMap<&str, &Entity> = entities
            .iter()
            .map(|e| (e.symbol_path.as_str(), e))
            .collect();
        assert_eq!(by_symbol["widgets::a"].file, "src/a.rs");

        let _ = fs::remove_dir_all(&dir);
    }

    #[test]
    fn mod_with_path_attribute_resolves_relative_to_declaring_files_dir() {
        let dir = temp_crate_dir("path-attr");
        write_crate(
            &dir,
            "widgets",
            "#[path = \"custom_impl.rs\"]\nmod weird;\n",
        );
        fs::write(
            dir.join("src").join("custom_impl.rs"),
            "pub fn weird_fn() {}\n",
        )
        .unwrap();

        let entities = dump_entities(&dir);
        let by_symbol: std::collections::BTreeMap<&str, &Entity> = entities
            .iter()
            .map(|e| (e.symbol_path.as_str(), e))
            .collect();

        assert_eq!(by_symbol["widgets::weird"].file, "src/custom_impl.rs");
        assert_eq!(by_symbol["widgets::weird"].resolution, "clean");
        assert_eq!(
            by_symbol["widgets::weird::weird_fn"].file,
            "src/custom_impl.rs"
        );

        let _ = fs::remove_dir_all(&dir);
    }

    #[test]
    fn unresolvable_mod_include_emits_a_gap_entity_but_does_not_abort() {
        let dir = temp_crate_dir("unresolvable-mod");
        write_crate(&dir, "widgets", "pub fn ok_fn() {}\n\nmod missing;\n");

        let entities = dump_entities(&dir);
        let by_symbol: std::collections::BTreeMap<&str, &Entity> = entities
            .iter()
            .map(|e| (e.symbol_path.as_str(), e))
            .collect();

        assert_eq!(by_symbol["widgets"].resolution, "clean");
        assert!(by_symbol.contains_key("widgets::ok_fn"));

        assert_eq!(by_symbol["widgets::missing"].kind, "module");
        assert_eq!(by_symbol["widgets::missing"].resolution, "gap");
        // No target file exists, so the gap is grounded in the declaring
        // file's own `mod missing;` line instead.
        assert_eq!(by_symbol["widgets::missing"].file, "src/lib.rs");
        assert_eq!(
            by_symbol["widgets::missing"].parent_symbol_path.as_deref(),
            Some("widgets")
        );

        let _ = fs::remove_dir_all(&dir);
    }

    #[test]
    fn mod_target_found_but_unparseable_emits_gap_grounded_in_the_target_file() {
        let dir = temp_crate_dir("mod-target-unparseable");
        write_crate(&dir, "widgets", "mod broken;\n");
        fs::write(dir.join("src").join("broken.rs"), "fn broken( {\n").unwrap();

        let entities = dump_entities(&dir);
        let by_symbol: std::collections::BTreeMap<&str, &Entity> = entities
            .iter()
            .map(|e| (e.symbol_path.as_str(), e))
            .collect();

        assert_eq!(by_symbol["widgets::broken"].kind, "module");
        assert_eq!(by_symbol["widgets::broken"].resolution, "gap");
        // Here the target file WAS found, just failed to parse, so the gap
        // is grounded in the submodule file itself, not the declaring one.
        assert_eq!(by_symbol["widgets::broken"].file, "src/broken.rs");

        let _ = fs::remove_dir_all(&dir);
    }

    #[test]
    fn dump_is_deterministic_across_runs() {
        let dir = temp_crate_dir("mod-determinism");
        write_crate(&dir, "widgets", "pub fn ok_fn() {}\n\nmod a;\n");
        fs::write(
            dir.join("src").join("a.rs"),
            "pub fn a_fn() {}\n\nmod b;\n",
        )
        .unwrap();
        fs::create_dir_all(dir.join("src").join("a")).unwrap();
        fs::write(dir.join("src").join("a").join("b.rs"), "pub fn b_fn() {}\n").unwrap();

        let json_1 = render_json("p", "r", &dump_entities(&dir));
        let json_2 = render_json("p", "r", &dump_entities(&dir));
        assert_eq!(json_1, json_2, "two runs over the same tree must be byte-identical");

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
        // v1.2: package-qualified root symbolPath, same as any other
        // tests/*.rs root.
        assert_eq!(gap[0].symbol_path, "widgets#tests/broken");
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
        // v1.2: the tests/*.rs root's symbolPath is qualified by its
        // owning package ("widgets#tests/it"), not the bare file stem.
        assert!(
            by_symbol["widgets#tests/it"].is_test,
            "the tests/*.rs root itself is test-ish"
        );
        assert_eq!(by_symbol["widgets#tests/it"].file, "tests/it.rs");
        assert!(by_symbol["widgets#tests/it::it_works"].is_test);

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
