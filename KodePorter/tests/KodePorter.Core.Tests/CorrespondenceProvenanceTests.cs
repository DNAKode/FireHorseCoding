using KodePorter.Core.Domain;
using KodePorter.Core.Tests.Support;

namespace KodePorter.Core.Tests;

/// <summary>CONTRACT-M15.md §1.3: Correspondence.provenance (candidate|asserted), default
/// asserted for existing rows, round-trips honestly.</summary>
public class CorrespondenceProvenanceTests
{
    [Fact]
    public void ProvenanceDefaultsToAssertedWhenNotSpecified()
    {
        var c = new Correspondence("corr-1", "implements", null, "unit-a", null, null, null, null, null);
        Assert.Equal("asserted", c.Provenance);
    }

    [Fact]
    public void ProvenanceRoundTripsThroughCorrespondencesYaml()
    {
        using var dir = new TempDirectory();
        var items = new List<Correspondence>
        {
            new("corr-asserted", "implements", null, "unit-a", null, null, null, null, null, Stale: false, Provenance: "asserted"),
            new("corr-candidate", "maps-to", null, "", null, null, null, "inferred:name-norm", null, Stale: false, Provenance: "candidate"),
        };

        CorrespondencesYaml.Write(dir.Path, items);
        var reRead = CorrespondencesYaml.Read(dir.Path);

        Assert.Equal(items.OrderBy(c => c.Id, StringComparer.Ordinal), reRead);
        Assert.Contains("provenance: candidate", File.ReadAllText(CorrespondencesYaml.FilePath(dir.Path)));
    }

    [Fact]
    public void ReadingALegacyFileWithoutAProvenanceKeyDefaultsToAsserted()
    {
        using var dir = new TempDirectory();
        Directory.CreateDirectory(Path.Combine(dir.Path, ".kodeporter"));
        string path = CorrespondencesYaml.FilePath(dir.Path);
        // Hand-written, mirrors the pre-M15 field set exactly (no `provenance:` line at all).
        File.WriteAllText(path,
            "- id: \"corr-legacy\"\n" +
            "  type: \"implements\"\n" +
            "  unit: \"unit-a\"\n" +
            "  source: null\n" +
            "  target: null\n" +
            "  criterion: null\n" +
            "  note: null\n" +
            "  claimAid: null\n");

        var reRead = CorrespondencesYaml.Read(dir.Path);
        var corr = Assert.Single(reRead);
        Assert.Equal("asserted", corr.Provenance);
    }
}
