using KodePorter.Core.Advance;
using KodePorter.Core.Atlas;
using KodePorter.Core.Candidates;
using KodePorter.Core.Domain;
using KodePorter.Core.Export;
using KodePorter.Core.Gneiss;
using KodePorter.Core.Model;
using KodePorter.Core.Providers;
using KodePorter.Core.Store;
using KodePorter.Core.Verify;
using KodePorter.Core.Workspace;

namespace KodePorter.Cli;

/// <summary>
/// The `kp` CLI (CONTRACT.md §9): manual arg parsing, no packages, all verbs wired to the
/// existing Core services. <see cref="Run"/> is the whole entry point — Program.cs is a one-line
/// shim so this is directly callable in-process from tests.
/// </summary>
public static class KpCliApp
{
    public static int Run(string[] args, TextWriter stdout, TextWriter stderr)
    {
        try
        {
            if (args.Length == 0 || args[0] is "-h" or "--help")
            {
                stdout.WriteLine(UsageText.Full);
                return 0;
            }

            string verb = args[0];
            var rest = args[1..];

            switch (verb)
            {
                case "init": RunInit(new ArgReader(rest), stdout); break;
                case "pin": RunPin(new ArgReader(rest), stdout); break;
                case "map": RunMap(new ArgReader(rest), stdout); break;
                case "unit": RunUnit(rest, stdout); break;
                case "corr": RunCorr(rest, stdout); break;
                case "claim": RunClaim(rest, stdout); break;
                case "decide": RunDecide(new ArgReader(rest), stdout); break;
                case "verify": RunVerify(rest, stdout); break;
                case "advance": RunAdvance(new ArgReader(rest), stdout); break;
                case "status": RunStatus(new ArgReader(rest), stdout); break;
                case "export": RunExport(new ArgReader(rest), stdout); break;
                case "export-ledger": RunExportLedger(new ArgReader(rest), stdout); break;
                case "atlas": RunAtlas(new ArgReader(rest), stdout); break;
                case "note": RunNote(new ArgReader(rest), stdout); break;
                case "notes": RunNotes(new ArgReader(rest), stdout); break;
                case "candidates": RunCandidates(rest, stdout); break;
                case "absence": RunAbsence(rest, stdout); break;
                default: throw new CliUsageException($"Unknown command '{verb}'.");
            }
            return 0;
        }
        catch (CliUsageException ex)
        {
            stderr.WriteLine($"kp: {ex.Message}");
            stderr.WriteLine(UsageText.Short);
            return 1;
        }
        catch (Exception ex)
        {
            stderr.WriteLine($"kp: {FirstLine(ex.Message)}");
            return 2;
        }
    }

    private static string FirstLine(string message)
    {
        int idx = message.IndexOf('\n');
        return idx < 0 ? message : message[..idx];
    }

    private static string RequireSubVerb(IReadOnlyList<string> args, string command)
    {
        if (args.Count == 0)
            throw new CliUsageException($"'{command}' requires a sub-command (see 'kp -h').");
        return args[0];
    }

    private static BasisSide ParseSide(string raw) => raw switch
    {
        "source" => BasisSide.Source,
        "target" => BasisSide.Target,
        _ => throw new CliUsageException($"--side must be 'source' or 'target' (got '{raw}')."),
    };

    private static Basis? LatestBasis(MapStore store, BasisSide side)
    {
        var bases = store.ListBases(side);
        return bases.Count > 0 ? bases[^1] : null;
    }

    // ---- init -------------------------------------------------------------------------------

