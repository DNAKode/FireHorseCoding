# Survey: The PARC–VPRI Lineage (Smalltalk, Croquet, Worlds, PIE, STEPS)

*Primary-source mine for Gneiss, 2026-07-05 (Phase H, miner 2 of 3). Primary documents fetched and read in full are marked in Sources. Claims K1–K4 adjudicated. Guesses and memory-only items labeled inline. The "STEPS procedure, extracted" section is the direct input to Phase M.*

## TL;DR — top 5 takeaways for Gneiss

1. **Gneiss's architecture has run in production since 1976.** Smalltalk's image/.changes pair is exactly log + materialized view + replay + condense — applied to code. The failure modes they hit (condensing destroys history; replay of `doIt`s diverges because context changed) are the exact problems Gneiss's sealed forgetting and versioned contexts are designed to fix.
2. **Croquet/TeaTime is the strongest known prior art for the Gneiss Contract.** `island state = f(snapshot, ordered message stream)` with bit-identical replicas, a dumb sequencer (reflector), synthetic time, and snapshot+replay joining — alive today as Multisynq (Croquet network deprecated July 30, 2025). Steal its determinism checklist verbatim.
3. **Worlds gives you the what-if commit rule.** Commit from child to parent succeeds iff every value the child *read* is unchanged in the parent (read-set serializability); failure aborts atomically with both worlds intact. That is the precise, machine-checkable semantics Gneiss what-if promotion needs — plus PIE's alternative (merge = new layer with recorded rationale) for when abort is wrong.
4. **PIE (1980) is the closest ancestor of Gneiss contexts and nobody remembers it.** Attribute values relative to a context; context = ordered sequence of immutable layers; layers close (become immutable) after contract checks; layers and contexts are themselves nodes (metacircular); merges are new layers with meta-described provenance. It died of UI/performance cost on 1980 hardware, not of a broken model.
5. **The STEPS method is a repeatable procedure, and its final report tells you the failure mode in advance:** the "siren's song" of making it fast on commodity hardware corrupted the meaning-budget. Gneiss should count meaning-code and optimization-code separately from day one, and treat the one-page kernel as the *meaning* artifact.

## 1. The Early History of Smalltalk (Kay, HOPL-II 1993)

Read in full (worrydream.com mirror). The lineage Kay himself draws: Sketchpad's master/instance distinction + Simula's activities/processes ("What Sketchpad called masters and instances, Simula called activities and processes") + the B5000's protected segmented architecture + LISP's metacircular eval, fused under Bob Barton's principle: **"The basic principle of recursive design is to make the parts have the same power as the whole."** Kay's collapse: don't divide the computer into weaker things (data, procedures) — make everything a whole computer: objects as servers exchanging messages, "unify object-oriented semantics with the ideal of a completely extensible language."

