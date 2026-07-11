using KodePorter.Core.Candidates;
using KodePorter.Core.Domain;
using KodePorter.Core.Model;
using KodePorter.Core.Store;
using KodePorter.Core.Tests.Support;

namespace KodePorter.Core.Tests;

/// <summary>
/// CONTRACT-M15.md §2: `kp candidates infer --heuristic name-norm` over a small synthetic map —
/// exact normalized-pair match on kind-compatible entities creates `maps-to`/`candidate`
/// correspondences; already-covered pairs are skipped; a source symbol matching more than 3
/// unclaimed targets is recorded once as ambiguous and never linked.
/// </summary>
public class CandidateInferenceServiceTests
{
    private static readonly string FakeHash = new('a', 64);

    [Fact]
    public void InfersCleanMatchesSkipsCoveredAndUnmatchedAndRecordsAmbiguousOnce()
    {
        using var scratch = new TempDirectory();
        string workspaceDir = Path.Combine(scratch.Path, "workspace");
        Directory.CreateDirectory(workspaceDir);
        using var store = new MapStore(Path.Combine(workspaceDir, "kpmap.db"));

        var sourceBasis = MakeBasis(BasisSide.Source, "d1");
        store.InsertBasis(sourceBasis);
        store.InsertEntities(sourceBasis.Id,
        [
            MakeEntity(sourceBasis.Id, BasisSide.Source, "struct", "HeaderParser", "krate::mod_a::HeaderParser"),
            MakeEntity(sourceBasis.Id, BasisSide.Source, "fn", "parse_line", "krate::mod_a::parse_line"),
            MakeEntity(sourceBasis.Id, BasisSide.Source, "struct", "Widget", "krate::mod_b::Widget"),           // no target match -> skipped
            MakeEntity(sourceBasis.Id, BasisSide.Source, "struct", "AlreadyLinked", "krate::mod_c::AlreadyLinked"), // already covered -> skipped
            MakeEntity(sourceBasis.Id, BasisSide.Source, "fn", "run", "krate::mod_d::run"),                     // 4 target matches -> ambiguous
        ]);

        var targetBasis = MakeBasis(BasisSide.Target, "base");
        store.InsertBasis(targetBasis);
        store.InsertEntities(targetBasis.Id,
        [
            MakeEntity(targetBasis.Id, BasisSide.Target, "class", "HeaderParser", "Ns.ModA.HeaderParser"),
            MakeEntity(targetBasis.Id, BasisSide.Target, "method", "ParseLine", "Ns.ModA.ParseLine(string)"),
            MakeEntity(targetBasis.Id, BasisSide.Target, "class", "AlreadyLinked", "Ns.ModC.AlreadyLinked"),
            MakeEntity(targetBasis.Id, BasisSide.Target, "method", "Run", "Ns1.ModD.Run(string)"),
            MakeEntity(targetBasis.Id, BasisSide.Target, "method", "Run", "Ns2.ModD.Run(int)"),
            MakeEntity(targetBasis.Id, BasisSide.Target, "method", "Run", "Ns3.ModD.Run()"),
            MakeEntity(targetBasis.Id, BasisSide.Target, "method", "Run", "Ns4.ModD.Run(bool)"),
        ]);

        CorrespondencesYaml.Write(workspaceDir,
        [
            new Correspondence("corr-existing", "implements", null, "unit-x",
                new AnchorRef("krate::mod_c::AlreadyLinked", "d1", FakeHash), null, null, null, null, Stale: false, Provenance: "asserted"),
        ]);

        var result = CandidateInferenceService.Infer(workspaceDir, store);

        Assert.Equal(2, result.Created);
        Assert.Equal(2, result.Skipped); // Widget (no match), AlreadyLinked (already covered)
        Assert.Equal(["krate::mod_d::run"], result.Ambiguous);

        var correspondences = CorrespondencesYaml.Read(workspaceDir);
        var newOnes = correspondences.Where(c => c.Id.StartsWith("cand-", StringComparison.Ordinal)).ToList();
        Assert.Equal(2, newOnes.Count);
        Assert.All(newOnes, c =>
        {
            Assert.Equal("maps-to", c.Type);
            Assert.Equal("candidate", c.Provenance);
            Assert.Equal("inferred:name-norm", c.Note);
        });
        Assert.Contains(newOnes, c => c.Source!.SymbolPath == "krate::mod_a::HeaderParser" && c.Target!.SymbolPath == "Ns.ModA.HeaderParser");
        Assert.Contains(newOnes, c => c.Source!.SymbolPath == "krate::mod_a::parse_line" && c.Target!.SymbolPath == "Ns.ModA.ParseLine(string)");

        // The pre-existing correspondence is untouched.
        Assert.Contains(correspondences, c => c.Id == "corr-existing");

        // Re-running does not re-link the now-covered pairs (idempotent against its own output).
        var second = CandidateInferenceService.Infer(workspaceDir, store);
        Assert.Equal(0, second.Created);
    }

