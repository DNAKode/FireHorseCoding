# KodeWork

A fluid hierarchical board built as a **minimal Gneiss domain**: items are entities, edits are assertions, the current tree is a belief view.

This directory is the **public, forkable system** — code, contract, and ideas. It lives in [FireHorseCoding](https://github.com/DNAKode/FireHorseCoding/tree/main/KodeWork).

**Do not put an organization's live board, host names, project inventory, or hosting config in this tree.** That belongs in a private data repo you point at with `KODEWORK_DATA`. The ledger (SQLite) stays beside the process; git holds the readable projection.

See [PLAN.md](PLAN.md) and [CONTRACT.md](CONTRACT.md).

## Run

.NET 10 SDK. From the FireHorseCoding repo root:

```bash
export KODEWORK_HOME=$HOME/var/kodework          # sqlite ledger + lock
export KODEWORK_DATA=$HOME/path/to/private-data  # optional git organ
export KODEWORK_ACTOR=local

dotnet run --project KodeWork/src/KodeWork.Cli -- init
dotnet run --project KodeWork/src/KodeWork.Cli -- seed   # Acme demo, only if the board is empty
dotnet run --project KodeWork/src/KodeWork.Cli -- tree
dotnet run --project KodeWork/src/KodeWork.Web            # http://127.0.0.1:18790/kodework/
```

`kw seed` will not run if the board already has roots. Put reverse-proxy, TLS, and auth in *your* host config, not in this repo.

## Layout

```
KodeWork/
  PLAN.md                 design
  CONTRACT.md             predicates + verbs
  src/KodeWork.Core/
  src/KodeWork.Web/
  src/KodeWork.Cli/
  tests/KodeWork.Tests/
```
