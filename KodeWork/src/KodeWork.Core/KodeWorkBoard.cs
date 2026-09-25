using System.Globalization;
using Gneiss.Cell;

namespace KodeWork.Core;

/// <summary>
/// KodeWork domain over a Gneiss.Cell ledger. Single-writer by named mutex + SQLite.
/// Current tree is always <c>Ask("kw-current")</c>, never a mutable document.
/// </summary>
public sealed class KodeWorkBoard : IDisposable
{
    public const string LedgerFileName = "gneiss.db";

    private readonly GneissLedger _ledger;
    private readonly Mutex _mutex;

    public string HomeDir { get; }
    public string LedgerPath { get; }
    public string? DataRepoPath { get; }

    private KodeWorkBoard(string homeDir, GneissLedger ledger, string? dataRepoPath)
    {
        HomeDir = homeDir;
        LedgerPath = Path.Combine(homeDir, LedgerFileName);
        DataRepoPath = dataRepoPath;
        _ledger = ledger;
        _mutex = new Mutex(false, MutexName(homeDir));
    }

    public static KodeWorkBoard Initialize(string homeDir, string? dataRepoPath = null)
    {
        Directory.CreateDirectory(homeDir);
        string path = Path.Combine(homeDir, LedgerFileName);
        var ledger = File.Exists(path) ? GneissLedger.Open(path) : GneissLedger.Create(path);
        var board = new KodeWorkBoard(homeDir, ledger, dataRepoPath);
        if (board.HighWater == 0)
        {
            board.DeclareDomain();
        }
        return board;
    }

    public static KodeWorkBoard Open(string homeDir, string? dataRepoPath = null)
    {
        string path = Path.Combine(homeDir, LedgerFileName);
        return new KodeWorkBoard(homeDir, GneissLedger.Open(path), dataRepoPath);
    }

    public long HighWater => _ledger.HighWater;

    public WorkItem Add(string actor, string reason, AddItemRequest req)
    {
        ValidateKind(req.Kind);
        string status = req.Status ?? DefaultStatus(req.Kind);
        ValidateStatus(status);
        if (string.IsNullOrWhiteSpace(req.Title))
        {
            throw new ArgumentException("Title is required.", nameof(req));
        }

        return WithLock(() =>
        {
            string? parent = string.IsNullOrWhiteSpace(req.ParentId) ? null : ItemId.Parse(req.ParentId).Value;
            if (parent is not null)
            {
                EnsureExists(parent);
            }

            var id = ItemId.New();
            decimal order = req.Order ?? NextOrder(parent);
            var items = new List<IAppendItem>
            {
                new NewAssertion(id.Value, Kw.PredTitle, GValue.Text(req.Title.Trim())),
                new NewAssertion(id.Value, Kw.PredKind, GValue.Text(req.Kind)),
                new NewAssertion(id.Value, Kw.PredStatus, GValue.Text(status)),
                new NewAssertion(id.Value, Kw.PredOrder, GValue.Number(order)),
            };
            if (parent is not null)
            {
                items.Add(new NewAssertion(id.Value, Kw.PredParent, GValue.Entity(parent)));
            }
            if (!string.IsNullOrEmpty(req.Body))
            {
                items.Add(new NewAssertion(id.Value, Kw.PredBody, GValue.Text(req.Body)));
            }
            if (!string.IsNullOrEmpty(req.Due))
            {
                items.Add(new NewAssertion(id.Value, Kw.PredDue, GValue.Text(req.Due)));
            }
            if (req.Tags is { Count: > 0 })
            {
                items.Add(new NewAssertion(id.Value, Kw.PredTags, GValue.Json(JsonLite.Array(req.Tags))));
            }
            if (req.Refs is { Count: > 0 })
            {
                items.Add(new NewAssertion(id.Value, Kw.PredRefs, GValue.Json(JsonLite.Array(req.Refs))));
            }

            _ledger.Append(Env(actor, reason), items);
            return GetRequired(id.Value);
        });
    }

