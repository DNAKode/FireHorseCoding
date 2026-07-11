namespace GovernanceLedger;

/// <summary>
/// The `govledger` CLI (CONTRACT-M15.md section 7): manual arg parsing, no packages, all verbs
/// wired to the ops in this project. <see cref="Run"/> is the whole entry point — Program.cs is a
/// one-line shim so this is directly callable in-process from tests (mirrors KodePorter.Cli's
/// KpCliApp).
/// </summary>
public static class GovLedgerApp
{
    public static int Run(string[] args, TextWriter stdout, TextWriter stderr)
    {
        try
        {
            if (args.Length == 0 || args[0] is "-h" or "--help")
            {
                stdout.WriteLine(UsageText.Full);
                return 0;
            }

            string verb = args[0];
            var a = new ArgReader(args[1..]);

            switch (verb)
            {
                case "seed": RunSeed(a, stdout); break;
                case "record": RunRecord(a, stdout); break;
                case "export": RunExport(a, stdout); break;
                case "rebuild": RunRebuild(a, stdout); break;
                default: throw new CliUsageException($"Unknown command '{verb}'.");
            }
            return 0;
        }
        catch (CliUsageException ex)
        {
            stderr.WriteLine($"govledger: {ex.Message}");
            stderr.WriteLine(UsageText.Short);
            return 1;
        }
        catch (Exception ex)
        {
            stderr.WriteLine($"govledger: {FirstLine(ex.Message)}");
            return 2;
        }
    }

    private static string FirstLine(string message)
    {
        int idx = message.IndexOf('\n');
        return idx < 0 ? message : message[..idx];
    }

    private static void RunSeed(ArgReader a, TextWriter stdout)
    {
        string dir = a.Require("dir");
        DateTimeOffset? wall = a.Optional("wall") is { } w ? WallClock.Parse(w) : null;
        SeedOp.Run(dir, wall);
        stdout.WriteLine($"Seeded governance ledger at '{dir}' ({SeedTable.Decisions.Count + 1} entries incl. supersession). Exported ledger-export.jsonl + LENS.html.");
    }

    private static void RunRecord(ArgReader a, TextWriter stdout)
    {
        var result = RecordOp.Run(a);
        stdout.WriteLine(result.DecisionAid is null
            ? $"Recorded aid {result.Aid}."
            : $"Recorded aid {result.Aid}; decision aid {result.DecisionAid}.");
    }

    private static void RunExport(ArgReader a, TextWriter stdout)
    {
        string dir = a.Require("dir");
        ExportOp.Run(dir);
        stdout.WriteLine($"Exported '{dir}/ledger-export.jsonl' and '{dir}/LENS.html'.");
    }

    private static void RunRebuild(ArgReader a, TextWriter stdout)
    {
        string dir = a.Require("dir");
        RebuildOp.Run(dir);
        stdout.WriteLine($"Rebuilt '{dir}/ledger.db' from ledger-export.jsonl and re-exported.");
    }
}
