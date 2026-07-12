namespace KodePorter.Cli;

internal static class UsageText
{
    public const string Full = """
        kp - the KodePorter CLI (CONTRACT.md section 9, CONTRACT-M15.md)

        Usage:
          kp init    --workspace <dir> --name <s> --source-root <p> --target-root <p>
          kp pin     --workspace <dir> --side source|target --root <p> --label <s> [--analyzer <s>]
          kp map     --workspace <dir> --side source|target --label <s> [--dump <rust-dump.json>]
          kp unit new    --workspace <dir> --id <s> --name <s> --source-anchors <sp,sp> [--target-anchors <sp,sp>]
          kp unit set-depth --workspace <dir> --id <s> --depth thin|dossiered
          kp corr add    --workspace <dir> --type <t> --unit <id> [--source <sp>] [--target <sp>]
                         [--criterion <c>] [--divergence-kind <k>] [--note <s>] [--id <s>]
                         [--provenance candidate|asserted]
          kp candidates infer --workspace <dir> [--heuristic name-norm|header-citation]
          kp claim add   --workspace <dir> --unit <id> --id <s> --predicate kp.behavior --value <s> [--anchors <sp,sp>]
          kp decide  --workspace <dir> --subject <claim subject> --verdict accept|reject --reason <s> [--actor <s>]
          kp verify run  --workspace <dir> --unit <id> --cases <p> --source-cmd <s> --target-cmd <s>
                         [--independence independently-derived|implementation-coupled|unknown]
          kp advance --workspace <dir> --side source|target --root <p> --label <s> [--dump <json>]
          kp absence set --workspace <dir> --symbol <sp> --kind <k> [--note <s>] [--side source|target]
          kp note    --workspace <dir> --text <s> [--actor <s>]
          kp notes   --workspace <dir>
          kp status  --workspace <dir>
          kp export  --workspace <dir> --out <p>
          kp export-ledger --workspace <dir> --out <p>
          kp atlas   --workspace <dir> --out <p>

        Exit codes: 0 ok, 1 usage error, 2 domain error.
        """;

    public const string Short = "Run 'kp -h' for usage.";
}
