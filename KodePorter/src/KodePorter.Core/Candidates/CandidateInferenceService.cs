using System.Text;
using System.Text.RegularExpressions;
using KodePorter.Core.Domain;
using KodePorter.Core.Model;
using KodePorter.Core.Store;

namespace KodePorter.Core.Candidates;

/// <summary>Summary of one `kp candidates infer` run (CONTRACT-M15.md §2).</summary>
/// <param name="Created">Correspondences created (provenance `candidate`).</param>
/// <param name="Skipped">Source symbols considered but not linked: already covered, no
/// normalized-name match, or a low-confidence multi-match (2-3 candidates; see remarks on
/// <see cref="CandidateInferenceService"/>).</param>
/// <param name="Ambiguous">Source symbols whose normalized name matched more than 3 unclaimed
/// target entities — recorded once each, never linked.</param>
public sealed record CandidateInferenceResult(int Created, int Skipped, IReadOnlyList<string> Ambiguous);

/// <summary>Summary of one `kp candidates infer --heuristic header-citation` run.</summary>
/// <param name="FilesScanned">Distinct target-basis files that exist on disk and were read for
/// a top-of-file header citation (files whose on-disk path is missing — a stale map — are not
/// counted here).</param>
/// <param name="CitationsFound">Individual `*.rs` path tokens extracted from header-citation
/// lines, across all scanned files (one file's header may cite more than one path).</param>
/// <param name="Matched">Of <see cref="CitationsFound"/>, how many normalized to a known source
/// file with a root module entity — whether or not that match went on to produce a new
/// correspondence (see <see cref="Created"/>: an already-covered source or target, or a target
/// file with no eligible primary type, matches without creating).</param>
/// <param name="Created">Correspondences actually created (provenance `candidate`, id
/// `cand-hc-&lt;n&gt;`).</param>
/// <param name="UnmatchedCitedPaths">Cited paths that did not normalize to any known source
/// file, in the order encountered — surfaced so a human can judge whether the grammar needs
/// widening or the source map is missing files.</param>
public sealed record HeaderCitationResult(
    int FilesScanned,
    int CitationsFound,
    int Matched,
    int Created,
    IReadOnlyList<string> UnmatchedCitedPaths);

/// <summary>
/// `kp candidates infer` (CONTRACT-M15.md §2): bootstrap tooling that proposes `maps-to`
/// correspondences with `provenance: candidate` from a purely syntactic name-normalization match
/// — never confirmed by this service, never touching Gneiss (candidates are surfaced in the Atlas
/// for human review; promotion to an asserted, unit-attached correspondence is a separate,
/// human/CLI-driven step).
/// </summary>
public static class CandidateInferenceService
{
    private static readonly HashSet<string> TypeKinds = new(StringComparer.Ordinal) { "struct", "enum", "class", "record" };
    private static readonly HashSet<string> FnKinds = new(StringComparer.Ordinal) { "fn", "method" };

