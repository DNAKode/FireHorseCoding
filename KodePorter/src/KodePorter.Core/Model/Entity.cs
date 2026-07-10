namespace KodePorter.Core.Model;

/// <summary>
/// One declared symbol imported into the map store (CONTRACT.md §2, table `entity`).
/// </summary>
/// <param name="Id">sha256(side|kind|symbol_path), lowercase hex — stable across bases (K-D3).</param>
/// <param name="BasisId">Foreign key into `basis.id`.</param>
/// <param name="Kind">One of the closed kind set in fixtures/slice-zero/CONTRACT.md §6.</param>
/// <param name="Name">Simple (unqualified) name.</param>
/// <param name="SymbolPath">The stable identity coordinate (K-D3).</param>
/// <param name="File">Path relative to the basis root, forward slashes.</param>
/// <param name="StartLine">1-based inclusive start line of the declaration span.</param>
/// <param name="EndLine">1-based inclusive end line of the declaration span.</param>
/// <param name="ContentHash">sha256 of the declaration span text with \r\n normalized to \n.</param>
/// <param name="ParentId">Entity id of the containing entity within this basis, or null at the root.</param>
public sealed record Entity(
    string Id,
    string BasisId,
    string Kind,
    string Name,
    string SymbolPath,
    string File,
    int StartLine,
    int EndLine,
    string ContentHash,
    string? ParentId);
