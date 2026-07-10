namespace KodePorter.Core.Tests.Support;

internal static class CSharpFixture
{
    /// <summary>Writes <paramref name="sourceText"/> to <paramref name="rootDir"/>/<paramref name="relativeFileName"/>.</summary>
    public static string WriteSource(string rootDir, string relativeFileName, string sourceText)
    {
        string path = Path.Combine(rootDir, relativeFileName);
        string? dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
        File.WriteAllText(path, sourceText);
        return path;
    }
}
