using System.Text.Json;
using KodePorter.Core.Domain;
using KodePorter.Core.Gneiss;
using KodePorter.Core.Tests.Support;
using KodePorter.Core.Verify;

namespace KodePorter.Core.Tests;

/// <summary>CONTRACT.md §10 test 6: the verify harness against two tiny stub commands.</summary>
public class VerificationHarnessTests
{
    [Fact]
    public void MatchingStubOutputsProduceAPassVerdictAndAReportPlusLabNotebook()
    {
        using var dir = new TempDirectory();
        string workspaceDir = Path.Combine(dir.Path, "workspace");
        Directory.CreateDirectory(workspaceDir);

        string casesPath = WriteCasesFile(dir.Path);
        string sourceScript = WriteStubScript(dir.Path, "source.cmd",
            """{"name":"case1","result":{"value":1}}""",
            """{"name":"case2","result":{"value":2}}""");
        string targetScript = WriteStubScript(dir.Path, "target-match.cmd",
            """{"name":"case1","result":{"value":1}}""",
            """{"name":"case2","result":{"value":2}}""");

        var run = VerificationHarness.Run(
            workspaceDir, "unit-parse", "io-agreement-v1", casesPath,
            $"\"{sourceScript}\"", $"\"{targetScript}\"",
            "d1", "base", new DateTimeOffset(2026, 7, 10, 0, 0, 0, TimeSpan.Zero));

        Assert.Equal("pass", run.Verdict);
        Assert.Equal(2, run.Results.Count);
        Assert.All(run.Results, r => Assert.True(r.Match));
        Assert.True(File.Exists(run.ReportJsonPath));
        Assert.True(File.Exists(run.ReportMdPath));

        string json = File.ReadAllText(run.ReportJsonPath);
        Assert.Contains("\"verdict\": \"pass\"", json);
        Assert.DoesNotContain("http://", json);
        Assert.DoesNotContain("https://", json);

        string md = File.ReadAllText(run.ReportMdPath);
        Assert.Contains("kp verify run", md); // the exact rerun command
        AssertLf(run.ReportJsonPath);
        AssertLf(run.ReportMdPath);
    }

    [Fact]
    public void InjectedMismatchProducesAFailVerdictNamingTheMismatchedCase()
    {
        using var dir = new TempDirectory();
        string workspaceDir = Path.Combine(dir.Path, "workspace");
        Directory.CreateDirectory(workspaceDir);

        string casesPath = WriteCasesFile(dir.Path);
        string sourceScript = WriteStubScript(dir.Path, "source.cmd",
            """{"name":"case1","result":{"value":1}}""",
            """{"name":"case2","result":{"value":2}}""");
        string targetScript = WriteStubScript(dir.Path, "target-mismatch.cmd",
            """{"name":"case1","result":{"value":1}}""",
            """{"name":"case2","result":{"value":999}}""");

        var run = VerificationHarness.Run(
            workspaceDir, "unit-parse", "io-agreement-v1", casesPath,
            $"\"{sourceScript}\"", $"\"{targetScript}\"",
            "d1", "base", new DateTimeOffset(2026, 7, 10, 0, 0, 1, TimeSpan.Zero));

        Assert.Equal("fail", run.Verdict);
        Assert.Equal(2, run.Results.Count);

        var case1 = run.Results.Single(r => r.Name == "case1");
        var case2 = run.Results.Single(r => r.Name == "case2");
        Assert.True(case1.Match);
        Assert.False(case2.Match);

        string json = File.ReadAllText(run.ReportJsonPath);
        Assert.Contains("\"verdict\": \"fail\"", json);
        Assert.Contains("\"name\": \"case2\"", json);

        string md = File.ReadAllText(run.ReportMdPath);
        Assert.Contains("case2", md);
    }

