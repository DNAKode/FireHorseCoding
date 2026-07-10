namespace KodePorter.Core.Domain;

/// <summary>
/// A reference to one symbol at a specific pinned basis (CONTRACT.md §4: used by unit
/// sourceAnchors/targetAnchors and correspondence source/target).
/// </summary>
public sealed record AnchorRef(string SymbolPath, string BasisLabel, string ContentHash);