    private static void RunInit(ArgReader a, TextWriter stdout)
    {
        string workspaceDir = a.Require("workspace");
        string name = a.Require("name");
        string sourceRoot = a.Require("source-root");
        string targetRoot = a.Require("target-root");
        const string direction = "rust->csharp"; // the only direction this increment supports (CONTRACT.md §1)
        const string policyRef = "kp-default@1";

        using var workspace = KpWorkspace.Initialize(workspaceDir, new ProjectDescriptor(name, direction, sourceRoot, targetRoot, policyRef));
        using var binding = GneissBinding.Initialize(workspaceDir);

        if (!File.Exists(ProjectYaml.FilePath(workspaceDir)))
            ProjectYaml.Write(workspaceDir, new ProjectYamlDoc(name, direction, sourceRoot, targetRoot, policyRef));

        if (!File.Exists(PolicyYaml.FilePath(workspaceDir)))
        {
            PolicyYaml.Write(workspaceDir, new PolicyDoc("kp-default", "1",
                new Dictionary<string, bool> { ["kpVerification"] = true, ["kpBehavior"] = false },
                new Dictionary<string, IReadOnlyList<string>> { ["kpVerification"] = ["verification-run"] }));
        }

        if (!File.Exists(CorrespondencesYaml.FilePath(workspaceDir)))
            CorrespondencesYaml.Write(workspaceDir, []);

        stdout.WriteLine($"Initialized kp workspace at '{workspaceDir}' (project '{name}', {direction}).");
    }

    // ---- pin --------------------------------------------------------------------------------

    private static void RunPin(ArgReader a, TextWriter stdout)
    {
        string workspaceDir = a.Require("workspace");
        var side = ParseSide(a.Require("side"));
        string root = a.Require("root");
        string label = a.Require("label");
        string? analyzer = a.Optional("analyzer");

        using var workspace = KpWorkspace.Open(workspaceDir);
        var basis = BasisPinner.Pin(workspace.Map, side, root, label, analyzer: analyzer);
        stdout.WriteLine($"Pinned {side.ToWireString()} '{label}' -> basis {ShortHash(basis.Id)} (tree_hash {ShortHash(basis.TreeHash)}).");
    }

    // ---- map --------------------------------------------------------------------------------

    private static void RunMap(ArgReader a, TextWriter stdout)
    {
        string workspaceDir = a.Require("workspace");
        var side = ParseSide(a.Require("side"));
        string label = a.Require("label");
        string? dump = a.Optional("dump");

        using var workspace = KpWorkspace.Open(workspaceDir);
        var basis = workspace.Map.FindBasis(side, label)
            ?? throw new CliDomainException($"No pinned basis for {side.ToWireString()} '{label}'; run 'kp pin' first.");

        ImportResult result = side switch
        {
            BasisSide.Source => new RustSynProvider().Import(workspace.Map, basis,
                dump ?? throw new CliUsageException("--dump is required when mapping the source side.")),
            BasisSide.Target => new CSharpRoslynProvider().Import(workspace.Map, basis),
            _ => throw new CliUsageException("Unknown side."),
        };

        stdout.WriteLine($"Imported {result.EntityCount} entities for {side.ToWireString()} '{label}' ({result.ErrorDiagnosticCount} diagnostics).");
    }

    // ---- unit new / set-depth -----------------------------------------------------------------

    private static void RunUnit(IReadOnlyList<string> args, TextWriter stdout)
    {
        string sub = RequireSubVerb(args, "unit");
        var a = new ArgReader(args.Skip(1).ToList());
        switch (sub)
        {
            case "new": RunUnitNew(a, stdout); break;
            case "set-depth": RunUnitSetDepth(a, stdout); break;
            default: throw new CliUsageException($"Unknown 'unit' sub-command '{sub}' (expected 'new' or 'set-depth').");
        }
    }

