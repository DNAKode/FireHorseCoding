using System.Text.Json;
using KodePorter.Cli;
using KodePorter.Core.Domain;
using KodePorter.Core.Gneiss;
using KodePorter.Core.Providers;
using KodePorter.Core.Tests.Support;

namespace KodePorter.Core.Tests;

/// <summary>
/// CLI smoke coverage for the M1.5 vocabulary wired into `kp` (CONTRACT-M15.md §§1.3-1.6, 2, 3):
/// `kp note`/`kp notes`, `kp candidates infer`, `kp corr add --provenance`, `kp unit set-depth`,
/// `kp absence set`, `kp verify run --independence`, and `kp status` printing Health v2 in full.
/// Each test drives <see cref="KpCliApp.Run"/> exactly as Program.cs's Main would, against a
/// throwaway temp workspace.
/// </summary>
public class CliM15SmokeTests
{
    // ---- kp note / kp notes --------------------------------------------------------------------

    [Fact]
    public void NoteThenNotesListsInOrderWithPromotionStatus()
    {
        using var dir = new TempDirectory();
        string workspaceDir = InitWorkspace(dir);

        Run(workspaceDir, out string emptyOut, "notes", "--workspace", workspaceDir);
        Assert.Contains("No notes.", emptyOut);

        Run(workspaceDir, out string note1Out, "note", "--workspace", workspaceDir, "--text", "first observation");
        Assert.Contains("Recorded note", note1Out);

        Run(workspaceDir, out _, "note", "--workspace", workspaceDir, "--text", "second, by govert", "--actor", "govert");

        Run(workspaceDir, out string listOut, "notes", "--workspace", workspaceDir);
        var lines = listOut.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        Assert.Equal(2, lines.Count);
        Assert.Contains("kodeporter", lines[0]);
        Assert.Contains("first observation", lines[0]);
        Assert.Contains("promoted=no", lines[0]);
        Assert.Contains("govert", lines[1]);
        Assert.Contains("second, by govert", lines[1]);
    }

    // ---- kp unit set-depth ----------------------------------------------------------------------

    [Fact]
    public void UnitSetDepthUpdatesUnitDocDepth()
    {
        using var dir = new TempDirectory();
        string workspaceDir = InitWorkspace(dir);

        Run(workspaceDir, out _, "unit", "new", "--workspace", workspaceDir, "--id", "unit-a", "--name", "A");
        Assert.Equal("thin", UnitYaml.Read(workspaceDir, "unit-a").Depth);

        Run(workspaceDir, out string setOut, "unit", "set-depth", "--workspace", workspaceDir, "--id", "unit-a", "--depth", "dossiered");
        Assert.Contains("dossiered", setOut);
        Assert.Equal("dossiered", UnitYaml.Read(workspaceDir, "unit-a").Depth);
    }

    [Fact]
    public void UnitSetDepthRejectsInvalidDepthAndUnknownUnit()
    {
        using var dir = new TempDirectory();
        string workspaceDir = InitWorkspace(dir);
        Run(workspaceDir, out _, "unit", "new", "--workspace", workspaceDir, "--id", "unit-a", "--name", "A");

        int badDepthExit = KpCliApp.Run(
            ["unit", "set-depth", "--workspace", workspaceDir, "--id", "unit-a", "--depth", "medium-rare"],
            new StringWriter(), new StringWriter());
        Assert.Equal(1, badDepthExit);

        int missingUnitExit = KpCliApp.Run(
            ["unit", "set-depth", "--workspace", workspaceDir, "--id", "no-such-unit", "--depth", "dossiered"],
            new StringWriter(), new StringWriter());
        Assert.Equal(2, missingUnitExit);
    }

    // ---- kp absence set ---------------------------------------------------------------------------

