using KodePorter.Core.Domain;
using KodePorter.Core.Tests.Support;

namespace KodePorter.Core.Tests;

/// <summary>CONTRACT-M15.md §1.4: UnitDoc.depth (thin|dossiered), default thin, a typed judgment
/// (never inferred from prose length) that round-trips through units/&lt;id&gt;.md.</summary>
public class UnitDepthTests
{
    [Fact]
    public void DepthDefaultsToThinAndIsOmittedFromTheWrittenFile()
    {
        using var dir = new TempDirectory();
        var doc = new UnitDoc("unit-a", "A", "mapped", [], [], [], false, "", "", "", "");

        UnitYaml.Write(dir.Path, doc);

        Assert.Equal("thin", doc.Depth);
        Assert.DoesNotContain("depth:", File.ReadAllText(UnitYaml.FilePath(dir.Path, "unit-a")));

        var reRead = UnitYaml.Read(dir.Path, "unit-a");
        Assert.Equal("thin", reRead.Depth);
    }

    [Fact]
    public void DossieredDepthRoundTrips()
    {
        using var dir = new TempDirectory();
        var doc = new UnitDoc("unit-a", "A", "mapped", [], [], [], false, "", "", "", "", Depth: "dossiered");

        UnitYaml.Write(dir.Path, doc);
        Assert.Contains("depth: dossiered", File.ReadAllText(UnitYaml.FilePath(dir.Path, "unit-a")));

        var reRead = UnitYaml.Read(dir.Path, "unit-a");
        Assert.Equal("dossiered", reRead.Depth);
    }
}
