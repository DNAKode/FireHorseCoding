using System.Text.Json.Serialization;

namespace KodePorter.Core.Gneiss;

/// <summary>
/// The kp.evidence.anchor claim payload (CONTRACT.md §5). Fixed property declaration order is
/// relied on for deterministic JSON serialization (System.Text.Json emits POCO properties in
/// declaration order).
/// </summary>
public sealed record AnchorEvidenceValue(
    [property: JsonPropertyName("symbolPath")] string SymbolPath,
    [property: JsonPropertyName("basisLabel")] string BasisLabel,
    [property: JsonPropertyName("contentHash")] string ContentHash,
    [property: JsonPropertyName("file")] string File,
    [property: JsonPropertyName("startLine")] int StartLine,
    [property: JsonPropertyName("endLine")] int EndLine);

/// <summary>The kp.correspondence claim payload (CONTRACT.md §5).</summary>
public sealed record CorrespondenceClaimValue(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("source")] AnchorRefValue? Source,
    [property: JsonPropertyName("target")] AnchorRefValue? Target,
    [property: JsonPropertyName("unit")] string Unit,
    [property: JsonPropertyName("criterion")] string? Criterion);

/// <summary>An anchor reference nested inside a claim JSON payload.</summary>
public sealed record AnchorRefValue(
    [property: JsonPropertyName("symbolPath")] string SymbolPath,
    [property: JsonPropertyName("basisLabel")] string BasisLabel,
    [property: JsonPropertyName("contentHash")] string ContentHash);

/// <summary>The kp.verification claim payload (CONTRACT.md §5/§6).</summary>
public sealed record VerificationClaimValue(
    [property: JsonPropertyName("verdict")] string Verdict, // pass | fail
    [property: JsonPropertyName("corpusHash")] string CorpusHash,
    [property: JsonPropertyName("sourceBasis")] string SourceBasis,
    [property: JsonPropertyName("targetBasis")] string TargetBasis,
    [property: JsonPropertyName("cases")] int Cases,
    [property: JsonPropertyName("mismatches")] IReadOnlyList<string> Mismatches,
    [property: JsonPropertyName("reportPath")] string ReportPath);

/// <summary>The kp.stale fact payload (CONTRACT.md §7).</summary>
public sealed record StaleValue(
    [property: JsonPropertyName("basisLabel")] string BasisLabel,
    [property: JsonPropertyName("cause")] string Cause,
    [property: JsonPropertyName("changedSymbols")] IReadOnlyList<string> ChangedSymbols);
