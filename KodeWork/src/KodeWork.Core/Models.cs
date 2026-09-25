using Gneiss.Cell;

namespace KodeWork.Core;

public sealed record WorkItem(
    string Id,
    string Title,
    string Kind,
    string Status,
    string? ParentId,
    decimal Order,
    string? Body,
    string? Due,
    IReadOnlyList<string> Tags,
    IReadOnlyList<string> Refs,
    string? TitleAid,
    string? KindAid,
    string? StatusAid,
    string? ParentAid,
    string? BodyAid);

public sealed record TreeNode(WorkItem Item, IReadOnlyList<TreeNode> Children);

public sealed record BoardView(
    Label Label,
    IReadOnlyList<TreeNode> Roots,
    IReadOnlyList<WorkItem> Orphans,
    IReadOnlyList<NoteInfo> Notes);

public sealed record AddItemRequest(
    string Title,
    string Kind,
    string? ParentId = null,
    string? Status = null,
    string? Body = null,
    string? Due = null,
    IReadOnlyList<string>? Tags = null,
    IReadOnlyList<string>? Refs = null,
    decimal? Order = null);

public sealed record SetItemRequest(
    string? Title = null,
    string? Kind = null,
    string? Status = null,
    string? Body = null,
    string? Due = null,
    string? ParentId = null,
    bool ClearParent = false,
    decimal? Order = null,
    IReadOnlyList<string>? Tags = null,
    IReadOnlyList<string>? Refs = null);