    public WorkItem Set(string actor, string reason, string id, SetItemRequest req)
    {
        string subject = ItemId.Parse(id).Value;
        return WithLock(() =>
        {
            EnsureExists(subject);
            if (req.Kind is not null)
            {
                ValidateKind(req.Kind);
            }
            if (req.Status is not null)
            {
                ValidateStatus(req.Status);
            }

            var items = new List<IAppendItem>();
            if (req.Title is not null)
            {
                items.Add(new NewAssertion(subject, Kw.PredTitle, GValue.Text(req.Title.Trim())));
            }
            if (req.Kind is not null)
            {
                items.Add(new NewAssertion(subject, Kw.PredKind, GValue.Text(req.Kind)));
            }
            if (req.Status is not null)
            {
                items.Add(new NewAssertion(subject, Kw.PredStatus, GValue.Text(req.Status)));
            }
            if (req.Body is not null)
            {
                items.Add(new NewAssertion(subject, Kw.PredBody, GValue.Text(req.Body)));
            }
            if (req.Due is not null)
            {
                items.Add(new NewAssertion(subject, Kw.PredDue, GValue.Text(req.Due)));
            }
            if (req.Order is not null)
            {
                items.Add(new NewAssertion(subject, Kw.PredOrder, GValue.Number(req.Order.Value)));
            }
            if (req.Tags is not null)
            {
                items.Add(new NewAssertion(subject, Kw.PredTags, GValue.Json(JsonLite.Array(req.Tags))));
            }
            if (req.Refs is not null)
            {
                items.Add(new NewAssertion(subject, Kw.PredRefs, GValue.Json(JsonLite.Array(req.Refs))));
            }
            if (req.ClearParent)
            {
                RetractPredicateLocked(actor, reason, subject, Kw.PredParent);
            }
            else if (req.ParentId is not null)
            {
                string parent = ItemId.Parse(req.ParentId).Value;
                if (parent == subject)
                {
                    throw new InvalidOperationException("An item cannot be its own parent.");
                }
                EnsureExists(parent);
                if (WouldCycle(subject, parent))
                {
                    throw new InvalidOperationException($"Moving {subject} under {parent} would cycle.");
                }
                items.Add(new NewAssertion(subject, Kw.PredParent, GValue.Entity(parent)));
            }

            if (items.Count == 0 && !req.ClearParent)
            {
                return GetRequired(subject);
            }
            if (items.Count > 0)
            {
                _ledger.Append(Env(actor, reason), items);
            }
            return GetRequired(subject);
        });
    }

    public void RetractPredicate(string actor, string reason, string id, string predicate)
    {
        string subject = ItemId.Parse(id).Value;
        WithLock(() =>
        {
            EnsureExists(subject);
            RetractPredicateLocked(actor, reason, subject, predicate);
        });
    }

    public string Note(string actor, string text) =>
        WithLock(() => _ledger.Note(Env(actor, "note"), text));

    public IReadOnlyList<NoteInfo> ListNotes() => WithLock(() => _ledger.ListNotes());

    public BoardView Current() => WithLock(CurrentLocked);

    public WorkItem? Get(string id)
    {
        string subject = ItemId.Parse(id).Value;
        return WithLock(() => TryGet(subject));
    }

    public Explanation Why(string aid) => WithLock(() => _ledger.Why(Kw.ContextName, aid));

    public IReadOnlyList<string> ExportLedgerJsonl() => WithLock(() => _ledger.ExportLedgerJsonl());

    public void Project()
    {
        WithLock(() =>
        {
            var board = CurrentLocked();
            WriteProjection(board);
        });
    }

    public void ProjectAndCommit(string message)
    {
        WithLock(() =>
        {
            var board = CurrentLocked();
            WriteProjection(board);
            if (DataRepoPath is not null)
            {
                GitOrgan.CommitAll(DataRepoPath, message);
            }
        });
    }

    public void Dispose()
    {
        _ledger.Dispose();
        _mutex.Dispose();
    }

    private void DeclareDomain()
    {
        var env = Env("kodework", "init: declare kw.* predicates and kw-current");
        foreach (string pred in Kw.Predicates)
        {
            _ledger.DeclarePredicate(env, new PredicateDecl(pred, Comparator: "exact", StopRung: 6));
        }
        _ledger.DeclareContext(env, new ContextDecl(Kw.ContextName, Admit: "decided-only"));
    }

