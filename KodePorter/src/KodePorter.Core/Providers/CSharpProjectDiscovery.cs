using System.Text.RegularExpressions;
using System.Xml.Linq;
using KodePorter.Core.Hashing;

namespace KodePorter.Core.Providers;

/// <summary>One `.csproj` discovered under an import root, with its resolved compile-item file
/// set and its `&lt;ProjectReference&gt;` edges (as absolute, normalized paths).</summary>
internal sealed record CSharpProjectFile(
    string Path,
    string Directory,
    string Name,
    IReadOnlyList<string> ProjectReferencePaths,
    IReadOnlyList<string> CompileFiles);

/// <summary>
/// Discovers `.csproj` files under an import root and parses just enough of each — without
/// invoking MSBuild — to build a per-project compile-item file set and a project dependency
/// graph (from `&lt;ProjectReference&gt;`), suitable for building one `CSharpCompilation` per
/// project in topological order (CONTRACT-M15.md §1.1 degraded-resolution scoping, extended to
/// per-project rather than per-flat-tree).
///
/// SUPPORTED (documented, not exhaustive):
///  - Any `.csproj` found by a recursive scan of the import root (excluding `bin/`/`obj/`
///    segments), unioned with whatever `.csproj` paths a top-level `.sln` (if any) resolves to
///    — matching the existing importer's "root is the whole tree" behavior (fixture and scratch
///    projects outside any `.sln` are still discovered and still compiled).
///  - SDK-style projects (`&lt;Project Sdk="..."/&gt;`): default compile glob is every `*.cs`
///    file under the project's own directory tree, excluding `bin/`/`obj/` segments (the same
///    rule as <see cref="TreeHasher.EnumerateCSharpFiles"/>), unless
///    `&lt;EnableDefaultCompileItems&gt;false&lt;/EnableDefaultCompileItems&gt;` is present.
///  - Legacy (non-SDK, `xmlns=".../msbuild/2003"`) projects: no default glob; only explicit
///    `&lt;Compile Include&gt;` items are honored.
///  - `&lt;Compile Remove="glob"/&gt;` and `&lt;Compile Include="glob"/&gt;` items (semicolon-
///    separated values), using only `*` / `**` / `?` wildcards, evaluated relative to the
///    project's own directory. `&lt;Compile Include&gt;` patterns that would resolve outside the
///    project's own directory tree (e.g. `../Shared/*.cs`) are not supported and are ignored.
///  - `&lt;ProjectReference Include="path.csproj"/&gt;` (semicolon-separated values), resolved
///    relative to the referencing project's directory.
///
/// NOT SUPPORTED (left as-is / ignored — this never throws, in keeping with the map-first
/// principle that a parse hiccup degrades data quality, not the import):
///  - Any `&lt;Compile&gt;` / `&lt;ProjectReference&gt;` item carrying a `Condition` attribute
///    (on the item itself or its parent `&lt;ItemGroup&gt;`), an `Update` attribute, other
///    metadata attributes, child metadata elements, or a value containing an MSBuild property
///    reference (`$(...)`) — such an item is skipped entirely rather than guessed at.
///  - MSBuild `&lt;Import&gt;`s, `.props`/`.targets` files, multi-targeting
///    (`&lt;TargetFrameworks&gt;`) and its per-TFM conditional items, and `&lt;PackageReference&gt;`
///    resolution (referenced NuGet package types stay unresolved in the resulting compilation —
///    same acceptance as the previous flat-compilation-only behavior).
///  - Nested project directories: a project whose directory contains another project's directory
///    does not have that inner directory's files excluded from its own default glob — such a
///    file can end up "owned" by two projects, each producing its own resolution grade for it;
///    the shared (kind, symbolPath) de-duplication in <see cref="EntityResolution"/> then keeps
///    whichever occurrence sorts first (deterministic, not necessarily the "right" one).
///  - Dependency cycles between projects: broken deterministically (by discovery order) so the
///    topological sort always terminates; a real `dotnet build` would refuse such a graph anyway.
/// </summary>
internal static class CSharpProjectDiscovery
{
    private static readonly Regex SlnProjectLine = new(
        "^Project\\(\"\\{[0-9A-Fa-f-]+\\}\"\\)\\s*=\\s*\"[^\"]*\",\\s*\"([^\"]+)\",\\s*\"\\{[0-9A-Fa-f-]+\\}\"",
        RegexOptions.Compiled);

