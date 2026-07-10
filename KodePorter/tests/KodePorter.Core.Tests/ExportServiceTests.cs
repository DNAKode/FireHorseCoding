using KodePorter.Core.Domain;
using KodePorter.Core.Export;
using KodePorter.Core.Gneiss;
using KodePorter.Core.Tests.Support;

namespace KodePorter.Core.Tests;

/// <summary>CONTRACT.md §10 test 8: the export floor — every unit, correspondence, and claim status.</summary>
public class ExportServiceTests
{
    [Fact]
    public void PortingMdContainsEveryUnitCorrespondenceAndClaimStatusFromTheCurrentView()
    {
        using var dir = new TempDirectory();
        string workspaceDir = Path.Combine(dir.Path, "workspace");
        Directory.CreateDirectory(workspaceDir);
        using var binding = GneissBinding.Initialize(workspaceDir);

        ProjectYaml.Write(workspaceDir, new ProjectYamlDoc("headscan-port", "rust->csharp", "src", "target", "kp-default@1"));
        PolicyYaml.Write(workspaceDir, new PolicyDoc("kp-default", "1",
            new Dictionary<string, bool> { ["kpVerification"] = true, ["kpBehavior"] = false },
            new Dictionary<string, IReadOnlyList<string>> { ["kpVerification"] = ["verification-run"] }));

        UnitYaml.Write(workspaceDir, new UnitDoc("unit-accepted", "Accepted unit", "accepted", [], [], [], false, "", "", "", ""));
        UnitYaml.Write(workspaceDir, new UnitDoc("unit-proposed", "Proposed unit", "mapped", [], [], [], false, "", "", "", ""));

        CorrespondencesYaml.Write(workspaceDir,
        [
            new Correspondence("corr-accepted", "implements", null, "unit-accepted", null, null, "io-agreement-v1", null, null, Stale: false),
            new Correspondence("corr-proposed", "maps-to", null, "unit-proposed", null, null, null, null, null, Stale: false),
        ]);

        // unit-accepted: behavior claim proposed then human-accepted.
        string behaviorAid = binding.ProposeBehaviorClaim("unit-accepted", "B1", "Parses headers.", [], "kodeporter", "generated");
        binding.HumanDecide(behaviorAid, KpVerdict.Accept, "looks right", "govert");
        UnitYaml.Write(workspaceDir, UnitYaml.Read(workspaceDir, "unit-accepted") with { Claims = [behaviorAid] });

        // unit-proposed: behavior claim left undecided (proposed).
        string proposedAid = binding.ProposeBehaviorClaim("unit-proposed", "B1", "Some other behavior.", [], "kodeporter", "generated");
        UnitYaml.Write(workspaceDir, UnitYaml.Read(workspaceDir, "unit-proposed") with { Claims = [proposedAid] });

        // corr-accepted: correspondence claim proposed then policy-auto-accepted.
        string corrAid = binding.ProposeCorrespondenceClaim("corr-accepted",
            new CorrespondenceClaimValue("implements", null, null, "unit-accepted", "io-agreement-v1"),
            evidenceAids: null, actor: "kodeporter", reason: "generated");
        binding.PolicyAutoAccept(corrAid, "kp-default", "1", "auto-accept: policy allows kpCorrespondence");

        // corr-proposed: correspondence claim left undecided.
        binding.ProposeCorrespondenceClaim("corr-proposed",
            new CorrespondenceClaimValue("maps-to", null, null, "unit-proposed", null),
            evidenceAids: null, actor: "kodeporter", reason: "generated");

        // verification claim for corr-accepted's criterion: pass + auto-accepted.
        var verifyValue = new VerificationClaimValue("pass", "hash", "d1", "base", 2, [], "runs/verify-1.json");
        string verifyAid = binding.ProposeVerificationClaim("unit-accepted", "io-agreement-v1", verifyValue, evidenceAids: null,
            actor: "kodeporter", reason: "verify run");
        binding.PolicyAutoAccept(verifyAid, "kp-default", "1", "auto-accept: verification green");

        string outPath = Path.Combine(workspaceDir, "PORTING.md");
        ExportService.Export(workspaceDir, binding, outPath);

        Assert.True(File.Exists(outPath));
        string text = File.ReadAllText(outPath);

        // Every unit present.
        Assert.Contains("unit-accepted", text);
        Assert.Contains("unit-proposed", text);

        // Every correspondence present.
        Assert.Contains("corr-accepted", text);
        Assert.Contains("corr-proposed", text);

        // Claim statuses reflect the current belief view, not just the yaml.
        Assert.Contains("behavior:unit-accepted:B1 -> accepted", text);
        Assert.Contains("behavior:unit-proposed:B1 -> proposed", text);
        Assert.Contains("corr:corr-accepted -> accepted", text);
        Assert.Contains("corr:corr-proposed -> proposed", text);
        Assert.Contains("verify:unit-accepted:io-agreement-v1 -> accepted", text);

        AssertLfNoBom(outPath);
    }

    private static void AssertLfNoBom(string path)
    {
        byte[] bytes = File.ReadAllBytes(path);
        Assert.DoesNotContain((byte)'\r', bytes);
    }
}
