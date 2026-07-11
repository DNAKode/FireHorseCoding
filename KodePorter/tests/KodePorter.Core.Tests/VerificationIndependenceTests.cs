using KodePorter.Core.Domain;
using KodePorter.Core.Gneiss;
using KodePorter.Core.Tests.Support;
using KodePorter.Core.Verify;

namespace KodePorter.Core.Tests;

/// <summary>
/// CONTRACT-M15.md §1.6: kp.verification's `independence` field (default `unknown`, caller-
/// attested via `--independence`) and policy.yaml's optional `requiredIndependence` gate on
/// auto-accept.
/// </summary>
public class VerificationIndependenceTests
{
    [Fact]
    public void RunDefaultsIndependenceToUnknownAndRecordsItInTheReportAndClaimValue()
    {
        using var dir = new TempDirectory();
        string workspaceDir = Path.Combine(dir.Path, "workspace");
        Directory.CreateDirectory(workspaceDir);

        string casesPath = WriteCasesFile(dir.Path);
        string sourceScript = WriteStubScript(dir.Path, "source.cmd", """{"name":"case1","result":{"value":1}}""");
        string targetScript = WriteStubScript(dir.Path, "target.cmd", """{"name":"case1","result":{"value":1}}""");

        var run = VerificationHarness.Run(
            workspaceDir, "unit-parse", "io-agreement-v1", casesPath,
            $"\"{sourceScript}\"", $"\"{targetScript}\"",
            "d1", "base", new DateTimeOffset(2026, 7, 10, 0, 0, 0, TimeSpan.Zero));

        Assert.Equal("unknown", run.Independence);
        Assert.Contains("\"independence\": \"unknown\"", File.ReadAllText(run.ReportJsonPath));
    }

    [Fact]
    public void RunRecordsAnExplicitlyAttestedIndependenceLevel()
    {
        using var dir = new TempDirectory();
        string workspaceDir = Path.Combine(dir.Path, "workspace");
        Directory.CreateDirectory(workspaceDir);

        string casesPath = WriteCasesFile(dir.Path);
        string sourceScript = WriteStubScript(dir.Path, "source.cmd", """{"name":"case1","result":{"value":1}}""");
        string targetScript = WriteStubScript(dir.Path, "target.cmd", """{"name":"case1","result":{"value":1}}""");

        var run = VerificationHarness.Run(
            workspaceDir, "unit-parse", "io-agreement-v1", casesPath,
            $"\"{sourceScript}\"", $"\"{targetScript}\"",
            "d1", "base", new DateTimeOffset(2026, 7, 10, 0, 0, 0, TimeSpan.Zero),
            independence: "independently-derived");

        Assert.Equal("independently-derived", run.Independence);
        Assert.Contains("\"independence\": \"independently-derived\"", File.ReadAllText(run.ReportJsonPath));
    }

    [Fact]
    public void PolicyRequiredIndependenceBlocksAutoAcceptUntilTheFloorIsMet()
    {
        using var dir = new TempDirectory();
        string workspaceDir = Path.Combine(dir.Path, "workspace");
        Directory.CreateDirectory(workspaceDir);
        using var binding = GneissBinding.Initialize(workspaceDir);

        var policy = new PolicyDoc("kp-default", "1",
            new Dictionary<string, bool> { ["kpVerification"] = true },
            new Dictionary<string, IReadOnlyList<string>>(),
            new Dictionary<string, string> { ["kpVerification"] = "independently-derived" });

        var weakRun = new VerifyRunResult("unit-weak", "io-agreement-v1", "hash", "src", "tgt", "d1", "base",
            [new VerifyCaseResult("case1", true, "{}", "{}")], "pass", "runs/weak.json", "runs/weak.md", "runs/weak.json",
            Independence: "implementation-coupled");
        string weakAid = VerificationHarness.PromoteResult(binding, policy, weakRun, evidenceAids: null, actor: "kodeporter", reason: "verify run");
        Assert.Empty(binding.AskClaim(GneissBinding.VerificationSubject("unit-weak", "io-agreement-v1")).Accepted);

        var strongRun = new VerifyRunResult("unit-strong", "io-agreement-v1", "hash", "src", "tgt", "d1", "base",
            [new VerifyCaseResult("case1", true, "{}", "{}")], "pass", "runs/strong.json", "runs/strong.md", "runs/strong.json",
            Independence: "independently-derived");
        VerificationHarness.PromoteResult(binding, policy, strongRun, evidenceAids: null, actor: "kodeporter", reason: "verify run");
        var strongView = binding.AskClaim(GneissBinding.VerificationSubject("unit-strong", "io-agreement-v1"));
        Assert.Single(strongView.Accepted);

        // The weak-independence claim is still just proposed, never silently accepted later.
        var weakExplanation = binding.Why(weakAid);
        Assert.Equal("proposed-unadmitted", weakExplanation.Status);
    }

    [Fact]
    public void AbsentRequiredIndependenceImposesNoConstraintExistingBehaviorPreserved()
    {
        var policy = new PolicyDoc("kp-default", "1",
            new Dictionary<string, bool> { ["kpVerification"] = true },
            new Dictionary<string, IReadOnlyList<string>>());

        Assert.True(PolicyEngine.AllowsAutoAccept(policy, "kpVerification", "unknown"));
        Assert.True(PolicyEngine.MeetsIndependence(policy, "kpVerification", "unknown"));
    }

    [Fact]
    public void RankOrderingIsIndependentlyDerivedAboveImplementationCoupledAboveUnknown()
    {
        var policy = new PolicyDoc("kp-default", "1",
            new Dictionary<string, bool> { ["kpVerification"] = true },
            new Dictionary<string, IReadOnlyList<string>>(),
            new Dictionary<string, string> { ["kpVerification"] = "implementation-coupled" });

        Assert.False(PolicyEngine.MeetsIndependence(policy, "kpVerification", "unknown"));
        Assert.True(PolicyEngine.MeetsIndependence(policy, "kpVerification", "implementation-coupled"));
        Assert.True(PolicyEngine.MeetsIndependence(policy, "kpVerification", "independently-derived"));
    }

    private static string WriteCasesFile(string dir)
    {
        string path = Path.Combine(dir, "cases.jsonl");
        File.WriteAllText(path, "{\"name\":\"case1\"}\n");
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
}
