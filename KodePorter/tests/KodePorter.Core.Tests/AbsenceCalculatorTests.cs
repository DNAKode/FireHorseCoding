using KodePorter.Core.Absence;
using KodePorter.Core.Domain;
using KodePorter.Core.Model;
using KodePorter.Core.Store;
using KodePorter.Core.Tests.Support;

namespace KodePorter.Core.Tests;

/// <summary>
/// CONTRACT-M15.md §1.5: the computed default (source -> `unknown`, target-only -> `unexplained`)
/// for eligible, uncovered, non-test entities, overridden by .kodeporter/absences.yaml entries.
/// </summary>
public class AbsenceCalculatorTests
{
    private static readonly string FakeHash = new('a', 64);

    [Fact]
    public void ComputesDefaultsExcludesTestsAndCoveredEntitiesAndAppliesOverrides()
    {
        using var scratch = new TempDirectory();
        string workspaceDir = Path.Combine(scratch.Path, "workspace");
        Directory.CreateDirectory(workspaceDir);

        using var store = new MapStore(Path.Combine(workspaceDir, "kpmap.db"));

        var sourceBasis = MakeBasis(BasisSide.Source, "d1");
        store.InsertBasis(sourceBasis);
        store.InsertEntities(sourceBasis.Id,
        [
            MakeEntity(sourceBasis.Id, BasisSide.Source, "fn", "a", "krate::a"),                       // eligible, uncovered -> default "unknown"
            MakeEntity(sourceBasis.Id, BasisSide.Source, "fn", "b", "krate::b"),                       // eligible, uncovered, overridden -> "not-yet-ported"
            MakeEntity(sourceBasis.Id, BasisSide.Source, "fn", "c", "krate::tests::c", isTest: true),  // eligible but is_test -> excluded entirely
            MakeEntity(sourceBasis.Id, BasisSide.Source, "struct", "D", "krate::D"),                    // eligible but covered by a unit anchor -> excluded
            MakeEntity(sourceBasis.Id, BasisSide.Source, "module", "krate", "krate"),                   // not an eligible kind -> excluded
        ]);

        var targetBasis = MakeBasis(BasisSide.Target, "base");
        store.InsertBasis(targetBasis);
        store.InsertEntities(targetBasis.Id,
        [
            MakeEntity(targetBasis.Id, BasisSide.Target, "class", "X", "Ns.X"),                        // eligible, uncovered -> default "unexplained"
            MakeEntity(targetBasis.Id, BasisSide.Target, "method", "Y", "Ns.Y.Method()"),               // eligible, uncovered, overridden -> "intentional"
            MakeEntity(targetBasis.Id, BasisSide.Target, "class", "ZTests", "Ns.ZTests", isTest: true), // is_test -> excluded
            MakeEntity(targetBasis.Id, BasisSide.Target, "method", "Impl", "Ns.D.Method()"),            // covered by correspondence target -> excluded
        ]);

        UnitYaml.Write(workspaceDir, new UnitDoc("unit-d", "D", "mapped",
            SourceAnchors: [new AnchorRef("krate::D", "d1", FakeHash)],
            TargetAnchors: [], Claims: [], Stale: false, Purpose: "", Contract: "", Questions: "", Evidence: ""));

        CorrespondencesYaml.Write(workspaceDir,
        [
            new Correspondence("corr-d", "implements", null, "unit-d", null,
                new AnchorRef("Ns.D.Method()", "base", FakeHash), null, null, null, Stale: false),
        ]);

        AbsencesYaml.Write(workspaceDir,
        [
            new AbsenceOverride("krate::b", "not-yet-ported", "queued for M2", Side: "source"),
            new AbsenceOverride("Ns.Y.Method()", "intentional", "deliberate glue", Side: "target"),
        ]);

        var resolved = AbsenceCalculator.Compute(workspaceDir, store);

        var byKey = resolved.ToDictionary(r => (r.Side, r.SymbolPath));

        Assert.Equal(4, resolved.Count); // krate::a, krate::b, Ns.X, Ns.Y.Method() — nothing else

        Assert.Equal(("source", "unknown", false), (byKey[("source", "krate::a")].Side, byKey[("source", "krate::a")].Kind, byKey[("source", "krate::a")].IsOverride));
        Assert.Equal(("source", "not-yet-ported", true), (byKey[("source", "krate::b")].Side, byKey[("source", "krate::b")].Kind, byKey[("source", "krate::b")].IsOverride));
        Assert.Equal("queued for M2", byKey[("source", "krate::b")].Note);

        Assert.Equal(("target", "unexplained", false), (byKey[("target", "Ns.X")].Side, byKey[("target", "Ns.X")].Kind, byKey[("target", "Ns.X")].IsOverride));
        Assert.Equal(("target", "intentional", true), (byKey[("target", "Ns.Y.Method()")].Side, byKey[("target", "Ns.Y.Method()")].Kind, byKey[("target", "Ns.Y.Method()")].IsOverride));

        Assert.DoesNotContain(("source", "krate::tests::c"), byKey.Keys); // is_test excluded
        Assert.DoesNotContain(("source", "krate::D"), byKey.Keys);        // covered by unit anchor
        Assert.DoesNotContain(("source", "krate"), byKey.Keys);           // ineligible kind
        Assert.DoesNotContain(("target", "Ns.ZTests"), byKey.Keys);       // is_test excluded
        Assert.DoesNotContain(("target", "Ns.D.Method()"), byKey.Keys);   // covered by correspondence target
    }

    [Fact]
    public void ReturnsEmptyWhenAbsencesYamlDoesNotExistYet()
    {
        using var scratch = new TempDirectory();
        string workspaceDir = Path.Combine(scratch.Path, "workspace");
        Directory.CreateDirectory(workspaceDir);
        using var store = new MapStore(Path.Combine(workspaceDir, "kpmap.db"));

        Assert.Empty(AbsenceCalculator.Compute(workspaceDir, store));
        Assert.Empty(AbsencesYaml.Read(workspaceDir));
    }

    private static Basis MakeBasis(BasisSide side, string label)
    {
        string treeHash = side == BasisSide.Source ? new string('1', 64) : new string('2', 64);
        return new Basis(
            Id: EntityIdCalculator.ComputeBasisId(side, label, treeHash),
            Side: side, Label: label, Root: side == BasisSide.Source ? "src" : "target",
            TreeHash: treeHash, Toolchain: null, Analyzer: null, Created: DateTimeOffset.UtcNow);
    }

    private static Entity MakeEntity(string basisId, BasisSide side, string kind, string name, string symbolPath, bool isTest = false) => new(
        Id: EntityIdCalculator.ComputeEntityId(side, kind, symbolPath),
        BasisId: basisId, Kind: kind, Name: name, SymbolPath: symbolPath,
        File: "f.txt", StartLine: 1, EndLine: 1, ContentHash: FakeHash, ParentId: null,
        Resolution: "clean", IsTest: isTest);
}
