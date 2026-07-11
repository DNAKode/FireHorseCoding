using System.Globalization;
using KodePorter.Core.Model;
using Microsoft.Data.Sqlite;

namespace KodePorter.Core.Store;

/// <summary>
/// The map store: kpmap.db, schema per CONTRACT.md §2. Owns the sqlite connection for
/// one workspace's map database.
/// </summary>
public sealed class MapStore : IDisposable
{
    private readonly SqliteConnection _connection;

    public string DbPath { get; }

    public MapStore(string dbPath)
    {
        DbPath = dbPath;
        string? dir = Path.GetDirectoryName(Path.GetFullPath(dbPath));
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        _connection = new SqliteConnection($"Data Source={dbPath}");
        _connection.Open();
        EnsureSchema();
    }

    private void EnsureSchema()
    {
        using (var cmd = _connection.CreateCommand())
        {
            cmd.CommandText = """
                CREATE TABLE IF NOT EXISTS basis (
                    id TEXT PRIMARY KEY,
                    side TEXT NOT NULL CHECK(side IN ('source','target')),
                    label TEXT NOT NULL,
                    root TEXT NOT NULL,
                    tree_hash TEXT NOT NULL,
                    toolchain TEXT,
                    analyzer TEXT,
                    created TEXT NOT NULL
                );

                CREATE TABLE IF NOT EXISTS entity (
                    id TEXT NOT NULL,
                    basis_id TEXT NOT NULL REFERENCES basis(id),
                    kind TEXT NOT NULL,
                    name TEXT NOT NULL,
                    symbol_path TEXT NOT NULL,
                    file TEXT NOT NULL,
                    start_line INTEGER NOT NULL,
                    end_line INTEGER NOT NULL,
                    content_hash TEXT NOT NULL,
                    parent_id TEXT,
                    resolution TEXT NOT NULL DEFAULT 'clean',
                    is_test INTEGER NOT NULL DEFAULT 0,
                    PRIMARY KEY (id, basis_id)
                );

                CREATE TABLE IF NOT EXISTS continuity_candidate (
                    basis_from TEXT NOT NULL,
                    basis_to TEXT NOT NULL,
                    from_id TEXT NOT NULL,
                    to_id TEXT NOT NULL,
                    heuristic TEXT NOT NULL,
                    status TEXT NOT NULL DEFAULT 'candidate'
                );
                """;
            cmd.ExecuteNonQuery();
        }

        // CONTRACT-M15.md §1.1: "optional-with-defaults so existing workspaces ... keep working
        // unchanged" — a kpmap.db created before this increment has an `entity` table without
        // the two new columns; `CREATE TABLE IF NOT EXISTS` above is a no-op against it, so add
        // them defensively via ALTER TABLE (idempotent: skipped once present).
        MigrateEntityColumnsIfMissing();
    }

    private void MigrateEntityColumnsIfMissing()
    {
        var existingColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using (var pragma = _connection.CreateCommand())
        {
            pragma.CommandText = "PRAGMA table_info(entity);";
            using var reader = pragma.ExecuteReader();
            while (reader.Read())
                existingColumns.Add(reader.GetString(1));
        }

        if (!existingColumns.Contains("resolution"))
        {
            using var alter = _connection.CreateCommand();
            alter.CommandText = "ALTER TABLE entity ADD COLUMN resolution TEXT NOT NULL DEFAULT 'clean';";
            alter.ExecuteNonQuery();
        }
        if (!existingColumns.Contains("is_test"))
        {
            using var alter = _connection.CreateCommand();
            alter.CommandText = "ALTER TABLE entity ADD COLUMN is_test INTEGER NOT NULL DEFAULT 0;";
            alter.ExecuteNonQuery();
        }
    }

