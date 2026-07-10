using Gneiss.Cell;

namespace Gneiss.Cell.Tests;

/// <summary>
/// CONTRACT.md section 6, test 5: a decision by tgt_aid targeting a same-tx (or later) aid raises
/// GneissException and the whole Append aborts, leaving the ledger unchanged.
/// </summary>
public sealed class Test5_I6
{
    private static readonly DateTimeOffset T0 = DateTimeOffset.Parse("2026-07-01T00:00:00Z");

    [Fact]
    public void SameTx_AidDecision_Throws_And_Ledger_Is_Unchanged()
    {
        using var path = new TempFile();
        using var l = GneissLedger.Create(path.Path);

        // seed one unrelated, legal transaction first.
        l.Append(TestHelpers.Env("x", "seed", T0), new IAppendItem[] { new NewAssertion("Seed", "p", GValue.Bool(true)) });

        var highWaterBefore = l.HighWater;
        var nextTx = highWaterBefore + 1;

        const string subj = "Illegal";
        const string pred = "q";
        var value = GValue.Text("v");

        // the aid the target assertion (ordinal 0 in the upcoming Append) will be assigned — predictable
        // per CONTRACT.md section 1's determinism guarantee, since we supply every field that feeds the hash.
        var predictedAid = TestHelpers.PredictAid(nextTx, 0, subj, pred, value.Canonical, null, null, "fact", null, null);

        var items = new IAppendItem[]
        {
            new NewAssertion(subj, pred, value),
            new NewDecision(DecisionKind.Retracts, TargetAid: predictedAid), // targets an assertion at the SAME tx: not strictly lower.
        };

        var ex = Assert.Throws<GneissException>(() => l.Append(TestHelpers.Env("x", "illegal", T0), items));
        Assert.Equal("I6Violation", ex.Code);

        Assert.Equal(highWaterBefore, l.HighWater); // no new tx row committed
        var lines = l.ExportLedgerJsonl();
        Assert.DoesNotContain(lines, line => line.Contains(subj));
    }
}
