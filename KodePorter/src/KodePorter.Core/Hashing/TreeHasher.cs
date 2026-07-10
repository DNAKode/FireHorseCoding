namespace KodePorter.Core.Hashing;

/// <summary>
/// Computes the basis `tree_hash` (CONTRACT.md §2).
///
/// Canonical form (documented here because the contract leaves the exact bytes to the
/// implementation):
///   1. Collect the included files for the side (see <see cref="EnumerateRustFiles"/> /
///      <see cref="EnumerateCSharpFiles"/>), each identified by its path relative to the
///      root with forward slashes.
///   2. For each file compute `sha256HexOfFileBytes` = lowercase hex sha256 of the file's
///      raw bytes (no line-ending or encoding normalization — this is a byte-identity
///      check, distinct from entity `content_hash`, which does normalize).
///   3. Build one entry per file: `relativePath + "|" + sha256HexOfFileBytes`.
///   4. Sort entries by `relativePath` using ordinal string comparison.
///   5. Join the sorted entries with `"\n"` (no trailing newline).
///   6. `tree_hash` = lowercase hex sha256 of the UTF-8 bytes of that joined string.
/// </summary>
public static class TreeHasher
{
    /// <summary>Rust root: <c>**/*.rs</c> under <c>src/</c> plus <c>Cargo.toml</c> at the root.</summary>
    public static string ComputeRustTreeHash(string rootDir) => ComputeTreeHash(rootDir, EnumerateRustFiles(rootDir));

    /// <summary>C# root: <c>**/*.cs</c> excluding any path with a <c>bin/</c> or <c>obj/</c> segment.</summary>
    public static string ComputeCSharpTreeHash(string rootDir) => ComputeTreeHash(rootDir, EnumerateCSharpFiles(rootDir));

    public static string ComputeTreeHash(string rootDir, IEnumerable<string> absoluteFilePaths)
    {
        string fullRoot = Path.GetFullPath(rootDir);
        var entries = absoluteFilePaths
            .Select(f => (RelativePath: ToRelativeForwardSlash(fullRoot, f), FileHash: Sha256Util.HexOfFile(f)))
            .OrderBy(e => e.RelativePath, StringComparer.Ordinal)
            .Select(e => $"{e.RelativePath}|{e.FileHash}")
            .ToList();
        return Sha256Util.HexOfUtf8(string.Join('\n', entries));
    }

    public static IEnumerable<string> EnumerateRustFiles(string rootDir)
    {
        string fullRoot = Path.GetFullPath(rootDir);
        string srcDir = Path.Combine(fullRoot, "src");
        if (Directory.Exists(srcDir))
        {
            foreach (var f in Directory.EnumerateFiles(srcDir, "*.rs", SearchOption.AllDirectories))
                yield return f;
        }

        string cargoToml = Path.Combine(fullRoot, "Cargo.toml");
        if (File.Exists(cargoToml))
            yield return cargoToml;
    }

    public static IEnumerable<string> EnumerateCSharpFiles(string rootDir)
    {
        string fullRoot = Path.GetFullPath(rootDir);
        if (!Directory.Exists(fullRoot))
            yield break;

        foreach (var f in Directory.EnumerateFiles(fullRoot, "*.cs", SearchOption.AllDirectories))
        {
            string relative = ToRelativeForwardSlash(fullRoot, f);
            if (relative.Split('/').Any(segment => segment is "bin" or "obj"))
                continue;
            yield return f;
        }
    }

    public static string ToRelativeForwardSlash(string rootDir, string absolutePath)
        => Path.GetRelativePath(rootDir, absolutePath).Replace('\\', '/');
}
