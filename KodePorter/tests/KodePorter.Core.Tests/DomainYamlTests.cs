using KodePorter.Core.Domain;
using KodePorter.Core.Tests.Support;

namespace KodePorter.Core.Tests;

/// <summary>CONTRACT.md §4: the minimal exact YAML-subset readers/writers round-trip stably.</summary>
public class DomainYamlTests
{
    [Fact]
    public void ProjectYamlRoundTrips()
    {
        using var dir = new TempDirectory();
        var doc = new ProjectYamlDoc("headscan-port", "rust->csharp", "fixtures/slice-zero/rust", "fixtures/slice-zero/csharp", "kp-default@1");

        ProjectYaml.Write(dir.Path, doc);
        var reRead = ProjectYaml.Read(dir.Path);

        Assert.Equal(doc, reRead);
        AssertLfNoBom(ProjectYaml.FilePath(dir.Path));
    }

    [Fact]
    public void ProjectYamlRoundTripsWithNullPolicyVersion()
    {
        using var dir = new TempDirectory();
        var doc = new ProjectYamlDoc("headscan-port", "rust->csharp", "src", "target", null);

        ProjectYaml.Write(dir.Path, doc);
        var reRead = ProjectYaml.Read(dir.Path);

        Assert.Equal(doc, reRead);
    }

    [Fact]
    public void UnitDocRoundTripsWithAnchorsClaimsAndBody()
    {
        using var dir = new TempDirectory();
        var doc = new UnitDoc(
            Id: "unit-parse",
            Name: "Header parsing",
            Status: "in-progress",
            SourceAnchors: [new AnchorRef("headscan::parse", "d1", "aaaa")],
            TargetAnchors: [new AnchorRef("HeadScan.HeaderParser.Parse(string)", "base", "bbbb")],
            Claims: ["aid1", "aid2"],
            Stale: false,
            Purpose: "Parses headers.",
            Contract: "Given a string, returns tokens or an error code.",
            Questions: "What about empty input?",
            Evidence: "See runs/verify-unit-parse-*.json");

        UnitYaml.Write(dir.Path, doc);
        var reRead = UnitYaml.Read(dir.Path, doc.Id);

        AssertUnitDocEqual(doc, reRead);
        AssertLfNoBom(UnitYaml.FilePath(dir.Path, doc.Id));
    }

    [Fact]
    public void UnitDocRoundTripsWithEmptyAnchorsAndStaleFlag()
    {
        using var dir = new TempDirectory();
        var doc = new UnitDoc(
            Id: "unit-empty",
            Name: "Empty",
            Status: "mapped",
            SourceAnchors: [],
            TargetAnchors: [],
            Claims: [],
            Stale: true,
            Purpose: "",
            Contract: "",
            Questions: "",
            Evidence: "");

        UnitYaml.Write(dir.Path, doc);
        var reRead = UnitYaml.Read(dir.Path, doc.Id);

        AssertUnitDocEqual(doc, reRead);
        Assert.Contains("stale: true", File.ReadAllText(UnitYaml.FilePath(dir.Path, doc.Id)));
    }

    [Fact]
    public void UnitYamlListUnitIdsAndReadAllAreSortedAndComplete()
    {
        using var dir = new TempDirectory();
        UnitYaml.Write(dir.Path, MakeUnit("unit-b"));
        UnitYaml.Write(dir.Path, MakeUnit("unit-a"));

        var ids = UnitYaml.ListUnitIds(dir.Path);
        Assert.Equal(["unit-a", "unit-b"], ids);

        var all = UnitYaml.ReadAll(dir.Path);
        Assert.Equal(["unit-a", "unit-b"], all.Select(u => u.Id));
    }

