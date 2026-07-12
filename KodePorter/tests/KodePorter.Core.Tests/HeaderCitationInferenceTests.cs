using KodePorter.Core.Candidates;
using KodePorter.Core.Domain;
using KodePorter.Core.Model;
using KodePorter.Core.Providers;
using KodePorter.Core.Store;
using KodePorter.Core.Tests.Support;

namespace KodePorter.Core.Tests;

/// <summary>
/// `kp candidates infer --heuristic header-citation`: extracts a top-of-file `// Port of ...` /
/// `// Upstream source: ...` citation from real on-disk target files (synthetic here, but written
/// through <see cref="CSharpFixture"/> and read back exactly as the real FrankenTui.Net files
/// are), normalizes the cited Rust path against the source map, and — on a match — links the
/// source file's root module entity to the target file's primary (largest top-level) type. The
/// two citation forms and the mount-prefix normalization mirror what a survey of real
/// FrankenTui.Net headers actually contains (see CandidateInferenceService's header-citation
/// remarks for the full grammar write-up).
/// </summary>
public class HeaderCitationInferenceTests
{
    private static readonly string FakeHash = new('a', 64);

    [Fact]
    public void MatchesBothCitationFormsAndSkipsFilesWithNoOrNonGrammarCitations()
    {
        using var scratch = new TempDirectory();
        string workspaceDir = scratch.Combine("workspace");
        Directory.CreateDirectory(workspaceDir);
        using var store = new MapStore(Path.Combine(workspaceDir, "kpmap.db"));

        // Source side: two root-module entities, one per cited Rust file.
        string sourceRoot = scratch.Combine("src-root");
        Directory.CreateDirectory(sourceRoot);
        var sourceBasis = BasisPinner.Pin(store, BasisSide.Source, sourceRoot, "d1");
        var dump = new ProviderDump("rust-map-dump@0.2.0", "frankentui", [
            new DumpEntity("module", "bocpd", "ftui_runtime::bocpd", "crates/ftui-runtime/src/bocpd.rs", 1, 40, FakeHash, null),
            new DumpEntity("module", "clipboard", "ftui_extras::clipboard", "crates/ftui-extras/src/clipboard.rs", 1, 30, FakeHash, null),
        ]);
        string dumpPath = scratch.Combine("dump.json");
        File.WriteAllText(dumpPath, System.Text.Json.JsonSerializer.Serialize(dump));
        new RustSynProvider().Import(store, sourceBasis, dumpPath);

        // Target side: real files on disk, read back by the heuristic exactly like the real tree.
        string targetRoot = scratch.Combine("tgt-root");

        // Convention 1: "Port of <mount-prefixed path>" — the mount prefix (".external/frankentui/")
        // is not part of the source map's own File coordinate and must be stripped by suffix match.
        CSharpFixture.WriteSource(targetRoot, "FrankenTui.Runtime/Bocpd.cs", """
            // SPDX-License-Identifier: Apache-2.0
            // Port of .external/frankentui/crates/ftui-runtime/src/bocpd.rs
            // Upstream commit: 1111111111111111111111111111111111111111

            namespace FrankenTui.Runtime
            {
                public sealed class BocpdConfig
                {
                    public double MuSteadyMs = 200.0;
                }
            }
            """);

        // Convention 2: "Upstream source: <bare, already source-root-relative path>".
        CSharpFixture.WriteSource(targetRoot, "FrankenTui.Extras/Clipboard.cs", """
            // Upstream source: crates/ftui-extras/src/clipboard.rs
            // Upstream basis: 2222222222222222222222222222222222222222

            namespace FrankenTui.Extras
            {
                public static class ClipboardSystem
                {
                    public static string? MemoryBuffer;
                }
            }
            """);

        // No citation at all: the common case, must not create anything or count as a citation.
        CSharpFixture.WriteSource(targetRoot, "FrankenTui.Core/Rect.cs", """
            namespace FrankenTui.Core
            {
                public readonly struct Rect
                {
                    public int X;
                }
            }
            """);

        // A THIRD, non-grammar "Upstream: <free text / external URL>" form — must not match, even
        // though it mentions the word "Rust" and even sits next to a "Port of <prose>" line with
        // no .rs token in it (mirrors RustLib.Tracing/Facade.cs in the real tree exactly).
        CSharpFixture.WriteSource(targetRoot, "RustLib.Tracing/Facade.cs", """
            // Port of the Rust `tracing` facade macros that FrankenTUI depends on.
            // Upstream: https://github.com/tokio-rs/tracing (tracing crate)

            namespace RustLib.Tracing
            {
                public static class Tracing
                {
                    public static int Foo;
                }
            }
            """);

        var targetBasis = BasisPinner.Pin(store, BasisSide.Target, targetRoot, "base");
        new CSharpRoslynProvider().Import(store, targetBasis);

        var result = CandidateInferenceService.InferHeaderCitation(workspaceDir, store);

        Assert.Equal(4, result.FilesScanned); // Bocpd, Clipboard, Rect, Facade
        Assert.Equal(2, result.CitationsFound); // only the two grammar-matching lines carry a .rs token
        Assert.Equal(2, result.Matched);
        Assert.Equal(2, result.Created);
        Assert.Empty(result.UnmatchedCitedPaths);

        var candidates = CorrespondencesYaml.Read(workspaceDir).Where(c => c.Id.StartsWith("cand-hc-", StringComparison.Ordinal)).ToList();
        Assert.Equal(2, candidates.Count);
        Assert.All(candidates, c =>
        {
            Assert.Equal("maps-to", c.Type);
            Assert.Equal("candidate", c.Provenance);
            Assert.StartsWith("inferred:header-citation \"", c.Note);
        });

        var bocpdCandidate = Assert.Single(candidates, c => c.Source!.SymbolPath == "ftui_runtime::bocpd");
        Assert.Equal("FrankenTui.Runtime.BocpdConfig", bocpdCandidate.Target!.SymbolPath);
        Assert.Equal("inferred:header-citation \"// Port of .external/frankentui/crates/ftui-runtime/src/bocpd.rs\"", bocpdCandidate.Note);

        var clipboardCandidate = Assert.Single(candidates, c => c.Source!.SymbolPath == "ftui_extras::clipboard");
        Assert.Equal("FrankenTui.Extras.ClipboardSystem", clipboardCandidate.Target!.SymbolPath);
        Assert.Equal("inferred:header-citation \"// Upstream source: crates/ftui-extras/src/clipboard.rs\"", clipboardCandidate.Note);

        // Deterministic ids, own namespace separate from name-norm's "cand-<n>".
        Assert.All(candidates, c => Assert.Matches("^cand-hc-[0-9]+$", c.Id));

        // Idempotent: re-running does not re-link the now-covered pairs, though the citations
        // still resolve (matched), just no longer create.
        var second = CandidateInferenceService.InferHeaderCitation(workspaceDir, store);
        Assert.Equal(2, second.Matched);
        Assert.Equal(0, second.Created);
    }