    [Fact]
    public void AbsenceSetRecordsOverrideForSourceAndTarget()
    {
        using var dir = new TempDirectory();
        string workspaceDir = InitWorkspace(dir);

        Run(workspaceDir, out string sourceOut, "absence", "set", "--workspace", workspaceDir,
            "--symbol", "krate::mod::unused_fn", "--kind", "deliberately-dropped", "--note", "superseded by X");
        Assert.Contains("deliberately-dropped", sourceOut);

        Run(workspaceDir, out _, "absence", "set", "--workspace", workspaceDir,
            "--symbol", "Ns.Extra", "--kind", "intentional", "--side", "target");

        var overrides = AbsencesYaml.Read(workspaceDir);
        Assert.Equal(2, overrides.Count);
        var sourceOverride = Assert.Single(overrides, o => o.SymbolPath == "krate::mod::unused_fn");
        Assert.Equal("source", sourceOverride.Side);
        Assert.Equal("deliberately-dropped", sourceOverride.Kind);
        Assert.Equal("superseded by X", sourceOverride.Note);

        var targetOverride = Assert.Single(overrides, o => o.SymbolPath == "Ns.Extra");
        Assert.Equal("target", targetOverride.Side);
        Assert.Equal("intentional", targetOverride.Kind);

        // Re-setting the same (side, symbol) replaces rather than duplicates.
        Run(workspaceDir, out _, "absence", "set", "--workspace", workspaceDir,
            "--symbol", "krate::mod::unused_fn", "--kind", "not-yet-ported");
        var reRead = AbsencesYaml.Read(workspaceDir);
        Assert.Equal(2, reRead.Count);
        Assert.Equal("not-yet-ported", Assert.Single(reRead, o => o.SymbolPath == "krate::mod::unused_fn").Kind);
    }

    [Fact]
    public void AbsenceSetRejectsInvalidKindForSide()
    {
        using var dir = new TempDirectory();
        string workspaceDir = InitWorkspace(dir);

        int exit = KpCliApp.Run(
            ["absence", "set", "--workspace", workspaceDir, "--symbol", "a::b", "--kind", "intentional"], // "intentional" is a target-side kind
            new StringWriter(), new StringWriter());
        Assert.Equal(1, exit);
    }

    // ---- kp corr add --provenance ------------------------------------------------------------------

    [Fact]
    public void CorrAddDefaultsToAssertedProvenanceAndProposesAClaim()
    {
        using var dir = new TempDirectory();
        string workspaceDir = InitWorkspace(dir);

        Run(workspaceDir, out string addOut, "corr", "add", "--workspace", workspaceDir,
            "--type", "implements", "--unit", "unit-a", "--id", "corr-a");
        Assert.Contains("provenance asserted", addOut);

        var corr = Assert.Single(CorrespondencesYaml.Read(workspaceDir));
        Assert.Equal("asserted", corr.Provenance);
        Assert.NotNull(corr.ClaimAid);
    }

    [Fact]
    public void CorrAddWithProvenanceCandidateSkipsTheGneissClaim()
    {
        using var dir = new TempDirectory();
        string workspaceDir = InitWorkspace(dir);

        Run(workspaceDir, out string addOut, "corr", "add", "--workspace", workspaceDir,
            "--type", "maps-to", "--unit", "unit-a", "--id", "corr-cand", "--provenance", "candidate");
        Assert.Contains("provenance candidate", addOut);

        var corr = Assert.Single(CorrespondencesYaml.Read(workspaceDir));
        Assert.Equal("candidate", corr.Provenance);
        Assert.Null(corr.ClaimAid);
    }

    [Fact]
    public void CorrAddRejectsAnUnknownProvenance()
    {
        using var dir = new TempDirectory();
        string workspaceDir = InitWorkspace(dir);

        int exit = KpCliApp.Run(
            ["corr", "add", "--workspace", workspaceDir, "--type", "implements", "--unit", "unit-a", "--provenance", "verified"],
            new StringWriter(), new StringWriter());
        Assert.Equal(1, exit);
    }

    // ---- kp decide --actor (policy actors accepting on the record) ----------------------------------

    [Fact]
    public void DecideAcceptsOnTheRecordForACustomPolicyActor()
    {
        using var dir = new TempDirectory();
        string workspaceDir = InitWorkspace(dir);

        Run(workspaceDir, out _, "corr", "add", "--workspace", workspaceDir,
            "--type", "implements", "--unit", "unit-a", "--id", "corr-policy");
        var corr = Assert.Single(CorrespondencesYaml.Read(workspaceDir));
        Assert.NotNull(corr.ClaimAid);

        Run(workspaceDir, out string decideOut, "decide", "--workspace", workspaceDir,
            "--subject", "corr:corr-policy", "--verdict", "accept", "--reason", "policy autoAccept on the record",
            "--actor", "policy:kp-default@1");

        Assert.Contains("Accepted claim", decideOut);
        Assert.Contains("actor 'policy:kp-default@1'", decideOut);

        using var binding = GneissBinding.Initialize(workspaceDir);
        var view = binding.AskClaim(GneissBinding.CorrespondenceSubject("corr-policy"));
        Assert.Single(view.Accepted);
    }

