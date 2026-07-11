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

/// <summary>One entity in the source/target data island (CONTRACT-M15.md §6.1): the whole tree
/// lives here; the DOM is built lazily from this array by client-side JS — never pre-rendered
/// per-entity server-side at scale.</summary>
/// <param name="Resolution">CONTRACT-M15.md §1.1: `clean|degraded|gap`, rendered as a hatched/dim
/// node badge for non-`clean` values.</param>
/// <param name="IsTest">CONTRACT-M15.md §1.1: drives the dimmed-by-default "hide tests" rendering.</param>
internal sealed record AtlasEntityNode(string Id, string Kind, string Name, string SymbolPath, string? ParentId, bool Stale, string Resolution, bool IsTest);

/// <summary>One status bucket's count within a unit's aggregated behavior-claim summary, e.g.
/// {status: "accepted", count: 2} — rendered as its own single-token status badge rather than
/// being joined into one multi-word string (a joined string like "2 accepted, 1 proposed" is not a
/// valid single CSS class token; see AtlasHtmlRenderer.StatusBadge).</summary>
internal sealed record AtlasStatusCount(string Status, int Count);

/// <param name="Provenance">CONTRACT-M15.md §1.3: `candidate|asserted` (+ display-derived
/// `verified`, computed by <see cref="AtlasGenerator"/> when an accepted kp.verification claim
/// covers the correspondence's unit+criterion). Rendered as a badge: candidate = dashed/gray,
/// asserted = solid, verified = green tick.</param>
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
    bool Stale,
    string Provenance);

/// <param name="Depth">CONTRACT-M15.md §1.4: `thin|dossiered`, rendered as a unit badge.</param>
internal sealed record AtlasUnit(
    string Id,
    string Name,
    string Status,
    bool Stale,
    IReadOnlyList<AnchorRef> SourceAnchors,
    IReadOnlyList<AnchorRef> TargetAnchors,
    IReadOnlyList<AtlasStatusCount> BehaviorClaimCounts,
    string BodyHtml,
    string Depth);

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

/// <param name="Independence">CONTRACT-M15.md §1.6: `independently-derived |
/// implementation-coupled | unknown`, caller-attested at `kp verify run` time.</param>
internal sealed record AtlasRun(
    string Unit,
    string Criterion,
    string Verdict,
    int PassCount,
    int FailCount,
    IReadOnlyList<string> Mismatches,
    string RerunCommand,
    string ReportJsonPath,
    string ReportMdPath,
    string Independence);

internal sealed record AtlasFooter(string LedgerPath, string LedgerSha256);

// ---- Atlas v2 additions (CONTRACT-M15.md §6) -------------------------------------------------

/// <summary>One resolved absence classification, embedded in the data island for lazy/paginated
/// drill-down lists under the health strip (CONTRACT-M15.md §1.5/§6.3) — never pre-rendered as a
/// flat unbounded HTML list at scale.</summary>
internal sealed record AtlasAbsenceItem(string SymbolPath, string? Note, bool IsOverride);

internal sealed record AtlasAbsenceLists(
    IReadOnlyList<AtlasAbsenceItem> SourceUnknown,
    IReadOnlyList<AtlasAbsenceItem> SourceNotYetPorted,
    IReadOnlyList<AtlasAbsenceItem> SourceDeliberatelyDropped,
    IReadOnlyList<AtlasAbsenceItem> TargetUnexplained,
    IReadOnlyList<AtlasAbsenceItem> TargetIntentional);

/// <summary>One continuity_candidate row (CONTRACT-M15.md §1.2), resolved to display symbolPaths
/// + kind for the Atlas's small "identity" section.</summary>
internal sealed record AtlasContinuityCandidate(
    string BasisFrom, string BasisTo,
    string FromSymbolPath, string ToSymbolPath, string Kind,
    string Heuristic, string Status);

/// <summary>One rectangle of a build-time-generated overview treemap (CONTRACT-M15.md §6.2):
/// area = non-test entity count of the top-level crate/namespace group, fill = coverage class.
/// Pure SVG, generated deterministically server-side — no client JS layout for level 1.</summary>
internal sealed record AtlasTreemapGroup(
    string Key, int NonTestCount, int TestCount, string Coverage, bool Stale);

/// <param name="Svg">Raw, already-escaped inline &lt;svg&gt; markup for this side's level-1 treemap.</param>
/// <param name="Groups">Non-test groups only (NonTestCount &gt; 0) — the same set that was laid
/// out into the SVG, in the same descending-area order; a group with zero non-test entities
/// renders no rectangle (area = non-test entity count) and must not inflate this list or its
/// <c>Count</c> either (visual-review defect: test-only per-file groups pollute the group count).</param>
/// <param name="TestOnlyGroupCount">Number of first-segment groups excluded from
/// <paramref name="Groups"/> because they contain zero non-test entities (e.g. per-file Rust
/// integration-test crates) — reported honestly in the Overview caption alongside
/// <paramref name="TestTotal"/> rather than silently dropped.</param>
/// <param name="SplitPrefix">The one first-segment (<see cref="AtlasOverviewBuilder.TopLevelKey"/>)
/// key that <see cref="AtlasOverviewBuilder"/> re-keyed by its first TWO segments because it held
/// more than half this side's non-test entities (visual-review defect: a single dominant group
/// collapsing the whole treemap into one rectangle), or null if the adaptive rule did not fire.
/// Threaded through to the JS data island so client-side tree-row tagging and the drill-down
/// breakdown key entities identically to the server-rendered rectangles.</param>
internal sealed record AtlasOverviewSide(
    string Svg,
    IReadOnlyList<AtlasTreemapGroup> Groups,
    int NonTestTotal,
    int TestTotal,
    int TestOnlyGroupCount,
    string? SplitPrefix);

internal sealed record AtlasOverview(AtlasOverviewSide Source, AtlasOverviewSide Target);

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

/// <param name="SourceTreemapSplitPrefix">Mirrors <c>Overview.Source.SplitPrefix</c> — serialized
/// separately (unlike <see cref="Overview"/> itself) because the client-side tree/drill-down JS
/// needs to key entities the same way the server-rendered treemap rectangles did, and
/// <see cref="Overview"/>'s SVG is embedded directly as markup rather than through the data
/// island.</param>
/// <param name="TargetTreemapSplitPrefix">Target-side counterpart of
/// <paramref name="SourceTreemapSplitPrefix"/>.</param>
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
    AtlasFooter Footer,
    AtlasAbsenceLists Absences,
    IReadOnlyList<AtlasContinuityCandidate> ContinuityCandidates,
    string? SourceTreemapSplitPrefix,
    string? TargetTreemapSplitPrefix,
    [property: System.Text.Json.Serialization.JsonIgnore] AtlasOverview Overview);