    /// <summary>
    /// Discovers every project under <paramref name="root"/>, parses it, and returns the
    /// projects ordered so that every project appears after all projects it depends on
    /// (topological order by `&lt;ProjectReference&gt;`). Returns an empty list when no
    /// `.csproj` exists under the root (callers fall back to the flat, whole-root compilation).
    /// </summary>
    public static IReadOnlyList<CSharpProjectFile> DiscoverProjectsInDependencyOrder(string root)
    {
        var csprojPaths = EnumerateCsprojPaths(root);
        if (csprojPaths.Count == 0)
            return [];

        var projects = csprojPaths
            .Select(ParseProject)
            .OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(p => p.Path, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return TopologicalSort(projects);
    }

    private static List<string> EnumerateCsprojPaths(string root)
    {
        var paths = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        if (!Directory.Exists(root))
            return [];

        foreach (var f in Directory.EnumerateFiles(root, "*.csproj", SearchOption.AllDirectories))
        {
            string relative = TreeHasher.ToRelativeForwardSlash(root, f);
            if (relative.Split('/').Any(seg => seg is "bin" or "obj"))
                continue;
            paths.Add(Path.GetFullPath(f));
        }

        foreach (var slnPath in Directory.EnumerateFiles(root, "*.sln", SearchOption.TopDirectoryOnly)
                     .OrderBy(p => p, StringComparer.Ordinal))
        {
            foreach (var projectPath in ParseSlnProjectPaths(slnPath))
            {
                if (File.Exists(projectPath))
                    paths.Add(Path.GetFullPath(projectPath));
            }
        }

        return paths.ToList();
    }

    private static IEnumerable<string> ParseSlnProjectPaths(string slnPath)
    {
        string slnDir = Path.GetDirectoryName(slnPath)!;
        IEnumerable<string> lines;
        try
        {
            lines = File.ReadLines(slnPath);
        }
        catch (IOException)
        {
            yield break;
        }

        foreach (var line in lines)
        {
            var match = SlnProjectLine.Match(line);
            if (!match.Success)
                continue;
            string relative = match.Groups[1].Value.Replace('\\', '/');
            if (!relative.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
                continue;
            yield return Path.Combine(slnDir, relative.Replace('/', Path.DirectorySeparatorChar));
        }
    }

    private static CSharpProjectFile ParseProject(string path)
    {
        string projectDir = Path.GetDirectoryName(path)!;
        string name = Path.GetFileNameWithoutExtension(path);

        XDocument doc;
        try
        {
            doc = XDocument.Load(path);
        }
        catch (Exception ex) when (ex is System.Xml.XmlException or IOException or UnauthorizedAccessException)
        {
            // Malformed/unreadable project file: treat it as an empty, reference-less project
            // rather than aborting the whole import (map-first principle).
            return new CSharpProjectFile(path, projectDir, name, [], []);
        }

        var rootEl = doc.Root;
        if (rootEl is null)
            return new CSharpProjectFile(path, projectDir, name, [], []);

        XNamespace ns = rootEl.Name.Namespace;
        bool isSdkStyle = rootEl.Attribute("Sdk") is not null;

        var projectReferences = ExtractSimpleItemValues(doc, ns, "ProjectReference", "Include")
            .Select(v => Path.GetFullPath(Path.Combine(projectDir, v.Replace('/', Path.DirectorySeparatorChar))))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(v => v, StringComparer.OrdinalIgnoreCase)
            .ToList();

        bool enableDefaultCompileItems = isSdkStyle;
        var enableDefaultEl = doc.Descendants(ns + "EnableDefaultCompileItems").FirstOrDefault();
        if (enableDefaultEl is not null && bool.TryParse(enableDefaultEl.Value.Trim(), out var enabled))
            enableDefaultCompileItems = enabled;

        var files = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        if (enableDefaultCompileItems)
        {
            foreach (var f in TreeHasher.EnumerateCSharpFiles(projectDir))
                files.Add(Path.GetFullPath(f));
        }

        foreach (var pattern in ExtractSimpleItemValues(doc, ns, "Compile", "Include"))
        {
            if (pattern.Contains("$(", StringComparison.Ordinal))
                continue;
            foreach (var f in ExpandCompileIncludeGlob(projectDir, pattern))
                files.Add(f);
        }

        foreach (var pattern in ExtractSimpleItemValues(doc, ns, "Compile", "Remove"))
        {
            if (pattern.Contains("$(", StringComparison.Ordinal))
                continue;
            var regex = GlobToRegex(pattern);
            files.RemoveWhere(f => regex.IsMatch(TreeHasher.ToRelativeForwardSlash(projectDir, f)));
        }

        return new CSharpProjectFile(path, projectDir, name, projectReferences, files.ToList());
    }

    /// <summary>
    /// Yields the semicolon-split values of <paramref name="attributeName"/> on every
    /// unconditioned, metadata-free `&lt;<paramref name="elementName"/>&gt;` item — see the
    /// class doc's NOT SUPPORTED list for exactly what gets skipped.
    /// </summary>
    private static IEnumerable<string> ExtractSimpleItemValues(XDocument doc, XNamespace ns, string elementName, string attributeName)
    {
        foreach (var el in doc.Descendants(ns + elementName))
        {
            if (el.Attribute("Condition") is not null)
                continue;
            if (el.Parent?.Attribute("Condition") is not null)
                continue;
            if (el.HasElements)
                continue;
            bool hasUnsupportedAttribute = el.Attributes()
                .Any(a => a.Name.LocalName is not ("Include" or "Remove"));
            if (hasUnsupportedAttribute)
                continue;

            var value = el.Attribute(attributeName)?.Value;
            if (string.IsNullOrWhiteSpace(value))
                continue;

            foreach (var part in value.Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                string trimmed = part.Trim();
                if (trimmed.Length > 0)
                    yield return trimmed;
            }
        }
    }

    private static IEnumerable<string> ExpandCompileIncludeGlob(string projectDir, string pattern)
    {
        string normalized = pattern.Replace('\\', '/');
        if (normalized.StartsWith("../", StringComparison.Ordinal) || normalized.Contains("/../", StringComparison.Ordinal))
            yield break; // escapes the project directory - not supported (see class doc).

        if (!normalized.Contains('*') && !normalized.Contains('?'))
        {
            string literal = Path.GetFullPath(Path.Combine(projectDir, normalized));
            if (literal.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) && File.Exists(literal))
                yield return literal;
            yield break;
        }

        var regex = GlobToRegex(normalized);
        foreach (var f in TreeHasher.EnumerateCSharpFiles(projectDir))
        {
            string relative = TreeHasher.ToRelativeForwardSlash(projectDir, f);
            if (regex.IsMatch(relative))
                yield return Path.GetFullPath(f);
        }
    }

    /// <summary>Translates a simple MSBuild glob (`*`, `**`, `?`) to an anchored, case-insensitive
    /// regex matched against a forward-slash relative path.</summary>
    private static Regex GlobToRegex(string pattern)
    {
        string normalized = pattern.Replace('\\', '/');
        var sb = new System.Text.StringBuilder("^");
        int i = 0;
        while (i < normalized.Length)
        {
            char c = normalized[i];
            if (c == '*' && i + 1 < normalized.Length && normalized[i + 1] == '*')
            {
                if (i + 2 < normalized.Length && normalized[i + 2] == '/')
                {
                    sb.Append("(?:.*/)?");
                    i += 3;
                }
                else
                {
                    sb.Append(".*");
                    i += 2;
                }
            }
            else if (c == '*')
            {
                sb.Append("[^/]*");
                i++;
            }
            else if (c == '?')
            {
                sb.Append("[^/]");
                i++;
            }
            else
            {
                sb.Append(Regex.Escape(c.ToString()));
                i++;
            }
        }
        sb.Append('$');
        return new Regex(sb.ToString(), RegexOptions.IgnoreCase | RegexOptions.Compiled);
    }

    private static List<CSharpProjectFile> TopologicalSort(List<CSharpProjectFile> projects)
    {
        var byPath = projects.ToDictionary(p => p.Path, p => p, StringComparer.OrdinalIgnoreCase);
        var state = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase); // 1=visiting, 2=done
        var order = new List<CSharpProjectFile>(projects.Count);

        void Visit(CSharpProjectFile project)
        {
            if (state.TryGetValue(project.Path, out var s))
            {
                if (s is 1 or 2)
                    return; // already done, or a cycle - break deterministically here.
            }
            state[project.Path] = 1;
            foreach (var refPath in project.ProjectReferencePaths)
            {
                if (byPath.TryGetValue(refPath, out var dep))
                    Visit(dep);
            }
            state[project.Path] = 2;
            order.Add(project);
        }

        foreach (var project in projects)
            Visit(project);

        return order;
    }
}