    [Fact]
    public void RecordsUnmatchedCitedPathsAndExtractsMultiplePathsFromOneLine()
    {
        using var scratch = new TempDirectory();
        string workspaceDir = scratch.Combine("workspace");
        Directory.CreateDirectory(workspaceDir);
        using var store = new MapStore(Path.Combine(workspaceDir, "kpmap.db"));

        string sourceRoot = scratch.Combine("src-root");
        Directory.CreateDirectory(sourceRoot);
        var sourceBasis = BasisPinner.Pin(store, BasisSide.Source, sourceRoot, "d1");
        var dump = new ProviderDump("rust-map-dump@0.2.0", "frankentui", [
            new DumpEntity("module", "known", "ftui_widgets::known", "crates/ftui-widgets/src/known.rs", 1, 10, FakeHash, null),
        ]);
        string dumpPath = scratch.Combine("dump.json");
        File.WriteAllText(dumpPath, System.Text.Json.JsonSerializer.Serialize(dump));
        new RustSynProvider().Import(store, sourceBasis, dumpPath);

        string targetRoot = scratch.Combine("tgt-root");

        // One header citing a source file the map does not know about -> unmatched.
        CSharpFixture.WriteSource(targetRoot, "FrankenTui.Ghost/Ghost.cs", """
            // Port of .external/frankentui/crates/ftui-ghost/src/nope.rs

            namespace FrankenTui.Ghost
            {
                public sealed class GhostThing
                {
                    public int Value;
                }
            }
            """);

        // A real-shaped multi-path citation line (mirrors e.g. Rune.cs's
        // "Port of choreography.rs (737L), error_boundary.rs (1000L), log_ring.rs (775L)," in the
        // real tree): three bare filenames with no directory context, none of which can uniquely
        // resolve against the source map -> three unmatched entries from one line.
        CSharpFixture.WriteSource(targetRoot, "FrankenTui.Runtime/Facet.cs", """
            // Port of choreography.rs (737L), error_boundary.rs (1000L), log_ring.rs (775L)

            namespace FrankenTui.Runtime
            {
                public sealed class Facet
                {
                    public int Value;
                }
            }
            """);

        var targetBasis = BasisPinner.Pin(store, BasisSide.Target, targetRoot, "base");
        new CSharpRoslynProvider().Import(store, targetBasis);

        var result = CandidateInferenceService.InferHeaderCitation(workspaceDir, store);

        Assert.Equal(2, result.FilesScanned);
        Assert.Equal(4, result.CitationsFound); // 1 (Ghost) + 3 (Facet's comma-separated line)
        Assert.Equal(0, result.Matched);
        Assert.Equal(0, result.Created);
        Assert.Equal(4, result.UnmatchedCitedPaths.Count);
        Assert.Contains(".external/frankentui/crates/ftui-ghost/src/nope.rs", result.UnmatchedCitedPaths);
        Assert.Contains("choreography.rs", result.UnmatchedCitedPaths);
        Assert.Contains("error_boundary.rs", result.UnmatchedCitedPaths);
        Assert.Contains("log_ring.rs", result.UnmatchedCitedPaths);

        Assert.Empty(CorrespondencesYaml.Read(workspaceDir));
    }