    private static void RunUnitNew(ArgReader a, TextWriter stdout)
    {
        string workspaceDir = a.Require("workspace");
        string id = a.Require("id");
        string name = a.Require("name");

        using var workspace = KpWorkspace.Open(workspaceDir);
        using var binding = GneissBinding.Initialize(workspaceDir);

        var sourceEntities = ResolveAnchorEntities(workspace.Map, BasisSide.Source, a.Optional("source-anchors"), out string? sourceLabel);
        var targetEntities = ResolveAnchorEntities(workspace.Map, BasisSide.Target, a.Optional("target-anchors"), out string? targetLabel);

        var sourceAnchors = sourceEntities.Select(e => new AnchorRef(e.SymbolPath, sourceLabel!, e.ContentHash)).ToList();
        var targetAnchors = targetEntities.Select(e => new AnchorRef(e.SymbolPath, targetLabel!, e.ContentHash)).ToList();

        var doc = new UnitDoc(id, name, "mapped", sourceAnchors, targetAnchors, Claims: [], Stale: false, Purpose: "", Contract: "", Questions: "", Evidence: "");
        UnitYaml.Write(workspaceDir, doc);

        foreach (var e in sourceEntities)
            binding.PromoteAnchor(new AnchorEvidenceValue(e.SymbolPath, sourceLabel!, e.ContentHash, e.File, e.StartLine, e.EndLine), "kodeporter", "kp unit new");
        foreach (var e in targetEntities)
            binding.PromoteAnchor(new AnchorEvidenceValue(e.SymbolPath, targetLabel!, e.ContentHash, e.File, e.StartLine, e.EndLine), "kodeporter", "kp unit new");

        stdout.WriteLine($"Created unit '{id}' ({sourceAnchors.Count} source anchor(s), {targetAnchors.Count} target anchor(s)).");
    }

    private static void RunUnitSetDepth(ArgReader a, TextWriter stdout)
    {
        string workspaceDir = a.Require("workspace");
        string id = a.Require("id");
        string depth = a.Require("depth");
        if (depth is not ("thin" or "dossiered"))
            throw new CliUsageException($"--depth must be 'thin' or 'dossiered' (got '{depth}').");

        if (!UnitYaml.ListUnitIds(workspaceDir).Contains(id, StringComparer.Ordinal))
            throw new CliDomainException($"No unit '{id}'; run 'kp unit new' first.");

        var unit = UnitYaml.Read(workspaceDir, id);
        UnitYaml.Write(workspaceDir, unit with { Depth = depth });

        stdout.WriteLine($"Set unit '{id}' depth to '{depth}'.");
    }