    /// <summary>Inserts a basis row. Idempotent: a basis with the same content-addressed id is a no-op.</summary>
    public void InsertBasis(Basis basis)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            INSERT OR IGNORE INTO basis (id, side, label, root, tree_hash, toolchain, analyzer, created)
            VALUES ($id, $side, $label, $root, $treeHash, $toolchain, $analyzer, $created);
            """;
        cmd.Parameters.AddWithValue("$id", basis.Id);
        cmd.Parameters.AddWithValue("$side", basis.Side.ToWireString());
        cmd.Parameters.AddWithValue("$label", basis.Label);
        cmd.Parameters.AddWithValue("$root", basis.Root);
        cmd.Parameters.AddWithValue("$treeHash", basis.TreeHash);
        cmd.Parameters.AddWithValue("$toolchain", (object?)basis.Toolchain ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$analyzer", (object?)basis.Analyzer ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$created", basis.Created.ToString("O", CultureInfo.InvariantCulture));
        cmd.ExecuteNonQuery();
    }

    public Basis? GetBasis(string id)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT id, side, label, root, tree_hash, toolchain, analyzer, created FROM basis WHERE id = $id;";
        cmd.Parameters.AddWithValue("$id", id);
        using var reader = cmd.ExecuteReader();
        return reader.Read() ? ReadBasis(reader) : null;
    }

    /// <summary>Finds a basis by (side, label). If more than one exists (re-pin under the same
    /// label with different content), the most recently created one is returned.</summary>
    public Basis? FindBasis(BasisSide side, string label)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            SELECT id, side, label, root, tree_hash, toolchain, analyzer, created
            FROM basis WHERE side = $side AND label = $label
            ORDER BY created DESC, id ASC
            LIMIT 1;
            """;
        cmd.Parameters.AddWithValue("$side", side.ToWireString());
        cmd.Parameters.AddWithValue("$label", label);
        using var reader = cmd.ExecuteReader();
        return reader.Read() ? ReadBasis(reader) : null;
    }

    /// <summary>All bases for a side, ordered by created then id (oldest first).</summary>
    public IReadOnlyList<Basis> ListBases(BasisSide side)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            SELECT id, side, label, root, tree_hash, toolchain, analyzer, created
            FROM basis WHERE side = $side
            ORDER BY created ASC, id ASC;
            """;
        cmd.Parameters.AddWithValue("$side", side.ToWireString());
        using var reader = cmd.ExecuteReader();
        var result = new List<Basis>();
        while (reader.Read())
            result.Add(ReadBasis(reader));
        return result;
    }

    private static Basis ReadBasis(SqliteDataReader reader) => new(
        Id: reader.GetString(0),
        Side: BasisSideExtensions.ParseWireString(reader.GetString(1)),
        Label: reader.GetString(2),
        Root: reader.GetString(3),
        TreeHash: reader.GetString(4),
        Toolchain: reader.IsDBNull(5) ? null : reader.GetString(5),
        Analyzer: reader.IsDBNull(6) ? null : reader.GetString(6),
        Created: DateTimeOffset.Parse(reader.GetString(7), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind));

    /// <summary>
    /// Inserts entity rows for a basis. Writes in sorted order (id, then symbol_path) as
    /// required by the import-determinism drill (CONTRACT.md §2); re-running the same import
    /// (same ids) is idempotent via INSERT OR REPLACE.
    /// </summary>
    public void InsertEntities(string basisId, IEnumerable<Entity> entities)
    {
        var sorted = entities
            .OrderBy(e => e.Id, StringComparer.Ordinal)
            .ThenBy(e => e.SymbolPath, StringComparer.Ordinal)
            .ToList();

        using var transaction = _connection.BeginTransaction();
        using var cmd = _connection.CreateCommand();
        cmd.Transaction = transaction;
        cmd.CommandText = """
            INSERT OR REPLACE INTO entity
                (id, basis_id, kind, name, symbol_path, file, start_line, end_line, content_hash, parent_id, resolution, is_test)
            VALUES
                ($id, $basisId, $kind, $name, $symbolPath, $file, $startLine, $endLine, $contentHash, $parentId, $resolution, $isTest);
            """;
        var pId = cmd.Parameters.Add("$id", SqliteType.Text);
        var pBasisId = cmd.Parameters.Add("$basisId", SqliteType.Text);
        var pKind = cmd.Parameters.Add("$kind", SqliteType.Text);
        var pName = cmd.Parameters.Add("$name", SqliteType.Text);
        var pSymbolPath = cmd.Parameters.Add("$symbolPath", SqliteType.Text);
        var pFile = cmd.Parameters.Add("$file", SqliteType.Text);
        var pStartLine = cmd.Parameters.Add("$startLine", SqliteType.Integer);
        var pEndLine = cmd.Parameters.Add("$endLine", SqliteType.Integer);
        var pContentHash = cmd.Parameters.Add("$contentHash", SqliteType.Text);
        var pParentId = cmd.Parameters.Add("$parentId", SqliteType.Text);
        var pResolution = cmd.Parameters.Add("$resolution", SqliteType.Text);
        var pIsTest = cmd.Parameters.Add("$isTest", SqliteType.Integer);

        foreach (var e in sorted)
        {
            if (e.BasisId != basisId)
                throw new ArgumentException($"Entity '{e.Id}' has basisId '{e.BasisId}' but was passed to InsertEntities for basis '{basisId}'.", nameof(entities));

            pId.Value = e.Id;
            pBasisId.Value = e.BasisId;
            pKind.Value = e.Kind;
            pName.Value = e.Name;
            pSymbolPath.Value = e.SymbolPath;
            pFile.Value = e.File;
            pStartLine.Value = e.StartLine;
            pEndLine.Value = e.EndLine;
            pContentHash.Value = e.ContentHash;
            pParentId.Value = (object?)e.ParentId ?? DBNull.Value;
            pResolution.Value = e.Resolution;
            pIsTest.Value = e.IsTest ? 1 : 0;
            cmd.ExecuteNonQuery();
        }

        transaction.Commit();
    }

    /// <summary>All entities for a basis, ordered by id then symbol_path (deterministic dump order).</summary>
    public IReadOnlyList<Entity> GetEntities(string basisId)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            SELECT id, basis_id, kind, name, symbol_path, file, start_line, end_line, content_hash, parent_id, resolution, is_test
            FROM entity WHERE basis_id = $basisId
            ORDER BY id, basis_id;
            """;
        cmd.Parameters.AddWithValue("$basisId", basisId);
        using var reader = cmd.ExecuteReader();
        var result = new List<Entity>();
        while (reader.Read())
            result.Add(ReadEntity(reader));
        return result;
    }

    /// <summary>
    /// The full entity table dump, `SELECT * ORDER BY id, basis_id` (CONTRACT.md §2's exact
    /// regeneration-drill query) — used to compare two independently populated map stores.
    /// </summary>
    public IReadOnlyList<Entity> DumpAllEntities()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            SELECT id, basis_id, kind, name, symbol_path, file, start_line, end_line, content_hash, parent_id, resolution, is_test
            FROM entity
            ORDER BY id, basis_id;
            """;
        using var reader = cmd.ExecuteReader();
        var result = new List<Entity>();
        while (reader.Read())
            result.Add(ReadEntity(reader));
        return result;
    }

    private static Entity ReadEntity(SqliteDataReader reader) => new(
        Id: reader.GetString(0),
        BasisId: reader.GetString(1),
        Kind: reader.GetString(2),
        Name: reader.GetString(3),
        SymbolPath: reader.GetString(4),
        File: reader.GetString(5),
        StartLine: reader.GetInt32(6),
        EndLine: reader.GetInt32(7),
        ContentHash: reader.GetString(8),
        ParentId: reader.IsDBNull(9) ? null : reader.GetString(9),
        Resolution: reader.GetString(10),
        IsTest: reader.GetInt64(11) != 0);

    // ---- continuity_candidate (CONTRACT-M15.md §1.2) -----------------------------------------

    /// <summary>Inserts continuity-candidate rows, sorted deterministically. Never de-duplicates
    /// against pre-existing rows (Advance appends a fresh candidate set per run; the table is a
    /// log of suggestions, not a set keyed by pair).</summary>
    public void InsertContinuityCandidates(IEnumerable<ContinuityCandidate> candidates)
    {
        var sorted = candidates
            .OrderBy(c => c.BasisFrom, StringComparer.Ordinal)
            .ThenBy(c => c.BasisTo, StringComparer.Ordinal)
            .ThenBy(c => c.FromId, StringComparer.Ordinal)
            .ThenBy(c => c.ToId, StringComparer.Ordinal)
            .ToList();
        if (sorted.Count == 0)
            return;

        using var transaction = _connection.BeginTransaction();
        using var cmd = _connection.CreateCommand();
        cmd.Transaction = transaction;
        cmd.CommandText = """
            INSERT INTO continuity_candidate (basis_from, basis_to, from_id, to_id, heuristic, status)
            VALUES ($basisFrom, $basisTo, $fromId, $toId, $heuristic, $status);
            """;
        var pBasisFrom = cmd.Parameters.Add("$basisFrom", SqliteType.Text);
        var pBasisTo = cmd.Parameters.Add("$basisTo", SqliteType.Text);
        var pFromId = cmd.Parameters.Add("$fromId", SqliteType.Text);
        var pToId = cmd.Parameters.Add("$toId", SqliteType.Text);
        var pHeuristic = cmd.Parameters.Add("$heuristic", SqliteType.Text);
        var pStatus = cmd.Parameters.Add("$status", SqliteType.Text);

        foreach (var c in sorted)
        {
            pBasisFrom.Value = c.BasisFrom;
            pBasisTo.Value = c.BasisTo;
            pFromId.Value = c.FromId;
            pToId.Value = c.ToId;
            pHeuristic.Value = c.Heuristic;
            pStatus.Value = c.Status;
            cmd.ExecuteNonQuery();
        }

        transaction.Commit();
    }

    /// <summary>Continuity candidates, optionally scoped to one (basisFrom, basisTo) pair,
    /// deterministically ordered.</summary>
    public IReadOnlyList<ContinuityCandidate> GetContinuityCandidates(string? basisFrom = null, string? basisTo = null)
    {
        using var cmd = _connection.CreateCommand();
        if (basisFrom is not null && basisTo is not null)
        {
            cmd.CommandText = """
                SELECT basis_from, basis_to, from_id, to_id, heuristic, status FROM continuity_candidate
                WHERE basis_from = $basisFrom AND basis_to = $basisTo
                ORDER BY basis_from, basis_to, from_id, to_id;
                """;
            cmd.Parameters.AddWithValue("$basisFrom", basisFrom);
            cmd.Parameters.AddWithValue("$basisTo", basisTo);
        }
        else
        {
            cmd.CommandText = """
                SELECT basis_from, basis_to, from_id, to_id, heuristic, status FROM continuity_candidate
                ORDER BY basis_from, basis_to, from_id, to_id;
                """;
        }
        using var reader = cmd.ExecuteReader();
        var result = new List<ContinuityCandidate>();
        while (reader.Read())
        {
            result.Add(new ContinuityCandidate(
                reader.GetString(0), reader.GetString(1), reader.GetString(2),
                reader.GetString(3), reader.GetString(4), reader.GetString(5)));
        }
        return result;
    }

    public void Dispose()
    {
        _connection.Dispose();
    }
}