    [Fact]
    public void DecideDefaultsToTheHumanActorWhenNoActorGiven()
    {
        using var dir = new TempDirectory();
        string workspaceDir = InitWorkspace(dir);

        Run(workspaceDir, out _, "corr", "add", "--workspace", workspaceDir,
            "--type", "implements", "--unit", "unit-a", "--id", "corr-human");

        Run(workspaceDir, out string decideOut, "decide", "--workspace", workspaceDir,
            "--subject", "corr:corr-human", "--verdict", "accept", "--reason", "looks right");

        Assert.Contains("Accepted claim", decideOut);
        Assert.Contains("actor 'govert'", decideOut);
    }

    // ---- kp verify run --independence --------------------------------------------------------------

    [Fact]
    public void VerifyRunWiresIndependenceFlagThroughToTheReport()
    {
        using var dir = new TempDirectory();
        string workspaceDir = InitWorkspace(dir);

        Run(workspaceDir, out _, "pin", "--workspace", workspaceDir, "--side", "source", "--root", dir.Combine("src-root"), "--label", "d1");
        Run(workspaceDir, out _, "pin", "--workspace", workspaceDir, "--side", "target", "--root", dir.Combine("tgt-root"), "--label", "base");

        string casesPath = dir.Combine("cases.jsonl");
        File.WriteAllText(casesPath, "{\"name\":\"case1\"}\n");
        string sourceScript = WriteStubScript(dir.Path, "source.cmd", """{"name":"case1","result":{"value":1}}""");
        string targetScript = WriteStubScript(dir.Path, "target.cmd", """{"name":"case1","result":{"value":1}}""");

        Run(workspaceDir, out string verifyOut, "verify", "run", "--workspace", workspaceDir,
            "--unit", "unit-a", "--cases", casesPath,
            "--source-cmd", $"\"{sourceScript}\"", "--target-cmd", $"\"{targetScript}\"",
            "--independence", "independently-derived");

        Assert.Contains("pass", verifyOut);

        string reportPath = ExtractReportPath(verifyOut);
        Assert.Contains("\"independence\": \"independently-derived\"", File.ReadAllText(reportPath));
    }

    [Fact]
    public void VerifyRunRejectsAnUnknownIndependenceLevel()
    {
        using var dir = new TempDirectory();
        string workspaceDir = InitWorkspace(dir);
        Run(workspaceDir, out _, "pin", "--workspace", workspaceDir, "--side", "source", "--root", dir.Combine("src-root"), "--label", "d1");
        Run(workspaceDir, out _, "pin", "--workspace", workspaceDir, "--side", "target", "--root", dir.Combine("tgt-root"), "--label", "base");

        int exit = KpCliApp.Run(
            ["verify", "run", "--workspace", workspaceDir, "--unit", "unit-a", "--cases", "cases.jsonl",
             "--source-cmd", "x", "--target-cmd", "y", "--independence", "trust-me"],
            new StringWriter(), new StringWriter());
        Assert.Equal(1, exit);
    }

    // ---- kp candidates infer (end-to-end via the CLI) ------------------------------------------------

