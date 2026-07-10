using KodePorter.Core.Hashing;
using KodePorter.Core.Model;
using KodePorter.Core.Store;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace KodePorter.Core.Providers;

/// <summary>
/// In-proc C# provider (CONTRACT.md §3). Globs **/*.cs under a root (excluding bin/obj), parses
/// with `CSharpParseOptions.Default` at the latest language version, builds one
/// `CSharpCompilation` named "KpImport" referencing the trusted platform assemblies, and walks
/// declaration syntax for namespace / class / record / struct / enum / enummember / method /
/// property / field entities.
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
        var files = TreeHasher.EnumerateCSharpFiles(root)
            .OrderBy(f => f, StringComparer.Ordinal)
            .ToList();

        var parseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest);
        var trees = files
            .Select(f => (SyntaxTree)CSharpSyntaxTree.ParseText(File.ReadAllText(f), parseOptions, path: f))
            .ToList();

        var references = LoadTrustedPlatformAssemblyReferences();
        var compilation = CSharpCompilation.Create(
            "KpImport",
            trees,
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        // Compilation diagnostics are counted, never fatal (map-first principle, CONTRACT.md §3).
        int errorDiagnosticCount = compilation.GetDiagnostics()
            .Count(d => d.Severity == DiagnosticSeverity.Error);

        var candidates = new List<DumpEntity>();
        foreach (var tree in trees)
        {
            var semanticModel = compilation.GetSemanticModel(tree);
            string relativeFile = TreeHasher.ToRelativeForwardSlash(root, tree.FilePath);
            WalkTree(tree.GetRoot(), tree, semanticModel, relativeFile, candidates);
        }

        var deduplicated = EntityResolution.SortAndDeduplicate(candidates);
        var entities = EntityResolution.ToEntities(deduplicated, basis.Side, basis.Id);

        store.InsertEntities(basis.Id, entities);

        return new ImportResult(entities.Count, errorDiagnosticCount);
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
        List<DumpEntity> candidates)
    {
        foreach (var descendant in node.DescendantNodes())
        {
            switch (descendant)
            {
                case BaseNamespaceDeclarationSyntax namespaceDecl:
                    AddEntity(candidates, "namespace", semanticModel.GetDeclaredSymbol(namespaceDecl), namespaceDecl, tree, relativeFile);
                    break;
                case RecordDeclarationSyntax recordDecl:
                    AddEntity(candidates, "record", semanticModel.GetDeclaredSymbol(recordDecl), recordDecl, tree, relativeFile);
                    break;
                case ClassDeclarationSyntax classDecl:
                    AddEntity(candidates, "class", semanticModel.GetDeclaredSymbol(classDecl), classDecl, tree, relativeFile);
                    break;
                case StructDeclarationSyntax structDecl:
                    AddEntity(candidates, "struct", semanticModel.GetDeclaredSymbol(structDecl), structDecl, tree, relativeFile);
                    break;
                case EnumDeclarationSyntax enumDecl:
                    AddEntity(candidates, "enum", semanticModel.GetDeclaredSymbol(enumDecl), enumDecl, tree, relativeFile);
                    break;
                case EnumMemberDeclarationSyntax enumMemberDecl:
                    AddEntity(candidates, "enummember", semanticModel.GetDeclaredSymbol(enumMemberDecl), enumMemberDecl, tree, relativeFile);
                    break;
                case MethodDeclarationSyntax methodDecl:
                    AddEntity(candidates, "method", semanticModel.GetDeclaredSymbol(methodDecl), methodDecl, tree, relativeFile);
                    break;
                case PropertyDeclarationSyntax propertyDecl:
                    AddEntity(candidates, "property", semanticModel.GetDeclaredSymbol(propertyDecl), propertyDecl, tree, relativeFile);
                    break;
                case FieldDeclarationSyntax fieldDecl:
                    // A single `FieldDeclarationSyntax` can declare several variables
                    // (`private int a, b;`); each gets its own entity (own symbolPath) but
                    // shares the full field declaration as its span/content, since that
                    // statement is what fully declares each of them.
                    foreach (var variable in fieldDecl.Declaration.Variables)
                    {
                        var fieldSymbol = semanticModel.GetDeclaredSymbol(variable);
                        AddEntity(candidates, "field", fieldSymbol, fieldDecl, tree, relativeFile);
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
        string relativeFile)
    {
        if (symbol is null)
            return;

        string symbolPath = FormatSymbolPath(symbol);
        string? parentSymbolPath = GetParentSymbolPath(symbol);

        var lineSpan = tree.GetLineSpan(spanNode.Span);
        string contentText = spanNode.ToString().Replace("\r\n", "\n");
        string contentHash = Sha256Util.HexOfUtf8(contentText);

        candidates.Add(new DumpEntity(
            kind,
            symbol.Name,
            symbolPath,
            relativeFile,
            lineSpan.StartLinePosition.Line + 1,
            lineSpan.EndLinePosition.Line + 1,
            contentHash,
            parentSymbolPath));
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
