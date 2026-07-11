namespace GovernanceLedger;

/// <summary>File layout under the `--dir` governance directory (CONTRACT-M15.md section 7):
/// `ledger.db` is the live SQLite ledger (gitignored, `*.db`); `ledger-export.jsonl` is the
/// committed durable artifact; `LENS.html` is the generated visual.</summary>
internal static class LedgerPaths
{
    public const string GovContextName = "gov-current";
    public const string PredGovDecision = "gov.decision";

    public static string DbPath(string dir) => Path.Combine(dir, "ledger.db");
    public static string ExportPath(string dir) => Path.Combine(dir, "ledger-export.jsonl");
    public static string LensPath(string dir) => Path.Combine(dir, "LENS.html");

    public static string DecisionSubject(string id) => $"decision:{id}";
}
