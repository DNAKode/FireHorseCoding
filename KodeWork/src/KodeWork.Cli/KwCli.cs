using KodeWork.Core;

namespace KodeWork.Cli;

public static class KwCli
{
    public static int Run(string[] args)
    {
        if (args.Length == 0 || args[0] is "-h" or "--help")
        {
            Console.WriteLine(Usage);
            return 0;
        }

        string cmd = args[0];
        var rest = args.Skip(1).ToArray();
        try
        {
            return cmd switch
            {
                "init" => Init(rest),
                "tree" => WithBoard(b => { PrintTree(b.Current()); return 0; }),
                "show" => Show(rest),
                "add" => Add(rest),
                "set" => Set(rest),
                "note" => Note(rest),
                "notes" => WithBoard(b =>
                {
                    foreach (var n in b.ListNotes())
                    {
                        Console.WriteLine($"{n.Id}  {n.Actor}  {n.Text}");
                    }
                    return 0;
                }),
                "why" => Why(rest),
                "project" => WithBoard(b =>
                {
                    b.ProjectAndCommit(Flag(rest, "--message") ?? "kw project");
                    Console.WriteLine("projected");
                    return 0;
                }),
                "seed" => WithBoard(b =>
                {
                    if (b.Current().Roots.Count > 0)
                    {
                        Console.WriteLine("board not empty; seed skipped");
                        return 0;
                    }
                    DemoSeed.Apply(b, Actor(rest));
                    b.ProjectAndCommit("kw seed");
                    Console.WriteLine("seeded demo (Acme)");
                    return 0;
                }),
                _ => Fail($"unknown command '{cmd}'"),
            };
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }

    private static int Init(string[] rest)
    {
        string home = Home();
        string? data = Data();
        using var board = KodeWorkBoard.Initialize(home, data);
        if (data is not null)
        {
            GitOrgan.EnsureRepo(data, Environment.GetEnvironmentVariable("KODEWORK_DATA_REMOTE"));
        }
        board.Project();
        Console.WriteLine($"initialized {home}");
        return 0;
    }

    private static int Add(string[] rest)
    {
        string title = Flag(rest, "--title") ?? Positional(rest, 0) ?? throw new ArgumentException("--title is required");
        string kind = Flag(rest, "--kind") ?? "task";
        return WithBoard(b =>
        {
            var item = b.Add(Actor(rest), Flag(rest, "--reason") ?? "kw add", new AddItemRequest(
                Title: title,
                Kind: kind,
                ParentId: Flag(rest, "--parent"),
                Status: Flag(rest, "--status"),
                Body: Flag(rest, "--body"),
                Due: Flag(rest, "--due")));
            b.ProjectAndCommit($"add {item.Title}");
            Console.WriteLine(item.Id);
            return 0;
        });
    }

    private static int Set(string[] rest)
    {
        string id = Positional(rest, 0) ?? throw new ArgumentException("id required");
        return WithBoard(b =>
        {
            b.Set(Actor(rest), Flag(rest, "--reason") ?? "kw set", id, new SetItemRequest(
                Title: Flag(rest, "--title"),
                Kind: Flag(rest, "--kind"),
                Status: Flag(rest, "--status"),
                Body: Flag(rest, "--body"),
                Due: Flag(rest, "--due"),
                ParentId: Flag(rest, "--parent"),
                ClearParent: Has(rest, "--root"),
                Order: Flag(rest, "--order") is string o ? decimal.Parse(o, System.Globalization.CultureInfo.InvariantCulture) : null));
            b.ProjectAndCommit($"set {id}");
            return 0;
        });
    }

    private static int Show(string[] rest)
    {
        string id = Positional(rest, 0) ?? throw new ArgumentException("id required");
        return WithBoard(b =>
        {
            var item = b.Get(id) ?? throw new InvalidOperationException("not found");
            Console.WriteLine($"{item.Id}");
            Console.WriteLine($"{item.Kind}  {item.Status}  {item.Title}");
            if (item.ParentId is not null)
            {
                Console.WriteLine($"parent {item.ParentId}");
            }
            if (item.Due is not null)
            {
                Console.WriteLine($"due {item.Due}");
            }
            if (item.Body is not null)
            {
                Console.WriteLine();
                Console.WriteLine(item.Body);
            }
            return 0;
        });
    }

    private static int Note(string[] rest)
    {
        string text = string.Join(' ', rest.Where(a => !a.StartsWith("--", StringComparison.Ordinal)));
        if (string.IsNullOrWhiteSpace(text))
        {
            return Fail("note text required");
        }
        return WithBoard(b =>
        {
            string id = b.Note(Actor(rest), text);
            Console.WriteLine(id);
            return 0;
        });
    }

    private static int Why(string[] rest)
    {
        string id = Positional(rest, 0) ?? throw new ArgumentException("item id required");
        return WithBoard(b =>
        {
            var item = b.Get(id) ?? throw new InvalidOperationException("not found");
            if (item.TitleAid is null)
            {
                return Fail("no title aid");
            }
            var expl = b.Why(item.TitleAid);
            Console.WriteLine($"{expl.Aid} {expl.Status}");
            if (expl.DefeatedBy is not null)
            {
                Console.WriteLine($"defeated-by {expl.DefeatedBy}");
            }
            foreach (var d in expl.Decisions)
            {
                Console.WriteLine($"decision {d}");
            }
            return 0;
        });
    }

    private static int WithBoard(Func<KodeWorkBoard, int> fn)
    {
        using var board = KodeWorkBoard.Initialize(Home(), Data());
        return fn(board);
    }

    private static void PrintTree(BoardView view)
    {
        void Walk(IReadOnlyList<TreeNode> nodes, int depth)
        {
            foreach (var n in nodes)
            {
                Console.WriteLine($"{new string(' ', depth * 2)}[{n.Item.Status}] {n.Item.Title} ({n.Item.Kind}) {n.Item.Id}");
                Walk(n.Children, depth + 1);
            }
        }
        Walk(view.Roots, 0);
        Console.WriteLine($"# tx {view.Label.DataCut} {view.Label.ResultHash[..12]}");
    }

    private static string Home() =>
        Environment.GetEnvironmentVariable("KODEWORK_HOME")
        ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "var", "kodework");

