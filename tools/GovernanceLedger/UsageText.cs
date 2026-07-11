namespace GovernanceLedger;

internal static class UsageText
{
    public const string Short = "usage: govledger <seed|record|export|rebuild> --dir <governance-dir> [...]  (see -h)";

    public const string Full = """
        govledger — the FireHorseCoding governance ledger tool (CONTRACT-M15.md section 7)

        Verbs:
          seed --dir <dir> [--wall <iso8601>]
              Create ledger.db from scratch, record the seed history, then export.

          record --dir <dir> --actor <a> --reason <r> --subject <s> --predicate <p> --value <v>
                 --wall <iso8601> [--valid-from <iso8601>] [--source <s>] [--method <m>] [--proposed]
                 [--decide accept|reject|retract|supersede --target <subject>]
              Append one governed decision (and, with --decide, a Gneiss decision against it).

          export --dir <dir>
              Write the canonical ledger-export.jsonl and regenerate LENS.html.

          rebuild --dir <dir>
              Recreate ledger.db from ledger-export.jsonl (the committed durable artifact), then
              re-export — proving the round trip is byte-identical.

        --dir is the governance directory (e.g. 'governance'). ledger.db is derived/gitignored;
        ledger-export.jsonl and LENS.html are the committed artifacts.
        """;
}
