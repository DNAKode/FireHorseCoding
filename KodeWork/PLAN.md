# KodeWork — planning note (2026-08-23)

Status: Slice Zero is live (2026-08-31). Source landed in this repo 2026-09-25.
Host: `https://koderbot.dnakode.com/kodework` (Caddy basicauth). CLI: `kw`. Ledger: `~/var/kodework/gneiss.db`. Organ: private `DNAKode/KodeWorkData`.
KodeWork is the smallest useful Gneiss domain: a shared, hierarchical operational scratchpad.

## 1. What it is

KodeWork is the live page at `koderbot.dnakode.com` where Govert and Koderbot keep the Koderbot-universe board: VPCs, projects, work items, reminders, ideas, and the things we are juggling. It sits *above* per-project trackers (GitHub issues, KodePorter, CompSeek, etc.). Those remain the organs for their own work. KodeWork remembers the overlay: what exists, what it is for, what is open, and what changed.

It should feel like a fluid outline / wiki, not a ticket product.

- Initially text.
- Later images and other media.
- Both humans and the agent edit the same tree.
- History, correction, and “why do we believe this?” come from Gneiss, not from a second ad-hoc database.

## 2. Why Gneiss, and how little of it

Gneiss already has a working cell in this repo (`Gneiss/src/Gneiss.Cell`). KodePorter is the demanding first realization. KodeWork is the *minimal* second-domain example:

| Gneiss piece | KodeWork use |
|---|---|
| Entity | every node (`kw:<id>`) |
| Assertion | title, body, kind, status, parent, order, due, tags, later media |
| Transaction | one edit / one import / one decision |
| Justification | optional links to GitHub issues, other items, imported git blobs |
| Context | `kw-current` now; later `kw-as-of` |
| `note` inbox | capture without ceremony |
| `decide` | accept a proposal, retract a wrong title, supersede a parent |
| Label / `why()` | one gesture from any node |
| Evidence organ | private git repo `DNAKode/KodeWorkData` |

We stay at adoption layer **A2** (embed the library) and modeling rings **B1–B3** (items, corrections, a little history). No federation, no seals, no sprout/commit, no incremental engine.

The Gneiss contract stays intact:

```
current tree = evaluate(recorded history, declared context)
```

Edits append. They do not rewrite the ledger. The page shows the current belief view.

## 3. Recommended shape

```
browser / agent
    │
    ▼
KodeWork.Web  (ASP.NET on .NET 10, Kestrel localhost)
    │  domain verbs only
    ▼
KodeWork.Core  (predicates, tree fold, git projection)
    │
    ├── Gneiss.Cell  (SQLite ledger, single writer)
    └── KodeWorkData (private git repo = text organ + projection)
```

Host routing (today the whole host goes to OpenClaw on `:18789`):

```
koderbot.dnakode.com/           → OpenClaw
koderbot.dnakode.com/kodework*  → KodeWork Kestrel
```

Recommended default URL: `https://koderbot.dnakode.com/kodework`.

### Repo layout (implementation lives in FireHorseCoding)

```
KodeWork/
  PLAN.md                 ← this file
  README.md
  CONTRACT.md             ← domain predicates + verbs, once we freeze v0
  src/KodeWork.Core/
  src/KodeWork.Web/
  src/KodeWork.Cli/
  tests/KodeWork.Tests/
```

Add the projects to `FireHorseCoding.slnx`. Do not put live work data in this public repo.

### Data repo (private): `DNAKode/KodeWorkData`

Text representation of the *current belief*, plus enough to rebuild:

```
README.md
items/<id>.md             # one file per entity; id is identity, not path
tree.md                   # generated outline of the current view
media/<sha256-prefix>/    # later
ledger/export.jsonl       # Gneiss.Cell export, rebuildable
ledger/HIGHWATER          # last projected tx
```

Item file:

```markdown
---
id: kw:01j8example
kind: task
status: open
parent: kw:01j8parent
order: 30
due: 2026-08-30
tags: [infra]
---

# Short title

Body. Scratch text is allowed. This is the current belief, not the history.
```

Identity is the `id`. Folder trees are *not* identity. Moving a node changes `parent` / `order` claims; the filename stays the id. That is the Senzing lesson from Gneiss: the overlay is only as durable as its key.

## 4. Domain vocabulary (v0, small on purpose)

Entities: `kw:<ulid>`.

| Predicate | Value | Notes |
|---|---|---|
| `kw.title` | text | required |
| `kw.body` | text | optional, markdown |
| `kw.kind` | text | `area` `project` `task` `reminder` `idea` `vpc` `note` `link` |
| `kw.status` | text | kind-dependent; start with `open` `doing` `blocked` `done` `dropped` `running` `paused` |
| `kw.parent` | entity | null = root |
| `kw.order` | number | sibling sort |
| `kw.due` | text | ISO date; enough for reminders in v0 |
| `kw.tag` | text | repeatable; later we may move to a set value |
| `kw.ref` | text | “also tracked in …” URL or `github:DNAKode/CompSeek#12` |

Contexts:

- `kw-current` — dataCut = latest, admit decided-only (direct human/agent writes are `fact`; imported guesses can be `proposed`).

Actors:

- `govert`
- `koderbot`
- `kw-import` for git-imported testimony

Verbs the UI/CLI expose (mapped onto Gneiss):

| User action | Gneiss |
|---|---|
| add child | `record` title/kind/parent/order |
| edit title/body | `record` new assertion (later tx wins) or `supersede` if we want an explicit scar |
| move / indent | `record` new parent+order |
| mark done | `record` status |
| capture dump | `note` (inbox) |
| promote a note | `record` as item |
| “this was wrong” | `retract` + replacement |
| “I disagree” | `proposed` rival + talk surface |