    private static List<Entity> ResolveAnchorEntities(MapStore store, BasisSide side, string? csv, out string? basisLabel)
    {
        basisLabel = null;
        if (string.IsNullOrWhiteSpace(csv))
            return [];

        var basis = LatestBasis(store, side)
            ?? throw new CliDomainException($"No {side.ToWireString()} basis has been mapped yet; run 'kp pin' and 'kp map' first.");
        basisLabel = basis.Label;

        var bySymbol = store.GetEntities(basis.Id).GroupBy(e => e.SymbolPath, StringComparer.Ordinal).ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);
        var result = new List<Entity>();
        foreach (var sp in ArgReader.SplitCsv(csv))
        {
            if (!bySymbol.TryGetValue(sp, out var e))
                throw new CliDomainException($"No {side.ToWireString()} entity with symbolPath '{sp}' in basis '{basis.Label}'.");
            result.Add(e);
        }
        return result;
    }

    // ---- corr add -----------------------------------------------------------------------------

    private static void RunCorr(IReadOnlyList<string> args, TextWriter stdout)
    {
        string sub = RequireSubVerb(args, "corr");
        var a = new ArgReader(args.Skip(1).ToList());
        if (sub != "add")
            throw new CliUsageException($"Unknown 'corr' sub-command '{sub}' (expected 'add').");

        string workspaceDir = a.Require("workspace");
        string type = a.Require("type");
        string unit = a.Require("unit");
        string? sourceSp = a.Optional("source");
        string? targetSp = a.Optional("target");
        string? criterion = a.Optional("criterion");
        string? divergenceKind = a.Optional("divergence-kind");
        string? note = a.Optional("note");
        string provenance = a.OptionalOr("provenance", "asserted");
        if (provenance is not ("candidate" or "asserted"))
            throw new CliUsageException($"--provenance must be 'candidate' or 'asserted' (got '{provenance}').");

        using var workspace = KpWorkspace.Open(workspaceDir);
        using var binding = GneissBinding.Initialize(workspaceDir);

        var correspondences = CorrespondencesYaml.Read(workspaceDir);

        // DIVERGENCE: CONTRACT.md §9's `kp corr add` synopsis has no --id flag; without one there
        // is no way to name a correspondence, so we accept an optional --id and otherwise
        // auto-generate one deterministically from the unit and the caller's existing count.
        string id = a.OptionalOr("id", $"corr-{unit}-{correspondences.Count(c => c.Unit == unit) + 1}");

        AnchorRef? source = sourceSp is null ? null : ResolveSingleAnchor(workspace.Map, BasisSide.Source, sourceSp);
        AnchorRef? target = targetSp is null ? null : ResolveSingleAnchor(workspace.Map, BasisSide.Target, targetSp);

        // CONTRACT-M15.md §1.3/§2: a `candidate` correspondence is machine-inferred/unreviewed;
        // matching CandidateInferenceService's own rule, it never gets a Gneiss claim — only an
        // `asserted` row is proposed for decision.
        string? aid = null;
        if (provenance == "asserted")
        {
            var value = new CorrespondenceClaimValue(
                type,
                source is null ? null : new AnchorRefValue(source.SymbolPath, source.BasisLabel, source.ContentHash),
                target is null ? null : new AnchorRefValue(target.SymbolPath, target.BasisLabel, target.ContentHash),
                unit, criterion);
            aid = binding.ProposeCorrespondenceClaim(id, value, evidenceAids: null, actor: "kodeporter", reason: "kp corr add");
        }

        var corr = new Correspondence(id, type, divergenceKind, unit, source, target, criterion, note, ClaimAid: aid, Stale: false, Provenance: provenance);
        correspondences.Add(corr);
        CorrespondencesYaml.Write(workspaceDir, correspondences);

        stdout.WriteLine($"Added correspondence '{id}' ({type}, provenance {provenance}) for unit '{unit}'.");
    }

    private static AnchorRef ResolveSingleAnchor(MapStore store, BasisSide side, string symbolPath)
    {
        var basis = LatestBasis(store, side)
            ?? throw new CliDomainException($"No {side.ToWireString()} basis has been mapped yet; run 'kp pin' and 'kp map' first.");
        var entities = store.GetEntities(basis.Id);
        var match = entities.FirstOrDefault(e => e.SymbolPath == symbolPath)
            ?? throw new CliDomainException($"No {side.ToWireString()} entity with symbolPath '{symbolPath}' in basis '{basis.Label}'.");
        return new AnchorRef(match.SymbolPath, basis.Label, match.ContentHash);
    }

    // ---- claim add ------------------------------------------------------------------------------

    private static void RunClaim(IReadOnlyList<string> args, TextWriter stdout)
    {
        string sub = RequireSubVerb(args, "claim");
        var a = new ArgReader(args.Skip(1).ToList());
        if (sub != "add")
            throw new CliUsageException($"Unknown 'claim' sub-command '{sub}' (expected 'add').");

        string workspaceDir = a.Require("workspace");
        string unitId = a.Require("unit");
        string claimId = a.Require("id");
        string predicate = a.Require("predicate");
        string value = a.Require("value");
        string? anchorsCsv = a.Optional("anchors");

        if (claimId.Length == 0 || !claimId.All(ch => char.IsAsciiLetterOrDigit(ch) || ch is '-' or '_'))
            throw new CliUsageException($"--id '{claimId}' must be non-empty and use only letters, digits, '-' or '_'.");

        if (predicate != GneissBinding.PredBehavior)
            throw new CliUsageException($"--predicate '{predicate}' is not supported by 'kp claim add' (only '{GneissBinding.PredBehavior}').");

        if (!UnitYaml.ListUnitIds(workspaceDir).Contains(unitId, StringComparer.Ordinal))
            throw new CliDomainException($"No unit '{unitId}'; run 'kp unit new' first.");

        using var workspace = KpWorkspace.Open(workspaceDir);
        using var binding = GneissBinding.Initialize(workspaceDir);

        var evidenceAids = new List<string>();
        if (!string.IsNullOrWhiteSpace(anchorsCsv))
        {
            foreach (var sp in ArgReader.SplitCsv(anchorsCsv))
            {
                var (side, entity) = FindEntityEitherSide(workspace.Map, sp)
                    ?? throw new CliDomainException($"No entity with symbolPath '{sp}' in any current basis.");
                var basis = LatestBasis(workspace.Map, side)!;
                string aid = binding.PromoteAnchor(new AnchorEvidenceValue(entity.SymbolPath, basis.Label, entity.ContentHash, entity.File, entity.StartLine, entity.EndLine),
                    "kodeporter", "kp claim add");
                evidenceAids.Add(aid);
            }
        }

        string claimAid = binding.ProposeBehaviorClaim(unitId, claimId, value, evidenceAids, actor: "kodeporter", reason: "kp claim add");

        var unit = UnitYaml.Read(workspaceDir, unitId);
        UnitYaml.Write(workspaceDir, unit with { Claims = [.. unit.Claims, claimAid] });

        stdout.WriteLine($"Proposed kp.behavior claim '{GneissBinding.BehaviorSubject(unitId, claimId)}' ({ShortHash(claimAid)}).");
    }

    private static (BasisSide Side, Entity Entity)? FindEntityEitherSide(MapStore store, string symbolPath)
    {
        var sourceBasis = LatestBasis(store, BasisSide.Source);
        if (sourceBasis is not null)
        {
            var e = store.GetEntities(sourceBasis.Id).FirstOrDefault(e => e.SymbolPath == symbolPath);
            if (e is not null)
                return (BasisSide.Source, e);
        }
        var targetBasis = LatestBasis(store, BasisSide.Target);
        if (targetBasis is not null)
        {
            var e = store.GetEntities(targetBasis.Id).FirstOrDefault(e => e.SymbolPath == symbolPath);
            if (e is not null)
                return (BasisSide.Target, e);
        }
        return null;
    }

    // ---- decide ---------------------------------------------------------------------------------

    private static void RunDecide(ArgReader a, TextWriter stdout)
    {
        string workspaceDir = a.Require("workspace");
        string subject = a.Require("subject");
        string verdictRaw = a.Require("verdict");
        string reason = a.Require("reason");
        // CONTRACT.md §5/§9: the human default actor is "govert"; an explicit --actor lets a
        // policy actor (`policy:<name>@<version>`) accept on the record through the same command
        // rather than only ever via the autoAccept path — wired straight through to
        // GneissBinding.HumanDecide's own `actor` parameter (default unchanged).
        string actor = a.OptionalOr("actor", "govert");

        var verdict = verdictRaw switch
        {
            "accept" => KpVerdict.Accept,
            "reject" => KpVerdict.Reject,
            _ => throw new CliUsageException($"--verdict must be 'accept' or 'reject' (got '{verdictRaw}')."),
        };

        using var binding = GneissBinding.Initialize(workspaceDir);

        var lines = binding.ExportLedgerJsonl();
        string? claimAid = null;
        long bestTx = -1;
        foreach (string line in lines)
        {
            using var doc = System.Text.Json.JsonDocument.Parse(line);
            var root = doc.RootElement;
            if (root.GetProperty("kind").GetString() != "assrt")
                continue;
            if (root.GetProperty("subj").GetString() != subject)
                continue;
            string pred = root.GetProperty("pred").GetString()!;
            if (pred is GneissBinding.PredStale or "gneiss.decision" or "gneiss.predicate" or "gneiss.context")
                continue;
            long tx = root.GetProperty("tx").GetInt64();
            if (tx > bestTx)
            {
                bestTx = tx;
                claimAid = root.GetProperty("aid").GetString();
            }
        }

        if (claimAid is null)
            throw new CliDomainException($"No claim found for subject '{subject}'.");

        binding.HumanDecide(claimAid, verdict, reason, actor);
        stdout.WriteLine($"{(verdict == KpVerdict.Accept ? "Accepted" : "Rejected")} claim {ShortHash(claimAid)} for subject '{subject}' (actor '{actor}').");
    }

    // ---- verify run -----------------------------------------------------------------------------

    private static void RunVerify(IReadOnlyList<string> args, TextWriter stdout)
    {
        string sub = RequireSubVerb(args, "verify");
        var a = new ArgReader(args.Skip(1).ToList());
        if (sub != "run")
            throw new CliUsageException($"Unknown 'verify' sub-command '{sub}' (expected 'run').");

        string workspaceDir = a.Require("workspace");
        string unitId = a.Require("unit");
        string cases = a.Require("cases");
        string sourceCmd = a.Require("source-cmd");
        string targetCmd = a.Require("target-cmd");
        string independence = a.OptionalOr("independence", "unknown");
        if (independence is not ("independently-derived" or "implementation-coupled" or "unknown"))
            throw new CliUsageException(
                $"--independence must be 'independently-derived', 'implementation-coupled', or 'unknown' (got '{independence}').");
        const string criterion = "io-agreement-v1"; // the only criterion this increment implements (CONTRACT.md §6)

        using var workspace = KpWorkspace.Open(workspaceDir);
        using var binding = GneissBinding.Initialize(workspaceDir);

        string sourceBasisLabel = LatestBasis(workspace.Map, BasisSide.Source)?.Label
            ?? throw new CliDomainException("No source basis pinned; run 'kp pin --side source' first.");
        string targetBasisLabel = LatestBasis(workspace.Map, BasisSide.Target)?.Label
            ?? throw new CliDomainException("No target basis pinned; run 'kp pin --side target' first.");

        var run = VerificationHarness.Run(workspaceDir, unitId, criterion, cases, sourceCmd, targetCmd, sourceBasisLabel, targetBasisLabel, DateTimeOffset.UtcNow, independence);

        var policy = File.Exists(PolicyYaml.FilePath(workspaceDir))
            ? PolicyYaml.Read(workspaceDir)
            : new PolicyDoc("none", "0", new Dictionary<string, bool>(), new Dictionary<string, IReadOnlyList<string>>());

        VerificationHarness.PromoteResult(binding, policy, run, evidenceAids: null, actor: "kodeporter", reason: "kp verify run");

        stdout.WriteLine($"Verify {unitId}/{criterion}: {run.Verdict} ({run.Results.Count(r => r.Match)}/{run.Results.Count} cases). Report: {run.ReportJsonPath}");
    }

    // ---- advance --------------------------------------------------------------------------------

    private static void RunAdvance(ArgReader a, TextWriter stdout)
    {
        string workspaceDir = a.Require("workspace");
        var side = ParseSide(a.Require("side"));
        string root = a.Require("root");
        string label = a.Require("label");
        string? dump = a.Optional("dump");

        using var workspace = KpWorkspace.Open(workspaceDir);
        using var binding = GneissBinding.Initialize(workspaceDir);

        var report = AdvanceService.Advance(workspaceDir, workspace.Map, binding, side, root, label, dump, analyzer: null,
            timestamp: DateTimeOffset.UtcNow, actor: "kodeporter", reason: "kp advance");

        stdout.WriteLine($"Advance {side.ToWireString()} -> '{label}': +{report.Diff.Added.Count} -{report.Diff.Removed.Count} ~{report.Diff.Changed.Count}. " +
            $"Stale: {report.StaleUnitIds.Count} unit(s), {report.StaleCorrespondenceIds.Count} correspondence(s). Report: {report.ReportPath}");
    }

    // ---- status ---------------------------------------------------------------------------------

    /// <summary>Prints Health v2 in full (CONTRACT-M15.md §1.7: "kp status prints all"; same
    /// numbers as the Atlas health strip, CONTRACT.md §8/§9).</summary>
    private static void RunStatus(ArgReader a, TextWriter stdout)
    {
        string workspaceDir = a.Require("workspace");
        using var workspace = KpWorkspace.Open(workspaceDir);
        using var binding = GneissBinding.Initialize(workspaceDir);

        var h = HealthCalculator.Compute(workspaceDir, workspace.Map, binding);
        stdout.WriteLine($"mapped: {h.Mapped}");
        stdout.WriteLine($"corresponded: {h.Corresponded}");
        stdout.WriteLine($"candidates: {h.Candidates}");
        stdout.WriteLine($"implemented: {h.Implemented}");
        stdout.WriteLine($"verified: {h.Verified}");
        stdout.WriteLine($"stale: {h.Stale}");
        stdout.WriteLine("absence:");
        stdout.WriteLine($"  unknown: {h.Absence.Unknown}");
        stdout.WriteLine($"  notYetPorted: {h.Absence.NotYetPorted}");
        stdout.WriteLine($"  deliberatelyDropped: {h.Absence.DeliberatelyDropped}");
        stdout.WriteLine("targetOnly:");
        stdout.WriteLine($"  unexplained: {h.TargetOnly.Unexplained}");
        stdout.WriteLine($"  intentional: {h.TargetOnly.Intentional}");
    }

    // ---- export ---------------------------------------------------------------------------------

    private static void RunExport(ArgReader a, TextWriter stdout)
    {
        string workspaceDir = a.Require("workspace");
        string outPath = a.Require("out");
        using var binding = GneissBinding.Initialize(workspaceDir);
        string path = ExportService.Export(workspaceDir, binding, outPath);
        stdout.WriteLine($"Exported PORTING.md to '{path}'.");
    }

    // ---- export-ledger (the golden ledger: canonical JSONL of tx+assrt+dec+just) -----------------

    private static void RunExportLedger(ArgReader a, TextWriter stdout)
    {
        string workspaceDir = a.Require("workspace");
        string outPath = a.Require("out");
        using var binding = GneissBinding.Initialize(workspaceDir);
        var lines = binding.ExportLedgerJsonl();
        string joined = string.Join('\n', lines);
        File.WriteAllBytes(outPath, System.Text.Encoding.UTF8.GetBytes(joined + "\n"));
        string sha = Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(joined)));
        stdout.WriteLine($"Exported golden ledger ({lines.Count} rows) to '{outPath}' (sha256 {sha[..12]}).");
    }

    // ---- atlas ----------------------------------------------------------------------------------

    private static void RunAtlas(ArgReader a, TextWriter stdout)
    {
        string workspaceDir = a.Require("workspace");
        string outPath = a.Require("out");
        using var workspace = KpWorkspace.Open(workspaceDir);
        using var binding = GneissBinding.Initialize(workspaceDir);
        string path = AtlasGenerator.GenerateToFile(workspaceDir, workspace.Map, binding, outPath, DateTimeOffset.UtcNow);
        stdout.WriteLine($"Wrote Port Atlas to '{path}'.");
    }

    // ---- note / notes (K-A8 two-tier capture, CONTRACT-M15.md §3) ---------------------------------

    private static void RunNote(ArgReader a, TextWriter stdout)
    {
        string workspaceDir = a.Require("workspace");
        string text = a.Require("text");
        string actor = a.OptionalOr("actor", "kodeporter");

        using var binding = GneissBinding.Initialize(workspaceDir);
        string id = binding.Note(text, actor, "kp note");

        stdout.WriteLine($"Recorded note {ShortHash(id)}.");
    }

    private static void RunNotes(ArgReader a, TextWriter stdout)
    {
        string workspaceDir = a.Require("workspace");

        using var binding = GneissBinding.Initialize(workspaceDir);
        var notes = binding.ListNotes();

        if (notes.Count == 0)
        {
            stdout.WriteLine("No notes.");
            return;
        }

        foreach (var n in notes)
        {
            string promoted = n.PromotedAid is null ? "no" : "yes";
            stdout.WriteLine($"{ShortHash(n.Id)}  {n.Wall}  {n.Actor}  promoted={promoted}  {n.Text}");
        }
    }

    // ---- candidates infer (CONTRACT-M15.md §2) -----------------------------------------------------

    private static void RunCandidates(IReadOnlyList<string> args, TextWriter stdout)
    {
        string sub = RequireSubVerb(args, "candidates");
        var a = new ArgReader(args.Skip(1).ToList());
        if (sub != "infer")
            throw new CliUsageException($"Unknown 'candidates' sub-command '{sub}' (expected 'infer').");

        string workspaceDir = a.Require("workspace");
        string heuristic = a.OptionalOr("heuristic", "name-norm");

        using var workspace = KpWorkspace.Open(workspaceDir);

        if (heuristic == "header-citation")
        {
            var hcResult = CandidateInferenceService.InferHeaderCitation(workspaceDir, workspace.Map);
            stdout.WriteLine($"Header-citation: scanned {hcResult.FilesScanned} file(s), {hcResult.CitationsFound} citation(s) found, " +
                $"{hcResult.Matched} matched, {hcResult.Created} candidate(s) created, {hcResult.UnmatchedCitedPaths.Count} unmatched cited path(s).");
            foreach (var unmatchedPath in hcResult.UnmatchedCitedPaths.Take(3))
                stdout.WriteLine($"  unmatched: {unmatchedPath}");
            return;
        }

        var result = CandidateInferenceService.Infer(workspaceDir, workspace.Map, heuristic);

        stdout.WriteLine($"Candidates: created {result.Created}, skipped {result.Skipped}, ambiguous {result.Ambiguous.Count}.");
        foreach (var symbolPath in result.Ambiguous)
            stdout.WriteLine($"  ambiguous: {symbolPath}");
    }

    // ---- absence set (CONTRACT-M15.md §1.5) --------------------------------------------------------

    private static void RunAbsence(IReadOnlyList<string> args, TextWriter stdout)
    {
        string sub = RequireSubVerb(args, "absence");
        var a = new ArgReader(args.Skip(1).ToList());
        if (sub != "set")
            throw new CliUsageException($"Unknown 'absence' sub-command '{sub}' (expected 'set').");

        string workspaceDir = a.Require("workspace");
        string symbol = a.Require("symbol");
        string kind = a.Require("kind");
        string? note = a.Optional("note");
        string side = a.OptionalOr("side", "source");
        if (side is not ("source" or "target"))
            throw new CliUsageException($"--side must be 'source' or 'target' (got '{side}').");

        var validKinds = side == "source"
            ? new[] { "not-yet-ported", "deliberately-dropped", "unknown" }
            : new[] { "intentional", "unexplained" };
        if (!validKinds.Contains(kind, StringComparer.Ordinal))
            throw new CliUsageException($"--kind must be one of [{string.Join(", ", validKinds)}] for --side {side} (got '{kind}').");

        var overrides = AbsencesYaml.Read(workspaceDir);
        overrides.RemoveAll(o => o.Side == side && o.SymbolPath == symbol);
        overrides.Add(new AbsenceOverride(symbol, kind, note, side));
        AbsencesYaml.Write(workspaceDir, overrides);

        stdout.WriteLine($"Set absence '{symbol}' ({side}) to '{kind}'.");
    }

    private static string ShortHash(string hash) => hash.Length > 12 ? hash[..12] : hash;
}
