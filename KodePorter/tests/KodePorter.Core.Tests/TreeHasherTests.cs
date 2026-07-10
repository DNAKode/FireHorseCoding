using KodePorter.Core.Hashing;
using KodePorter.Core.Tests.Support;

namespace KodePorter.Core.Tests;

/// <summary>Sanity coverage for BUILD step 2 (basis pinning) beyond what tests 1-4 exercise indirectly.</summary>
public class TreeHasherTests
{
    [Fact]
    public void CSharpTreeHashIsStableAndExcludesBinAndObj()
    {
        using var dir = new TempDirectory();
        CSharpFixture.WriteSource(dir.Path, "A.cs", "class A {}");
        CSharpFixture.WriteSource(dir.Path, Path.Combine("sub", "B.cs"), "class B {}");
        CSharpFixture.WriteSource(dir.Path, Path.Combine("bin", "Debug", "Ignored.cs"), "class Ignored {}");
        CSharpFixture.WriteSource(dir.Path, Path.Combine("obj", "Ignored2.cs"), "class Ignored2 {}");

        string hash1 = TreeHasher.ComputeCSharpTreeHash(dir.Path);
        string hash2 = TreeHasher.ComputeCSharpTreeHash(dir.Path);
        Assert.Equal(hash1, hash2);
        Assert.Matches("^[0-9a-f]{64}$", hash1);

        var included = TreeHasher.EnumerateCSharpFiles(dir.Path)
            .Select(f => TreeHasher.ToRelativeForwardSlash(dir.Path, f))
            .OrderBy(f => f, StringComparer.Ordinal)
            .ToList();
        Assert.Equal(["A.cs", "sub/B.cs"], included);
    }

    [Fact]
    public void CSharpTreeHashChangesWhenAFileChanges()
    {
        using var dir = new TempDirectory();
        CSharpFixture.WriteSource(dir.Path, "A.cs", "class A {}");
        string before = TreeHasher.ComputeCSharpTreeHash(dir.Path);

        CSharpFixture.WriteSource(dir.Path, "A.cs", "class A { void M() {} }");
        string after = TreeHasher.ComputeCSharpTreeHash(dir.Path);

        Assert.NotEqual(before, after);
    }

    [Fact]
    public void RustTreeHashCoversSrcRsFilesAndCargoToml()
    {
        using var dir = new TempDirectory();
        CSharpFixture.WriteSource(dir.Path, "Cargo.toml", "[package]\nname = \"headscan\"\n");
        CSharpFixture.WriteSource(dir.Path, Path.Combine("src", "lib.rs"), "pub fn parse() {}\n");
        CSharpFixture.WriteSource(dir.Path, Path.Combine("src", "sub", "helper.rs"), "pub fn helper() {}\n");
        // Not under src/ and not Cargo.toml -- excluded.
        CSharpFixture.WriteSource(dir.Path, "README.md", "ignored\n");

        var included = TreeHasher.EnumerateRustFiles(dir.Path)
            .Select(f => TreeHasher.ToRelativeForwardSlash(dir.Path, f))
            .OrderBy(f => f, StringComparer.Ordinal)
            .ToList();
        Assert.Equal(["Cargo.toml", "src/lib.rs", "src/sub/helper.rs"], included);

        string hash1 = TreeHasher.ComputeRustTreeHash(dir.Path);
        CSharpFixture.WriteSource(dir.Path, Path.Combine("src", "lib.rs"), "pub fn parse() { /* changed */ }\n");
        string hash2 = TreeHasher.ComputeRustTreeHash(dir.Path);
        Assert.NotEqual(hash1, hash2);
    }
}
