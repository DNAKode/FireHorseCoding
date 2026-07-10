namespace Gneiss.Cell.Internal;

/// <summary>DDL per CONTRACT.md section 1. Six base relations are append-only, enforced by triggers.</summary>
internal static class Schema
{
    internal const string Ddl = """
        CREATE TABLE tx (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            wall TEXT NOT NULL,
            actor TEXT NOT NULL,
            reason TEXT NOT NULL,
            batch TEXT
        ) STRICT;

        CREATE TABLE assrt (
            aid TEXT PRIMARY KEY,
            tx INTEGER NOT NULL REFERENCES tx(id),
            subj TEXT NOT NULL,
            pred TEXT NOT NULL,
            val TEXT NOT NULL,
            valkind TEXT NOT NULL CHECK(valkind IN ('text','number','bool','entity','json')),
            vfrom TEXT,
            vto TEXT,
            status TEXT NOT NULL CHECK(status IN ('fact','proposed')),
            src TEXT,
            meth TEXT,
            conf INTEGER,
            ckey TEXT NOT NULL
        ) STRICT;

        CREATE TABLE just (
            aid TEXT NOT NULL,
            input_aid TEXT,
            rule_ver TEXT,
            role TEXT
        ) STRICT;

        CREATE TABLE dec (
            aid TEXT PRIMARY KEY,
            kind TEXT NOT NULL CHECK(kind IN ('accepts','rejects','retracts','supersedes')),
            tgt_aid TEXT,
            tgt_ckey TEXT
        ) STRICT;

        CREATE TABLE cov (
            region TEXT,
            scope TEXT,
            state TEXT,
            seal_aid TEXT
        ) STRICT;

        CREATE TABLE seal (
            seal_aid TEXT,
            region TEXT,
            winner_ckey TEXT,
            winner_val TEXT,
            defeated_ckey TEXT
        ) STRICT;

        CREATE TABLE receipt (
            id TEXT PRIMARY KEY,
            question TEXT NOT NULL,
            ctx_name TEXT NOT NULL,
            ctx_hash TEXT NOT NULL,
            data_cut INTEGER NOT NULL,
            def_cut INTEGER NOT NULL,
            consumed TEXT NOT NULL,
            result_hash TEXT NOT NULL,
            created_wall TEXT NOT NULL
        );

        CREATE TABLE note (
            id TEXT PRIMARY KEY,
            wall TEXT NOT NULL,
            actor TEXT NOT NULL,
            text TEXT NOT NULL,
            promoted_aid TEXT
        );

        CREATE INDEX idx_assrt_tx ON assrt(tx);
        CREATE INDEX idx_assrt_subj_pred ON assrt(subj, pred);
        CREATE INDEX idx_assrt_ckey ON assrt(ckey);
        CREATE INDEX idx_just_aid ON just(aid);
        CREATE INDEX idx_dec_tgt_aid ON dec(tgt_aid);
        CREATE INDEX idx_dec_tgt_ckey ON dec(tgt_ckey);

        CREATE TRIGGER tx_no_update BEFORE UPDATE ON tx BEGIN SELECT RAISE(ABORT, 'append-only: tx'); END;
        CREATE TRIGGER tx_no_delete BEFORE DELETE ON tx BEGIN SELECT RAISE(ABORT, 'append-only: tx'); END;

        CREATE TRIGGER assrt_no_update BEFORE UPDATE ON assrt BEGIN SELECT RAISE(ABORT, 'append-only: assrt'); END;
        CREATE TRIGGER assrt_no_delete BEFORE DELETE ON assrt BEGIN SELECT RAISE(ABORT, 'append-only: assrt'); END;

        CREATE TRIGGER just_no_update BEFORE UPDATE ON just BEGIN SELECT RAISE(ABORT, 'append-only: just'); END;
        CREATE TRIGGER just_no_delete BEFORE DELETE ON just BEGIN SELECT RAISE(ABORT, 'append-only: just'); END;

        CREATE TRIGGER dec_no_update BEFORE UPDATE ON dec BEGIN SELECT RAISE(ABORT, 'append-only: dec'); END;
        CREATE TRIGGER dec_no_delete BEFORE DELETE ON dec BEGIN SELECT RAISE(ABORT, 'append-only: dec'); END;

        CREATE TRIGGER cov_no_update BEFORE UPDATE ON cov BEGIN SELECT RAISE(ABORT, 'append-only: cov'); END;
        CREATE TRIGGER cov_no_delete BEFORE DELETE ON cov BEGIN SELECT RAISE(ABORT, 'append-only: cov'); END;

        CREATE TRIGGER seal_no_update BEFORE UPDATE ON seal BEGIN SELECT RAISE(ABORT, 'append-only: seal'); END;
        CREATE TRIGGER seal_no_delete BEFORE DELETE ON seal BEGIN SELECT RAISE(ABORT, 'append-only: seal'); END;
        """;
}
