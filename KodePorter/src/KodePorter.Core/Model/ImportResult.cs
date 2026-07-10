namespace KodePorter.Core.Model;

/// <summary>Summary of a single provider import run.</summary>
/// <param name="EntityCount">Number of entity rows written.</param>
/// <param name="ErrorDiagnosticCount">
/// Count of severity-Error diagnostics observed during import. Never fatal (map-first
/// principle, CONTRACT.md §3) — always 0 for <c>RustSynProvider</c>, which has no
/// diagnostics concept of its own (the dump is produced upstream by a separate tool).
/// </param>
public sealed record ImportResult(int EntityCount, int ErrorDiagnosticCount);
