using Gneiss.Cell;

namespace Gneiss.Cell.Tests;

/// <summary>
/// CONTRACT.md section 6, test 4: Ask(ctx with dataCut = t) over the full ledger is equivalent to
/// Ask(latest) over a copy truncated at t (two ledgers, compared by ResultHash).
/// </summary>
public sealed class Test4_CutoffCoherence
{
    private static readonly DateTimeOffset T0 = DateTimeOffset.Parse("2026-07-01T00:00:00Z");

    [Fact]
    public void Ask_With_DataCut_Equals_Ask_Latest_Over_Truncated_Copy()
    {
        using var pathA = new TempFile();
        using var pathB = new TempFile();

        long cutTx;
        string hashA;
        using (var a = GneissLedger.Create(pathA.Path))
        {
            a.Append(TestHelpers.Env("x", "r1", T0), new IAppendItem[] { new NewAssertion("Thing1", "weight", GValue.Number(10m)) });
            cutTx = a.Append(TestHelpers.Env("x", "r2", T0), new IAppendItem[] { new NewAssertion("Thing2", "weight", GValue.Number(20m)) }).Value;
            // this transaction must be excluded by the cutoff:
            a.Append(TestHelpers.Env("x", "r3", T0), new IAppendItem[] { new NewAssertion("Thing3", "weight", GValue.Number(30m)) });

            a.DeclareContext(TestHelpers.Env("x", "ctx", T0), new ContextDecl("Cut", DataCut: cutTx, DefCut: cutTx));
            var viewA = a.Ask("Cut", new Question(Predicate: "weight"));
            Assert.Equal(2, viewA.Accepted.Count); // Thing1, Thing2 only
            hashA = viewA.Label.ResultHash;
        }

        string hashB;
        using (var b = GneissLedger.Create(pathB.Path))
        {
            // identical prefix, then stop (truncated copy).
            b.Append(TestHelpers.Env("x", "r1", T0), new IAppendItem[] { new NewAssertion("Thing1", "weight", GValue.Number(10m)) });
            b.Append(TestHelpers.Env("x", "r2", T0), new IAppendItem[] { new NewAssertion("Thing2", "weight", GValue.Number(20m)) });

            b.DeclareContext(TestHelpers.Env("x", "ctx", T0), new ContextDecl("Latest", DataCut: null, DefCut: null));
            var viewB = b.Ask("Latest", new Question(Predicate: "weight"));
            Assert.Equal(2, viewB.Accepted.Count);
            hashB = viewB.Label.ResultHash;
        }

        Assert.Equal(hashA, hashB);
    }
}