    private BoardView CurrentLocked()
    {
        var view = _ledger.Ask(Kw.ContextName, new Question());
        var byId = FoldItems(view.Accepted);
        var children = new Dictionary<string, List<WorkItem>>(StringComparer.Ordinal);
        var roots = new List<WorkItem>();
        var orphans = new List<WorkItem>();
        var known = new HashSet<string>(byId.Keys, StringComparer.Ordinal);

        foreach (var item in byId.Values)
        {
            if (item.ParentId is null)
            {
                roots.Add(item);
                continue;
            }
            if (!known.Contains(item.ParentId))
            {
                orphans.Add(item);
                continue;
            }
            if (!children.TryGetValue(item.ParentId, out var list))
            {
                list = [];
                children[item.ParentId] = list;
            }
            list.Add(item);
        }

        IReadOnlyList<TreeNode> Build(IEnumerable<WorkItem> items) =>
            items
                .OrderBy(i => i.Order)
                .ThenBy(i => i.Id, StringComparer.Ordinal)
                .Select(i => new TreeNode(i, children.TryGetValue(i.Id, out var ch) ? Build(ch) : []))
                .ToList();

        return new BoardView(view.Label, Build(roots), orphans.OrderBy(o => o.Id, StringComparer.Ordinal).ToList(), _ledger.ListNotes());
    }

    private Dictionary<string, WorkItem> FoldItems(IReadOnlyList<BeliefEntry> accepted)
    {
        var bag = new Dictionary<string, Dictionary<string, BeliefEntry>>(StringComparer.Ordinal);
        foreach (var e in accepted)
        {
            if (!Kw.IsItemSubject(e.Subject) || !e.Predicate.StartsWith("kw.", StringComparison.Ordinal))
            {
                continue;
            }
            if (!bag.TryGetValue(e.Subject, out var props))
            {
                props = new Dictionary<string, BeliefEntry>(StringComparer.Ordinal);
                bag[e.Subject] = props;
            }
            props[e.Predicate] = e;
        }

        var result = new Dictionary<string, WorkItem>(StringComparer.Ordinal);
        foreach (var (id, props) in bag)
        {
            if (!props.TryGetValue(Kw.PredTitle, out var title))
            {
                continue;
            }
            string kind = props.TryGetValue(Kw.PredKind, out var k) ? k.Value.Canonical : "note";
            string status = props.TryGetValue(Kw.PredStatus, out var st) ? st.Value.Canonical : "open";
            string? parent = props.TryGetValue(Kw.PredParent, out var p) ? p.Value.Canonical : null;
            decimal order = 0;
            if (props.TryGetValue(Kw.PredOrder, out var o) &&
                decimal.TryParse(o.Value.Canonical, NumberStyles.Number, CultureInfo.InvariantCulture, out var od))
            {
                order = od;
            }
            string? body = props.TryGetValue(Kw.PredBody, out var b) ? b.Value.Canonical : null;
            string? due = props.TryGetValue(Kw.PredDue, out var d) ? d.Value.Canonical : null;
            var tags = props.TryGetValue(Kw.PredTags, out var tg) ? JsonLite.ParseArray(tg.Value.Canonical) : [];
            var refs = props.TryGetValue(Kw.PredRefs, out var rf) ? JsonLite.ParseArray(rf.Value.Canonical) : [];
            result[id] = new WorkItem(
                id, title.Value.Canonical, kind, status, parent, order, body, due, tags, refs,
                title.Aid,
                k?.Aid,
                st?.Aid,
                p?.Aid,
                b?.Aid);
        }
        return result;
    }

    private WorkItem GetRequired(string id) =>
        TryGet(id) ?? throw new InvalidOperationException($"Item '{id}' is not in the current view.");

    private WorkItem? TryGet(string id)
    {
        var board = CurrentLocked();
        return Find(board.Roots, id) ?? board.Orphans.FirstOrDefault(o => o.Id == id);
    }

    private static WorkItem? Find(IReadOnlyList<TreeNode> nodes, string id)
    {
        foreach (var n in nodes)
        {
            if (n.Item.Id == id)
            {
                return n.Item;
            }
            var child = Find(n.Children, id);
            if (child is not null)
            {
                return child;
            }
        }
        return null;
    }

    private void EnsureExists(string id)
    {
        if (TryGet(id) is null)
        {
            throw new InvalidOperationException($"Unknown item '{id}'.");
        }
    }

    private bool WouldCycle(string id, string newParent)
    {
        string? cursor = newParent;
        var seen = new HashSet<string>(StringComparer.Ordinal) { id };
        while (cursor is not null)
        {
            if (!seen.Add(cursor))
            {
                return true;
            }
            cursor = TryGet(cursor)?.ParentId;
        }
        return false;
    }

