using System.Globalization;
using System.Text.Json;
using Gneiss.Cell;
using KodePorter.Core.Absence;
using KodePorter.Core.Domain;
using KodePorter.Core.Gneiss;
using KodePorter.Core.Hashing;
using KodePorter.Core.Model;
using KodePorter.Core.Store;

namespace KodePorter.Core.Atlas;

/// <summary>
/// The Port Atlas (K-V, CONTRACT.md §8): one self-contained HTML file. All data is read via the
/// map store plus <c>Ask("kp-current", ...)</c> and the domain files — never from the yaml alone
/// for status. Assembles the deterministic data model here; <see cref="AtlasHtmlRenderer"/> turns
/// it into markup.
/// </summary>
public static class AtlasGenerator
{
    private static readonly HashSet<string> ClaimPredicates = new(StringComparer.Ordinal)
    {
        GneissBinding.PredBehavior,
        GneissBinding.PredEvidenceAnchor,
        GneissBinding.PredCorrespondence,
        GneissBinding.PredVerification,
    };

    /// <summary>Builds the Atlas HTML for <paramref name="workspaceDir"/>. Deterministic modulo <paramref name="generatedAt"/> (CONTRACT.md §8).</summary>
    public static string Generate(string workspaceDir, MapStore store, GneissBinding binding, DateTimeOffset generatedAt)
    {
        ArgumentException.ThrowIfNullOrEmpty(workspaceDir);
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(binding);

        string projectYamlPath = ProjectYaml.FilePath(workspaceDir);
        if (!File.Exists(projectYamlPath))
            throw new FileNotFoundException($"No project.yaml found in workspace '{workspaceDir}'; run 'kp init' first.", projectYamlPath);
        var project = ProjectYaml.Read(workspaceDir);

        var sourceBases = store.ListBases(BasisSide.Source);
        var targetBases = store.ListBases(BasisSide.Target);
        var latestSource = sourceBases.Count > 0 ? sourceBases[^1] : null;
        var latestTarget = targetBases.Count > 0 ? targetBases[^1] : null;

        IReadOnlyList<Entity> sourceEntities = latestSource is null ? Array.Empty<Entity>() : store.GetEntities(latestSource.Id);
        IReadOnlyList<Entity> targetEntities = latestTarget is null ? Array.Empty<Entity>() : store.GetEntities(latestTarget.Id);
        var sourceBySymbol = sourceEntities.GroupBy(e => e.SymbolPath, StringComparer.Ordinal).ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);
        var targetBySymbol = targetEntities.GroupBy(e => e.SymbolPath, StringComparer.Ordinal).ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);

        var correspondences = CorrespondencesYaml.Read(workspaceDir);
        var units = UnitYaml.ReadAll(workspaceDir);

        var health = HealthCalculator.Compute(workspaceDir, store, binding);

        // One "current view" Ask() backs every status shown below (CONTRACT.md §5: "All status
        // shown in the Atlas comes from Ask('kp-current', ...)") — a single consistent snapshot.
        var view = binding.CurrentView();
        var exportLines = binding.ExportLedgerJsonl();
        var index = LedgerIndex.Build(exportLines);

        var staleSubjects = view.Accepted.Where(e => e.Predicate == GneissBinding.PredStale)
            .Select(e => e.Subject).ToHashSet(StringComparer.Ordinal);

        var staleSourceSymbols = StaleSymbolsForSide(units, correspondences, BasisSide.Source);
        var staleTargetSymbols = StaleSymbolsForSide(units, correspondences, BasisSide.Target);
        var sourceNodes = BuildTreeNodes(sourceEntities, staleSourceSymbols);
        var targetNodes = BuildTreeNodes(targetEntities, staleTargetSymbols);

        var atlasCorrespondences = correspondences
            .OrderBy(c => c.Id, StringComparer.Ordinal)
            .Select(c => new AtlasCorrespondence(
                c.Id, c.Type, c.DivergenceKind, c.Unit,
                c.Source?.SymbolPath, c.Target?.SymbolPath,
                c.Source is not null && sourceBySymbol.TryGetValue(c.Source.SymbolPath, out var se) ? se.Id : null,
                c.Target is not null && targetBySymbol.TryGetValue(c.Target.SymbolPath, out var te) ? te.Id : null,
                c.Criterion, c.Note,
                ClaimStatusFor(view, index, GneissBinding.CorrespondenceSubject(c.Id), GneissBinding.PredCorrespondence),
                c.Stale, c.Provenance))
            .ToList();

        var atlasUnits = units
            .OrderBy(u => u.Id, StringComparer.Ordinal)
            .Select(u => new AtlasUnit(
                u.Id, u.Name, u.Status, u.Stale, u.SourceAnchors, u.TargetAnchors,
                BehaviorSummaryFor(view, index, u.Id),
                MiniMarkdown.Render(BuildUnitBodyMarkdown(u)),
                u.Depth))
            .ToList();

        var atlasClaims = index.AssrtsInTxOrder
            .Where(a => ClaimPredicates.Contains(a.Predicate))
            .Select(a => BuildClaim(binding, view, index, a, staleSubjects))
            .OrderBy(c => c.Predicate, StringComparer.Ordinal)
            .ThenBy(c => c.Subject, StringComparer.Ordinal)
            .ThenBy(c => c.Aid, StringComparer.Ordinal)
            .ToList();

        var atlasRuns = LoadRuns(workspaceDir);

        var header = new AtlasHeader(
            project.Name, project.Direction,
            sourceBases.Select(b => new AtlasBasis(b.Label, ShortHash(b.Id), store.GetEntities(b.Id).Count)).ToList(),
            targetBases.Select(b => new AtlasBasis(b.Label, ShortHash(b.Id), store.GetEntities(b.Id).Count)).ToList(),
            generatedAt.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture),
            "KodePorter v0");

        var labelInfo = new AtlasLabelInfo(
            view.Label.ContextName, ShortHash(view.Label.ContextHash),
            view.Label.DataCut, view.Label.DefCut, view.Label.ConsumedAids.Count, ShortHash(view.Label.ResultHash));

        string ledgerSha256 = Sha256Util.HexOfUtf8(string.Join('\n', exportLines));
        var footer = new AtlasFooter(ToWorkspaceRelative(workspaceDir, GneissBinding.LedgerPath(workspaceDir)), ledgerSha256);

        var absences = BuildAbsenceLists(workspaceDir, store);
        var continuityCandidates = BuildContinuityCandidates(store);
        var overview = BuildOverview(sourceEntities, targetEntities, correspondences, staleSourceSymbols, staleTargetSymbols);

        var data = new AtlasData(header, health, labelInfo, sourceNodes, targetNodes, atlasCorrespondences, atlasUnits, atlasClaims, atlasRuns, footer,
            absences, continuityCandidates, overview.Source.SplitPrefix, overview.Target.SplitPrefix, overview);

        return AtlasHtmlRenderer.Render(data);
    }

    /// <summary>Generates and writes the Atlas to <paramref name="outPath"/> (LF, UTF-8 no BOM). Returns <paramref name="outPath"/>.</summary>
    public static string GenerateToFile(string workspaceDir, MapStore store, GneissBinding binding, string outPath, DateTimeOffset generatedAt)
    {
        string html = Generate(workspaceDir, store, binding, generatedAt);
        DomainFileIo.WriteLf(outPath, html);
        return outPath;
    }

    // ---- Assembly helpers -----------------------------------------------------------------------

    private static List<AtlasEntityNode> BuildTreeNodes(IReadOnlyList<Entity> entities, ISet<string> staleSymbols) =>
        entities
            .OrderBy(e => e.SymbolPath, StringComparer.Ordinal)
            .Select(e => new AtlasEntityNode(e.Id, e.Kind, e.Name, e.SymbolPath, e.ParentId, staleSymbols.Contains(e.SymbolPath), e.Resolution, e.IsTest))
            .ToList();

    /// <summary>CONTRACT-M15.md §6.3: the health strip's absence breakdown drill-down lists, one
    /// bucket per (side, kind) — embedded in the data island for lazy/paginated rendering (never a
    /// flat pre-rendered HTML list, since "eligible uncovered entities" can still be sizable at
    /// real-world scale).</summary>
    private static AtlasAbsenceLists BuildAbsenceLists(string workspaceDir, MapStore store)
    {
        var resolved = AbsenceCalculator.Compute(workspaceDir, store);
        List<AtlasAbsenceItem> Bucket(string side, string kind) => resolved
            .Where(r => r.Side == side && r.Kind == kind)
            .OrderBy(r => r.SymbolPath, StringComparer.Ordinal)
            .Select(r => new AtlasAbsenceItem(r.SymbolPath, r.Note, r.IsOverride))
            .ToList();

        return new AtlasAbsenceLists(
            SourceUnknown: Bucket("source", "unknown"),
            SourceNotYetPorted: Bucket("source", "not-yet-ported"),
            SourceDeliberatelyDropped: Bucket("source", "deliberately-dropped"),
            TargetUnexplained: Bucket("target", "unexplained"),
            TargetIntentional: Bucket("target", "intentional"));
    }

    /// <summary>CONTRACT-M15.md §1.2/§6.3: resolves each continuity_candidate row's entity ids
    /// (which may reference any pinned basis, not just the latest two) to display symbolPaths.</summary>
    private static List<AtlasContinuityCandidate> BuildContinuityCandidates(MapStore store)
    {
        var candidates = store.GetContinuityCandidates();
        if (candidates.Count == 0)
            return [];

        var entitiesByBasis = new Dictionary<string, Dictionary<string, Entity>>(StringComparer.Ordinal);
        Dictionary<string, Entity> EntitiesFor(string basisId)
        {
            if (!entitiesByBasis.TryGetValue(basisId, out var map))
                entitiesByBasis[basisId] = map = store.GetEntities(basisId).ToDictionary(e => e.Id, StringComparer.Ordinal);
            return map;
        }

        var result = new List<AtlasContinuityCandidate>();
        foreach (var c in candidates)
        {
            var fromEntities = EntitiesFor(c.BasisFrom);
            var toEntities = EntitiesFor(c.BasisTo);
            fromEntities.TryGetValue(c.FromId, out var from);
            toEntities.TryGetValue(c.ToId, out var to);
            result.Add(new AtlasContinuityCandidate(
                c.BasisFrom, c.BasisTo,
                from?.SymbolPath ?? c.FromId, to?.SymbolPath ?? c.ToId,
                to?.Kind ?? from?.Kind ?? "",
                c.Heuristic, c.Status));
        }
        return result;
    }

    /// <summary>CONTRACT-M15.md §6.2: builds both sides' Overview treemaps. "corresponded" /
    /// "candidate-only" mirror Health v2's own asserted-vs-candidate split (§1.7) so the Overview
    /// panel and the health strip never disagree about what counts as covered.</summary>
    private static AtlasOverview BuildOverview(
        IReadOnlyList<Entity> sourceEntities, IReadOnlyList<Entity> targetEntities,
        IReadOnlyList<Correspondence> correspondences, ISet<string> staleSourceSymbols, ISet<string> staleTargetSymbols)
    {
        var asserted = correspondences.Where(c => c.Provenance != "candidate").ToList();
        var candidateOnly = correspondences.Where(c => c.Provenance == "candidate").ToList();

        var sourceCorresponded = asserted.Where(c => c.Source is not null).Select(c => c.Source!.SymbolPath).ToHashSet(StringComparer.Ordinal);
        var targetCorresponded = asserted.Where(c => c.Target is not null).Select(c => c.Target!.SymbolPath).ToHashSet(StringComparer.Ordinal);
        var sourceCandidateOnly = candidateOnly.Where(c => c.Source is not null).Select(c => c.Source!.SymbolPath)
            .Where(sp => !sourceCorresponded.Contains(sp)).ToHashSet(StringComparer.Ordinal);
        var targetCandidateOnly = candidateOnly.Where(c => c.Target is not null).Select(c => c.Target!.SymbolPath)
            .Where(sp => !targetCorresponded.Contains(sp)).ToHashSet(StringComparer.Ordinal);

        return AtlasOverviewBuilder.Build(
            sourceEntities, targetEntities,
            sourceCorresponded, sourceCandidateOnly, staleSourceSymbols,
            targetCorresponded, targetCandidateOnly, staleTargetSymbols);
    }

    private static HashSet<string> StaleSymbolsForSide(IReadOnlyList<UnitDoc> units, IReadOnlyList<Correspondence> correspondences, BasisSide side)
    {
        var set = new HashSet<string>(StringComparer.Ordinal);
        foreach (var u in units.Where(u => u.Stale))
        {
            var anchors = side == BasisSide.Source ? u.SourceAnchors : u.TargetAnchors;
            foreach (var a in anchors)
                set.Add(a.SymbolPath);
        }
        foreach (var c in correspondences.Where(c => c.Stale))
        {
            var anchor = side == BasisSide.Source ? c.Source : c.Target;
            if (anchor is not null)
                set.Add(anchor.SymbolPath);
        }
        return set;
    }

    /// <summary>Aggregated status counts of a unit's kp.behavior claims (per-claim subjects
    /// behavior:&lt;unit&gt;:&lt;id&gt;), e.g. [{accepted,2},{proposed,1}]; empty when the unit has
    /// no behavior claims. Rendered as one single-token status badge per bucket
    /// (AtlasHtmlRenderer), never joined into one multi-word string.</summary>
    private static List<AtlasStatusCount> BehaviorSummaryFor(BeliefView view, LedgerIndex index, string unitId)
    {
        string prefix = GneissBinding.BehaviorSubjectPrefix(unitId);
        var statuses = index.AssrtsInTxOrder
            .Where(a => a.Predicate == GneissBinding.PredBehavior && a.Subject.StartsWith(prefix, StringComparison.Ordinal))
            .Select(a => StatusOfAid(view, a.Aid))
            .ToList();
        return new[] { "accepted", "contested", "defeated", "proposed" }
            .Select(s => new AtlasStatusCount(s, statuses.Count(x => x == s)))
            .Where(t => t.Count > 0)
            .ToList();
    }

    private static string StatusOfAid(BeliefView view, string aid) =>
        view.Accepted.Any(e => e.Aid == aid) ? "accepted"
        : view.Defeated.Any(e => e.Aid == aid) ? "defeated"
        : view.Contested.Any(g => g.Aids.Contains(aid)) ? "contested"
        : "proposed";

    private static string ClaimStatusFor(BeliefView view, LedgerIndex index, string subject, string predicate)
    {
        if (view.Accepted.Any(e => e.Subject == subject && e.Predicate == predicate))
            return "accepted";
        if (view.Defeated.Any(e => e.Subject == subject && e.Predicate == predicate))
            return "defeated";
        bool contested = view.Contested.Any(g => g.Aids.Any(aid =>
            index.AssrtByAid.TryGetValue(aid, out var a) && a.Subject == subject && a.Predicate == predicate));
        return contested ? "contested" : "proposed";
    }

    private static AtlasClaim BuildClaim(GneissBinding binding, BeliefView view, LedgerIndex index, LedgerAssrt a, ISet<string> staleSubjects)
    {
        string status;
        bool autoAdmitted = false;
        var acceptedEntry = view.Accepted.FirstOrDefault(e => e.Aid == a.Aid);
        var defeatedEntry = view.Defeated.FirstOrDefault(e => e.Aid == a.Aid);
        if (acceptedEntry is not null)
        {
            status = "accepted";
            autoAdmitted = acceptedEntry.AutoAdmitted;
        }
        else if (defeatedEntry is not null)
        {
            status = "defeated";
        }
        else if (view.Contested.Any(g => g.Aids.Contains(a.Aid)))
        {
            status = "contested";
        }
        else
        {
            status = "proposed";
        }

        var explanation = binding.Why(a.Aid);

        string? decidedBy = null;
        if (explanation.Decisions.Count > 0)
        {
            string latestDecisionAid = explanation.Decisions
                .Select(d => (Aid: d, Tx: index.AssrtByAid.TryGetValue(d, out var da) ? da.Tx : -1))
                .OrderByDescending(t => t.Tx)
                .ThenBy(t => t.Aid, StringComparer.Ordinal)
                .First().Aid;
            decidedBy = index.ActorFor(latestDecisionAid);
        }

        bool stale = staleSubjects.Contains(a.Subject);

        return new AtlasClaim(a.Aid, a.Predicate, a.Subject, SummarizeValue(a.Predicate, a.Value, a.ValueKind), a.Value, status, autoAdmitted, stale, decidedBy, ToWhyNode(explanation));
    }

    private static AtlasWhyNode ToWhyNode(Explanation e) => new(
        e.Aid, e.Status, e.DefeatedBy, e.Inputs.Select(ToWhyNode).ToList(), e.RuleVersions, e.Decisions);

    private static string SummarizeValue(string predicate, string canonical, string valueKind)
    {
        if (valueKind != "json")
            return canonical;
        try
        {
            using var doc = JsonDocument.Parse(canonical);
            var root = doc.RootElement;
            return predicate switch
            {
                GneissBinding.PredVerification =>
                    $"verdict={GetStr(root, "verdict")} cases={GetInt(root, "cases")} mismatches={GetArrayLength(root, "mismatches")} independence={(root.TryGetProperty("independence", out var indepEl) ? indepEl.GetString() : "unknown")}",
                GneissBinding.PredCorrespondence =>
                    $"type={GetStr(root, "type")} unit={GetStr(root, "unit")}",
                GneissBinding.PredEvidenceAnchor =>
                    $"{GetStr(root, "symbolPath")} @ {GetStr(root, "basisLabel")}",
                _ => canonical,
            };
        }
        catch (JsonException)
        {
            return canonical;
        }
    }

    private static string GetStr(JsonElement root, string prop) => root.TryGetProperty(prop, out var v) ? v.GetString() ?? "" : "";
    private static string GetInt(JsonElement root, string prop) => root.TryGetProperty(prop, out var v) ? v.GetRawText() : "";
    private static int GetArrayLength(JsonElement root, string prop) => root.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.Array ? v.GetArrayLength() : 0;

    private static string BuildUnitBodyMarkdown(UnitDoc u)
    {
        var sb = new System.Text.StringBuilder();
        AppendSection(sb, "Purpose", u.Purpose);
        AppendSection(sb, "Contract", u.Contract);
        AppendSection(sb, "Questions", u.Questions);
        AppendSection(sb, "Evidence", u.Evidence);
        return sb.ToString();

        static void AppendSection(System.Text.StringBuilder sb, string title, string body)
        {
            sb.Append("## ").Append(title).Append('\n').Append('\n');
            if (!string.IsNullOrWhiteSpace(body))
                sb.Append(body.Trim()).Append('\n').Append('\n');
        }
    }

    private static List<AtlasRun> LoadRuns(string workspaceDir)
    {
        string runsDir = Path.Combine(workspaceDir, "runs");
        if (!Directory.Exists(runsDir))
            return [];

        var jsonFiles = Directory.EnumerateFiles(runsDir, "verify-*.json")
            .OrderBy(f => Path.GetFileName(f), StringComparer.Ordinal)
            .ToList();

        var runs = new List<AtlasRun>();
        foreach (var jsonPath in jsonFiles)
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(jsonPath));
            var root = doc.RootElement;
            string unit = GetStr(root, "unit");
            string criterion = GetStr(root, "criterion");
            string verdict = GetStr(root, "verdict");
            int pass = 0, fail = 0;
            var mismatches = new List<string>();
            if (root.TryGetProperty("results", out var results) && results.ValueKind == JsonValueKind.Array)
            {
                foreach (var r in results.EnumerateArray())
                {
                    bool match = r.TryGetProperty("match", out var m) && m.GetBoolean();
                    if (match)
                        pass++;
                    else
                    {
                        fail++;
                        mismatches.Add(GetStr(r, "name"));
                    }
                }
            }

            string mdPath = Path.ChangeExtension(jsonPath, ".md");
            string rerun = File.Exists(mdPath) ? ExtractRerunCommand(File.ReadAllText(mdPath)) : "";
            string independence = root.TryGetProperty("independence", out var indepEl) && indepEl.ValueKind == JsonValueKind.String
                ? indepEl.GetString()!
                : "unknown";

            runs.Add(new AtlasRun(unit, criterion, verdict, pass, fail, mismatches, rerun,
                ToWorkspaceRelative(workspaceDir, jsonPath), ToWorkspaceRelative(workspaceDir, mdPath), independence));
        }
        return runs;
    }

    private static string ExtractRerunCommand(string markdown)
    {
        int idx = markdown.IndexOf("## Rerun", StringComparison.Ordinal);
        if (idx < 0)
            return "";
        int fenceStart = markdown.IndexOf("```", idx, StringComparison.Ordinal);
        if (fenceStart < 0)
            return "";
        int contentStart = markdown.IndexOf('\n', fenceStart) + 1;
        if (contentStart <= 0)
            return "";
        int fenceEnd = markdown.IndexOf("```", contentStart, StringComparison.Ordinal);
        if (fenceEnd < 0)
            return "";
        return markdown[contentStart..fenceEnd].Trim();
    }

    private static string ToWorkspaceRelative(string workspaceDir, string path)
    {
        string full = Path.GetFullPath(path);
        string fullWs = Path.GetFullPath(workspaceDir);
        return Path.GetRelativePath(fullWs, full).Replace('\\', '/');
    }

    private static string ShortHash(string hash) => hash.Length > 12 ? hash[..12] : hash;
}
