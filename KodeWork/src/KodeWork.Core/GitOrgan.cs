using System.Diagnostics;

namespace KodeWork.Core;

/// <summary>Git as the KodeWork evidence organ. Projection files only; never the live SQLite file.</summary>
public static class GitOrgan
{
    public static void EnsureRepo(string path, string? remoteUrl = null)
    {
        Directory.CreateDirectory(path);
        if (!Directory.Exists(Path.Combine(path, ".git")))
        {
            Run(path, "init", "-b", "main");
            ConfigureIdentity(path);
            if (!File.Exists(Path.Combine(path, "README.md")))
            {
                File.WriteAllText(Path.Combine(path, "README.md"),
                    "# KodeWork data\n\nPrivate projection of a KodeWork board. Keep this repo private. The system code is separate.\n");
            }
            EnsureGitignore(path);
            if (!string.IsNullOrWhiteSpace(remoteUrl))
            {
                Run(path, "remote", "add", "origin", remoteUrl);
            }
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
        ConfigureIdentity(path);
        Run(path, "add", "-A");
        var diff = RunCapture(path, "diff", "--cached", "--quiet");
        if (diff.ExitCode == 0)
        {
            return;
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

    private static void ConfigureIdentity(string path)
    {
        string name = Environment.GetEnvironmentVariable("KODEWORK_GIT_NAME") ?? "KodeWork";
        string email = Environment.GetEnvironmentVariable("KODEWORK_GIT_EMAIL") ?? "kodework@localhost";
        Run(path, "config", "user.name", name);
        Run(path, "config", "user.email", email);
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
