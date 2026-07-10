# Survey: Incremental Computation & Belief-View Materialization

*Research-agent survey for Gneiss, 2026-07-04. Append-only assertion ledger → deterministic nonmonotonic belief views; .NET + SQL; single node; millions-to-low-billions of assertions; human-in-the-loop latency. Verified against public sources July 2026; guesses labeled.*

> **Correction banner (2026-07-04, later the same day).** This survey was commissioned under briefing assumptions that the discussion has since withdrawn: *single-node* (federation is first-class — kb/25 §5), *human-cadence writes* (agent verdicts and flow events arrive at machine rate — kb/26, kb/28), *human-cadence reads* (agents read belief views at machine rate — kb/24 §7), and a *.NET/SQL target* (platform-neutral policy — kb/03). Sentences like "Gneiss is single-node… its durable, totally-ordered, replayable log already exists" state aspirations and defaults as facts about an unbuilt system. The survey's findings remain valid as evidence, and its recommendation ladder remains valid **as a function** — but the rung must be selected per (view, envelope), not once for "Gneiss." See [29-ENVELOPE.md](29-ENVELOPE.md). The text below is preserved unedited as dated evidence of what was believed under that brief.

## TL;DR — top 5 takeaways for Gneiss

1. **There is no credible .NET-native incremental Datalog or truth-maintenance library.** The practical .NET answer really is "SQL views + per-key recompute + optional NRules," with the build-system dirty-set pattern layered on top in plain C# + SQL.
2. **The event-sourcing projection pattern (replay + checkpoint + per-key fold), as industrialized by Marten and KurrentDB, is the closest production-proven analog** to Gneiss's "belief view = deterministic function of ledger prefix + context." Steal its rebuild, checkpoint, and blue/green-versioned-projection machinery, not a dataflow engine.
3. **The "Build Systems à la Carte" framing (Mokhov/Mitchell/Peyton Jones) is the single most transferable idea**: treat each (entity, predicate, context) belief as a build target with a verifying trace; recompute only dirty targets; use early cutoff (recomputed value unchanged → don't dirty dependents). It implements Gneiss's stale-derived-value requirement almost verbatim, and it fits in a few hundred lines of C# over a dependency-edge table.
4. **DBSP/Feldera is the correct theory and a healthy project (MIT, v0.316.0 released 2026-07-03), but it is a Rust server, not an embeddable .NET library.** It's the right L4 escape hatch, not the starting point. Materialize is a heavier, cloud-oriented version of the same idea and is overkill.
5. **Cozo — the one embeddable Datalog engine with time travel — is effectively dormant** (last release v0.7.6, Dec 2023; last commits Dec 2024, community patches only). Do not put it on the critical path. Datalevin is alive but JVM-only; Soufflé is alive but batch-compiled C++; RelationalAI has fused itself into Snowflake.

---

## 1. DBSP theory + Feldera

**Theory.** DBSP ("Automatic Incremental View Maintenance for Rich Query Languages," VLDB 2023; extended in the VLDB Journal, April 2025) gives a clean algebra: any query built from a small set of operators over Z-sets (relations with integer multiplicities, so deletions are first-class negative weights) can be mechanically incrementalized, including recursion. This is the most rigorous available answer to "how do you maintain a view under retractions" — and retraction-as-negative-weight maps directly onto Gneiss's nonmonotonic acceptance (a superseding assertion retracts the previously accepted belief from the view).

**Feldera (the implementation), July 2026.** Actively developed — v0.316.0 released July 3, 2026. Core is MIT-licensed; commercial Enterprise edition exists. It runs as a **server** (Docker `pipeline-manager`, WebConsole, REST API, Python SDK, CLI). You define SQL views; Feldera maintains them incrementally as you push change events and subscribe to view deltas.

**Embeddability / .NET path.** No .NET SDK. The underlying `dbsp` Rust crate is a real library, but embedding means Rust + a hand-rolled C ABI bridge you'd own forever. The realistic .NET integration is REST to a sidecar container: push ledger deltas, read view deltas. Note Feldera pipelines maintain their own internal state; Gneiss's "exactly recomputable from (ledger prefix, context)" invariant means treating Feldera state as a disposable cache and replaying into a fresh pipeline — supported, but the discipline is yours to build.

**Verdict for Gneiss:** the right theory to imitate and the right L4 sidecar to graduate to; not an embeddable .NET component today.

## 2. Materialize / differential + timely dataflow

Materialize is the productization of timely/differential dataflow: SQL views maintained incrementally with strong consistency, positioned in 2026 as "the live data layer for apps and AI agents." Differential dataflow's "arrangements" — indexed, multi-versioned state — are the most battle-tested incremental-join machinery in existence.

**Cost/operational profile.** It wants to be a running service with substantial memory (arrangements live in RAM), its own consistency machinery, and (in cloud form) a per-credit bill. Business signals are mixed: last disclosed raise was the 2021 $60M Series C; a 2025 Glassdoor review mentions "uncertain transitional period and layoffs" (*soft evidence, not established fact*).

**When it's overkill:** when your update rate is human-in-the-loop, your data fits one node, and your consumers tolerate seconds of staleness. That is exactly Gneiss. Everything Materialize buys — millisecond-fresh multi-way joins over high-rate streams — is capacity Gneiss doesn't need, paid for in a second stateful system to operate.

**Verdict for Gneiss:** the strongest engineering in the space and the clearest example of what *not* to deploy for a single-node, human-latency system.

## 3. Postgres-native IVM

**pg_ivm** (sraoss) is alive and steadily maintained: v1.15 released June 30, 2026; supports PostgreSQL 13–18. It creates IMMVs (incrementally maintained materialized views) via AFTER triggers — **immediate maintenance in the same transaction**, cost proportional to the delta. Restrictions matter for Gneiss: aggregates limited to count/sum/avg/min/max; **no WITH RECURSIVE**, no window functions, no HAVING/UNION/LIMIT; outer joins only as simple equijoins and not combinable with aggregates; base relations must be plain tables. Core-Postgres IVM still hasn't landed in mainline.

Without pg_ivm: `REFRESH MATERIALIZED VIEW [CONCURRENTLY]` (full recompute) or hand-written trigger maintenance — the latter being exactly the L1/L2 patterns below, spelled in PL/pgSQL.

Two Gneiss-specific problems: (a) the acceptance rules (source precedence, cutoff, retraction handling per context) exceed pg_ivm's supported SQL subset — belief resolution is a windowed/argmax-per-key computation; (b) Gneiss targets SQL Server and SQLite too, so a Postgres-only extension can't be the core mechanism. (SQL Server indexed views are the T-SQL cousin — auto-maintained but with an even narrower SQL subset; SQLite has nothing native.)

**Verdict for Gneiss:** healthy, but too restricted and too Postgres-specific to be the portable core; a legitimate accelerator for simple per-context current-state tables on PG deployments only.

## 4. Datalog engines

- **Soufflé** — active (v2.5, March 2025; commits into May 2026). Compiles Datalog to parallel C++; superb for whole-program batch analysis. But batch (no cross-run incrementality), a compiler/CLI rather than a library, awkward on Windows. Sidecar-only for .NET.
- **Ascent** — active Rust proc-macro Datalog (v0.8.0, ~early 2026; lattices, parallel execution, BYODS). Rules compile into your Rust binary. Not incremental across runs, Rust-embedded only — same FFI toll as dbsp for .NET.
- **Cozo** — on paper the best fit in this survey: embeddable (single C-ABI library with Python/Node/Java/Go/Swift/C wrappers — no official .NET binding, but P/Invoke-able), Datalog with recursion and aggregations, **and native time-travel queries via validity-stamped relations** — strikingly close to Gneiss's valid-time. But: last release v0.7.6 (Dec 2023); last commits Dec 2024, community patches; the original author inactive; never reached 1.0. Adopting it means adopting maintenance of a Rust database engine.
- **Datascript / Datalevin** — Datascript is in-memory Datalog for Clojure/JS; irrelevant to .NET. Datalevin is genuinely active (major Jan 2026 release: async transactions, new rule engine, vector search) but is a JVM/Clojure library — sidecar-only from .NET, and query-time Datalog rather than incremental maintenance.
- **RelationalAI / Rel** — as of 2026 exists only as a Snowflake Native App ("relational knowledge graph coprocessor," expanded June 2026 with agentic decision-intelligence features). Not obtainable as an engine. Interesting as design validation — a commercial bet that "business rules as relational/Datalog views over facts" is right — but unusable here.

**Verdict for Gneiss:** no Datalog engine is simultaneously embeddable-in-.NET, incremental, and maintained; Cozo is the near-miss that proves the niche is real but unstaffed.

## 5. Rete / NRules

NRules is the .NET production-rules engine: Rete matching, C# internal DSL, latest release v1.0.4 (Feb 2024). Mature and stable but slow-cadence (*guess: maintenance mode rather than abandonment*). It does have the TMS-like behavior in question, verified: **linked facts** produced by forward chaining participate in truth maintenance — when the conditions that yielded a linked fact no longer hold, the linked fact is automatically retracted, cascading through dependent rules. That is a real, if bounded, justification-style TMS.

Fit problems: (1) Rete's payoff is many rules × many facts with shared join patterns; Gneiss's acceptance rules are closer to a deterministic *fold with precedence* over the assertion history of one (entity, predicate) — a per-key argmax, not a combinatorial match. (2) Rete holds working memory in RAM with no persistence; sessions at millions of facts are memory-hungry and rebuilding a session means re-asserting everything (fact-propagation performance: NRules issue #200). *Honest guess: comfortable at 10⁵–10⁶ facts in a session; 10⁸⁺ ledger-scale working memory is not realistic.* (3) Determinism/replayability under agenda-based firing takes discipline that SQL gives you for free.

**Verdict for Gneiss:** viable as an *optional* in-memory evaluator for genuinely rule-shaped derived logic over a bounded working set (e.g., one site's active entities), never as the ledger-scale belief-view maintainer.

## 6. Demand-driven memoization / the build-system framing

- **Salsa** (Rust) — very much alive: powers rust-analyzer and Astral's `ty` Python type checker (2025–26), the largest new deployment of the model. Red-green dependency tracking, durability tiers, early cutoff. Rust-only.
- **Adapton** — the academic root; research code, dormant. Read the papers, don't run the code.
- **Jane Street `incremental`** — production OCaml DAG-recompute library with stabilization and cutoff. OCaml-only.
- **Build Systems à la Carte** (Mokhov, Mitchell, Peyton Jones, ICFP 2018) — the Rosetta stone. It decomposes every build system into a *scheduler* (which dirty keys, in what order) and a *rebuilder* (how to decide a key is dirty: dirty bits, verifying traces, constructive traces).

**Applying the framing to Gneiss** — close to a direct blueprint:

- **Key** = (entity, predicate, context-id) for belief views; (derived-quantity-id, context-id) for derived values.
- **Inputs** = the set of ledger assertion-ids consulted, plus rule/formula version, plus context parameters.
- **Verifying trace** (a SQL row): key → hash(inputs) → stored output + max ledger seq consulted. A key is dirty iff a new ledger entry touches its input set or its rule version changed.
- **Early cutoff**: after recompute, if the accepted value is unchanged, do *not* dirty downstream derived values — this prevents one noisy re-assertion from cascading through the silo-mass → inventory-report chain.
- **Scheduler**: a suspending/topological scheduler is overkill; a restarting scheduler over a SQL `dirty_keys` queue is fine at human latency.

Salsa/incremental prove the model works at scale; none are .NET libraries, but the model is small enough that *reimplementing the rebuilder in C# over two SQL tables (traces, dependency edges) is cheaper than binding any of them.*

**Verdict for Gneiss:** adopt the framing wholesale — belief views are a build system whose source tree is the ledger; implement verifying traces + early cutoff natively.

## 7. Event-sourcing projection rebuilds (Marten / Kurrent)

This ecosystem — .NET-native, SQL-backed, actively developed — already ships Gneiss's operational skeleton. **Marten** (Marten 8 in 2025 with a reworked projection subsystem): inline projections (same transaction as the append), async-daemon projections (background workers folding events into documents/tables with a stored checkpoint = high-water-mark sequence number), full rebuild on demand, and — recently — true **blue/green deployments where old and new versions of a projection run in parallel** until the new one catches up and traffic flips. **KurrentDB** (26.1 shipped a new Projections V2 engine plus catch-up subscriptions whose contract is exactly "consumer tracks its own last-known position").

Why plain replay + checkpoint + per-key fold may simply be sufficient for Gneiss: the acceptance computation for one (entity, predicate, context) is a deterministic fold over that key's assertion history — small (typically 10⁰–10⁴ assertions per key, *guess*), cheap, and embarrassingly independent across keys. New ledger entries touch few keys. Recomputability-from-prefix is the *definition* of a projection rebuild. Blue/green rebuild is precisely how you deploy a changed acceptance-rule version without downtime: materialize `belief_view_v(n+1)` alongside `v(n)`, replay, diff (free regression test!), flip.

**Verdict for Gneiss:** the closest production-proven pattern, in the right language, on the right substrate — the default to build on (borrow the pattern; whether to take Marten as a dependency is a separate call since Gneiss's ledger schema is richer than an event stream).

## 8. Streaming (Kafka + Flink)

Wrong weight class, briefly: Kafka + Flink buys horizontal partitioning, sub-second latency on unbounded streams, and exactly-once across a distributed topology — paid for with brokers, a JVM cluster, state backends, checkpoint/savepoint management, schema-registry ceremony, and a second source of truth to reconcile with the ledger. Gneiss is single-node, its writes arrive at human cadence, its consumers read at human cadence, and its durable, totally-ordered, replayable log *already exists* — it's the ledger table. Every distinctive capability of the streaming stack is either unneeded (partitioned parallelism) or already possessed (replay).

---

## Is there ANY credible .NET library for incremental Datalog or truth maintenance?

**No.** Verified state of the field: **TED** (Horswill) is a typed Datalog embedded in C# but pedagogical/game-AI oriented; **DDlog** (the one real incremental-Datalog compiler) is VMware-archived with no C# binding and is dead anyway; **DynamicData** and **ObservableComputations** are genuinely incremental .NET collection-operator libraries, but in-memory, UI/Rx-oriented, with no persistence, provenance, or recursion (Microsoft's Reaqtor has been quiet for years — *guess: dormant*); **NRules** gives Rete + linked-fact truth maintenance but not at ledger scale. So the practical answer is exactly the hypothesized one: **SQL views + per-key recompute + a dependency-edge table, with NRules optional at the margins** — and that's not a consolation prize; it's what the build-system literature says to build.

---

## Recommendation ladder for Gneiss

**L0 — Full recompute per context, on demand.**
One SQL query (or a C# fold) computes a whole belief view from the ledger prefix ≤ cutoff: greatest-per-key by (precedence, valid-time, tx-time) with retraction filtering. *Enough when:* ad-hoc contexts, audits, tests. *Ceiling (guess):* ~10⁶–10⁷ relevant assertions per view at interactive latency; unbounded if you accept minutes. *Cost:* days. *Never remove it* — it is the semantics, the correctness oracle every higher rung is differential-tested against. *Climb when:* any regularly-consumed view is recomputed more than rarely.

**L1 — Per-key recompute on append (projection pattern).**
Per materialized context: a checkpoint (last ledger seq) + a worker that, per new assertion, recomputes the touched (entity, predicate) beliefs by folding that key's history; blue/green rebuild for rule-version changes. Marten-style, whether or not Marten is the dependency. *Enough when:* belief views only, dependencies no deeper than assertion → belief. *Ceiling (guess):* 10⁸–10⁹ total assertions comfortably, provided per-key histories stay ≤ ~10⁴ and write rate is human. *Cost:* 1–2 weeks over L0. *Climb when:* derived values create belief → belief dependency chains — which Gneiss's requirements already do.

**L2 — Dirty-set propagation via stored dependency edges (poor-man's provenance = Build Systems à la Carte in SQL).**
Tables: `dependency_edge(output_key, input_key, rule_version)` recorded at compute time, and `trace(key, input_hash, output, computed_at_seq)`. New/retracted input → enqueue dependents as dirty; worker recomputes in dependency order; **early cutoff** stops unchanged values from cascading. This *is* the stale-flagging requirement (retract fill-level → silo-mass flagged stale), so parts of L2 are mandatory Gneiss functionality, not an optimization. *Ceiling (guess):* same 10⁸–10⁹ assertions; the new limit is dependency-graph churn — fine up to ~10⁶–10⁷ edges with proper indexing. *Cost:* 2–4 weeks over L1. *Climb when:* rules become genuinely recursive/join-heavy (transitive closure over part-of hierarchies, ontology closure policies) so one input dirties an unbounded frontier and per-key recompute degenerates toward full recompute.

**L3 — Embedded/sidecar Datalog or DBSP engine for the recursive subset only.**
Order of preference: (a) stay in SQL — recursive CTEs plus hand-rolled semi-naive delta tables for the two or three closure rules that need it; (b) a small Rust sidecar (dbsp crate or Ascent) behind a local gRPC/stdio contract; (c) Soufflé-compiled batch programs; (d) Cozo only with eyes open about dormancy. Keep the sidecar stateless-restartable from the ledger. *Ceiling:* effectively removes the recursion bottleneck; single-node memory becomes the limit. *Cost:* 1–2 months incl. the FFI/process contract and replay discipline. *Climb when:* even the sidecar can't meet freshness because view count × context count × change rate has genuinely outgrown one process.

**L4 — External IVM server (Feldera; Materialize if managed-cloud is acceptable).**
Ledger deltas streamed to Feldera pipelines; materialized views read back over REST. *Enough when:* you've become a streaming shop with multiple consumers and >10⁹ assertions under continuous change. *Cost:* operating a second stateful service + keeping the "Feldera state is a disposable cache of (prefix, context)" invariant honest. Most Gneiss deployments should never reach this rung.

**Recommendation: start at L1 with L2's dependency-edge and trace tables designed in from day one, keeping L0 as the permanent oracle.** Justification: L2's machinery is required anyway by the stale-derived-value requirement; per-key folds cover belief views at the stated scale with the lowest-risk, fully-portable (SQL Server/SQLite/Postgres) implementation; every credible higher rung (DBSP, Feldera, Salsa) confirms the *model* while offering no .NET-embeddable shortcut worth its integration cost today. The differential test "L0(prefix, context) == materialized view" is the invariant that makes climbing rungs safe later.

## Sources

- Feldera (MIT, v0.316.0, 2026-07-03): https://github.com/feldera/feldera · https://docs.feldera.com/get-started · https://docs.feldera.com/literature/papers/ · DBSP SIGMOD Record: https://dl.acm.org/doi/10.1145/3665252.3665271
- Materialize: https://materialize.com/ · https://materialize.com/blog/self-managed-materialize-early-access/ · https://www.crunchbase.com/organization/materialize-38fc · https://github.com/MaterializeInc/materialize/
- pg_ivm (v1.15, 2026-06-30, PG 13–18): https://github.com/sraoss/pg_ivm · https://wiki.postgresql.org/wiki/Incremental_View_Maintenance
- Soufflé 2.5: https://souffle-lang.github.io/release-2.5.0.html · https://github.com/souffle-lang/souffle/releases
- Ascent: https://github.com/s-arash/ascent · https://s-arash.github.io/ascent/
- Cozo (releases end Dec 2023; commits end Dec 2024): https://github.com/cozodb/cozo · https://github.com/cozodb/cozo/releases
- Datalevin (Jan 2026 release): https://github.com/juji-io/datalevin
- RelationalAI on Snowflake (June 2026): https://www.globenewswire.com/news-release/2026/06/02/3305546/0/en/ · https://www.relational.ai/post/relationalai-snowflake-native-app-architecture-white-paper
- NRules (v1.0.4): https://github.com/NRules/NRules/releases · https://nrules.net/ · https://github.com/NRules/NRules/issues/200 · https://en.wikipedia.org/wiki/Rete_algorithm
- Salsa: https://github.com/salsa-rs/salsa · https://talkpython.fm/episodes/show/506/ty-astrals-new-type-checker-formerly-red-knot
- Build Systems à la Carte (ICFP 2018): https://dl.acm.org/doi/10.1145/3236774
- Marten projections/rebuilds: https://martendb.io/events/projections/rebuilding · https://martendb.io/events/projections/async-daemon.html · https://jeremydmiller.com/2025/03/26/projections-consistency-models-and-zero-downtime-deployments-with-the-critter-stack/ · https://jeremydmiller.com/2025/04/13/preview-of-hopefully-improved-projections-in-marten-8/
- KurrentDB 26.1: https://www.kurrent.io/releases/kurrentdb/26-1/ · https://docs.kurrent.io/clients/dotnet/legacy/v23.3/subscriptions
- DDlog (archived): https://github.com/vmware-archive/differential-datalog · TED: https://github.com/ianhorswill/TED · DynamicData: https://github.com/reactivemarbles/DynamicData · ObservableComputations: https://github.com/IgorBuchelnikov/ObservableComputations