    [Fact]
    public void DuplicateResultLineForACaseNameProducesAFailVerdictNamingThatCaseEvenWhenTheLaterLineWouldHaveMatched()
    {
        using var dir = new TempDirectory();
        string workspaceDir = Path.Combine(dir.Path, "workspace");
        Directory.CreateDirectory(workspaceDir);

        string casesPath = WriteCasesFile(dir.Path);
        string sourceScript = WriteStubScript(dir.Path, "source.cmd",
            """{"name":"case1","result":{"value":1}}""",
            """{"name":"case2","result":{"value":2}}""");
        // target emits case1 twice: wrong-then-right. The old harness silently kept the last
        // (right) line and scored this a pass — laundering the duplicate. It must fail instead,
        // naming case1, regardless of which of the two lines "would have" matched.
        string targetScript = WriteStubScript(dir.Path, "target-dup.cmd",
            """{"name":"case1","result":{"value":999}}""",
            """{"name":"case1","result":{"value":1}}""",
            """{"name":"case2","result":{"value":2}}""");

        var run = VerificationHarness.Run(
            workspaceDir, "unit-parse", "io-agreement-v1", casesPath,
            $"\"{sourceScript}\"", $"\"{targetScript}\"",
            "d1", "base", new DateTimeOffset(2026, 7, 10, 0, 0, 2, TimeSpan.Zero));

        Assert.Equal("fail", run.Verdict);
        var case1 = run.Results.Single(r => r.Name == "case1");
        var case2 = run.Results.Single(r => r.Name == "case2");
        Assert.False(case1.Match);
        Assert.NotNull(case1.Reason);
        Assert.Contains("duplicate result line for case case1 in target output", case1.Reason);
        Assert.True(case2.Match); // the unrelated case is unaffected

        string json = File.ReadAllText(run.ReportJsonPath);
        Assert.Contains("\"verdict\": \"fail\"", json);
        Assert.Contains("duplicate result line for case case1 in target output", json);

        string md = File.ReadAllText(run.ReportMdPath);
        Assert.Contains("duplicate result line for case case1 in target output", md);
    }

    [Fact]
    public void DuplicateResultLineInTheSourceStreamAlsoFailsTheCase()
    {
        using var dir = new TempDirectory();
        string workspaceDir = Path.Combine(dir.Path, "workspace");
        Directory.CreateDirectory(workspaceDir);

        string casesPath = WriteCasesFile(dir.Path);
        string sourceScript = WriteStubScript(dir.Path, "source-dup.cmd",
            """{"name":"case1","result":{"value":1}}""",
            """{"name":"case1","result":{"value":1}}""",
            """{"name":"case2","result":{"value":2}}""");
        string targetScript = WriteStubScript(dir.Path, "target.cmd",
            """{"name":"case1","result":{"value":1}}""",
            """{"name":"case2","result":{"value":2}}""");

        var run = VerificationHarness.Run(
            workspaceDir, "unit-parse", "io-agreement-v1", casesPath,
            $"\"{sourceScript}\"", $"\"{targetScript}\"",
            "d1", "base", new DateTimeOffset(2026, 7, 10, 0, 0, 3, TimeSpan.Zero));

        Assert.Equal("fail", run.Verdict);
        var case1 = run.Results.Single(r => r.Name == "case1");
        Assert.False(case1.Match);
        Assert.Contains("duplicate result line for case case1 in source output", case1.Reason);
    }