    /// <summary>
    /// Runs the `name-norm` heuristic (CONTRACT-M15.md §2) and, when it creates at least one
    /// candidate, writes the updated correspondences.yaml (existing rows preserved, new
    /// `cand-&lt;n&gt;` rows appended, n continuing past any existing `cand-` ids so re-runs never
    /// collide). Deterministic ordering throughout.
    /// </summary>
    public static CandidateInferenceResult Infer(string workspaceDir, MapStore store, string heuristic = "name-norm")
    {
        ArgumentException.ThrowIfNullOrEmpty(workspaceDir);
        ArgumentNullException.ThrowIfNull(store);
        if (heuristic != "name-norm")
            throw new ArgumentException($"Unknown candidate-inference heuristic '{heuristic}'; only 'name-norm' is supported.", nameof(heuristic));

        var sourceBases = store.ListBases(BasisSide.Source);
        var targetBases = store.ListBases(BasisSide.Target);
        string sourceLabel = sourceBases.Count > 0 ? sourceBases[^1].Label : "";
        string targetLabel = targetBases.Count > 0 ? targetBases[^1].Label : "";
        var sourceEntities = sourceBases.Count > 0 ? store.GetEntities(sourceBases[^1].Id) : [];
        var targetEntities = targetBases.Count > 0 ? store.GetEntities(targetBases[^1].Id) : [];

        var existing = CorrespondencesYaml.Read(workspaceDir);
        var coveredSourceSymbols = existing.Where(c => c.Source is not null).Select(c => c.Source!.SymbolPath).ToHashSet(StringComparer.Ordinal);
        var coveredTargetSymbols = existing.Where(c => c.Target is not null).Select(c => c.Target!.SymbolPath).ToHashSet(StringComparer.Ordinal);

        var typeTargetsByNorm = GroupByNormalizedTarget(targetEntities.Where(e => TypeKinds.Contains(e.Kind)));
        var fnTargetsByNorm = GroupByNormalizedTarget(targetEntities.Where(e => FnKinds.Contains(e.Kind)));

        var eligibleSource = sourceEntities
            .Where(e => TypeKinds.Contains(e.Kind) || FnKinds.Contains(e.Kind))
            .OrderBy(e => e.SymbolPath, StringComparer.Ordinal)
            .ToList();

        int created = 0, skipped = 0;
        var ambiguous = new List<string>();
        var newCorrespondences = new List<Correspondence>();
        int nextCandidateNumber = NextCandidateNumber(existing);

        foreach (var src in eligibleSource)
        {
            if (coveredSourceSymbols.Contains(src.SymbolPath))
            {
                skipped++;
                continue;
            }

            bool isType = TypeKinds.Contains(src.Kind);
            var pool = isType ? typeTargetsByNorm : fnTargetsByNorm;
            string norm = NormalizeSourceSymbolPath(src.SymbolPath);

            if (!pool.TryGetValue(norm, out var matches))
            {
                skipped++;
                continue;
            }

            var unclaimed = matches.Where(m => !coveredTargetSymbols.Contains(m.SymbolPath)).ToList();
            if (unclaimed.Count == 0)
            {
                skipped++;
                continue;
            }

            // CONTRACT-M15.md §2: "a source symbol matching >3 targets is recorded ONCE as
            // ambiguous and NOT linked" — the literal threshold given by the contract. A match
            // count of 2-3 is not confident enough to auto-link either, but the contract only
            // names ">3" as the ambiguous bucket, so those are counted under `skipped`.
            if (unclaimed.Count > 3)
            {
                ambiguous.Add(src.SymbolPath);
                continue;
            }
            if (unclaimed.Count > 1)
            {
                skipped++;
                continue;
            }

            var target = unclaimed[0];
            string id = $"cand-{nextCandidateNumber++}";
            var corr = new Correspondence(
                id, "maps-to", DivergenceKind: null,
                // No unit is known yet at inference time (a candidate precedes unit assignment);
                // left blank pending promotion — see CandidateInferenceServiceTests for the
                // resulting round-trip shape. // DIVERGENCE: CONTRACT.md §4 models `unit` as a
                // required, non-nullable field on every correspondence row; candidates are a
                // pre-unit discovery step the base schema did not anticipate.
                Unit: "",
                Source: new AnchorRef(src.SymbolPath, sourceLabel, src.ContentHash),
                Target: new AnchorRef(target.SymbolPath, targetLabel, target.ContentHash),
                Criterion: null,
                Note: "inferred:name-norm",
                ClaimAid: null,
                Stale: false,
                Provenance: "candidate");

            newCorrespondences.Add(corr);
            coveredSourceSymbols.Add(src.SymbolPath);
            coveredTargetSymbols.Add(target.SymbolPath);
            created++;
        }

        if (newCorrespondences.Count > 0)
            CorrespondencesYaml.Write(workspaceDir, existing.Concat(newCorrespondences).ToList());

        ambiguous.Sort(StringComparer.Ordinal);
        return new CandidateInferenceResult(created, skipped, ambiguous);
    }

    private static int NextCandidateNumber(IReadOnlyList<Correspondence> existing)
    {
        int max = 0;
        foreach (var c in existing)
        {
            if (c.Id.StartsWith("cand-", StringComparison.Ordinal) &&
                int.TryParse(c.Id.AsSpan("cand-".Length), out int n) && n > max)
                max = n;
        }
        return max + 1;
    }

