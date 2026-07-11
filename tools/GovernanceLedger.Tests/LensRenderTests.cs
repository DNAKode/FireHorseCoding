using GovernanceLedger;
using GovernanceLedger.Tests.Support;

namespace GovernanceLedger.Tests;

/// <summary>
/// LENS.html requirements (CONTRACT-M15.md section 7): self-containment (no external requests),
/// escaped interpolations, and the supersession rendering as struck-through-but-present with the
/// superseding decision linked.
/// </summary>
public class LensRenderTests
{
    private static void RunOk(string[] args)
    {
        var err = new StringWriter();
        int exit = GovLedgerApp.Run(args, new StringWriter(), err);
        Assert.True(exit == 0, $"govledger {string.Join(' ', args)} failed ({exit}): {err}");
    }

    [Fact]
    public void LensIsSelfContainedWithNoExternalRequests()
    {
        using var dir = new TempDirectory();
        RunOk(["seed", "--dir", dir.Path]);
        string lens = File.ReadAllText(Path.Combine(dir.Path, "LENS.html"));

        Assert.StartsWith("<!doctype html>", lens);
        Assert.DoesNotContain("http://", lens);
        Assert.DoesNotContain("https://", lens);
        Assert.DoesNotContain("<script src=", lens);
        Assert.DoesNotContain("<link rel=\"stylesheet\"", lens);
        Assert.DoesNotContain("fetch(", lens);
        Assert.DoesNotContain("<iframe", lens);
        // No script at all is required by this contract (LENS is static, unlike the Atlas).
        Assert.DoesNotContain("<script", lens);
    }

    [Fact]
    public void LensSupportsLightAndDarkViaPrefersColorScheme()
    {
        using var dir = new TempDirectory();
        RunOk(["seed", "--dir", dir.Path]);
        string lens = File.ReadAllText(Path.Combine(dir.Path, "LENS.html"));

        Assert.Contains("prefers-color-scheme: dark", lens);
        Assert.Contains("-apple-system", lens); // system font stack
    }

    [Fact]
    public void SupersededDecisionRendersStruckThroughWithLinkedSupersedingDecision()
    {
        using var dir = new TempDirectory();
        RunOk(["seed", "--dir", dir.Path]);
        string lens = File.ReadAllText(Path.Combine(dir.Path, "LENS.html"));

        // The target card is visibly struck-through-but-present (still fully rendered, marked
        // superseded/defeated) rather than removed.
        Assert.Contains("id=\"card-post-M1-positioning-map-is-product\"", lens);
        Assert.Contains("class=\"card status-defeated superseded\"", lens);
        Assert.Contains("KodePorter positioning: the map is the product", lens);
        Assert.Contains("badge-defeated", lens);

        // ...and links to the superseding decision's transaction.
        Assert.Contains("Superseded by", lens);
        Assert.Matches("Superseded by <a href=\"#tx-\\d+\">", lens);

        // The linked tx anchor actually exists in the timeline.
        var match = System.Text.RegularExpressions.Regex.Match(lens, "Superseded by <a href=\"#tx-(\\d+)\">");
        Assert.True(match.Success);
        Assert.Contains($"id=\"tx-{match.Groups[1].Value}\"", lens);

        // Every OTHER card is accepted (not defeated) — the supersession is targeted, not global.
        var otherCardCount = System.Text.RegularExpressions.Regex.Matches(lens, "class=\"card status-accepted\"").Count;
        Assert.Equal(7, otherCardCount);
    }

    [Fact]
    public void CardTrailShowsTheSupersedingDecision()
    {
        using var dir = new TempDirectory();
        RunOk(["seed", "--dir", dir.Path]);
        string lens = File.ReadAllText(Path.Combine(dir.Path, "LENS.html"));

        int cardStart = lens.IndexOf("id=\"card-post-M1-positioning-map-is-product\"", StringComparison.Ordinal);
        int cardEnd = lens.IndexOf("</article>", cardStart, StringComparison.Ordinal);
        string card = lens[cardStart..cardEnd];

        Assert.Contains("Decision trail", card);
        Assert.Contains("kind-supersedes", card);
        Assert.Contains("supersedes", card);
    }

    [Fact]
    public void DangerousReasonTextIsEscapedNotInjected()
    {
        using var dir = new TempDirectory();
        RunOk(["seed", "--dir", dir.Path]);
        RunOk([
            "record", "--dir", dir.Path,
            "--actor", "govert",
            "--reason", "<script>alert(1)</script> & \"quoted\" 'stuff'",
            "--subject", "decision:xss-probe",
            "--predicate", "gov.decision",
            "--value", "XSS probe entry",
            "--wall", "2026-07-12T00:00:00.0000000Z",
        ]);
        RunOk(["export", "--dir", dir.Path]);

        string lens = File.ReadAllText(Path.Combine(dir.Path, "LENS.html"));
        Assert.DoesNotContain("<script>alert(1)</script>", lens);
        Assert.Contains("&lt;script&gt;alert(1)&lt;/script&gt;", lens);
        Assert.Contains("&amp;", lens);
        Assert.Contains("&quot;quoted&quot;", lens);
    }

    [Fact]
    public void RecordThenSelfDecideAcceptAppearsAcceptedInNextExport()
    {
        using var dir = new TempDirectory();
        RunOk(["seed", "--dir", dir.Path]);
        RunOk([
            "record", "--dir", dir.Path,
            "--actor", "govert",
            "--reason", "A follow-up governance decision recorded via 'record', not seed.",
            "--subject", "decision:follow-up-one",
            "--predicate", "gov.decision",
            "--value", "Follow-up decision recorded via record.",
            "--wall", "2026-07-12T09:00:00.0000000Z",
            "--source", "manual",
            "--decide", "accept",
            "--target", "decision:follow-up-one",
        ]);
        RunOk(["export", "--dir", dir.Path]);

        var export = LedgerExport.Parse(File.ReadAllLines(Path.Combine(dir.Path, "ledger-export.jsonl")));
        Assert.Contains(export.Assrt, a => a.Subj == "decision:follow-up-one" && a.Pred == "gov.decision");
        Assert.Contains(export.Dec, d => d.DecisionKind == "accepts");

        string lens = File.ReadAllText(Path.Combine(dir.Path, "LENS.html"));
        Assert.Contains("id=\"card-follow-up-one\"", lens);
        Assert.Contains("Follow-up decision recorded via record.", lens);
    }
}