    [Fact]
    public void TwoRunsOverIdenticalInputsAtDifferentWallClockTimesProduceIdenticalReportFileNamesAndByteIdenticalClaimValueJson()
    {
        using var dir = new TempDirectory();
        string workspaceDir = Path.Combine(dir.Path, "workspace");
        Directory.CreateDirectory(workspaceDir);
        using var binding = GneissBinding.Initialize(workspaceDir);

        string casesPath = WriteCasesFile(dir.Path);
        string sourceScript = WriteStubScript(dir.Path, "source.cmd",
            """{"name":"case1","result":{"value":1}}""",
            """{"name":"case2","result":{"value":2}}""");
        string targetScript = WriteStubScript(dir.Path, "target-match.cmd",
            """{"name":"case1","result":{"value":1}}""",
            """{"name":"case2","result":{"value":2}}""");

        var policy = new PolicyDoc("kp-default", "1",
            new Dictionary<string, bool> { ["kpVerification"] = true },
            new Dictionary<string, IReadOnlyList<string>>());

        var run1 = VerificationHarness.Run(
            workspaceDir, "unit-parse", "io-agreement-v1", casesPath,
            $"\"{sourceScript}\"", $"\"{targetScript}\"",
            "d1", "base", new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        string aid1 = VerificationHarness.PromoteResult(binding, policy, run1, evidenceAids: null, actor: "kodeporter", reason: "verify run 1");

        // Same corpus, same commands, same everything except real wall-clock time — this must
        // overwrite the same report files (deterministic, content-addressed naming) and produce a
        // byte-identical claim value JSON (CONTRACT.md §6/§8).
        var run2 = VerificationHarness.Run(
            workspaceDir, "unit-parse", "io-agreement-v1", casesPath,
            $"\"{sourceScript}\"", $"\"{targetScript}\"",
            "d1", "base", new DateTimeOffset(2099, 12, 31, 23, 59, 59, TimeSpan.Zero));
        string aid2 = VerificationHarness.PromoteResult(binding, policy, run2, evidenceAids: null, actor: "kodeporter", reason: "verify run 2");

        Assert.Equal(run1.ReportJsonPath, run2.ReportJsonPath);
        Assert.Equal(run1.ReportMdPath, run2.ReportMdPath);
        Assert.Equal(run1.ReportRelativePath, run2.ReportRelativePath);
        Assert.DoesNotContain(workspaceDir, run1.ReportRelativePath, StringComparison.OrdinalIgnoreCase); // never absolute
        Assert.False(Path.IsPathRooted(run1.ReportRelativePath));
        Assert.DoesNotContain('\\', run1.ReportRelativePath);

        string value1 = RawClaimValueJson(binding, aid1);
        string value2 = RawClaimValueJson(binding, aid2);
        Assert.Equal(value1, value2);
        Assert.DoesNotContain("2026-01-01", value1);
        Assert.DoesNotContain("2099-12-31", value1);
    }

    /// <summary>Recovers the raw canonical claim-value JSON the ledger stored for <paramref name="aid"/>,
    /// via the one public, sanctioned export (<see cref="GneissBinding.ExportLedgerJsonl"/>).</summary>
    private static string RawClaimValueJson(GneissBinding binding, string aid)
    {
        foreach (string line in binding.ExportLedgerJsonl())
        {
            using var doc = JsonDocument.Parse(line);
            var root = doc.RootElement;
            if (root.TryGetProperty("kind", out var kind) && kind.GetString() == "assrt"
                && root.TryGetProperty("aid", out var a) && a.GetString() == aid)
            {
                return root.GetProperty("val").GetString()!;
            }
        }
        throw new InvalidOperationException($"No assrt row found for aid '{aid}'.");
    }

    [Fact]
    public void PromoteResultAutoAcceptsOnPassWhenPolicyAllowsAndNeverAutoAcceptsOnFail()
    {
        using var dir = new TempDirectory();
        string workspaceDir = Path.Combine(dir.Path, "workspace");
        Directory.CreateDirectory(workspaceDir);
        using var binding = GneissBinding.Initialize(workspaceDir);

        var policyAllows = new PolicyDoc("kp-default", "1",
            new Dictionary<string, bool> { ["kpVerification"] = true },
            new Dictionary<string, IReadOnlyList<string>>());

        var passRun = new VerifyRunResult("unit-parse", "io-agreement-v1", "hash", "src-cmd", "tgt-cmd", "d1", "base",
            [new VerifyCaseResult("case1", true, "{}", "{}")], "pass", "runs/verify-1.json", "runs/verify-1.md", "runs/verify-1.json");

        string passAid = VerificationHarness.PromoteResult(binding, policyAllows, passRun, evidenceAids: null, actor: "kodeporter", reason: "verify run");
        var passView = binding.AskClaim(GneissBinding.VerificationSubject("unit-parse", "io-agreement-v1"));
        var accepted = Assert.Single(passView.Accepted);
        Assert.Equal(passAid, accepted.Aid);

        var failRun = new VerifyRunResult("unit-parse2", "io-agreement-v1", "hash", "src-cmd", "tgt-cmd", "d1", "base",
            [new VerifyCaseResult("case1", false, "{\"a\":1}", "{\"a\":2}")], "fail", "runs/verify-2.json", "runs/verify-2.md", "runs/verify-2.json");

        VerificationHarness.PromoteResult(binding, policyAllows, failRun, evidenceAids: null, actor: "kodeporter", reason: "verify run");
        var failView = binding.AskClaim(GneissBinding.VerificationSubject("unit-parse2", "io-agreement-v1"));
        Assert.Empty(failView.Accepted); // never auto-accepted as a pass
    }

    private static string WriteCasesFile(string dir)
    {
        string path = Path.Combine(dir, "cases.jsonl");
        File.WriteAllText(path, "{\"name\":\"case1\"}\n{\"name\":\"case2\"}\n");
        return path;
    }

    private static string WriteStubScript(string dir, string fileName, params string[] jsonLines)
    {
        string path = Path.Combine(dir, fileName);
        var lines = new List<string> { "@echo off" };
        lines.AddRange(jsonLines.Select(j => "echo " + j));
        File.WriteAllText(path, string.Join("\r\n", lines) + "\r\n");
        return path;
    }

    private static void AssertLf(string path)
    {
        byte[] bytes = File.ReadAllBytes(path);
        Assert.DoesNotContain((byte)'\r', bytes);
    }
}