    [Fact]
    public void CorrespondencesYamlRoundTripsWithNullAndPopulatedAnchorsAndDivergenceKind()
    {
        using var dir = new TempDirectory();
        var items = new List<Correspondence>
        {
            new("corr-1", "implements", null, "unit-a",
                new AnchorRef("headscan::parse", "d1", "aaaa"),
                new AnchorRef("HeadScan.HeaderParser.Parse(string)", "base", "bbbb"),
                "io-agreement-v1", "note text", "claimaid-1", Stale: false),
            new("corr-2", "diverges", "adaptation", "unit-b", null, null, null, null, null, Stale: true),
        };

        CorrespondencesYaml.Write(dir.Path, items);
        var reRead = CorrespondencesYaml.Read(dir.Path);

        Assert.Equal(items.OrderBy(c => c.Id, StringComparer.Ordinal), reRead);
        AssertLfNoBom(CorrespondencesYaml.FilePath(dir.Path));
    }

    [Fact]
    public void CorrespondencesYamlRoundTripsEmptyList()
    {
        using var dir = new TempDirectory();
        CorrespondencesYaml.Write(dir.Path, []);
        var reRead = CorrespondencesYaml.Read(dir.Path);
        Assert.Empty(reRead);
        Assert.Equal("[]\n", File.ReadAllText(CorrespondencesYaml.FilePath(dir.Path)));
    }

    [Fact]
    public void CorrespondencesYamlReadReturnsEmptyWhenFileMissing()
    {
        using var dir = new TempDirectory();
        Assert.Empty(CorrespondencesYaml.Read(dir.Path));
    }

    [Fact]
    public void PolicyYamlRoundTrips()
    {
        using var dir = new TempDirectory();
        var doc = new PolicyDoc(
            "kp-default", "1",
            new Dictionary<string, bool> { ["kpVerification"] = true, ["kpBehavior"] = false },
            new Dictionary<string, IReadOnlyList<string>> { ["kpVerification"] = ["verification-run"] });

        PolicyYaml.Write(dir.Path, doc);
        var reRead = PolicyYaml.Read(dir.Path);

        Assert.Equal(doc.Name, reRead.Name);
        Assert.Equal(doc.Version, reRead.Version);
        Assert.Equal(doc.AutoAccept, reRead.AutoAccept);
        Assert.Equal(doc.RequiredEvidence.Keys, reRead.RequiredEvidence.Keys);
        foreach (var key in doc.RequiredEvidence.Keys)
            Assert.Equal(doc.RequiredEvidence[key], reRead.RequiredEvidence[key]);
        Assert.Equal("policy:kp-default@1", reRead.ActorName);
        AssertLfNoBom(PolicyYaml.FilePath(dir.Path));
    }

    [Fact]
    public void PolicyEngineAllowsAutoAcceptOnlyWhenTrue()
    {
        var policy = new PolicyDoc("p", "1",
            new Dictionary<string, bool> { ["kpVerification"] = true, ["kpBehavior"] = false },
            new Dictionary<string, IReadOnlyList<string>>());

        Assert.True(PolicyEngine.AllowsAutoAccept(policy, "kpVerification"));
        Assert.False(PolicyEngine.AllowsAutoAccept(policy, "kpBehavior"));
        Assert.False(PolicyEngine.AllowsAutoAccept(policy, "kpUnknownClass"));
    }

    private static void AssertUnitDocEqual(UnitDoc expected, UnitDoc actual)
    {
        Assert.Equal(expected.Id, actual.Id);
        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.Status, actual.Status);
        Assert.Equal(expected.SourceAnchors, actual.SourceAnchors);
        Assert.Equal(expected.TargetAnchors, actual.TargetAnchors);
        Assert.Equal(expected.Claims, actual.Claims);
        Assert.Equal(expected.Stale, actual.Stale);
        Assert.Equal(expected.Purpose, actual.Purpose);
        Assert.Equal(expected.Contract, actual.Contract);
        Assert.Equal(expected.Questions, actual.Questions);
        Assert.Equal(expected.Evidence, actual.Evidence);
    }

    private static UnitDoc MakeUnit(string id) =>
        new(id, id, "mapped", [], [], [], false, "", "", "", "");

    private static void AssertLfNoBom(string path)
    {
        byte[] bytes = File.ReadAllBytes(path);
        Assert.DoesNotContain((byte)'\r', bytes);
        if (bytes.Length >= 3)
            Assert.False(bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF, "File must not have a UTF-8 BOM.");
    }
}