v0 can let later-tx win on the same claim key for ordinary edits, and keep explicit retract/supersede for corrections we care about. That keeps the outline fluid.

## 5. Two stores, one writer

This is the discussion the request called out.

**Recommendation: the ledger is the write path; git is the organ and the human-readable projection.**

Why:

- Gneiss is append-only and single-writer. Git is neither (amend, rebase, merge).
- Two humans + one agent can serialize through one host process. They cannot serialize through two sources of truth.
- The private repo still gives us: readable files, clone anywhere, PR-shaped review later, backup, and a place media can live.
- `ExportLedgerJsonl()` already exists; the data repo can carry a rebuildable ledger dump.

Write cycle:

1. Actor edits in the page or CLI.
2. `KodeWork.Core` appends one Gneiss transaction.
3. Recompute `kw-current`.
4. Rewrite the affected `items/*.md` and `tree.md`.
5. Commit in KodeWorkData with the transaction reason.
6. Push when the host can (credentials via git, not via chat).

Import cycle (v1, not v0 unless we need it immediately):

- If someone edits files and pushes, `kw import` reads commits after `HIGHWATER`, records them as testimony from `kw-import`, and the next belief fold absorbs them.
- Path/filename changes do not change identity.
- Conflicting file edits vs live ledger become `contested` or later-tx wins, visibly.

v0 may refuse freehand file edits and say “edit through the page/CLI.” That is an honest stop rule, not a failure.

The SQLite file itself can live *beside* the clone (`/home/koderbot/var/kodework/gneiss.db`) or inside a gitignored path of KodeWorkData. Do not commit the live DB; commit the jsonl export.

## 6. Page

Presentation philosophy is Gneiss kb/34 (wiki doors), not a ticket kanban.

Front page:

- Fluid outline. Expand/collapse. Indent/outdent. Add sibling/child. Inline title edit.
- Filter by kind / status / tag / due.
- Quiet footer: context `kw-current`, high-water tx, result hash.

One gesture away:

- Body editor
- History / diff
- `why()` reveal
- Talk (contested + notes)

Implementation taste for v0:

- ASP.NET Minimal APIs + server-rendered HTML + a little vanilla JS or htmx.
- No SPA framework. No separate node build.
- Matches the Gneiss “no server UI for the Lens” spirit *for the ledger*, while still being a live editor for the domain.

Auth is not optional. The host is on the public internet. OpenClaw already occupies `/`. KodeWork needs its own perimeter (Caddy forward_auth / basicauth, or a shared cookie). Soft security inside that perimeter: attributed, reversible edits.

## 7. Agent use

Koderbot should treat KodeWork as the standing universe board.

- CLI: `kw tree`, `kw show <id>`, `kw add`, `kw set`, `kw note`, `kw why`.
- Same Core library as the web app.
- Heartbeats can later scan `kw.due` and inbox notes. Not in v0 unless it falls out cheaply.

Do not scrape the HTML. The CLI/HTTP API is the agent surface.

## 8. Host / toolchain facts (measured 2026-08-23)

- FireHorseCoding cloned to `/home/koderbot/projects/FireHorseCoding` (public, `main`).
- `global.json` wants SDK `10.0.301`. `dotnet` is **not** installed on `dna-koderbot`.
- Caddy terminates TLS for `koderbot.dnakode.com` and reverse-proxies everything to `127.0.0.1:18789`.
- `DNAKode/KodeWorkData` does not exist yet.
- gh is authenticated as `dnakoderbot`.

## 9. Slice Zero — smallest thing that is KodeWork

A weekend-sized cell, judged by using it, not by feature count.

1. Install .NET 10 SDK (pin via existing `global.json`).
2. Create `KodeWork.Core` on `Gneiss.Cell`: init ledger, declare `kw.*` + `kw-current`.
3. Tree operations + tests (add, rename, reparent, complete, retract; current view is a tree; old title still replayable under its receipt).
4. Project to markdown files.
5. `KodeWork.Cli` for agent and for us.
6. `KodeWork.Web` outline page.
7. Caddy path + auth.
8. Create private `KodeWorkData`, seed a few real nodes (this host, Outlook connector, CompSeek, KodeWork itself).
9. Golden ledger + a tiny static snapshot page (Gneiss Lens-style) so the first week is inspectable.

Stop after that. Media, reminders engine, git import, and fancy filters wait until the page is actually the place we look.

## 10. Success / kill

Succeeds if we start putting real juggling into it within a week, and a later session can answer “what is going on, and why is this marked running?” without rereading chat logs.

Kill or descope if:

- we end up maintaining the tree by hand in git *and* in the page;
- ordinary markdown + git already does the job and Gneiss is ceremony;
- the outline is too stiff to scratch in;
- the public URL is too awkward to auth, so nobody opens it.

## 11. Open decisions

Defaults in **bold**. Change these before code if they are wrong.

1. URL: **`/kodework` on koderbot.dnakode.com** vs a subdomain.
2. Auth: **Caddy basicauth or existing host SSO**, not an anonymous page.
3. Source of truth: **ledger writes, git projection**; file import is v1.
4. Item identity: **stable `kw:<ulid>` files**, not path-as-id.
5. Create `DNAKode/KodeWorkData` as a **private** repo under the org, yes.
6. First kinds: **area / project / task / reminder / idea / vpc / note / link**.
7. Ordinary edits: **later tx wins**; explicit retract for “that was wrong.”
8. Steward for this domain: **Govert**; Koderbot is a first-class actor, not a hidden writer.

## 12. What this planning pass did not do

- No .NET install.
- No repo created.
- No Caddy edit.
- No code.
- This file is local until someone chooses to commit it.
