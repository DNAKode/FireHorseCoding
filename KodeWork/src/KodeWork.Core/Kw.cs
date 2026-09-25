namespace KodeWork.Core;

/// <summary>KodeWork domain constants. Vocabulary lives here, not in Gneiss.</summary>
public static class Kw
{
    public const string ContextName = "kw-current";
    public const string IdPrefix = "kw:";

    public const string PredTitle = "kw.title";
    public const string PredBody = "kw.body";
    public const string PredKind = "kw.kind";
    public const string PredStatus = "kw.status";
    public const string PredParent = "kw.parent";
    public const string PredOrder = "kw.order";
    public const string PredDue = "kw.due";
    public const string PredTags = "kw.tags";
    public const string PredRefs = "kw.refs";

    public static readonly string[] Predicates =
    [
        PredTitle, PredBody, PredKind, PredStatus, PredParent, PredOrder, PredDue, PredTags, PredRefs,
    ];

    public static readonly string[] Kinds =
    [
        "area", "project", "task", "reminder", "idea", "vpc", "note", "link",
    ];

    public static readonly string[] Statuses =
    [
        "open", "doing", "blocked", "done", "dropped", "running", "paused",
    ];

    public static bool IsItemSubject(string subject) =>
        subject.StartsWith(IdPrefix, StringComparison.Ordinal);
}
