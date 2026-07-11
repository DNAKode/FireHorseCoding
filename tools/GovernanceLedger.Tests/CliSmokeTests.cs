using GovernanceLedger;
using GovernanceLedger.Tests.Support;

namespace GovernanceLedger.Tests;

/// <summary>CLI smoke tests: verb routing, usage errors, and exit codes (mirrors KodePorter.Cli's
/// CliSmokeTests pattern) via <see cref="GovLedgerApp.Run"/> in-process.</summary>
public class CliSmokeTests
{
    [Fact]
    public void UnknownCommandReturnsExitCodeOneWithUsageHint()
    {
        var err = new StringWriter();
        int exit = GovLedgerApp.Run(["frobnicate"], new StringWriter(), err);
        Assert.Equal(1, exit);
        Assert.Contains("Unknown command", err.ToString());
    }

    [Fact]
    public void MissingRequiredFlagReturnsExitCodeOne()
    {
        var err = new StringWriter();
        int exit = GovLedgerApp.Run(["seed"], new StringWriter(), err);
        Assert.Equal(1, exit);
        Assert.Contains("Missing required --dir", err.ToString());
    }

    [Fact]
    public void HelpFlagPrintsUsageAndReturnsZero()
    {
        var outw = new StringWriter();
        int exit = GovLedgerApp.Run(["-h"], outw, new StringWriter());
        Assert.Equal(0, exit);
        Assert.Contains("govledger", outw.ToString());
    }

    [Fact]
    public void ExportBeforeSeedReturnsExitCodeTwoWithDomainError()
    {
        using var dir = new TempDirectory();
        var err = new StringWriter();
        int exit = GovLedgerApp.Run(["export", "--dir", dir.Path], new StringWriter(), err);
        Assert.Equal(2, exit);
        Assert.Contains("No ledger at", err.ToString());
    }

    [Fact]
    public void RebuildBeforeSeedReturnsExitCodeTwoWithDomainError()
    {
        using var dir = new TempDirectory();
        var err = new StringWriter();
        int exit = GovLedgerApp.Run(["rebuild", "--dir", dir.Path], new StringWriter(), err);
        Assert.Equal(2, exit);
        Assert.Contains("No export at", err.ToString());
    }

    [Fact]
    public void SeedingTwiceWithoutDeletingTheDbReturnsExitCodeTwo()
    {
        using var dir = new TempDirectory();
        Assert.Equal(0, GovLedgerApp.Run(["seed", "--dir", dir.Path], new StringWriter(), new StringWriter()));
        var err = new StringWriter();
        int exit = GovLedgerApp.Run(["seed", "--dir", dir.Path], new StringWriter(), err);
        Assert.Equal(2, exit);
        Assert.Contains("already exists", err.ToString());
    }
}