    private static string? Data() => Environment.GetEnvironmentVariable("KODEWORK_DATA");

    private static string Actor(string[] rest) =>
        Flag(rest, "--actor")
        ?? Environment.GetEnvironmentVariable("KODEWORK_ACTOR")
        ?? "local";

    private static string? Flag(string[] rest, string name)
    {
        for (int i = 0; i < rest.Length - 1; i++)
        {
            if (rest[i] == name)
            {
                return rest[i + 1];
            }
        }
        return null;
    }

    private static bool Has(string[] rest, string name) => rest.Contains(name, StringComparer.Ordinal);

    private static string? Positional(string[] rest, int index)
    {
        var pos = rest.Where(a => !a.StartsWith("--", StringComparison.Ordinal)).ToList();
        // flags consume the following token
        var skip = new HashSet<int>();
        for (int i = 0; i < rest.Length - 1; i++)
        {
            if (rest[i].StartsWith("--", StringComparison.Ordinal) && rest[i] != "--root")
            {
                skip.Add(i + 1);
            }
        }
        var values = rest.Where((a, i) => !a.StartsWith("--", StringComparison.Ordinal) && !skip.Contains(i)).ToList();
        return index < values.Count ? values[index] : null;
    }

    private static int Fail(string msg)
    {
        Console.Error.WriteLine(msg);
        return 1;
    }

    private const string Usage = """
        kw — KodeWork CLI
          kw init
          kw tree
          kw show <id>
          kw add --title T --kind task [--parent id] [--status open] [--body ...]
          kw set <id> [--title T] [--status S] [--parent id] [--root] [--body ...]
          kw note TEXT
          kw notes
          kw why <id>
          kw seed
          kw project [--message M]
        Env: KODEWORK_HOME  KODEWORK_DATA  KODEWORK_ACTOR
        """;
}