    private decimal NextOrder(string? parentId)
    {
        var board = CurrentLocked();
        IEnumerable<WorkItem> siblings = parentId is null
            ? board.Roots.Select(r => r.Item)
            : Siblings(board.Roots, parentId) ?? [];
        decimal max = 0;
        foreach (var s in siblings)
        {
            if (s.Order > max)
            {
                max = s.Order;
            }
        }
        return max + 10;
    }

    private static IEnumerable<WorkItem>? Siblings(IReadOnlyList<TreeNode> nodes, string parentId)
    {
        foreach (var n in nodes)
        {
            if (n.Item.Id == parentId)
            {
                return n.Children.Select(c => c.Item);
            }
            var found = Siblings(n.Children, parentId);
            if (found is not null)
            {
                return found;
            }
        }
        return null;
    }

    private void RetractPredicateLocked(string actor, string reason, string subject, string predicate)
    {
        var view = _ledger.Ask(Kw.ContextName, new Question(Subject: subject, Predicate: predicate));
        if (view.Accepted.Count == 0)
        {
            return;
        }
        _ledger.Append(Env(actor, reason),
            new IAppendItem[] { new NewDecision(DecisionKind.Retracts, TargetClaimKey: view.Accepted[0].ClaimKey) });
    }

    private void WriteProjection(BoardView board)
    {
        string dest = DataRepoPath ?? Path.Combine(HomeDir, "projection");
        Directory.CreateDirectory(dest);
        string itemsDir = Path.Combine(dest, "items");
        Directory.CreateDirectory(itemsDir);
        foreach (var leftover in Directory.EnumerateFiles(itemsDir, "*.md"))
        {
            File.Delete(leftover);
        }

        void Walk(IReadOnlyList<TreeNode> nodes)
        {
            foreach (var n in nodes)
            {
                string stem = new ItemId(n.Item.Id).FileStem;
                File.WriteAllText(Path.Combine(itemsDir, stem + ".md"), MarkdownProjector.ItemFile(n.Item));
                Walk(n.Children);
            }
        }
        Walk(board.Roots);
        foreach (var o in board.Orphans)
        {
            string stem = new ItemId(o.Id).FileStem;
            File.WriteAllText(Path.Combine(itemsDir, stem + ".md"), MarkdownProjector.ItemFile(o));
        }

        File.WriteAllText(Path.Combine(dest, "tree.md"), MarkdownProjector.TreeFile(board));
        File.WriteAllText(Path.Combine(dest, "snapshot.html"), MarkdownProjector.SnapshotHtml(board));

        string ledgerDir = Path.Combine(dest, "ledger");
        Directory.CreateDirectory(ledgerDir);
        File.WriteAllLines(Path.Combine(ledgerDir, "export.jsonl"), _ledger.ExportLedgerJsonl());
        File.WriteAllText(Path.Combine(ledgerDir, "HIGHWATER"), board.Label.DataCut.ToString(CultureInfo.InvariantCulture) + "\n");
        GitOrgan.EnsureGitignore(dest);
    }

    private T WithLock<T>(Func<T> fn)
    {
        _mutex.WaitOne();
        try
        {
            return fn();
        }
        finally
        {
            _mutex.ReleaseMutex();
        }
    }

    private void WithLock(Action fn) => WithLock(() =>
    {
        fn();
        return 0;
    });

    private static TxEnvelope Env(string actor, string reason) =>
        new(actor, reason, DateTimeOffset.UtcNow);

    private static void ValidateKind(string kind)
    {
        if (!Kw.Kinds.Contains(kind, StringComparer.Ordinal))
        {
            throw new ArgumentException($"Unknown kind '{kind}'.", nameof(kind));
        }
    }

    private static void ValidateStatus(string status)
    {
        if (!Kw.Statuses.Contains(status, StringComparer.Ordinal))
        {
            throw new ArgumentException($"Unknown status '{status}'.", nameof(status));
        }
    }

    private static string DefaultStatus(string kind) => kind switch
    {
        "vpc" => "running",
        "idea" => "open",
        "note" => "open",
        _ => "open",
    };

    private static string MutexName(string homeDir)
    {
        string norm = Path.GetFullPath(homeDir).Replace('/', '_').Replace('\\', '_');
        if (norm.Length > 80)
        {
            norm = Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(norm)))[..16];
        }
        return @"Global\kodework-" + norm;
    }
}
