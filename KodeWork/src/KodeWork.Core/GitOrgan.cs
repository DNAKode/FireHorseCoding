using System.Diagnostics;

namespace KodeWork.Core;

/// <summary>Git as the KodeWork evidence organ. Projection files only; never the live SQLite file.</summary>
public static class GitOrgan
{
    public static void EnsureRepo(string path, string remoteUrl)
    {
        Directory.CreateDirectory(path);
        if (!Directory.Exists(Path.Combine(path, ".git")))
        {
            Run(path, "init", "-b", "main");
            Run(path, "config", "user.name", "Koderbot");
            Run(path, "config", "user.email", "koderbot@dnakode.com");
            File.WriteAllText(Path.Combine(path, "README.md"),
                "# KodeWorkData\n\nPrivate text projection of the KodeWork board. The live ledger is SQLite beside the host; this repo is the readable organ.\n");
            EnsureGitignore(path);
            Run(path, "remote", "add", "origin", remoteUrl);
        }
    }

    public static void EnsureGitignore(string path)
    {
        string gi = Path.Combine(path, ".gitignore");
        const string body = "gneiss.db\n*.db\n*.db-wal\n*.db-shm\n*.lock\n";
        if (!File.Exists(gi) || File.ReadAllText(gi) != body)
        {
            File.WriteAllText(gi, body);
        }
    }

    public static void CommitAll(string path, string message)
    {
        if (!Directory.Exists(Path.Combine(path, ".git")))
        {
            return;
        }
        Run(path, "config", "user.name", "Koderbot");
        Run(path, "config", "user.email", "koderbot@dnakode.com");
        Run(path, "add", "-A");
        var diff = RunCapture(path, "diff", "--cached", "--quiet");
        if (diff.ExitCode == 0)
        {
            return; // nothing staged
        }
        Run(path, "commit", "-m", message);
    }

    public static int TryPush(string path)
    {
        if (!Directory.Exists(Path.Combine(path, ".git")))
        {
            return 0;
        }
        return RunCapture(path, "push", "-u", "origin", "main").ExitCode;
    }

    private static void Run(string cwd, params string[] args)
    {
        var r = RunCapture(cwd, args);
        if (r.ExitCode != 0)
        {
            throw new InvalidOperationException($"git {string.Join(' ', args)} failed ({r.ExitCode}): {r.StdErr}");
        }
    }

    private static (int ExitCode, string StdErr) RunCapture(string cwd, params string[] args)
    {
        var psi = new ProcessStartInfo("git")
        {
            WorkingDirectory = cwd,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
        };
        foreach (var a in args)
        {
            psi.ArgumentList.Add(a);
        }
        using var p = Process.Start(psi) ?? throw new InvalidOperationException("failed to start git");
        string err = p.StandardError.ReadToEnd();
        p.WaitForExit();
        return (p.ExitCode, err);
    }
}
