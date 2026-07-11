namespace GovernanceLedger;

/// <summary>One `gov.decision` content entry: a steward decision or delegate proposal, recorded
/// as a Gneiss fact (CONTRACT-M15.md section 7). <see cref="ValueText"/> is a short title;
/// <see cref="Reason"/> quotes AMENDMENTS.md (or, for entries that predate it, the founding
/// commit message) faithfully — never invented. <see cref="CommitHash"/> is the evidence note.
/// Dates are fixed literals (never DateTimeOffset.UtcNow): <see cref="ValidFrom"/> is the
/// AMENDMENTS.md section date; <see cref="Wall"/> is the commit's own authored timestamp,
/// reinterpreted as the ledger's nominal UTC clock so the two stay date-consistent.</summary>
internal sealed record SeedDecision(
    string Id,
    string Actor,
    string Method,
    string CommitHash,
    string ValueText,
    string Reason,
    DateTimeOffset Wall,
    DateTimeOffset ValidFrom);

/// <summary>The supersession entry: not a content assertion but a real Gneiss `supersedes`
/// decision targeting an earlier entry's assertion aid — the machinery demonstrating
/// correction-without-erasure on our own governance (CONTRACT-M15.md section 7).</summary>
internal sealed record SeedSupersession(
    string Id,
    string Actor,
    string CommitHash,
    string Reason,
    DateTimeOffset Wall,
    string TargetId);

/// <summary>
/// The FireHorseCoding governance ledger's seed history, hardcoded per CONTRACT-M15.md section 7's
/// exact content list. Actor is `govert` for steward decisions, `fable` for delegate proposals and
/// findings — drawn from AMENDMENTS.md's own ceremony language ("By steward direction…",
/// "Steward Q&A…", "RATIFIED by steward…" vs. the delegate-authored review/build commits). Order
/// here IS append order (and therefore tx order): <see cref="Decisions"/> hold indices 0-6 seed
/// before <see cref="Supersession"/>, index 7 seeds after — see SeedRunner.
/// </summary>
internal static class SeedTable
{
    /// <summary>Wall clock for the bootstrap transactions (declare `gov.decision` predicate + the
    /// `gov-current` context) — a fixed instant just before the first recorded decision.</summary>
    public static readonly DateTimeOffset BootstrapWall = WallClock.Utc(2026, 7, 10, 18, 58, 0);

