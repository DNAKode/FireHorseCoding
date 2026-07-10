using KodePorter.Cli;
using KodePorter.Core.Tests.Support;

namespace KodePorter.Core.Tests;

/// <summary>
/// CLI smoke test (BUILD step 4): invokes <see cref="KpCliApp.Run"/> — the same command routing
/// Program.cs's Main calls — in-process against a temp workspace, for init/status plus the
/// exit-code contract (CONTRACT.md §9: 0 ok, 1 usage, 2 domain error).
/// </summary>
public class CliSmokeTests
{
    [Fact]
    public void InitThenStatusOnATempWorkspaceSucceedsAndReportsZeroedHealth()
    {
        using var dir = new TempDirectory();
        string workspaceDir = Path.Combine(dir.Path, "workspace");
        string sourceRoot = Path.Combine(dir.Path, "src");
        string targetRoot = Path.Combine(dir.Path, "target");
        Directory.CreateDirectory(sourceRoot);
        Directory.CreateDirectory(targetRoot);

        var initOut = new StringWriter();
        var initErr = new StringWriter();
        int initExit = KpCliApp.Run(
            ["init", "--workspace", workspaceDir, "--name", "headscan-port", "--source-root", sourceRoot, "--target-root", targetRoot],
            initOut, initErr);

        Assert.Equal(0, initExit);
        Assert.Equal("", initErr.ToString());
        Assert.Contains("Initialized kp workspace", initOut.ToString());
        Assert.True(File.Exists(Path.Combine(workspaceDir, "kp.json")));
        Assert.True(File.Exists(Path.Combine(workspaceDir, "kpmap.db")));
        Assert.True(File.Exists(Path.Combine(workspaceDir, "gneiss.db")));
        Assert.True(File.Exists(Path.Combine(workspaceDir, ".kodeporter", "project.yaml")));
        Assert.True(File.Exists(Path.Combine(workspaceDir, ".kodeporter", "policy.yaml")));

        var statusOut = new StringWriter();
        var statusErr = new StringWriter();
        int statusExit = KpCliApp.Run(["status", "--workspace", workspaceDir], statusOut, statusErr);

        Assert.Equal(0, statusExit);
        Assert.Equal("", statusErr.ToString());
        string status = statusOut.ToString();
        Assert.Contains("mapped: 0", status);
        Assert.Contains("corresponded: 0", status);
        Assert.Contains("implemented: 0", status);
        Assert.Contains("verified: 0", status);
        Assert.Contains("stale: 0", status);
        Assert.Contains("unknown: 0", status);
    }

    [Fact]
    public void UnknownCommandReturnsExitCodeOneWithUsageHint()
    {
        var err = new StringWriter();
        int exit = KpCliApp.Run(["frobnicate"], new StringWriter(), err);
        Assert.Equal(1, exit);
        Assert.Contains("Unknown command", err.ToString());
    }

    [Fact]
    public void MissingRequiredFlagReturnsExitCodeOne()
    {
        var err = new StringWriter();
        int exit = KpCliApp.Run(["init", "--workspace", "somewhere"], new StringWriter(), err);
        Assert.Equal(1, exit);
        Assert.Contains("Missing required --name", err.ToString());
    }

    [Fact]
    public void OpeningANonexistentWorkspaceReturnsExitCodeTwo()
    {
        using var dir = new TempDirectory();
        var err = new StringWriter();
        int exit = KpCliApp.Run(["status", "--workspace", Path.Combine(dir.Path, "missing")], new StringWriter(), err);
        Assert.Equal(2, exit);
        Assert.NotEqual("", err.ToString());
    }

    [Fact]
    public void HelpFlagPrintsUsageAndReturnsZero()
    {
        var outw = new StringWriter();
        int exit = KpCliApp.Run(["-h"], outw, new StringWriter());
        Assert.Equal(0, exit);
        Assert.Contains("kp init", outw.ToString());
    }
}
