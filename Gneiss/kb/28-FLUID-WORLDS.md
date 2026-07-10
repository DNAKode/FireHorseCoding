# Fluid Worlds: Bulk Materials as the Spine's Hardest Test

*Added 2026-07-04 from discussion. Govert: much of the real work is bulk materials — identity of materials, batches, provenance is hard because it is literally and conceptually fluid; loads flow together and apart; similar in chemical and food manufacturing. Not a question about the spine per se, but a test: can the spine underlie different entity concepts and styles of world modelling?*

## 1. The trap, named

The failure mode would be forcing classical object-persistence onto stuff: "Batch 47" as an entity with stable identity, mutated as material flows. Everything downstream of that mistake (merge handling, partial transfers, recursive blends) becomes special-case misery — because the *world model* is wrong, not the machinery.

The spine can avoid the trap because it deliberately contains **no ontology of the world**. It commits only to: identity handles are cheaply mintable; claims about anything addressable are recordable; groupings are derivable under policy; interpretations are contextual. Object-persistence is one *stance library*, not the spine. Fluid domains get a different one.

## 2. The stuff-and-flow stance library

**Portions.** Entities minted at observation and custody boundaries — a delivery load, the contents of bin 4 during a residence window, an outbound lot. A portion never changes; when material moves or mixes, *new portions* are minted. (Entities are cheap; minting freely is the kernel working as designed.)

**Flow events.** Transfers, merges, splits, transformations, consumptions are n-ary — a merge has multiple inputs. The standard reification answer applies: the flow is an *event-entity* with participation assertions: `input(E, PortionA, 12.4t ±0.2)`, `input(E, PortionB, 5.1t ±0.1)`, `output(E, PortionC)`, plus time, actor, instrument. Events earn entity status here (we kept Event out of the kernel; nothing stops it being a stance) because flows are genuinely things: they happened, at a time, measured by an instrument that has calibrations — which slots straight into the existing correction machinery.

**Lots as derived clusters — the reused mechanism.** A "batch" or "lot" is a policy-defined grouping over portions (same production day, same residence window, same recipe run). This is *the identity-cluster mechanism again*, verbatim: pairwise/flow evidence → derived equivalence-class entity → **graduation to pinned identity when the world grabs a handle** (a lot number printed on a bag is the same graduation event as a cluster id bookmarked by a user). Merges and splits are decisions over flow assertions; clusters recompute. One mechanism now covers Smoothscrape person identity and grain lot genealogy — the strongest evidence yet that the spine generalizes across world-modelling styles.

**Composition and quality as derived assertions.** Protein %, moisture, contaminant ppm of a portion are derived through mixing rules (rule versions, justifications, uncertainty propagation) from measured inputs. A lab result on a sample is an observation about a portion; blends propagate.

**Fractional provenance.** "Outbound lot C is ~68% delivery A, 32% delivery B under mixing model M" — quantitative, uncertain, method-attributed. Note: this is the one place the semiring-provenance machinery surveyed in [11-SURVEY-KR-BELIEF.md](11-SURVEY-KR-BELIEF.md) becomes *real* — but as **domain data computed by rules** (fractions are values with justifications), not as engine machinery. The engine still does why-provenance; the mixing math is a method.

**Mixing models as contexts.** Plug-flow vs perfect-mixing vs stratified silo are alternative rule versions — i.e., what-if contexts and the backtest cell, unchanged. Two consequences fall out free: (a) alternative physical interpretations of the same flows coexist without forking data; (b) **allocation restatement** — when a weighbridge calibration correction lands, fractional provenance and quality certs recompute under existing restatement machinery, with the diff report explaining every changed cert.

**Mass balance — the integrity check returns.** The industry survey noted accounting's balancing invariant (debits = credits) as the global check open-world Gneiss lacks. Stuff-and-flow restores it: per-node conservation (inputs = outputs + residual ± measurement tolerance) is a validation rule over flow events. Imbalance is not an error to hide — it is a *typed signal* that mints a hypothesis (unmeasured loss? meter drift? unrecorded flow?) routed to review. Shrinkage analysis becomes belief-engine business.

**Trace cones.** Contamination recall = reachability over the flow graph: forward cone ("which lots could contain material from delivery D") and backward cone ("where could this contaminant have entered"). These are *isomorphic to blast-radius queries over justification graphs* ([25-IMPERFECTION.md](25-IMPERFECTION.md) §6) — same query shape, different edge set. One graph-walk engine serves epistemic damage and physical contamination. The symmetry is not a pun; both are "propagate consequence through recorded dependence."

## 3. Prior art to mine (from domain knowledge, flagged for a later survey pass)

- **GS1 EPCIS 2.0** — supply-chain event standard: ObjectEvent, AggregationEvent, **TransformationEvent** (inputs → outputs with quantities) — an existing public vocabulary remarkably close to flow events; worth aligning names.
- **Hydrocarbon allocation** — the oil & gas practice of allocating commingled pipeline/platform flows to owners under declared allocation rules, *restated when meter recalibrations land*. This is fractional provenance + restatement as a decades-old, contractually-binding industry practice; the strongest existence proof for this whole section.
- **ISA-88/95 batch genealogy** (MES batch records), **FSMA 204** traceability lots (food), pharma serialization — each a partial vocabulary for lots, transformations, and trace obligations.
- Candidate: a fifth research survey (15-SURVEY-MATERIAL-FLOW) before any bulk-domain prototype.

## 4. The test, made operational: probe P-1c

Extend the retrofit-probe pattern ([31-PROTOTYPES.md](31-PROTOTYPES.md) P-1): paper-map one real cycle — grain intake → storage with partial blending → outload with quality certificates (or a chemical/food batch record) — into portions, flow events, derived lots, mixing rules.

**Kill signal:** if the mapping requires *kernel* changes (not merely a new stance library), the spine's world-model-agnosticism claim fails. **Predicted frictions** (position, to be tested): (a) n-ary ergonomics — event-entity reification is verbose; tolerable with library helpers, but watch it; (b) continuous processes produce unbounded micro-flows — expect to need **flow summarization windows**, which is the seal mechanism reused for the object domain (aggregate a shift's continuous transfer into one sealed flow event); (c) uncertainty representation on participation quantities needs a convention early.

## 5. The meta-claim

The spine supports *styles*, plural: object-persistent (persons, sensors), stuff-and-flow (bulk materials), field/continuum (dense series + descriptors already are this), artifact/document (media, configs). Choosing a style per domain = choosing a stance library; the kernel is untouched by the choice. That is the fixed-point property expressed in the world-modelling dimension — and P-1c is how we find out whether it is true or merely elegant.
