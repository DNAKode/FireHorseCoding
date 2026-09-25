# KodeWork — design

KodeWork is the smallest useful Gneiss domain: a shared, hierarchical operational scratchpad.

This file is **system design**. Instance facts (who hosts it, which VPCs exist, org project names, reverse-proxy layout) do not belong here. They belong in the private data repo (`KODEWORK_DATA`).

## 1. What it is

A fluid outline / wiki, not a ticket product. It sits *above* per-project trackers (issues, KodePorter, and so on). Those remain the organs for their own work. KodeWork remembers the overlay: what exists, what it is for, what is open, and what changed.

- Initially text.
- Later images and other media.
- Humans and agents edit the same tree.
- History, correction, and “why do we believe this?” come from Gneiss.

## 2. Why Gneiss, and how little of it

Gneiss already has a working cell in this repo (`Gneiss/src/Gneiss.Cell`). KodePorter is the demanding first realization. KodeWork is the *minimal* second-domain example:

| Gneiss piece | KodeWork use |
|---|---|
| Entity | every node (`kw:<id>`) |
| Assertion | title, body, kind, status, parent, order, due, tags, later media |
| Transaction | one edit / one import / one decision |
| Justification | optional links to issues, other items, imported git blobs |
| Context | `kw-current` now; later `kw-as-of` |
| `note` inbox | capture without ceremony |
| `decide` | accept a proposal, retract a wrong title, supersede a parent |
| Label / `why()` | one gesture from any node |
| Evidence organ | a **private** git repo of your choosing |

Adoption layer **A2** (embed the library) and modeling rings **B1–B3**. No federation, no seals, no sprout/commit, no incremental engine.

```
current tree = evaluate(recorded history, declared context)
```

Edits append. They do not rewrite the ledger.

## 3. Shape

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
    └── KODEWORK_DATA (private git repo = text organ + projection)
```

Suggested public path for a reverse proxy: `/kodework` on whatever host you already run. Auth is **your** perimeter (basic auth, SSO, not anonymous on the open internet). Soft security inside that perimeter: attributed, reversible edits.

### Data repo layout (private)

```
README.md
items/<id>.md             # one file per entity; id is identity, not path
tree.md                   # generated outline of the current view
media/<sha256-prefix>/    # later
ledger/export.jsonl       # Gneiss.Cell export, rebuildable
ledger/HIGHWATER          # last projected tx
```

Identity is the `id`. Folder trees are *not* identity. Moving a node changes `parent` / `order` claims; the filename stays the id.

Do not commit the live SQLite file. Commit the jsonl export.

## 4. Domain vocabulary (v0)

Entities: `kw:<ulid>`.

| Predicate | Value | Notes |
|---|---|---|
| `kw.title` | text | required |
| `kw.body` | text | optional, markdown |
| `kw.kind` | text | `area` `project` `task` `reminder` `idea` `vpc` `note` `link` |
| `kw.status` | text | `open` `doing` `blocked` `done` `dropped` `running` `paused` |
| `kw.parent` | entity | omitted = root |
| `kw.order` | number | sibling sort |
| `kw.due` | text | ISO date |
| `kw.tags` | json string array | one assertion (repeated `kw.tag` would collide on claim key) |
| `kw.refs` | json string array | “also tracked in …” |

Context: `kw-current` (dataCut = latest, admit decided-only).

Actors are strings you choose (`local`, a person, an agent). Git-imported testimony can use `kw-import`.

| User action | Gneiss |
|---|---|
| add child | `record` title/kind/parent/order |
| edit title/body | `record` new assertion (later tx wins) or `supersede` |
| move / indent | `record` new parent+order |
| mark done | `record` status |
| capture dump | `note` |
| “this was wrong” | `retract` + replacement |

Ordinary edits: later tx wins. Explicit retract for “that was wrong.”

## 5. Two stores, one writer

**The ledger is the write path; git is the organ and the human-readable projection.**

Git is a bad belief engine (amend, rebase, two writers). It is a good place to *see* the current board as text and to keep media later.

Write cycle: actor edits → one Gneiss transaction → recompute `kw-current` → rewrite `items/*.md` and `tree.md` → commit in the data repo.

v0 may refuse freehand file edits. Import from git is a later verb.

## 6. Page

Gneiss kb/34 wiki doors, not a kanban. Front page is the outline. One gesture away: body, history, `why()`, talk. ASP.NET Minimal APIs + HTML + a little JS. No SPA build.

## 7. Agent use

CLI: `kw tree`, `kw show`, `kw add`, `kw set`, `kw note`, `kw why`. Same Core as the web app. Do not scrape the HTML.

## 8. Slice Zero

1. `KodeWork.Core` on `Gneiss.Cell`.
2. Tree operations + tests.
3. Markdown projection.
4. CLI + web outline.
5. Optional private data repo.
6. Demo seed (`Acme`) only on an **empty** board.

Media, reminder engine, and git import wait until the page is actually used.

## 9. Success / kill

Succeeds if a later session can answer “what is going on, and why is this marked running?” without rereading chat logs.

Kill or descope if the tree is maintained by hand in git *and* in the page, or if Gneiss is ceremony around ordinary markdown.