Key retrospective judgments from the paper:
- LISP's deep flaw was pretending to be functional while depending on special forms; the lesson Kay drew: **"take the hardest and most profound thing you need to do, make it great, and then build every easier thing out of it."**
- **The one-page bet:** September 1972, Ingalls/Kaehler dared Kay to define "the most powerful language in the world" in a page. He worked ~8 consecutive mornings; Ingalls then implemented it (Smalltalk-72's interpreter). This is the direct ancestor of Gneiss's "one-page metacircular kernel" ambition — *and it worked as a social artifact*: the page was the spec, the bet was the forcing function.
- Smalltalk-72's six principles (everything is an object; objects communicate by messages; objects have private memory; every object is an instance of a class which is an object; classes hold shared behavior; a program list passes control to the first object, the remainder becomes its message).
- What went wrong, in his own words: "much of what is called 'object-oriented programming' today is simply old style programming with fancier constructs" — the industry adopted the syntax and missed the messaging/late-binding semantics. (The sharper form, "The big idea is 'messaging'", is from his 1998 squeak-dev email — from memory, verify wording before quoting.)
- The "LISP interpreter as Maxwell's equations of software" line is from the 2004 ACM Queue interview, not HOPL, though HOPL contains the same idea via McCarthy's self-describing interpreter.

**What they knew that we forgot:** a kernel is a *cultural* artifact — one page you can bet on, hand to an implementer, and have running in weeks. Kay's team treated the interpreter definition as the theory and the implementation as the experiment.

**Verdict for Gneiss:** the five-primitive kernel should be written as Kay's page was: definition-first, with a named implementer and a deadline. The historical evidence is that this works and that everything durable in Smalltalk traces to it.

## 2. Smalltalk's persistence machinery — the ledger that was always there

Verified via Squeak wiki (.changes file, ChangeSet, condenseChanges pages) plus Goldberg-Robson background. Mechanics, precisely:

- A running system is four files: **VM**, **image** (a snapshot of the entire object memory), **.sources** (static source for the base release), **.changes** (append-only text log). CompiledMethods hold *source pointers* into .sources/.changes; the image is self-sufficient without them except for source viewing.
- **Every** method compilation, class (re)definition, and evaluated expression (`doIt`) is appended to .changes at the moment it happens — independent of image saves. Saving the image is snapshotting the view; the log is already durable.
- **Crash recovery:** after a crash, "recover changes" replays the tail of .changes written after the last snapshot — the canonical demo of log+snapshot separation. Squeak documentation: the system "is built to retain all your code."
- **Condensing:** `Smalltalk condenseChanges` rewrites the log keeping only the latest version of each method — deliberate, lossy compaction. `condenseSources` folds everything into a new .sources baseline.
- **Change sets** are first-class objects grouping related changes for fileOut/fileIn — proto-patches. **Monticello** (Squeak wiki: "a distributed, optimistic, concurrent, versioning system") versions packages as full snapshots with ancestry metadata; **ENVY** did method-level versioning against a shared repository (ENVY details from memory — verify if load-bearing).

**What they knew that we forgot:** the log and the snapshot are different artifacts with different lifetimes, and the log is the more trustworthy one. Every Smalltalker has recovered a day's work from .changes after the image corrupted.

**Verdict for Gneiss:** direct ancestral validation of ledger/view/seal — including the warning: condensing was *silent, total forgetting*. Gneiss's contribution is making forgetting a recorded, monotone-degrading operation instead.

## 3. Croquet / TeaTime / David Reed

Read in full: "Croquet: A Collaboration System Architecture" (Smith, Kay, Raab, Reed, VPRI TR-2003-002). The determinism contract, from the paper:

- **Replicated computation, not state replication.** "Replicated, versioned objects — unifying replicated computation and distribution of results." A Tea object redirects messages sent to it to its replicas on all participating machines.
- **"A coordinated universal timebase embedded in the communications protocol."** Objects "behave like processes that exist in time"; "object internal states are maintained as ordered histories, and operations are performed at 'pseudo-time' instants that are properly ordered with respect to I/O operations." I/O events live in real time and are the bridge between real time and pseudo-time.
- **"A coordinated 'distributed two-phase commit' that is used to control the progression of computations at multiple sites, to provide resilience, deterministic results, and adaptation to available resources."** Behaviors are computed contingently, then atomically committed; missed deadlines cause contingent computation to be undone by the objects themselves.
- **The bit-identical requirement is explicit and non-negotiable:** the VM must run "bit-identical on Windows, Macintosh, Linux… Most attempts at true multiplatform systems have turned out to be dangerous approximations (cf. Java) rather than the bit-identical 'mathematically guaranteed' ports that are required." And: "even the bugs are the same."

**Reed 1978 (NAMOS):** "Naming and Synchronization in a Decentralized Computer System," MIT/LCS/TR-205 (located at mit.edu; model per the 1983 TOCS companion and the Croquet paper). Objects are histories of immutable versions named by pseudo-times; atomic actions claim pseudo-time ranges; synchronization and crash recovery are "two sides of the same problem." Croquet's 2003 paper cites Reed as the direct basis, distinguishing itself from Jefferson's Virtual Time by "maintaining a partial history, managing replicated objects, [and] incorporating two-phase commit."

**Status 2026 (verified):** modern lineage is Croquet OS for the web → **Multisynq**. The Croquet network was deprecated July 30, 2025 in favor of the Multisynq network (migration = upgrade @croquet/croquet ≥2.0, swap API key). Architecture unchanged: Model code runs in a deterministic "Teatime" VM; **reflectors "order all events into a single canonical stream"** with a global heartbeat; reflectors hold no logic; "given the same initial state and the same sequence of events, every user's device produces exactly the same result." Late joiners bootstrap from a published snapshot plus the event stream (snapshot+fast-forward — mechanism verified, wording from memory).

**What they knew that we forgot:** the sequencer can be *dumb*. Croquet's reflector does no computation at all — total order plus deterministic replicas is enough for shared reality. And determinism must be engineered down to the FPU and hash-iteration order, or it is fiction.

**Verdict for Gneiss:** this is the Gneiss Contract, twenty years early, deployed. Adopt its checklist: single ordered stream per island (= single-sequencer ledger), synthetic time only, all nondeterminism quarantined at the I/O boundary, snapshot+replay as the join protocol.

## 4. Worlds (Warth & Kay RN-2008-001; Warth, Ohshima, Kaehler, Kay ECOOP 2011)

Both read in full. The model: program state is reified as a first-class lookup table from (object tag, property) to value; all computation happens *in* some world; `sprout` creates a child whose state is derived copy-on-write from the parent; `in w { … }` executes code in a world; `commit` propagates the child's captured side effects to its parent.

**Lookup/update semantics (ECOOP §5):** a write creates/updates the child's `writes` entry. A read searches: child's `writes`, then child's `reads` (the memoized first-read value — this enforces "no surprises"), then ascends parents; on finding a value in an ancestor, the child records it in its own `reads` object.

**The two safety properties (quoted):**
- *No surprises:* "Once a variable… has been read or modified in a world w, subsequent changes to that variable in w's parent world are not visible in w."
- *Consistency:* "A commit from w_child to w_parent is only allowed to happen if, at commit-time, all of the variables… that were read in w_child have the same values in w_parent as they did when they were first read by w_child… A commit that fails the serializability check is aborted, leaving both child and parent worlds unchanged, and throws a CommitFailed exception."

Commit that passes: writes overwrite the parent's slots; the child's read-set is *propagated upward* into the parent's read-set (so a later commit from the parent still protects the grandparent); the child's tables are cleared (the world is reusable). A failed commit leaves the child fully inspectable — extract values or sprout again. Parent changes to slots the child never read *do* flow through silently (deliberate; not full snapshot isolation).

**Applications shown:** exception-safety (sprout, try, commit in finally), multi-level and tree undo, scoped extension methods/local rebinding as a module system, sandboxing, backtracking (OMeta/Prolog state).

**Performance/fate:** needed a VM primitive for slot lookup plus `flatten`; the bitmap editor went ~1 fps naive → >30 fps, degrading past ~50-deep world chains. No mainstream adoption. The STEPS final report lists Worlds under "More Than Planned": "the 'Worlds' mechanism for fine grain context capturing, parallel experiments and redoing… led to many experiments in 'possible worlds reasoning' and visualizations of multiple solution paths."

**What they knew that we forgot:** the browser-tab intuition — speculation should be as cheap as opening a tab — and that the *read set*, not the write set, is what makes merging speculative work safe.

**Verdict for Gneiss:** adopt the two properties as named theorems for what-if contexts ("no surprises" and "consistency") — they are already stated in machine-checkable form.

## 5. PIE (Goldstein & Bobrow, PARC 1980–81)

Read: CSL-81-3 "An Experimental Description-Based Programming Environment: Four Reports" (worrydream mirror, OCR layer). The actual model, from the text:

- A software system is a **network of nodes**; each node carries multiple **perspectives** ("each perspective describes a different aspect of the program structure represented by the node"), giving multiple inheritance of viewpoint-specific behavior.
- **"All values of attributes of a perspective are relative to a context."** A **context is structured as a sequence of layers**; assignment happens *in a layer*; "retrieval from a context is done by looking up the value of an attribute, layer by layer… until the layers are exhausted." (Lineage explicitly credited to Conniver's contexts.)
- **Layers are units of coordinated change:** "Values stored in a layer represent a coordinated set of values… one sees either both changes or neither in any view." PIE auto-creates a new layer per working session ("a user can easily back up to the state of a design during a previous working session").
- **Contracts and closure:** contracts between nodes are checked "at the closure of a layer… After contracts are checked, a closed layer is immutable. Subsequent changes must be made in new layers."
- **Metacircularity:** "Layers and contexts are themselves nodes in the network," so contexts are described, documented, even selected context-sensitively; "super-contexts can be created that act as big switches for altering designs."
- **Cooperation and merge:** different designers work in separate layers; "merging two designs is accomplished by creating a new layer into which are placed the desired values for attributes as selected from two or more competing contexts. Layers created by a merger have associated descriptions… specifying the contexts participating in the merger and the basis for the [selection]."
- **Distribution:** "contexts provide a mechanism for generating an incremental system release… by transmitting a layer with the changes."

**Fate:** the reports flag the costs — "the flexibility to represent alternative descriptions in layers comes at the cost of increased [retrieval complexity]"; they shipped commands to *suppress* the context machinery for speed; "it remains a research goal to make the context machinery available to the user in a convenient fashion." The project faded when Goldstein left PARC (early 1980s — from memory); the ideas resurface in context-oriented programming (Hirschfeld et al. 2008) and, arguably, in change sets and layered version management.

**What they knew that we forgot:** immutable-after-close layers with contract checks at closure; merges as first-class objects carrying their own rationale; contexts as *described data*, not configuration.

**Verdict for Gneiss:** PIE is the missing ancestor of Gneiss's Context primitive — a context literally is "a search path over immutable layers," i.e., a view function over a ledger prefix. Steal layer-closure (= sealing with validation) and meta-described merges.

## 6. STEPS (VPRI, 2007–2012)

Read in full: TR-2012-001 final report (text recovered through its font encoding) and TR-2007-008 first-year report (targeted). The goal: model "personal computing" — apps, UI, graphics/sound, systems services, down to the metal — in ~20,000 lines of *meaning-code*, versus "hundreds of millions of lines" ("one hundred million lines of code at 50 lines per page is 4000 books of 400 pages each. This is beyond human scale").

**The method, in their words:** "Look for *needs* and identify *kernels* that are close to needs, then *create languages* to implement the kernels. Finding kernels is heavily design intensive." Heuristics: "(a) 'Math Wins!', (b) build throwaways to 'find the arches', (c) 'particles and fields', (d) 'Simulating time'." Plus **"T-shirt programming"**: from "the aesthetic delight of Maxwell's equations on a t-shirt… How many t-shirts will be required to define TCP/IP (about 3), or all the graphical operations needed for personal computing? (about 10)."

**Concrete wins (verified numbers):**
- **Nile/Gezira:** all of 2.5D personal-computing graphics — rendering, compositing, filtering, gradients — "in just 435 lines of code," written as runnable mathematics. The Nile *language* itself: ~130 lines of form definition plus a few hundred of meanings, made by a metatranslator of ~100 lines of definition code — which "can make itself when fed a description of itself."
- **TCP/IP:** "well under 200 lines of code, including the definitions of the languages for decoding header format and for controlling the flow of packets" (2007 report, Appendix E) — the ASCII-art packet header diagram *is* the program ("The following looks like documentation, but it's a valid program").
- **OMeta** (Warth): the universal parsing/translation front-end. **Maru**: the self-hosting bottom (~1,750 lines — LOC from memory, order-of-magnitude right).
- **Frank/KScript/KSWorld:** the working suite (the final report and Turing-centenary talks were produced in it). Rewriting Frank "involved replacing 50,000+ lines of Smalltalk… with about 10,000 lines" of KScript. Final table: 10,055 LOC essential for the document editor, 17,358 total with bindings, importers, tests.
- **"We have been able to count more than 60 languages that we implemented as part of our learning process."**

**Their honest self-assessment (final report, decoded):** "Enough was done… to conclude that much of today's personal computing systems… can be built with just 1000s to tens of 1000s of lines of code… many orders of magnitude smaller than the targets we examined." But: "we wound up covering a bit less ground than we had expected, partly because we got interested in seeing if we could run in real-time on laptops, and this part of the design and optimization fought some of the original goals" — the report literally calls this the **"siren's song."** Less-than-planned: the full "bottom engine room," productivity-suite breadth, the system-as-exploratorium. Still unsolved, their words: "Massively scalable intermodule coordination and communication has not been achieved via any means in personal or any other kind of computing" — precisely Gneiss's federation problem. Their conclusion on method: "it was generally much easier to implement a language than to design it"; the best languages "were the ones which followed the most design, implementation, and redesign"; if done again, "employ some form of the original supercomputer strategy" to buy performance headroom without corrupting meaning. They also conclude they'd ultimately "prefer to distill the most important ideas into a more general specification language" rather than keep many DSLs.

**Why it ended:** NSF funding concluded October 2012 ("This report marks the end of the NSF part of the funding… the STEPS project itself continues"). Successor efforts (CDG at SAP 2014, HARC at YC Research 2016–2017) wound down by ~2018 (dates from memory/secondary).

**Verdict for Gneiss:** the method transfers; see the extracted procedure below. The two pre-announced failure modes for Gneiss are the siren's song (performance seduction) and DSL sprawl past the point where distillation should have begun.

## 7. Brief: OOPSLA 1997 and Kay on LLMs

**OOPSLA 1997** (transcripts/video verified): the doghouse parable (build a doghouse out of anything; scale it 100x and it collapses — most software is doghouse engineering); Egyptian pyramids — brute-force bricks with "very little usable space inside" vs the *arch*, the nonobvious structural idea (the same image opens the STEPS final report); biology — the cell as the model component: complex inside, simple protocol outside, no global shared state; the growth argument for Internet-style design; and the HTML verdict: "one of the worst 'reinventions' of ideas that had been done better earlier."

**Kay on LLMs/AI (2023–2025, recency verified):** the primary quotable item is his Quora statement — **"The Large Language Models are mostly syntactic and there is little in the techniques to actually create trustworthy models of the world — especially at the level needed in medicine."** A 2024 filmed interview exists (REBASE/SPLASH 2024, video March 2025); no verbatim transcript retrieved — treat longer quotes as unverified. His consistent position: current AI lacks *models* (semantics, world-models, epistemic grounding) and substitutes syntax for understanding.

**Relevance to Gneiss:** Kay's "trustworthy models of the world" critique is a direct argument *for* Gneiss — deterministic rules over an auditable ledger with explicit justification are exactly the trustworthy, inspectable world-model he says LLMs lack. Gneiss positions as the substrate an LLM writes assertions *into*, not the reasoner.

## The claims evaluated

### K1 — Smalltalk was ledgered all along; image = materialized view over the changes log
**Verdict: TRUE, and stronger than stated.** The .changes file is literally append-only; every change and every `doIt` is logged at execution time, independent of snapshots; crash recovery *is* log replay onto the last snapshot; condensing *is* compaction. **Caveats:** (1) flat text log — no bitemporality, no envelopes, no branching: a single linear stream, not a structured assertion ledger. (2) Condensing is *silent total forgetting*; Gneiss's recorded, monotone-degrading forgetting is the genuine advance. (3) Replaying old `doIt`s can diverge because they assume a system context that has moved — exactly why Gneiss needs versioned evaluation contexts. Strong ancestral proof-of-concept, not a drop-in design.

### K2 — Croquet/TeaTime is `view = f(initial state, ordered message stream)` with bit-identical determinism; Reed's pseudo-time anticipates transaction-time
**Verdict: TRUE and the single best prior art for the Gneiss Contract.** Bit-identical cross-platform replication, single coordinated timebase, deterministic results, objects-as-histories at ordered pseudo-times; Multisynq confirms the contract in production 2026 with a dumb reflector as sequencer. **Caveats:** (1) Croquet's ordered stream is *ephemeral coordination* — the durable artifact is the periodic snapshot; not designed as a permanent queryable ledger, so Gneiss adds durability + time-travel query. (2) Reed's pseudo-time is closer to valid-time versioning than to transaction-time provenance; the analogy is real but Gneiss's dual axes are a superset. (3) TeaTime's two-phase commit is tight real-time convergence; Gneiss federation (watermark beliefs over fallible sources) is deliberately weaker-coupled — borrow the determinism discipline, not the commit protocol.

### K3 — STEPS' method is a repeatable procedure Gneiss can apply
**Verdict: TRUE, with the method stated explicitly by its authors — and two documented traps.** The report gives the procedure (needs → kernels → languages), the heuristics (Math Wins, find the arches, T-shirt sizing), and honest numbers. **Caveats:** (1) the siren's song — performance seduction pulled effort from meaning and shrank coverage; budget meaning-code and optimization-code separately. (2) "It was much easier to implement a language than to design it" — the hard cost is design, and it does not parallelize. (3) They ultimately favored distilling many DSLs into one specification language — validating Gneiss's single-kernel goal over a DSL zoo.

### K4 — Worlds' sprout/commit is the template for what-if promotion
**Verdict: TRUE for the mechanism; INCOMPLETE for the promotion policy.** Worlds gives the exact rule: promotion succeeds iff the what-if's *read set* still matches the base; otherwise abort atomically, keeping the speculative context inspectable. **Caveats:** (1) abort-only is one policy; PIE's merge-as-new-layer-with-recorded-rationale is better when reconciliation is wanted; Gneiss can support both. (2) Worlds silently lets un-read parent changes flow into the child; Gneiss must decide explicitly whether what-if contexts track base advances or pin to a ledger prefix. (3) Worlds tracks reads at slot granularity via instrumented objects; Gneiss's analogue — tracking which assertions/rules a view consumed — must be computed by the deterministic evaluator: a design obligation, not free. (Note: this read-set is the same artifact as the verifying trace from the incremental survey — one mechanism serves both.)

## What Gneiss should steal

| From | Steal this | Gneiss mapping |
|---|---|---|
| Smalltalk .changes | Append-on-execute log, separate from snapshot; recover = replay tail | Ledger is source of truth; view is disposable materialization |
| Smalltalk condense | Compaction as an explicit operation | Sealed forgetting — recorded and monotone-degrading, never silent |
| Croquet/TeaTime | Dumb single sequencer + deterministic replicas; snapshot+replay join; nondeterminism only at I/O edge | Single-sequencer ledger; `view=f(prefix,ctx)`; federation join via snapshot+stream |
| Croquet | Bit-identical determinism engineered to FPU/hash-order; "even the bugs are the same" | Determinism theorem must cover float, iteration order, hashing — machine-checked |
| Reed NAMOS | Objects as histories of immutable versions at ordered pseudo-times; sync = recovery | Transaction-time discipline; immutable assertions addressed by time |
| Worlds | `sprout`; commit iff read-set unchanged; atomic abort with inspectable failed context; "no surprises" + "consistency" as named properties | What-if contexts; promotion rule + two machine-checkable theorems |
| PIE | Context = ordered sequence of immutable layers; close-with-contract-check; merges as first-class nodes with recorded rationale; contexts are described data | Versioned contexts; sealing = layer closure + validation; reconciling merge as recorded assertion |
| STEPS | needs→kernels→languages; count meaning-code only; find the arches with throwaways; T-shirt sizing | The Phase M procedure (below) |
| Kay/HOPL | Kernel as cultural artifact: one page, a named implementer, a deadline, a bet | The five-primitive metacircular kernel plan |

## The STEPS procedure, extracted (the Phase M input)

1. **Set a code budget in meaning-lines, and split the ledger.** Declare a target for *meaning-code*; keep two counters from day one: meaning-code and optimization-code. STEPS' single biggest regret was letting optimization silently consume the meaning budget.
2. **Enumerate needs, not features.** For Gneiss: assertion recording, deterministic view computation, context/what-if, forgetting, federation. Needs are behaviors, not modules.
3. **Find the kernel closest to each need — "find the mathematical center."** For each need, the smallest mathematical object that generates the behavior (STEPS: graphics → the coverage integral; TCP → the packet grammar). For Gneiss: `view = f(ledger prefix, context)` is the center; find the equivalents for federation (watermark belief) and forgetting (monotone degradation).
4. **T-shirt-size it before building.** If the honest answer is many pages, the center isn't found yet. STEPS: ~3 T-shirts for TCP/IP, ~10 for all graphics. Budget Gneiss's kernel at *one*.
5. **Build throwaways to "find the arches."** Early builds discover the load-bearing abstraction; the code is disposable. "The best language designs followed the most design, implementation, and redesign."
6. **Express the kernel as runnable mathematics in a problem-oriented notation.** The spec and the executable are one artifact ("looks like documentation, but it's a valid program").
7. **Make the notation self-supporting (metacircular bottom).** The language expressing the kernel can express itself; the kernel's five primitives should describe the kernel.
8. **Prefer distillation to proliferation.** STEPS made 60+ languages and concluded they'd rather distill into one specification language. A second and third DSL is the signal to unify.
9. **Accept "good enough" coverage, and record what you excluded.** Ship a kernel covering the core contract; *name* the excluded cases rather than pad the kernel.
10. **Validate by reimplementation and by machine-checked theorems.** STEPS validated by rewriting Frank (50K→10K). Gneiss adds the bar the era lacked: mechanized proofs. Reimplementation test = grad student, one week; theorem test = properties hold by proof, not demo.
11. **Guard against the siren's song.** Before any optimization pass, confirm it adds no new meaning. If performance matters, buy headroom externally rather than complicating the kernel.

## Sources

**Primary — fetched and read in full this session:** Kay, *Early History of Smalltalk* (HOPL-II 1993), worrydream.com mirror · Warth & Kay, *Worlds*, VPRI RN-2008-001 · Warth, Ohshima, Kaehler, Kay, *Worlds*, ECOOP 2011 (tr2011001, 26 pp; commit/serializability quoted verbatim) · Smith, Kay, Raab, Reed, *Croquet: A Collaboration System Architecture*, VPRI TR-2003-002 (determinism clauses quoted verbatim) · *STEPS 2012 Final Report*, VPRI TR-2012-001 (52 pp; read via font-encoding recovery) · *STEPS 2007 first-year report*, VPRI TR-2007-008 (targeted) · Goldstein & Bobrow, *PIE*, Xerox PARC CSL-81-3 (58 pp via OCR; layer/context/closure/merge quoted).

**Verified via targeted fetch/search:** Squeak wiki (.changes /49, ChangeSet /674, Monticello /1287); news.squeak.org "Recovering from Frozen Images" (2024) · croquet.io/multisynq, docs.multisynq.io (Croquet network deprecated 2025-07-30) · Reed MIT/LCS/TR-205 located; TOCS 1983 companion · OOPSLA 1997 transcripts (tinlizzie.org IA, moryton.blogspot.com, archive.org video) · Kay Quora on LLMs; REBASE/SPLASH 2024 interview listing.

**Labeled guesses / memory-only:** Maru ~1,750 LOC; ENVY specifics; exact wording of the 1998 "messaging" email and "Internet never stopped"; PIE's end tied to Goldstein's departure; CDG/HARC wind-down ~2018; Croquet late-joiner wording.