    [Fact]
    public void RejectsAnUnknownHeuristic()
    {
        using var scratch = new TempDirectory();
        string workspaceDir = Path.Combine(scratch.Path, "workspace");
        Directory.CreateDirectory(workspaceDir);
        using var store = new MapStore(Path.Combine(workspaceDir, "kpmap.db"));

        Assert.Throws<ArgumentException>(() => CandidateInferenceService.Infer(workspaceDir, store, "some-other-heuristic"));
    }

    /// <summary>
    /// Exercises the snake_case -> PascalCase / last-two-segment normalization (CONTRACT-M15.md
    /// §2) indirectly through the public <see cref="CandidateInferenceService.Infer"/> surface:
    /// a snake_case source fn ("parse_line") only matches its PascalCase target counterpart
    /// ("ParseLine(string)") because the normalizer converts the case, and the match survives a
    /// namespace prefix + parameter list difference because only the last two segments count.
    /// </summary>
    [Fact]
    public void SnakeCaseSourceNamesMatchPascalCaseTargetsAcrossNamespaceAndParameterListDifferences()
    {
        using var scratch = new TempDirectory();
        string workspaceDir = Path.Combine(scratch.Path, "workspace");
        Directory.CreateDirectory(workspaceDir);
        using var store = new MapStore(Path.Combine(workspaceDir, "kpmap.db"));

        var sourceBasis = MakeBasis(BasisSide.Source, "d1");
        store.InsertBasis(sourceBasis);
        store.InsertEntities(sourceBasis.Id,
            [MakeEntity(sourceBasis.Id, BasisSide.Source, "fn", "read_all_bytes", "krate::io::read_all_bytes")]);

        var targetBasis = MakeBasis(BasisSide.Target, "base");
        store.InsertBasis(targetBasis);
        store.InsertEntities(targetBasis.Id,
            [MakeEntity(targetBasis.Id, BasisSide.Target, "method", "ReadAllBytes", "Deeply.Nested.Io.ReadAllBytes(string, int)")]);

        var result = CandidateInferenceService.Infer(workspaceDir, store);

        Assert.Equal(1, result.Created);
        var created = Assert.Single(CorrespondencesYaml.Read(workspaceDir), c => c.Id.StartsWith("cand-", StringComparison.Ordinal));
        Assert.Equal("krate::io::read_all_bytes", created.Source!.SymbolPath);
        Assert.Equal("Deeply.Nested.Io.ReadAllBytes(string, int)", created.Target!.SymbolPath);
    }

    private static Basis MakeBasis(BasisSide side, string label)
    {
        string treeHash = side == BasisSide.Source ? new string('1', 64) : new string('2', 64);
        return new Basis(
            Id: EntityIdCalculator.ComputeBasisId(side, label, treeHash),
            Side: side, Label: label, Root: side == BasisSide.Source ? "src" : "target",
            TreeHash: treeHash, Toolchain: null, Analyzer: null, Created: DateTimeOffset.UtcNow);
    }

    private static Entity MakeEntity(string basisId, BasisSide side, string kind, string name, string symbolPath) => new(
        Id: EntityIdCalculator.ComputeEntityId(side, kind, symbolPath),
        BasisId: basisId, Kind: kind, Name: name, SymbolPath: symbolPath,
        File: "f.txt", StartLine: 1, EndLine: 1, ContentHash: FakeHash, ParentId: null);
}
