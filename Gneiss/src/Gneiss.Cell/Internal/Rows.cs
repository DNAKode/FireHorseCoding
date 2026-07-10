namespace Gneiss.Cell.Internal;

/// <summary>In-memory mirror of an `assrt` row.</summary>
internal sealed record AssrtRow(
    string Aid,
    long Tx,
    string Subj,
    string Pred,
    string Val,
    string ValKind,
    string? VFrom,
    string? VTo,
    string Status,
    string? Src,
    string? Meth,
    int? Conf,
    string CKey);

/// <summary>In-memory mirror of a `just` row.</summary>
internal sealed record JustRow(string Aid, string? InputAid, string? RuleVer, string? Role);

/// <summary>In-memory mirror of a `dec` row (joined to its own assrt row for Tx/Status/CKey/Subj).</summary>
internal sealed record DecRow(string Aid, long Tx, string Kind, string? TgtAid, string? TgtCKey, string Status, string CKey, string Subj);

/// <summary>A context declaration after resolving null DataCut/DefCut to a concrete high-water tx id.</summary>
internal sealed record ResolvedContext(
    string Name,
    string DeclAid,
    long DataCut,
    long DefCut,
    string Admit,
    int? AdmitThresholdBp,
    string ConfPolicy,
    string ContextHash);

/// <summary>A predicate declaration, resolved (or defaulted when undeclared).</summary>
internal sealed record ResolvedPredicate(
    string Name,
    string? DeclAid,
    string Comparator,
    decimal? TolAbs,
    decimal? TolRel,
    int StopRung,
    bool InstantSampled,
    IReadOnlyList<string> SourcePrecedence);
