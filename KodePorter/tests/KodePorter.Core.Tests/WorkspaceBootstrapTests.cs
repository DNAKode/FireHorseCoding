using KodePorter.Core.Tests.Support;
using KodePorter.Core.Workspace;

namespace KodePorter.Core.Tests;

/// <summary>Sanity coverage for BUILD step 1 (workspace bootstrap: kp.json + kpmap.db).</summary>
public class WorkspaceBootstrapTests
{
    [Fact]
    public void InitializeCreatesKpJsonAndKpMapDb()
    {
        using var dir = new TempDirectory();
        string workspaceDir = Path.Combine(dir.Path, "workspace");
        var project = new ProjectDescriptor("headscan-port", "rust->csharp", "fixtures/slice-zero/rust", "fixtures/slice-zero/csharp");

        using var workspace = KpWorkspace.Initialize(workspaceDir, project);

        Assert.True(File.Exists(KpWorkspace.KpJsonPath(workspaceDir)));
        Assert.True(File.Exists(KpWorkspace.KpMapDbPath(workspaceDir)));
        Assert.Equal(project, workspace.Project);
    }

    [Fact]
    public void OpenLoadsTheRecordedProjectDescriptorAndReinitializeDoesNotOverwriteIt()
    {
        using var dir = new TempDirectory();
        string workspaceDir = Path.Combine(dir.Path, "workspace");
        var project = new ProjectDescriptor("headscan-port", "rust->csharp", "src-root", "target-root");

        using (var first = KpWorkspace.Initialize(workspaceDir, project))
        {
            Assert.Equal(project, first.Project);
        }

        // Re-initializing with a different descriptor must not clobber the recorded one.
        var different = new ProjectDescriptor("different-name", "rust->csharp", "other-src", "other-target");
        using var reInitialized = KpWorkspace.Initialize(workspaceDir, different);
        Assert.Equal(project, reInitialized.Project);

        using var opened = KpWorkspace.Open(workspaceDir);
        Assert.Equal(project, opened.Project);
    }

    [Fact]
    public void OpenThrowsWhenNoKpJsonExists()
    {
        using var dir = new TempDirectory();
        string workspaceDir = Path.Combine(dir.Path, "missing-workspace");

        Assert.Throws<FileNotFoundException>(() => KpWorkspace.Open(workspaceDir));
    }
}
