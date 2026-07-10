namespace KodePorter.Cli;

internal static class UsageText
{
    public const string Full = """
        kp - the KodePorter CLI (CONTRACT.md section 9)

        Usage:
          kp init    --workspace <dir> --name <s> --source-root <p> --target-root <p>
          kp pin     --workspace <dir> --side source|target --root <p> --label <s> [--analyzer <s>]
          kp map     --workspace <dir> --side source|target --label <s> [--dump <rust-dump.json>]
          kp unit new    --workspace <dir> --id <s> --name <s> --source-anchors <sp,sp> [--target-anchors <sp,sp>]
          kp corr add    --workspace <dir> --type <t> --unit <id> [--source <sp>] [--target <sp>]
                         [--criterion <c>] [--divergence-kind <k>] [--note <s>] [--id <s>]
          kp claim add   --workspace <dir> --unit <id> --id <s> --predicate kp.behavior --value <s> [--anchors <sp,sp>]
          kp decide  --workspace <dir> --subject <claim subject> --verdict accept|reject --reason <s>
          kp verify run  --workspace <dir> --unit <id> --cases <p> --source-cmd <s> --target-cmd <s>
          kp advance --workspace <dir> --side source|target --root <p> --label <s> [--dump <json>]
          kp status  --workspace <dir>
          kp export  --workspace <dir> --out <p>
          kp export-ledger --workspace <dir> --out <p>
          kp atlas   --workspace <dir> --out <p>

        Exit codes: 0 ok, 1 usage error, 2 domain error.
        """;

    public const string Short = "Run 'kp -h' for usage.";
}
