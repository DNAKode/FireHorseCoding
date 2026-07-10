using Gneiss.Cell;

namespace Gneiss.Cell.Tests;

/// <summary>CONTRACT.md section 6, test 2: double-run + reopen -> identical ResultHash; ExportLedgerJsonl stable.</summary>
public sealed class Test2_Determinism
{
    private static readonly DateTimeOffset T0 = DateTimeOffset.Parse("2026-07-01T00:00:00Z");

    [Fact]
    public void DoubleRun_And_Reopen_Give_Identical_ResultHash_And_Export()
    {
        using var path = new TempFile();

        List<string> export1;
        string hash1;
        string hash2;

        using (var l = GneissLedger.Create(path.Path))
        {
            l.Append(TestHelpers.Env("a", "r1", T0), new IAppendItem[]
            {
                new NewAssertion("Widget", "color", GValue.Text("red")),
            });
            l.Append(TestHelpers.Env("a", "r2", T0), new IAppendItem[]
            {
                new NewAssertion("Widget", "weight", GValue.Number(3.5m)),
            });
            l.DeclareContext(TestHelpers.Env("a", "ctx", T0), new ContextDecl("D", DataCut: null, DefCut: null));

            var v1 = l.Ask("D", new Question(Subject: "Widget"));
            var v2 = l.Ask("D", new Question(Subject: "Widget"));
            hash1 = v1.Label.ResultHash;
            hash2 = v2.Label.ResultHash;

            var e1 = l.ExportLedgerJsonl();
            var e2 = l.ExportLedgerJsonl();
            Assert.Equal(e1, e2);
            export1 = e1.ToList();
        }

        Assert.Equal(hash1, hash2);

        using (var reopened = GneissLedger.Open(path.Path))
        {
            var v3 = reopened.Ask("D", new Question(Subject: "Widget"));
            Assert.Equal(hash1, v3.Label.ResultHash);

            var e3 = reopened.ExportLedgerJsonl();
            Assert.Equal(export1, e3);
        }
    }
}
