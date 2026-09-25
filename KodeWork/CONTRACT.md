# KodeWork v0 contract

Domain over `Gneiss.Cell`. Current tree = `Ask("kw-current", ask-all)`.

## Predicates

| Predicate | Value | Notes |
|---|---|---|
| `kw.title` | text | required for an item to appear |
| `kw.body` | text | markdown |
| `kw.kind` | text | `area` `project` `task` `reminder` `idea` `vpc` `note` `link` |
| `kw.status` | text | `open` `doing` `blocked` `done` `dropped` `running` `paused` |
| `kw.parent` | entity | omitted = root |
| `kw.order` | number | sibling sort |
| `kw.due` | text | ISO date |
| `kw.tags` | json string array | one assertion, not repeated `kw.tag` (claim-key collision) |
| `kw.refs` | json string array | |

Subjects: `kw:` + ULID. Ordinary edits append a new assertion on the same claim key; later tx wins. Explicit retract uses `DecisionKind.Retracts` by claim key.

## Verbs

`init` `add` `set` `note` `ask/tree` `why` `project`

Actors are caller-chosen strings. Ledger is the write path. Git (`KODEWORK_DATA`) is the projection organ — keep that repo private; this contract is public system code.
