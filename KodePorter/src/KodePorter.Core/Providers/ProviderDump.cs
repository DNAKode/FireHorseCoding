using System.Text.Json.Serialization;

namespace KodePorter.Core.Providers;

/// <summary>
/// The shared provider dump JSON format (fixtures/slice-zero/CONTRACT.md §6), produced by
/// `tools/rust-map-dump` and consumed by <see cref="RustSynProvider"/>. Also used as the
/// internal pre-entity shape built by <see cref="CSharpRoslynProvider"/> before ids are
/// resolved, so both providers share one candidate-entity representation.
/// </summary>
/// <param name="Provider">e.g. "rust-syn@0.1.0".</param>
/// <param name="Root">The crate/project root as recorded by the producing tool.</param>
/// <param name="Entities">Entities. Contract requires these sorted by (file, startLine,
/// symbolPath); both providers re-sort defensively regardless of input order.</param>
public sealed record ProviderDump(
    [property: JsonPropertyName("provider")] string Provider,
    [property: JsonPropertyName("root")] string Root,
    [property: JsonPropertyName("entities")] IReadOnlyList<DumpEntity> Entities);

/// <summary>One candidate entity from a provider dump, prior to entity-id / parent-id resolution.</summary>
/// <param name="Kind">One of the closed kind set (fixtures §6).</param>
/// <param name="Name">Simple (unqualified) name.</param>
/// <param name="SymbolPath">The stable identity coordinate (K-D3).</param>
/// <param name="File">Path relative to the basis root, forward slashes.</param>
/// <param name="StartLine">1-based inclusive start line.</param>
/// <param name="EndLine">1-based inclusive end line.</param>
/// <param name="ContentHash">sha256 hex of the declaration span text with \r\n normalized to \n.</param>
/// <param name="ParentSymbolPath">symbolPath of the containing entity, or null at the root.</param>
/// <param name="Resolution">Optional resolution grade (CONTRACT-M15.md §1.1/§5 dump format v1.1):
/// `clean|degraded|gap`. Absent (null) means the caller should default to `clean`.</param>
/// <param name="IsTest">Optional test-ness flag (CONTRACT-M15.md §1.1). Absent (null) means the
/// caller should default to false.</param>
public sealed record DumpEntity(
    [property: JsonPropertyName("kind")] string Kind,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("symbolPath")] string SymbolPath,
    [property: JsonPropertyName("file")] string File,
    [property: JsonPropertyName("startLine")] int StartLine,
    [property: JsonPropertyName("endLine")] int EndLine,
    [property: JsonPropertyName("contentHash")] string ContentHash,
    [property: JsonPropertyName("parentSymbolPath")] string? ParentSymbolPath,
    [property: JsonPropertyName("resolution")] string? Resolution = null,
    [property: JsonPropertyName("isTest")] bool? IsTest = null);