    // ---- header-citation heuristic ------------------------------------------------------------
    //
    // Grammar (surveyed by reading 8-12+ real files under FrankenTui.Net's src/**/*.cs — see the
    // KodePorter bootstrap report for the full tally): a top-of-file `//` comment line starting
    // with either "Port of " or "Upstream source: ", carrying one or more `*.rs` path tokens,
    // e.g.:
    //   // Port of .external/frankentui/crates/ftui-runtime/src/bocpd.rs
    //   // Upstream source: crates/ftui-extras/src/clipboard.rs
    // The path is usually relative to the pinned source root itself (the second example above)
    // but is sometimes additionally prefixed with the vendor-mount directory the tree happens to
    // be checked out under (the first example's leading ".external/frankentui/") — both forms are
    // reconciled by <see cref="MatchSourceFile"/> via path-suffix matching, so no mount-directory
    // name is hardcoded here. An optional preceding "// SPDX-License-Identifier: ..." line and/or
    // a following "// Upstream commit: &lt;sha&gt;" / "// Upstream basis: &lt;sha&gt;" companion
    // pin line are both ignored (a commit hash is not a path). A third, distinct "// Upstream:
    // &lt;text&gt;" form exists in the wild but cites free-form prose or an external
    // (non-source-tree) URL rather than a source-relative path, and deliberately does not match
    // this grammar. In the surveyed tree, only about a third of files carry any header citation
    // at all; the rest are undecorated.
    private static readonly Regex CitationLineRegex = new(@"^//\s*(?:Port of|Upstream source:)\s+(?<rest>.+)$", RegexOptions.Compiled);
    private static readonly Regex RsPathTokenRegex = new(@"[A-Za-z0-9_.\-/]+\.rs\b", RegexOptions.Compiled);
    private const int HeaderLineScanCount = 15;

    /// <summary>
    /// Runs the `header-citation` heuristic: for each target-basis file, reads its first
    /// <see cref="HeaderLineScanCount"/> on-disk lines, extracts cited Rust path(s) per the
    /// grammar above, normalizes and matches each against the source map's files, and — where a
    /// citation matches a source file that has a root module entity — links that root module to
    /// the target file's primary type entity (the largest top-level type declared in that file;
    /// ties broken by earliest declaration, then symbolPath). One-to-one only: like `name-norm`,
    /// a source or target entity already claimed (by an existing correspondence, or by an earlier
    /// citation in this same run) is not linked again. Deterministic ids `cand-hc-&lt;n&gt;`, `n`
    /// continuing past any existing `cand-hc-` id.
    /// </summary>
    public static HeaderCitationResult InferHeaderCitation(string workspaceDir, MapStore store)
    {
        ArgumentException.ThrowIfNullOrEmpty(workspaceDir);
        ArgumentNullException.ThrowIfNull(store);

        var sourceBases = store.ListBases(BasisSide.Source);
        var targetBases = store.ListBases(BasisSide.Target);
        if (sourceBases.Count == 0 || targetBases.Count == 0)
            return new HeaderCitationResult(0, 0, 0, 0, []);

        var sourceBasis = sourceBases[^1];
        var targetBasis = targetBases[^1];
        var sourceEntities = store.GetEntities(sourceBasis.Id);
        var targetEntities = store.GetEntities(targetBasis.Id);

        // Anchor entity per source file: prefer the file's module entity (crate roots are
        // parentless; out-of-line submodule files — walked since rust-map-dump v0.4.0 — carry a
        // module entity WITH a parent, so the anchor rule must not require ParentId null; found
        // during the first full-scale citation run, 2026-07-12: 185/187 citations of submodule
        // files went unmatched under the old parentless-root rule). Prefer the shallowest
        // symbolPath (the file's own module over any inline sub-modules), deterministic tie-break.
        var rootModuleByFile = sourceEntities
            .Where(e => e.Kind == "module")
            .GroupBy(e => e.File, StringComparer.Ordinal)
            .OrderBy(g => g.Key, StringComparer.Ordinal)
            .ToDictionary(
                g => g.Key,
                g => g.OrderBy(e => e.SymbolPath.Count(ch => ch == ':'))
                      .ThenBy(e => e.SymbolPath, StringComparer.Ordinal)
                      .First(),
                StringComparer.Ordinal);

        var targetEntitiesById = targetEntities.ToDictionary(e => e.Id, e => e);

        var existing = CorrespondencesYaml.Read(workspaceDir);
        var coveredSourceSymbols = existing.Where(c => c.Source is not null).Select(c => c.Source!.SymbolPath).ToHashSet(StringComparer.Ordinal);
        var coveredTargetSymbols = existing.Where(c => c.Target is not null).Select(c => c.Target!.SymbolPath).ToHashSet(StringComparer.Ordinal);

        var targetFiles = targetEntities.Select(e => e.File).Distinct(StringComparer.Ordinal).OrderBy(f => f, StringComparer.Ordinal).ToList();

        int filesScanned = 0, citationsFound = 0, matched = 0, created = 0;
        var unmatched = new List<string>();
        var newCorrespondences = new List<Correspondence>();
        int nextNumber = NextCandidateNumberForPrefix(existing, "cand-hc-");

        foreach (var file in targetFiles)
        {
            string absolutePath = Path.Combine(targetBasis.Root, file.Replace('/', Path.DirectorySeparatorChar));
            List<(string RawLine, List<string> CitedPaths)> citations;
            try
            {
                citations = ExtractHeaderCitationLines(absolutePath);
            }
            catch (IOException)
            {
                continue; // stale map entry (file moved/deleted since the file was mapped) — not scannable.
            }
            catch (UnauthorizedAccessException)
            {
                continue;
            }
            filesScanned++;

            foreach (var (rawLine, citedPaths) in citations)
            {
                foreach (var citedPath in citedPaths)
                {
                    citationsFound++;
                    var rootModule = MatchSourceFile(citedPath, rootModuleByFile);
                    if (rootModule is null)
                    {
                        unmatched.Add(citedPath);
                        continue;
                    }
                    matched++;

                    if (coveredSourceSymbols.Contains(rootModule.SymbolPath))
                        continue;

                    var primaryType = FindPrimaryTypeEntity(targetEntities, targetEntitiesById, file);
                    if (primaryType is null || coveredTargetSymbols.Contains(primaryType.SymbolPath))
                        continue;

                    string id = $"cand-hc-{nextNumber++}";
                    var corr = new Correspondence(
                        id, "maps-to", DivergenceKind: null,
                        Unit: "", // DIVERGENCE: see CandidateInferenceService.Infer's identical remark — a
                                  // candidate precedes unit assignment.
                        Source: new AnchorRef(rootModule.SymbolPath, sourceBasis.Label, rootModule.ContentHash),
                        Target: new AnchorRef(primaryType.SymbolPath, targetBasis.Label, primaryType.ContentHash),
                        Criterion: null,
                        Note: $"inferred:header-citation \"{rawLine}\"",
                        ClaimAid: null,
                        Stale: false,
                        Provenance: "candidate");

                    newCorrespondences.Add(corr);
                    coveredSourceSymbols.Add(rootModule.SymbolPath);
                    coveredTargetSymbols.Add(primaryType.SymbolPath);
                    created++;
                }
            }
        }

        if (newCorrespondences.Count > 0)
            CorrespondencesYaml.Write(workspaceDir, existing.Concat(newCorrespondences).ToList());

        unmatched.Sort(StringComparer.Ordinal);
        return new HeaderCitationResult(filesScanned, citationsFound, matched, created, unmatched);
    }

