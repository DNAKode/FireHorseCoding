namespace GovernanceLedger.Lens;

/// <summary>One transaction timeline row: wall, actor, reason (CONTRACT-M15.md section 7).</summary>
internal sealed record LensTxRow(long TxId, string Wall, string Actor, string Reason, string? Kind);

/// <summary>Who/when/why a decision card was superseded, and what it points at.</summary>
internal sealed record LensSupersession(string DecisionAid, string Actor, string Reason, string Wall, long TxId);

/// <summary>One expandable trail line under a decision card, from <c>GneissLedger.Why</c>.</summary>
internal sealed record LensTrailEntry(string DecisionAid, string Kind, string Actor, string Reason, string Wall);

/// <summary>One `gov.decision` decision card.</summary>
internal sealed record LensCard(
    string Id,
    string Subject,
    string ValueText,
    string Actor,
    string Method,
    string? Source,
    string Reason,
    string ValidFrom,
    string Wall,
    string Status, // "accepted" | "defeated" | "contested" | "proposed-unadmitted" | "not-visible"
    LensSupersession? SupersededBy,
    IReadOnlyList<LensTrailEntry> Trail);

internal sealed record LensModel(
    IReadOnlyList<LensTxRow> Timeline,
    IReadOnlyList<LensCard> Cards,
    string ExportSha256);