    [Fact]
    public void PicksTheLargestTopLevelTypeAndExcludesNestedTypes()
    {
        using var scratch = new TempDirectory();
        string workspaceDir = scratch.Combine("workspace");
        Directory.CreateDirectory(workspaceDir);
        using var store = new MapStore(Path.Combine(workspaceDir, "kpmap.db"));

        string sourceRoot = scratch.Combine("src-root");
        Directory.CreateDirectory(sourceRoot);
        var sourceBasis = BasisPinner.Pin(store, BasisSide.Source, sourceRoot, "d1");
        var dump = new ProviderDump("rust-map-dump@0.2.0", "frankentui", [
            new DumpEntity("module", "multi", "ftui_widgets::multi", "crates/ftui-widgets/src/multi.rs", 1, 50, FakeHash, null),
        ]);
        string dumpPath = scratch.Combine("dump.json");
        File.WriteAllText(dumpPath, System.Text.Json.JsonSerializer.Serialize(dump));
        new RustSynProvider().Import(store, sourceBasis, dumpPath);

        string targetRoot = scratch.Combine("tgt-root");
        CSharpFixture.WriteSource(targetRoot, "FrankenTui.Widgets/Multi.cs", """
            // Port of .external/frankentui/crates/ftui-widgets/src/multi.rs

            namespace FrankenTui.Widgets
            {
                public sealed class Small
                {
                    public int A;
                }

                public sealed class Big
                {
                    public int A;
                    public int B;
                    public int C;
                    public int D;
                    public int E;

                    public sealed class NestedInsideBig
                    {
                        public int Z;
                    }
                }
            }
            """);

        var targetBasis = BasisPinner.Pin(store, BasisSide.Target, targetRoot, "base");
        new CSharpRoslynProvider().Import(store, targetBasis);

        var result = CandidateInferenceService.InferHeaderCitation(workspaceDir, store);

        Assert.Equal(1, result.Created);
        var candidate = Assert.Single(CorrespondencesYaml.Read(workspaceDir));
        Assert.Equal("FrankenTui.Widgets.Big", candidate.Target!.SymbolPath); // largest top-level type, not Small and not the nested type
    }

    [Fact]
    public void AlreadyCoveredSourceSymbolCountsAsMatchedButDoesNotCreate()
    {
        using var scratch = new TempDirectory();
        string workspaceDir = scratch.Combine("workspace");
        Directory.CreateDirectory(workspaceDir);
        using var store = new MapStore(Path.Combine(workspaceDir, "kpmap.db"));

        string sourceRoot = scratch.Combine("src-root");
        Directory.CreateDirectory(sourceRoot);
        var sourceBasis = BasisPinner.Pin(store, BasisSide.Source, sourceRoot, "d1");
        var dump = new ProviderDump("rust-map-dump@0.2.0", "frankentui", [
            new DumpEntity("module", "bocpd", "ftui_runtime::bocpd", "crates/ftui-runtime/src/bocpd.rs", 1, 40, FakeHash, null),
        ]);
        string dumpPath = scratch.Combine("dump.json");
        File.WriteAllText(dumpPath, System.Text.Json.JsonSerializer.Serialize(dump));
        new RustSynProvider().Import(store, sourceBasis, dumpPath);

        string targetRoot = scratch.Combine("tgt-root");
        CSharpFixture.WriteSource(targetRoot, "FrankenTui.Runtime/Bocpd.cs", """
            // Port of .external/frankentui/crates/ftui-runtime/src/bocpd.rs

            namespace FrankenTui.Runtime
            {
                public sealed class BocpdConfig
                {
                    public double MuSteadyMs = 200.0;
                }
            }
            """);
        var targetBasis = BasisPinner.Pin(store, BasisSide.Target, targetRoot, "base");
        new CSharpRoslynProvider().Import(store, targetBasis);

        CorrespondencesYaml.Write(workspaceDir,
        [
            new Correspondence("corr-existing", "maps-to", null, "unit-x",
                new AnchorRef("ftui_runtime::bocpd", "d1", FakeHash), null, null, null, null, Stale: false, Provenance: "asserted"),
        ]);

        var result = CandidateInferenceService.InferHeaderCitation(workspaceDir, store);

        Assert.Equal(1, result.FilesScanned);
        Assert.Equal(1, result.CitationsFound);
        Assert.Equal(1, result.Matched); // the citation still resolves to a known source file...
        Assert.Equal(0, result.Created); // ...but the source symbol is already covered, so nothing new is linked.
        Assert.Empty(result.UnmatchedCitedPaths);

        var correspondences = CorrespondencesYaml.Read(workspaceDir);
        Assert.Single(correspondences); // only the pre-existing row; no cand-hc- row appended
    }
}
