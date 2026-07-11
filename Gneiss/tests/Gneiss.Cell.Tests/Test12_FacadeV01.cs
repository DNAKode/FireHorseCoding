using Gneiss.Cell;
using Microsoft.Data.Sqlite;

namespace Gneiss.Cell.Tests;

/// <summary>
/// CONTRACT-V01.md — Gneiss.Cell facade v0.1: Append returns per-item aids (section 1); receipt ids
/// become deterministic/content-derived with upsert (section 2); GetAssertion fetches by aid
/// (section 3).
/// </summary>
public sealed class Test12_FacadeV01
{
    private static readonly DateTimeOffset T0 = DateTimeOffset.Parse("2026-07-01T00:00:00Z");

    // ---- section 2: deterministic receipt ids -------------------------------------------------

    [Fact]
    public void Identical_Asks_Produce_Same_ReceiptId_And_One_Receipt_Row()
    {
        using var path = new TempFile();
        using var l = GneissLedger.Create(path.Path);

        l.Append(TestHelpers.Env("x", "seed", T0), new IAppendItem[] { new NewAssertion("R12", "p", GValue.Number(1m)) });
        l.DeclareContext(TestHelpers.Env("x", "ctx", T0), new ContextDecl("RId"));

        var v1 = l.Ask("RId", new Question(Subject: "R12", Predicate: "p"));
        var v2 = l.Ask("RId", new Question(Subject: "R12", Predicate: "p"));

        Assert.Equal(v1.Label.ReceiptId, v2.Label.ReceiptId);
        Assert.Equal(v1.Label.ResultHash, v2.Label.ResultHash);

        using var side = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = path.Path, Mode = SqliteOpenMode.ReadWrite }.ToString());
        side.Open();
        using var cmd = side.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM receipt WHERE id = $id";
        cmd.Parameters.AddWithValue("$id", v1.Label.ReceiptId);
        var rowsWithId = (long)cmd.ExecuteScalar()!;
        Assert.Equal(1, rowsWithId); // upsert, not accumulation

        using var totalCmd = side.CreateCommand();
        totalCmd.CommandText = "SELECT COUNT(*) FROM receipt";
        var totalRows = (long)totalCmd.ExecuteScalar()!;
        Assert.Equal(1, totalRows); // only one Ask was ever made (two identical asks -> one row)
    }

    [Fact]
    public void DataChanging_Append_Then_ReAsk_Produces_Different_ReceiptId()
    {
        using var path = new TempFile();
        using var l = GneissLedger.Create(path.Path);

        var seed = l.Append(TestHelpers.Env("x", "seed", T0), new IAppendItem[] { new NewAssertion("R13", "p", GValue.Number(1m)) });
        var aid = seed.Aids[0];

        l.DeclareContext(TestHelpers.Env("x", "ctx", T0), new ContextDecl("RId2"));

        var v1 = l.Ask("RId2", new Question(Subject: "R13", Predicate: "p"));
        Assert.Single(v1.Accepted);

        l.Append(TestHelpers.Env("x", "retract", T0), new IAppendItem[] { new NewDecision(DecisionKind.Retracts, TargetAid: aid) });

        var v2 = l.Ask("RId2", new Question(Subject: "R13", Predicate: "p"));
        Assert.Empty(v2.Accepted);
        Assert.Single(v2.Defeated);

        Assert.NotEqual(v1.Label.ResultHash, v2.Label.ResultHash);
        Assert.NotEqual(v1.Label.ReceiptId, v2.Label.ReceiptId);
    }

    // ---- section 1: AppendResult aids in item order, decisions included ----------------------

    [Fact]
    public void AppendResult_Aids_Are_In_Item_Order_Including_Decisions()
    {
        using var path = new TempFile();
        using var l = GneissLedger.Create(path.Path);

        var seed = l.Append(TestHelpers.Env("x", "seed", T0), new IAppendItem[]
        {
            new NewAssertion("Target12", "p", GValue.Number(1m)),
        });
        Assert.Single(seed.Aids);
        var targetAid = seed.Aids[0];

        var result = l.Append(TestHelpers.Env("x", "batch", T0), new IAppendItem[]
        {
            new NewAssertion("Other12", "q", GValue.Text("v")),
            new NewDecision(DecisionKind.Retracts, TargetAid: targetAid),
            new NewAssertion("Another12", "r", GValue.Bool(true)),
        });

        Assert.Equal(3, result.Aids.Count);

        var info0 = l.GetAssertion(result.Aids[0]);
        Assert.NotNull(info0);
        Assert.Equal("Other12", info0!.Subject);
        Assert.Equal("q", info0.Predicate);

        var info1 = l.GetAssertion(result.Aids[1]);
        Assert.NotNull(info1);
        Assert.Equal("gneiss.decision", info1!.Predicate);
        Assert.Equal(targetAid, info1.Subject); // decision's subj = target aid

        var info2 = l.GetAssertion(result.Aids[2]);
        Assert.NotNull(info2);
        Assert.Equal("Another12", info2!.Subject);
        Assert.Equal("r", info2.Predicate);

        // cross-check against the aid the decision actually targets, via ExportLedgerJsonl (the
        // pre-existing, hash-formula-independent way of resolving an aid).
        Assert.Equal(TestHelpers.FindAid(l, result.Tx.Value, "Other12", "q"), result.Aids[0]);
        Assert.Equal(TestHelpers.FindAid(l, result.Tx.Value, targetAid, "gneiss.decision"), result.Aids[1]);
        Assert.Equal(TestHelpers.FindAid(l, result.Tx.Value, "Another12", "r"), result.Aids[2]);
    }

    // ---- section 3: GetAssertion round-trip ----------------------------------------------------

    [Fact]
    public void GetAssertion_RoundTrips_NewAssertion_Fields()
    {
        using var path = new TempFile();
        using var l = GneissLedger.Create(path.Path);

        var result = l.Append(TestHelpers.Env("agent", "propose", T0), new IAppendItem[]
        {
            new NewAssertion("Widget12", "risk", GValue.Text("high"), Proposed: true, Source: "modelA", Method: "heuristicV2", ConfidenceBp: 7500),
        });
        var aid = result.Aids[0];

        var info = l.GetAssertion(aid);
        Assert.NotNull(info);
        Assert.Equal(aid, info!.Aid);
        Assert.Equal(result.Tx.Value, info.Tx);
        Assert.Equal("Widget12", info.Subject);
        Assert.Equal("risk", info.Predicate);
        Assert.Equal("text", info.Value.Kind);
        Assert.Equal("high", info.Value.Canonical);
        Assert.Equal("proposed", info.Status);
        Assert.Equal("modelA", info.Source);
        Assert.Equal("heuristicV2", info.Method);
        Assert.Equal(7500, info.ConfidenceBp);
        Assert.Equal(TestHelpers.FindCKey(l, result.Tx.Value, "Widget12", "risk"), info.ClaimKey);
    }

    [Fact]
    public void GetAssertion_Returns_Null_For_Unknown_Aid()
    {
        using var path = new TempFile();
        using var l = GneissLedger.Create(path.Path);
        l.Append(TestHelpers.Env("x", "seed", T0), new IAppendItem[] { new NewAssertion("Unrelated12", "p", GValue.Bool(true)) });

        Assert.Null(l.GetAssertion("not-a-real-aid"));
    }
}