    /// <summary>Reads the first <see cref="HeaderLineScanCount"/> lines of a file and returns each
    /// line matching <see cref="CitationLineRegex"/> together with the `*.rs` path token(s) it
    /// carries (empty citation lines — matched the intro phrase but no `.rs` token followed — are
    /// dropped).</summary>
    private static List<(string RawLine, List<string> CitedPaths)> ExtractHeaderCitationLines(string filePath)
    {
        var result = new List<(string, List<string>)>();
        string[] lines = File.ReadAllLines(filePath);

        int limit = Math.Min(HeaderLineScanCount, lines.Length);
        for (int i = 0; i < limit; i++)
        {
            string trimmed = lines[i].TrimStart();
            var lineMatch = CitationLineRegex.Match(trimmed);
            if (!lineMatch.Success)
                continue;

            var citedPaths = RsPathTokenRegex.Matches(lineMatch.Groups["rest"].Value).Select(m => m.Value).ToList();
            if (citedPaths.Count > 0)
                result.Add((trimmed, citedPaths));
        }
        return result;
    }

    /// <summary>
    /// Matches a cited path against the source map's files. Tries an exact match first (the
    /// "Upstream source:" form is already source-root-relative); failing that, matches by suffix
    /// — the cited text, once some unknown leading prefix (a vendor-mount directory name, e.g.
    /// ".external/frankentui/") is discounted, ends with exactly a known source file. No mount
    /// directory name is hardcoded: this is a pure string-suffix rule. A suffix shared by more
    /// than one source file (not observed in the surveyed tree, but possible in principle) is
    /// resolved by preferring the longest (most specific) file, then ordinal comparison, so the
    /// result is always deterministic.
    /// </summary>
    private static Entity? MatchSourceFile(string citedPath, IReadOnlyDictionary<string, Entity> rootModuleByFile)
    {
        if (rootModuleByFile.TryGetValue(citedPath, out var exact))
            return exact;

        Entity? best = null;
        foreach (var (file, entity) in rootModuleByFile)
        {
            if (!citedPath.EndsWith("/" + file, StringComparison.Ordinal))
                continue;
            if (best is null || file.Length > best.File.Length ||
                (file.Length == best.File.Length && string.CompareOrdinal(file, best.File) < 0))
                best = entity;
        }
        return best;
    }

