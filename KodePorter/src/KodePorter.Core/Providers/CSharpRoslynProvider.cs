using KodePorter.Core.Hashing;
using KodePorter.Core.Model;
using KodePorter.Core.Store;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace KodePorter.Core.Providers;

/// <summary>
/// In-proc C# provider (CONTRACT.md §3). When the import root contains one or more `.csproj`
/// files, builds one `CSharpCompilation` per project (in `&lt;ProjectReference&gt;` topological
/// order — see <see cref="CSharpProjectDiscovery"/> — referencing the trusted platform
/// assemblies plus each already-built dependency compilation) so resolution/degraded grading is
/// scoped per project rather than treating a whole multi-project tree as one flat compilation.
/// Falls back to the previous behavior — one flat `CSharpCompilation` named "KpImport" over
/// every `**/*.cs` file under the root (excluding bin/obj) — when no `.csproj` exists. Either
/// way, source is parsed with `CSharpParseOptions.Default` at the latest language version and
/// walked for namespace / class / record / struct / enum / enummember / method / property /
/// field entities; entity identity (kind + symbolPath) is unaffected by which path ran.
/// </summary>
public sealed class CSharpRoslynProvider
{
    private static readonly SymbolDisplayFormat SymbolPathFormat = new(
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
        memberOptions: SymbolDisplayMemberOptions.IncludeContainingType | SymbolDisplayMemberOptions.IncludeParameters,
        parameterOptions: SymbolDisplayParameterOptions.IncludeType,
        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

    /// <summary>Globs, parses, compiles and imports <paramref name="basis"/>.Root's C# tree.</summary>
    public ImportResult Import(MapStore store, Basis basis)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(basis);

        string root = Path.GetFullPath(basis.Root);
        var parseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest);
        var trustedPlatformReferences = LoadTrustedPlatformAssemblyReferences();

        var projects = CSharpProjectDiscovery.DiscoverProjectsInDependencyOrder(root);
        var candidates = new List<DumpEntity>();
        int errorDiagnosticCount = projects.Count == 0
            ? ImportFlat(root, parseOptions, trustedPlatformReferences, candidates)
            : ImportPerProject(root, projects, parseOptions, trustedPlatformReferences, candidates);

        var deduplicated = EntityResolution.SortAndDeduplicate(candidates);
        var entities = EntityResolution.ToEntities(deduplicated, basis.Side, basis.Id);

        store.InsertEntities(basis.Id, entities);

