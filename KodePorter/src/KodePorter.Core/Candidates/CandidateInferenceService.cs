using System.Text;
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
