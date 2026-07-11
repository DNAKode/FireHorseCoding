namespace KodePorter.Core.Model;

/// <summary>
/// One row of the `continuity_candidate` table (CONTRACT-M15.md §1.2): a machine-suggested
/// possible identity continuation of an entity that disappeared from one basis to one that
/// appeared in the next basis of the same side, under the `name-kind` heuristic (same kind,
/// exact name match) — nothing cleverer (K-D3 discipline). Never auto-confirmed; surfaced in
/// the Atlas for human review.
/// </summary>
/// <param name="BasisFrom">The basis id the removed entity belonged to.</param>
/// <param name="BasisTo">The basis id the added entity belongs to.</param>
/// <param name="FromId">Entity id of the removed (disappeared) entity.</param>
/// <param name="ToId">Entity id of the added (appeared) entity.</param>
/// <param name="Heuristic">Which heuristic produced this candidate, e.g. "name-kind".</param>
/// <param name="Status">"candidate" (default) — never auto-confirmed by this v0.</param>
public sealed record ContinuityCandidate(
    string BasisFrom,
    string BasisTo,
    string FromId,
    string ToId,
    string Heuristic,
    string Status = "candidate");