    /// <summary>The largest top-level (non-nested) type-kind entity declared in <paramref
    /// name="file"/>, or null when the file declares none (e.g. an interface-only file — this
    /// provider does not emit an "interface" kind — or a top-level-statements Program.cs with no
    /// type declaration at all). Ties broken by earliest start line, then symbolPath, for full
    /// determinism.</summary>
    private static Entity? FindPrimaryTypeEntity(IReadOnlyList<Entity> targetEntities, IReadOnlyDictionary<string, Entity> targetEntitiesById, string file)
    {
        var topLevelTypes = targetEntities
            .Where(e => e.File == file && TypeKinds.Contains(e.Kind) && !IsNestedType(e, targetEntitiesById))
            .ToList();
        if (topLevelTypes.Count == 0)
            return null;

        int maxSpan = topLevelTypes.Max(e => e.EndLine - e.StartLine);
        return topLevelTypes
            .Where(e => e.EndLine - e.StartLine == maxSpan)
            .OrderBy(e => e.StartLine)
            .ThenBy(e => e.SymbolPath, StringComparer.Ordinal)
            .First();
    }

    /// <summary>A type entity is "nested" (excluded from primary-type selection) exactly when its
    /// parent entity is itself a type-kind entity — CSharpRoslynProvider sets a type's
    /// parentSymbolPath to its containing NAMESPACE too (not just a containing type), so
    /// `ParentId is not null` alone does not mean nested.</summary>
    private static bool IsNestedType(Entity e, IReadOnlyDictionary<string, Entity> targetEntitiesById) =>
        e.ParentId is not null && targetEntitiesById.TryGetValue(e.ParentId, out var parent) && TypeKinds.Contains(parent.Kind);

    private static int NextCandidateNumberForPrefix(IReadOnlyList<Correspondence> existing, string prefix)
    {
        int max = 0;
        foreach (var c in existing)
        {
            if (c.Id.StartsWith(prefix, StringComparison.Ordinal) &&
                int.TryParse(c.Id.AsSpan(prefix.Length), out int n) && n > max)
                max = n;
        }
        return max + 1;
    }

    private static Dictionary<string, List<Entity>> GroupByNormalizedTarget(IEnumerable<Entity> entities) =>
        entities
            .GroupBy(e => NormalizeTargetSymbolPath(e.SymbolPath), StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.OrderBy(e => e.SymbolPath, StringComparer.Ordinal).ToList(), StringComparer.Ordinal);

    /// <summary>`crate::mod::Type` -> last two `::`-segments, snake_case -> PascalCase, joined
    /// with `.` (CONTRACT-M15.md §2), so it is directly comparable with a normalized C# path.</summary>
    internal static string NormalizeSourceSymbolPath(string symbolPath)
    {
        var segments = symbolPath.Split("::", StringSplitOptions.RemoveEmptyEntries);
        var lastTwo = LastN(segments, 2);
        return string.Join('.', lastTwo.Select(SnakeToPascal));
    }

    /// <summary>`Namespace.Type` -> last two `.`-segments (CONTRACT-M15.md §2).</summary>
    internal static string NormalizeTargetSymbolPath(string symbolPath)
    {
        // Strip a method's parameter list, if any (e.g. "Ns.Type.Method(string)"), before
        // segmenting on '.' so it does not get swept into the last segment.
        int parenIdx = symbolPath.IndexOf('(', StringComparison.Ordinal);
        string bare = parenIdx >= 0 ? symbolPath[..parenIdx] : symbolPath;
        var segments = bare.Split('.', StringSplitOptions.RemoveEmptyEntries);
        return string.Join('.', LastN(segments, 2));
    }

    private static string[] LastN(string[] segments, int n) =>
        segments.Length <= n ? segments : segments[^n..];

    private static string SnakeToPascal(string snake)
    {
        if (snake.Length == 0)
            return snake;
        var sb = new StringBuilder(snake.Length);
        bool upperNext = true;
        foreach (char c in snake)
        {
            if (c == '_')
            {
                upperNext = true;
                continue;
            }
            sb.Append(upperNext ? char.ToUpperInvariant(c) : c);
            upperNext = false;
        }
        return sb.ToString();
    }
}
