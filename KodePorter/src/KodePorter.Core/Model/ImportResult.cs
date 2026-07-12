namespace KodePorter.Core.Model;

/// <summary>Summary of a single provider import run.</summary>
/// <param name="EntityCount">Number of entity rows written.</param>
/// <param name="ErrorDiagnosticCount">
/// Count of severity-Error diagnostics observed during import. Never fatal (map-first
/// principle, CONTRACT.md §3) — always 0 for <c>RustSynProvider</c>, which has no
/// diagnostics concept of its own (the dump is produced upstream by a separate tool).
/// </param>
/// <param name="DroppedDuplicateCount">
/// Number of candidate entities <c>EntityResolution.SortAndDeduplicate</c> discarded because
/// their (kind, symbolPath) duplicated one already kept (e.g. a partial type's second
/// declaration — legitimate — or an identity collision the upstream provider should have
/// avoided, PROBE-REPORT.md §7 finding #2). Never fatal (map-first principle), but no longer
/// silent: surfaced here so callers can report it instead of the count simply vanishing.
/// Defaults to 0 so existing callers/tests are unaffected.
/// </param>
public sealed record ImportResult(int EntityCount, int ErrorDiagnosticCount, int DroppedDuplicateCount = 0);
