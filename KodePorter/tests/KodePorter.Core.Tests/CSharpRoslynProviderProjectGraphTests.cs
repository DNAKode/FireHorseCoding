using KodePorter.Core.Model;
using KodePorter.Core.Providers;
using KodePorter.Core.Store;
using KodePorter.Core.Tests.Support;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace KodePorter.Core.Tests;

/// <summary>Per-project Roslyn compilation (brownfield-scale degraded-resolution fix): when the
/// import root contains `.csproj` files, resolution is graded per project (with
/// `&lt;ProjectReference&gt;`-driven `CompilationReference`s) rather than by flattening every
/// `.cs` file under the root into one compilation.</summary>
public class CSharpRoslynProviderProjectGraphTests
{
    private const string LibCsproj = """
        <Project Sdk="Microsoft.NET.Sdk">
          <PropertyGroup>
            <TargetFramework>net10.0</TargetFramework>
          </PropertyGroup>
        </Project>
        """;

    private const string AppCsprojReferencingLib = """
        <Project Sdk="Microsoft.NET.Sdk">
          <PropertyGroup>
            <TargetFramework>net10.0</TargetFramework>
          </PropertyGroup>
          <ItemGroup>
            <ProjectReference Include="../Lib/Lib.csproj" />
          </ItemGroup>
        </Project>
        """;

    // Both projects declare `Demo.Marker` — perfectly legal as two separate assemblies (the
    // routine case in a many-project solution: same-named internal helper types in sibling
    // projects), but a duplicate declaration (CS0101) the instant both files land in one
    // compilation, as the pre-per-project flat importer would have done.
    private const string LibSource = """
        namespace Demo;

        public class Marker
        {
        }

        public class Widget
        {
            public int Value;
        }
        """;

    private const string AppSource = """
        namespace Demo;

        public class Marker
        {
        }

        public class Consumer
        {
            public Widget MakeWidget() => new Widget();
        }
        """;

    [Fact]
    public void PerProjectResolutionAvoidsTheCrossProjectCollisionThatFlatCompilationWouldHit()
    {
        // Prove the premise first: merging both projects' source into one flat compilation (the
        // provider's old, and still-used-when-there's-no-csproj, behavior) really does collide
        // on `Demo.Marker` and mark both files degraded.
        var parseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest);
        var flatTrees = new SyntaxTree[]
        {
            CSharpSyntaxTree.ParseText(LibSource, parseOptions, path: "Lib/Widgets.cs"),
            CSharpSyntaxTree.ParseText(AppSource, parseOptions, path: "App/Program.cs"),
        };
        var flatCompilation = CSharpCompilation.Create(
            "FlatProbe",
            flatTrees,
            references: [],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var flatDiagnostics = flatCompilation.GetDiagnostics();
        Assert.Contains(flatDiagnostics, d => d.Id == "CS0101" && d.Severity == DiagnosticSeverity.Error);
        var flatTreesWithErrors = flatDiagnostics
            .Where(d => d.Severity == DiagnosticSeverity.Error && d.Location.SourceTree is not null)
            .Select(d => d.Location.SourceTree)
            .ToHashSet();
        Assert.Equal(2, flatTreesWithErrors.Count); // both Widgets.cs and Program.cs marked degraded under flat.

        // Now run the real provider over an on-disk two-project fixture wired the same way.
        using var root = new TempDirectory();
        CSharpFixture.WriteSource(root.Path, "Lib/Lib.csproj", LibCsproj);
        CSharpFixture.WriteSource(root.Path, "Lib/Widgets.cs", LibSource);
        CSharpFixture.WriteSource(root.Path, "App/App.csproj", AppCsprojReferencingLib);
        CSharpFixture.WriteSource(root.Path, "App/Program.cs", AppSource);

        using var dbDir = new TempDirectory();
        using var store = new MapStore(Path.Combine(dbDir.Path, "kpmap.db"));
        var basis = BasisPinner.Pin(store, BasisSide.Target, root.Path, "base");

        new CSharpRoslynProvider().Import(store, basis);

        var entities = store.GetEntities(basis.Id).ToDictionary(e => e.SymbolPath);

        // Lib compiles alone: clean.
        Assert.Equal("clean", entities["Demo.Widget"].Resolution);

        // App compiles alone too (Demo.Marker is declared once per project's own compilation —
        // no cross-assembly collision — and Widget resolves through the ProjectReference's
        // CompilationReference): clean, where the flat approach above proved degraded.
        Assert.Equal("clean", entities["Demo.Consumer"].Resolution);
        Assert.Equal("clean", entities["Demo.Consumer.MakeWidget()"].Resolution);
    }

    [Fact]
    public void FallsBackToOneFlatCompilationWhenNoCsprojExistsUnderTheRoot()
    {
        using var root = new TempDirectory();
        CSharpFixture.WriteSource(root.Path, "HeaderParser.cs", HeaderParserSource.V1);

        using var dbDir = new TempDirectory();
        using var store = new MapStore(Path.Combine(dbDir.Path, "kpmap.db"));
        var basis = BasisPinner.Pin(store, BasisSide.Target, root.Path, "base");

        var result = new CSharpRoslynProvider().Import(store, basis);

        Assert.Equal(0, result.ErrorDiagnosticCount);
        Assert.Equal(8, result.EntityCount);

        var entities = store.GetEntities(basis.Id).ToDictionary(e => e.SymbolPath);
        Assert.Equal(8, entities.Count);
        Assert.All(entities.Values, e => Assert.Equal("clean", e.Resolution));
    }
}
