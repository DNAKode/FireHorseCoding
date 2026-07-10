using KodePorter.Core.Domain;

namespace KodePorter.Core.Atlas;

// The embedded data-island shapes (CONTRACT.md §8: "data embedded as
// <script type=\"application/json\">"). Internal — these are an implementation detail of Atlas
// generation, serialized with a camelCase naming policy at the render boundary; nothing outside
// this module needs to know their C# member names.

internal sealed record AtlasBasis(string Label, string ShortHash, int EntityCount);

internal sealed record AtlasHeader(
    string ProjectName,
    string Direction,
    IReadOnlyList<AtlasBasis> SourceBases,
    IReadOnlyList<AtlasBasis> TargetBases,
    string GeneratedAt,
    string ToolVersion);

internal sealed record AtlasEntityNode(string Id, string Kind, string Name, string SymbolPath, string? ParentId, bool Stale);

/// <summary>One status bucket's count within a unit's aggregated behavior-claim summary, e.g.
/// {status: "accepted", count: 2} — rendered as its own single-token status badge rather than
/// being joined into one multi-word string (a joined string like "2 accepted, 1 proposed" is not a
/// valid single CSS class token; see AtlasHtmlRenderer.StatusBadge).</summary>
internal sealed record AtlasStatusCount(string Status, int Count);

internal sealed record AtlasCorrespondence(
    string Id,
    string Type,
    string? DivergenceKind,
    string Unit,
    string? SourceSymbolPath,
    string? TargetSymbolPath,
    string? SourceEntityId,
    string? TargetEntityId,
    string? Criterion,
    string? Note,
    string Status,
    bool Stale);

internal sealed record AtlasUnit(
    string Id,
    string Name,
    string Status,
    bool Stale,
    IReadOnlyList<AnchorRef> SourceAnchors,
    IReadOnlyList<AnchorRef> TargetAnchors,
    IReadOnlyList<AtlasStatusCount> BehaviorClaimCounts,
    string BodyHtml);

internal sealed record AtlasWhyNode(
    string Aid,
    string Status,
    string? DefeatedBy,
    IReadOnlyList<AtlasWhyNode> Inputs,
    IReadOnlyList<string> RuleVersions,
    IReadOnlyList<string> Decisions);

internal sealed record AtlasClaim(
    string Aid,
    string Predicate,
    string Subject,
    string ValueSummary,
    string ValueJson,
    string Status,
    bool AutoAdmitted,
    bool Stale,
    string? DecidedBy,
    AtlasWhyNode Why);

internal sealed record AtlasRun(
    string Unit,
    string Criterion,
    string Verdict,
    int PassCount,
    int FailCount,
    IReadOnlyList<string> Mismatches,
    string RerunCommand,
    string ReportJsonPath,
    string ReportMdPath);

internal sealed record AtlasFooter(string LedgerPath, string LedgerSha256);

/// <summary>
/// The label of the one <c>Ask("kp-current", ...)</c> snapshot this whole Atlas render was built
/// from (CONTRACT.md §5/§8). // DIVERGENCE: Gneiss's <c>Label.ReceiptId</c> is a fresh random GUID
/// assigned on every Ask() call (a new row in the ledger's receipt table each time) — embedding it
/// literally would make two `kp atlas` runs over an unchanged workspace differ by more than the
/// generated-timestamp, violating CONTRACT.md §8's determinism guarantee, which the design bar
/// explicitly ranks above a literal reading of "receipt id". We substitute the query's
/// deterministic <c>ResultHash</c> (itself a content hash of the exact accepted/defeated/contested
/// result set) as the displayed "receipt" — reproducible from ledger state alone, which is what
/// actually matters for an audit trail baked into a static snapshot file.
/// </summary>
internal sealed record AtlasLabelInfo(string ContextName, string ContextHash, long DataCut, long DefCut, int ConsumedCount, string Receipt);

internal sealed record AtlasData(
    AtlasHeader Header,
    HealthReport Health,
    AtlasLabelInfo Label,
    IReadOnlyList<AtlasEntityNode> SourceTree,
    IReadOnlyList<AtlasEntityNode> TargetTree,
    IReadOnlyList<AtlasCorrespondence> Correspondences,
    IReadOnlyList<AtlasUnit> Units,
    IReadOnlyList<AtlasClaim> Claims,
    IReadOnlyList<AtlasRun> Runs,
    AtlasFooter Footer);