    [Fact]
    public void CandidatesInferEndToEndCreatesACandidateCorrespondenceViaCli()
    {
        using var dir = new TempDirectory();
        string workspaceDir = InitWorkspace(dir);

        // Source: a rust dump with one struct that name-normalizes to "ModA.HeaderParser".
        var dump = new ProviderDump("rust-map-dump@0.2.0", "krate", [
            new DumpEntity("struct", "HeaderParser", "krate::mod_a::HeaderParser", "src/mod_a.rs", 1, 5,
                new string('a', 64), null),
        ]);
        string dumpPath = dir.Combine("dump.json");
        File.WriteAllText(dumpPath, JsonSerializer.Serialize(dump));

        string sourceRoot = dir.Combine("src-root");
        Directory.CreateDirectory(sourceRoot);
        Run(workspaceDir, out _, "pin", "--workspace", workspaceDir, "--side", "source", "--root", sourceRoot, "--label", "d1");
        Run(workspaceDir, out string mapSourceOut, "map", "--workspace", workspaceDir, "--side", "source", "--label", "d1", "--dump", dumpPath);
        Assert.Contains("Imported 1 entities", mapSourceOut);

        // Target: a real C# class that normalizes to the same "ModA.HeaderParser".
        string targetRoot = dir.Combine("tgt-root");
        CSharpFixture.WriteSource(targetRoot, "HeaderParser.cs", """
            namespace Ns.ModA
            {
                public class HeaderParser
                {
                }
            }
            """);
        Run(workspaceDir, out _, "pin", "--workspace", workspaceDir, "--side", "target", "--root", targetRoot, "--label", "base");
        Run(workspaceDir, out string mapTargetOut, "map", "--workspace", workspaceDir, "--side", "target", "--label", "base");
        Assert.Contains("Imported 2 entities", mapTargetOut); // namespace + class

        Run(workspaceDir, out string inferOut, "candidates", "infer", "--workspace", workspaceDir);
        Assert.Contains("created 1", inferOut);
        Assert.Contains("ambiguous 0", inferOut);

        var candidates = CorrespondencesYaml.Read(workspaceDir).Where(c => c.Id.StartsWith("cand-", StringComparison.Ordinal)).ToList();
        var candidate = Assert.Single(candidates);
        Assert.Equal("candidate", candidate.Provenance);
        Assert.Equal("maps-to", candidate.Type);
        Assert.Equal("krate::mod_a::HeaderParser", candidate.Source!.SymbolPath);
        Assert.Equal("Ns.ModA.HeaderParser", candidate.Target!.SymbolPath);

        // `kp status` reflects the new candidate in Health v2's candidates count.
        Run(workspaceDir, out string statusOut, "status", "--workspace", workspaceDir);
        Assert.Contains("candidates: 1", statusOut);
    }

    [Fact]
    public void CandidatesInferHeaderCitationEndToEndCreatesACandidateCorrespondenceViaCli()
    {
        using var dir = new TempDirectory();
        string workspaceDir = InitWorkspace(dir);

        // Source: a rust dump with one root-module entity for the cited file.
        var dump = new ProviderDump("rust-map-dump@0.2.0", "krate", [
            new DumpEntity("module", "bocpd", "ftui_runtime::bocpd", "crates/ftui-runtime/src/bocpd.rs", 1, 40,
                new string('a', 64), null),
        ]);
        string dumpPath = dir.Combine("dump.json");
        File.WriteAllText(dumpPath, JsonSerializer.Serialize(dump));

        string sourceRoot = dir.Combine("src-root");
        Directory.CreateDirectory(sourceRoot);
        Run(workspaceDir, out _, "pin", "--workspace", workspaceDir, "--side", "source", "--root", sourceRoot, "--label", "d1");
        Run(workspaceDir, out _, "map", "--workspace", workspaceDir, "--side", "source", "--label", "d1", "--dump", dumpPath);

        // Target: a real C# file with a top-of-file "Port of <mount-prefixed path>" citation.
        string targetRoot = dir.Combine("tgt-root");
        CSharpFixture.WriteSource(targetRoot, "Bocpd.cs", """
            // SPDX-License-Identifier: Apache-2.0
            // Port of .external/frankentui/crates/ftui-runtime/src/bocpd.rs

            namespace FrankenTui.Runtime
            {
                public sealed class BocpdConfig
                {
                    public double MuSteadyMs = 200.0;
                }
            }
            """);
        Run(workspaceDir, out _, "pin", "--workspace", workspaceDir, "--side", "target", "--root", targetRoot, "--label", "base");
        Run(workspaceDir, out _, "map", "--workspace", workspaceDir, "--side", "target", "--label", "base");

        Run(workspaceDir, out string inferOut, "candidates", "infer", "--workspace", workspaceDir, "--heuristic", "header-citation");
        Assert.Contains("scanned 1 file(s)", inferOut);
        Assert.Contains("1 citation(s) found", inferOut);
        Assert.Contains("1 matched", inferOut);
        Assert.Contains("1 candidate(s) created", inferOut);
        Assert.Contains("0 unmatched cited path(s)", inferOut);

        var candidate = Assert.Single(CorrespondencesYaml.Read(workspaceDir), c => c.Id.StartsWith("cand-hc-", StringComparison.Ordinal));
        Assert.Equal("candidate", candidate.Provenance);
        Assert.Equal("maps-to", candidate.Type);
        Assert.Equal("ftui_runtime::bocpd", candidate.Source!.SymbolPath);
        Assert.Equal("FrankenTui.Runtime.BocpdConfig", candidate.Target!.SymbolPath);
        Assert.Equal("inferred:header-citation \"// Port of .external/frankentui/crates/ftui-runtime/src/bocpd.rs\"", candidate.Note);

        Run(workspaceDir, out string statusOut, "status", "--workspace", workspaceDir);
        Assert.Contains("candidates: 1", statusOut);
    }