    public static readonly IReadOnlyList<SeedDecision> Decisions =
    [
        new SeedDecision(
            Id: "charters-established",
            Actor: "govert",
            Method: "steward-decision",
            CommitHash: "af45f45",
            ValueText: "Gneiss and KodePorter charters established.",
            Reason: """
                Gneiss and KodePorter charters established, founding the formative-phase
                constitution for both projects (commit af45f45: "establish Gneiss and
                KodePorter charters").
                """,
            Wall: WallClock.Utc(2026, 7, 10, 18, 58, 17),
            ValidFrom: WallClock.Utc(2026, 7, 10)),

        new SeedDecision(
            Id: "review+roadmaps",
            Actor: "fable",
            Method: "delegate-proposal",
            CommitHash: "57b3716",
            ValueText: "Charter review (19 proposed amendments) and coordinated v0 roadmaps landed.",
            Reason: """
                "Critical review of both charters (adversarial passes; 19 proposed amendments
                G-A1..9 / K-A1..10) plus coordinated v0 roadmaps sharing one milestone spine (M0
                Alignment .. M4 Reality Test) and the joint Slice Zero fixture." ... "Incorporates
                steward review comments of 2026-07-10." (commit 57b3716)
                """,
            Wall: WallClock.Utc(2026, 7, 10, 20, 25, 9),
            ValidFrom: WallClock.Utc(2026, 7, 10)),

        new SeedDecision(
            Id: "amendments-adopted+m0-decision-lock",
            Actor: "govert",
            Method: "steward-decision",
            CommitHash: "3d7eadd",
            ValueText: "Amendments G-A1..9/K-A1..10 adopted; M0 substrate, scope, and autonomy posture locked.",
            Reason: """
                "By steward direction ("proceed with the roadmap"), amendments G-A1…G-A9 and
                K-A1…K-A10 as specified in CHARTER-REVIEW.md §7 are adopted for the formative
                phase." M0 decision lock — substrate (steward): "modern C# on .NET 10 (net10.0),
                SQLite databases, CLI + static HTML tooling and visualization." Cost posture
                (steward): "implementation by cost-optimized agents (Sonnet-class) against
                written contracts; steward-tier effort reserved for contracts, integration gates,
                and review." (AMENDMENTS.md 2026-07-10; commit 3d7eadd)
                """,
            Wall: WallClock.Utc(2026, 7, 10, 21, 40, 42),
            ValidFrom: WallClock.Utc(2026, 7, 10)),

        new SeedDecision(
            Id: "m1-increment-landed",
            Actor: "fable",
            Method: "delegate-proposal",
            CommitHash: "5c3e96d",
            ValueText: "M1 increment landed: the Port Atlas runs end to end on Slice Zero.",
            Reason: """
                "The full KodePorter-on-Gneiss story runs end to end on Slice Zero
                (showcase/m1/run-m1.ps1): pin + map both sides (102 Rust + 67 C# entities)... a
                28/28 differential run Rust-vs-C# under io-agreement-v1, the Atlas rendered... 77
                tests green (31 Gneiss + 46 KodePorter) + 47 Rust-side." (commit 5c3e96d)
                """,
            Wall: WallClock.Utc(2026, 7, 11, 0, 16, 56),
            ValidFrom: WallClock.Utc(2026, 7, 11)),

        new SeedDecision(
            Id: "behavior-subject-correction",
            Actor: "fable",
            Method: "delegate-proposal",
            CommitHash: "5c3e96d",
            ValueText: "Behavior claims corrected to per-claim subjects (behavior:<unit>:<id>).",
            Reason: """
                "Model corrections found by the story itself: behavior claims get per-claim
                subjects (behavior:<unit>:<id>) - shared unit subjects collided claim keys so
                accepting one claim defeated its siblings." (commit 5c3e96d, found during M1 story
                integration 2026-07-10)
                """,
            Wall: WallClock.Utc(2026, 7, 11, 0, 17, 56),
            ValidFrom: WallClock.Utc(2026, 7, 11)),

        new SeedDecision(
            Id: "post-M1-positioning-map-is-product",
            Actor: "govert",
            Method: "steward-decision",
            CommitHash: "648d29c",
            ValueText: "KodePorter positioning: the map is the product; orchestration is a consumer.",
            Reason: """
                "KodePorter positioning clarified (charter §14 paragraph added): the system of
                record for a port — the explicit map with typed imperfections of the mapping
                itself; orchestration methods are consumers, never components. Lessons about
                porting correctly are absorbed as schema and affordances, never as method prose in
                the product." (AMENDMENTS.md 2026-07-11; commit 648d29c)
                """,
            Wall: WallClock.Utc(2026, 7, 11, 1, 4, 14),
            ValidFrom: WallClock.Utc(2026, 7, 11)),

        new SeedDecision(
            Id: "service-realization-seven-decisions",
            Actor: "govert",
            Method: "steward-decision",
            CommitHash: "b5890cf",
            ValueText: "The service realization: two layers, three Gneiss tiers, seven ratified directions.",
            Reason: """
                "Seven decisions, superseding the same-day narrower positioning where they
                conflict… Three Gneiss tiers, governance now. Per-port ledgers; the KodePorter
                meta-ledger (porting knowledge, transformation rules, method-skill — flagships
                feed it, projects import pinned knowledge from it); the FireHorseCoding governance
                ledger (meta-meta), bootstrapped in M1.5 with this redirection as its first
                recorded decision. AMENDMENTS.md becomes its export." (AMENDMENTS.md 2026-07-11,
                second entry; commit b5890cf)
                """,
            Wall: WallClock.Utc(2026, 7, 11, 14, 48, 58),
            ValidFrom: WallClock.Utc(2026, 7, 11)),

        new SeedDecision(
            Id: "grounded-semantics-ratified",
            Actor: "govert",
            Method: "steward-decision",
            CommitHash: "3fcfd0b",
            ValueText: "Grounded pairwise conflict semantics and consumed-set transitive closure ratified.",
            Reason: """
                "RATIFIED: grounded pairwise conflict semantics + consumed-set transitive closure.
                The constitutional wording tightens to its intent: evaluation must be unique,
                deterministic, and monotone — no choice points, no nonmonotone revision loops;
                bounded monotone closures (the decision-effectiveness pass, grounded conflict
                labeling, consumed-set closure) satisfy it." (AMENDMENTS.md 2026-07-11, second
                entry item 7; commit 3fcfd0b)
                """,
            Wall: WallClock.Utc(2026, 7, 11, 22, 25, 44),
            ValidFrom: WallClock.Utc(2026, 7, 11)),
    ];

    /// <summary>
    /// The supersession — per the contract list, seeded after
    /// "service-realization-seven-decisions" (index 6) and before "grounded-semantics-ratified"
    /// (index 7), targeting "post-M1-positioning-map-is-product"'s assertion. A real
    /// <c>DecisionKind.Supersedes</c>, not a documented-in-prose stand-in.
    /// </summary>
    public static readonly SeedSupersession Supersession = new(
        Id: "morning-positioning-SUPERSEDED-by-service-realization",
        Actor: "govert",
        CommitHash: "b5890cf",
        Reason: """
            "Seven decisions, superseding the same-day narrower positioning where they conflict"
            — the service realization supersedes the same morning's post-M1 positioning decision
            where they conflict on ledger tiering and delivery shape. (AMENDMENTS.md 2026-07-11,
            second entry; commit b5890cf)
            """,
        Wall: WallClock.Utc(2026, 7, 11, 14, 49, 58),
        TargetId: "post-M1-positioning-map-is-product");

    /// <summary>Insertion index of <see cref="Supersession"/> within <see cref="Decisions"/>'s
    /// append order (after this many decisions have been seeded).</summary>
    public const int SupersessionAfterIndex = 6; // after "service-realization-seven-decisions"
}