        return new ImportResult(entities.Count, errorDiagnosticCount);
    }

    /// <summary>No `.csproj` under the root: one flat `CSharpCompilation` ("KpImport") over
    /// every `**/*.cs` file under the root (excluding bin/obj) — the original, pre-per-project
    /// behavior, kept so fixtures without a project file keep working unchanged.</summary>
    private static int ImportFlat(
        string root,
        CSharpParseOptions parseOptions,
        List<MetadataReference> trustedPlatformReferences,
        List<DumpEntity> candidates)
    {
        var files = TreeHasher.EnumerateCSharpFiles(root)
            .OrderBy(f => f, StringComparer.Ordinal)
            .ToList();

        var trees = files
            .Select(f => (SyntaxTree)CSharpSyntaxTree.ParseText(File.ReadAllText(f), parseOptions, path: f))
            .ToList();

        var compilation = CSharpCompilation.Create(
            "KpImport",
            trees,
            trustedPlatformReferences,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        return WalkCompilation(compilation, trees, root, candidates);
    }

    /// <summary>
    /// One or more `.csproj` under the root: builds one `CSharpCompilation` per project, in
    /// `&lt;ProjectReference&gt;` topological order, referencing the trusted platform assemblies
    /// plus a `CompilationReference` to each already-built dependency project (NuGet
    /// `&lt;PackageReference&gt;`s stay unresolved either way — accepted, see
    /// <see cref="CSharpProjectDiscovery"/>'s doc comment). Any `.cs` file under the root not
    /// owned by any discovered project's compile items (e.g. a stray script, or a project layout
    /// this discovery doesn't understand) is compiled together in one more flat, trusted-platform
    /// -only "leftover" compilation, so no file is silently dropped from the map.
    /// </summary>
    private static int ImportPerProject(
        string root,
        IReadOnlyList<CSharpProjectFile> projects,
        CSharpParseOptions parseOptions,
        List<MetadataReference> trustedPlatformReferences,
        List<DumpEntity> candidates)
    {
        int errorDiagnosticCount = 0;
        var compilationsByProjectPath = new Dictionary<string, CSharpCompilation>(StringComparer.OrdinalIgnoreCase);
        var ownedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var project in projects)
        {
            var files = project.CompileFiles.OrderBy(f => f, StringComparer.Ordinal).ToList();
            foreach (var f in files)
                ownedFiles.Add(f);

            var trees = files
                .Select(f => (SyntaxTree)CSharpSyntaxTree.ParseText(File.ReadAllText(f), parseOptions, path: f))
                .ToList();

            var references = new List<MetadataReference>(trustedPlatformReferences);
            foreach (var refPath in project.ProjectReferencePaths)
            {
                // A ProjectReference outside the discovered set (e.g. pointing outside the
                // import root, or a project this discovery couldn't parse) stays unresolved —
                // same acceptance as an unresolved NuGet package reference.
                if (compilationsByProjectPath.TryGetValue(refPath, out var dependencyCompilation))
                    references.Add(dependencyCompilation.ToMetadataReference());
            }

            var compilation = CSharpCompilation.Create(
                project.Name,
                trees,
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            compilationsByProjectPath[project.Path] = compilation;

            errorDiagnosticCount += WalkCompilation(compilation, trees, root, candidates);
        }

        var looseFiles = TreeHasher.EnumerateCSharpFiles(root)
            .Where(f => !ownedFiles.Contains(f))
            .OrderBy(f => f, StringComparer.Ordinal)
            .ToList();
        if (looseFiles.Count > 0)
        {
            var looseTrees = looseFiles
                .Select(f => (SyntaxTree)CSharpSyntaxTree.ParseText(File.ReadAllText(f), parseOptions, path: f))
                .ToList();
            var looseCompilation = CSharpCompilation.Create(
                "KpImportLoose",
                looseTrees,
                trustedPlatformReferences,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            errorDiagnosticCount += WalkCompilation(looseCompilation, looseTrees, root, candidates);
        }

        return errorDiagnosticCount;
    }

    /// <summary>Runs diagnostics on <paramref name="compilation"/> and walks every tree in it,
    /// appending entities to <paramref name="candidates"/>. Returns the Error-severity diagnostic
    /// count. Diagnostics are counted, never fatal (map-first principle, CONTRACT.md §3).
    /// CONTRACT-M15.md §1.1: entities whose file produced >=1 Error-severity diagnostic ->
    /// resolution "degraded", scoped per-tree (per-file) within this compilation.</summary>
    private static int WalkCompilation(
        CSharpCompilation compilation,
        List<SyntaxTree> trees,
        string root,
        List<DumpEntity> candidates)
    {
        var diagnostics = compilation.GetDiagnostics();
        int errorDiagnosticCount = diagnostics.Count(d => d.Severity == DiagnosticSeverity.Error);

        var treesWithErrors = diagnostics
            .Where(d => d.Severity == DiagnosticSeverity.Error && d.Location.SourceTree is not null)
            .Select(d => d.Location.SourceTree!)
            .ToHashSet();

        foreach (var tree in trees)
        {
            var semanticModel = compilation.GetSemanticModel(tree);
            string relativeFile = TreeHasher.ToRelativeForwardSlash(root, tree.FilePath);
            string resolution = treesWithErrors.Contains(tree) ? "degraded" : "clean";
            WalkTree(tree.GetRoot(), tree, semanticModel, relativeFile, resolution, candidates);
        }

        return errorDiagnosticCount;
    }

    private static List<MetadataReference> LoadTrustedPlatformAssemblyReferences()
    {
        var references = new List<MetadataReference>();
        if (AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") is not string trustedPlatformAssemblies)
            return references;

        foreach (var path in trustedPlatformAssemblies.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            try
            {
                references.Add(MetadataReference.CreateFromFile(path));
            }
            catch (IOException)
            {
                // Not a readable file; skip it rather than aborting the import (map-first principle).
            }
            catch (BadImageFormatException)
            {
                // Not a managed assembly (e.g. a native satellite); skip it.
            }
        }

        return references;
    }

    private static void WalkTree(
        SyntaxNode node,
        SyntaxTree tree,
        SemanticModel semanticModel,
        string relativeFile,
        string resolution,
        List<DumpEntity> candidates)
    {
        foreach (var descendant in node.DescendantNodes())
        {
            switch (descendant)
            {
                case BaseNamespaceDeclarationSyntax namespaceDecl:
                    AddEntity(candidates, "namespace", semanticModel.GetDeclaredSymbol(namespaceDecl), namespaceDecl, tree, relativeFile, resolution);
                    break;
                case RecordDeclarationSyntax recordDecl:
                    AddEntity(candidates, "record", semanticModel.GetDeclaredSymbol(recordDecl), recordDecl, tree, relativeFile, resolution);
                    break;
                case ClassDeclarationSyntax classDecl:
                    AddEntity(candidates, "class", semanticModel.GetDeclaredSymbol(classDecl), classDecl, tree, relativeFile, resolution);
                    break;
                case StructDeclarationSyntax structDecl:
                    AddEntity(candidates, "struct", semanticModel.GetDeclaredSymbol(structDecl), structDecl, tree, relativeFile, resolution);
                    break;
                case EnumDeclarationSyntax enumDecl:
                    AddEntity(candidates, "enum", semanticModel.GetDeclaredSymbol(enumDecl), enumDecl, tree, relativeFile, resolution);
                    break;
                case EnumMemberDeclarationSyntax enumMemberDecl:
                    AddEntity(candidates, "enummember", semanticModel.GetDeclaredSymbol(enumMemberDecl), enumMemberDecl, tree, relativeFile, resolution);
                    break;
                case MethodDeclarationSyntax methodDecl:
                    AddEntity(candidates, "method", semanticModel.GetDeclaredSymbol(methodDecl), methodDecl, tree, relativeFile, resolution);
                    break;
                case PropertyDeclarationSyntax propertyDecl:
                    AddEntity(candidates, "property", semanticModel.GetDeclaredSymbol(propertyDecl), propertyDecl, tree, relativeFile, resolution);
                    break;
                case FieldDeclarationSyntax fieldDecl:
                    // A single `FieldDeclarationSyntax` can declare several variables
                    // (`private int a, b;`); each gets its own entity (own symbolPath) but
                    // shares the full field declaration as its span/content, since that
                    // statement is what fully declares each of them.
                    foreach (var variable in fieldDecl.Declaration.Variables)
                    {
                        var fieldSymbol = semanticModel.GetDeclaredSymbol(variable);
                        AddEntity(candidates, "field", fieldSymbol, fieldDecl, tree, relativeFile, resolution);
                    }
                    break;
            }
        }
    }

    private static void AddEntity(
        List<DumpEntity> candidates,
        string kind,
        ISymbol? symbol,
        SyntaxNode spanNode,
        SyntaxTree tree,
        string relativeFile,
        string resolution)
    {
        if (symbol is null)
            return;

        string symbolPath = FormatSymbolPath(symbol);
        string? parentSymbolPath = GetParentSymbolPath(symbol);

        var lineSpan = tree.GetLineSpan(spanNode.Span);
        string contentText = spanNode.ToString().Replace("\r\n", "\n");
        string contentHash = Sha256Util.HexOfUtf8(contentText);

        // CONTRACT-M15.md §1.1: is_test = 1 when the file path contains a tests/ or test/
        // segment, or the containing type name ends with "Tests".
        bool isTest = FileHasTestSegment(relativeFile) || ContainingOrOwnTypeIsTestNamed(symbol);

        candidates.Add(new DumpEntity(
            kind,
            symbol.Name,
            symbolPath,
            relativeFile,
            lineSpan.StartLinePosition.Line + 1,
            lineSpan.EndLinePosition.Line + 1,
            contentHash,
            parentSymbolPath,
            Resolution: resolution,
            IsTest: isTest));
    }

    private static bool FileHasTestSegment(string relativeFile) =>
        relativeFile.Split('/').Any(seg =>
            seg.Equals("tests", StringComparison.OrdinalIgnoreCase) ||
            seg.Equals("test", StringComparison.OrdinalIgnoreCase));

    private static bool ContainingOrOwnTypeIsTestNamed(ISymbol symbol)
    {
        INamedTypeSymbol? type = symbol as INamedTypeSymbol ?? symbol.ContainingType;
        while (type is not null)
        {
            if (type.Name.EndsWith("Tests", StringComparison.Ordinal))
                return true;
            type = type.ContainingType;
        }
        return false;
    }

    private static string FormatSymbolPath(ISymbol symbol)
    {
        string formatted = symbol.ToDisplayString(SymbolPathFormat);
        return formatted.StartsWith("global::", StringComparison.Ordinal)
            ? formatted["global::".Length..]
            : formatted;
    }

    private static string? GetParentSymbolPath(ISymbol symbol)
    {
        var containing = symbol.ContainingSymbol;
        if (containing is null)
            return null;
        if (containing is INamespaceSymbol { IsGlobalNamespace: true })
            return null;
        return FormatSymbolPath(containing);
    }
}
