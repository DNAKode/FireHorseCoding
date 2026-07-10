namespace KodePorter.Core.Diff;

public enum EntityChangeKind
{
    Added,
    Removed,
    Changed,
}

/// <summary>One entity-level change between two bases of the same side.</summary>
public sealed record EntityDiffEntry(string EntityId, string Kind, string SymbolPath, EntityChangeKind ChangeKind);