    [Fact]
    public void CandidatesInferRejectsAnUnknownHeuristic()
    {
        using var dir = new TempDirectory();
        string workspaceDir = InitWorkspace(dir);

        int exit = KpCliApp.Run(
            ["candidates", "infer", "--workspace", workspaceDir, "--heuristic", "vibes"],
            new StringWriter(), new StringWriter());
        Assert.Equal(2, exit);
    }

    // ---- kp status: Health v2 in full ----------------------------------------------------------------

    [Fact]
    public void StatusPrintsHealthV2AllDimensions()
    {
        using var dir = new TempDirectory();
        string workspaceDir = InitWorkspace(dir);

        Run(workspaceDir, out string statusOut, "status", "--workspace", workspaceDir);

        Assert.Contains("mapped: 0", statusOut);
        Assert.Contains("corresponded: 0", statusOut);
        Assert.Contains("candidates: 0", statusOut);
        Assert.Contains("implemented: 0", statusOut);
        Assert.Contains("verified: 0", statusOut);
        Assert.Contains("stale: 0", statusOut);
        Assert.Contains("absence:", statusOut);
        Assert.Contains("unknown: 0", statusOut);
        Assert.Contains("notYetPorted: 0", statusOut);
        Assert.Contains("deliberatelyDropped: 0", statusOut);
        Assert.Contains("targetOnly:", statusOut);
        Assert.Contains("unexplained: 0", statusOut);
        Assert.Contains("intentional: 0", statusOut);
    }

    // ---- helpers ------------------------------------------------------------------------------------

    private static string InitWorkspace(TempDirectory dir)
    {
        string workspaceDir = dir.Combine("workspace");
        string sourceRoot = dir.Combine("src");
        string targetRoot = dir.Combine("target");
        Directory.CreateDirectory(sourceRoot);
        Directory.CreateDirectory(targetRoot);

        Run(workspaceDir, out string initOut, "init", "--workspace", workspaceDir, "--name", "m15-cli-tests",
            "--source-root", sourceRoot, "--target-root", targetRoot);
        Assert.Contains("Initialized kp workspace", initOut);
        return workspaceDir;
    }

    private static void Run(string workspaceDir, out string stdout, params string[] args)
    {
        var outw = new StringWriter();
        var errw = new StringWriter();
        int exit = KpCliApp.Run(args, outw, errw);
        stdout = outw.ToString();
        Assert.True(exit == 0, $"kp {string.Join(' ', args)} exited {exit} (workspace '{workspaceDir}'): {errw}{stdout}");
    }

    private static string WriteStubScript(string dir, string fileName, params string[] jsonLines)
    {
        string path = Path.Combine(dir, fileName);
        var lines = new List<string> { "@echo off" };
        lines.AddRange(jsonLines.Select(j => "echo " + j));
        File.WriteAllText(path, string.Join("\r\n", lines) + "\r\n");
        return path;
    }

    private static string ExtractReportPath(string verifyStdout)
    {
        const string marker = "Report: ";
        int idx = verifyStdout.IndexOf(marker, StringComparison.Ordinal);
        Assert.True(idx >= 0, $"'Report: ' marker not found in: {verifyStdout}");
        return verifyStdout[(idx + marker.Length)..].Trim();
    }
}
