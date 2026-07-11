using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using KodePorter.Core.Domain;
using KodePorter.Core.Gneiss;
using KodePorter.Core.Hashing;

namespace KodePorter.Core.Verify;

/// <summary>One case's comparison outcome (CONTRACT.md §6). <paramref name="Reason"/> is set (and
/// <paramref name="Match"/> forced false) when the case could not be scored at all — e.g. a
/// duplicated result line for the same case name in one of the two streams — as opposed to a
/// scored-but-differing mismatch.</summary>
public sealed record VerifyCaseResult(string Name, bool Match, string? SourceResultJson, string? TargetResultJson, string? Reason = null);

/// <summary>The result of one `kp verify run` (CONTRACT.md §6).</summary>
/// <param name="Independence">CONTRACT-M15.md §1.6: `independently-derived |
/// implementation-coupled | unknown`, caller-attested via `--independence`. Default `unknown`.</param>
public sealed record VerifyRunResult(
    string Unit,
    string Criterion,
    string CorpusHash,
    string SourceCmd,
    string TargetCmd,
    string SourceBasis,
    string TargetBasis,
    IReadOnlyList<VerifyCaseResult> Results,
    string Verdict, // pass | fail
    string ReportJsonPath,
    string ReportMdPath,
    string ReportRelativePath, // workspace-relative, forward-slashed; content-addressed, embedded in the claim value (never the absolute ReportJsonPath)
    string Independence = "unknown");

/// <summary>
/// K4-lite differential verification (CONTRACT.md §6, io-agreement-v1): runs source-cmd and
/// target-cmd, piping the cases file to stdin, comparing per-case byte equality of the `result`
/// JSON, and writing the run report + lab notebook.
/// </summary>
public static class VerificationHarness
{
    public static VerifyRunResult Run(
        string workspaceDir,
        string unitId,
        string criterion,
        string casesFilePath,
        string sourceCmd,
        string targetCmd,
        string sourceBasisLabel,
        string targetBasisLabel,
        DateTimeOffset timestamp,
        string independence = "unknown")
    {
        string cwd = Directory.GetParent(Path.GetFullPath(workspaceDir))?.FullName ?? Path.GetFullPath(workspaceDir);
        string casesContent = File.ReadAllText(casesFilePath);
        string corpusHash = Sha256Util.HexOfFile(casesFilePath);

        var caseNames = ReadCaseNames(casesContent);

        string sourceStdout = RunCommand(sourceCmd, casesContent, cwd);
        string targetStdout = RunCommand(targetCmd, casesContent, cwd);

        var sourceResults = ParseJsonlResultsByName(sourceStdout);
        var targetResults = ParseJsonlResultsByName(targetStdout);

        var results = new List<VerifyCaseResult>();
        foreach (string name in caseNames)
        {
            bool sourceDup = sourceResults.Duplicates.Contains(name);
            bool targetDup = targetResults.Duplicates.Contains(name);
            if (sourceDup || targetDup)
            {
                string stream = sourceDup ? "source" : "target";
                results.Add(new VerifyCaseResult(name, false, null, null,
                    $"duplicate result line for case {name} in {stream} output"));
                continue;
            }

            sourceResults.ByName.TryGetValue(name, out string? sourceResult);
            targetResults.ByName.TryGetValue(name, out string? targetResult);
            bool match = sourceResult is not null && targetResult is not null && sourceResult == targetResult;
            results.Add(new VerifyCaseResult(name, match, sourceResult, targetResult));
        }

        string verdict = results.Count > 0 && results.All(r => r.Match) ? "pass" : "fail";

        string runsDir = Path.Combine(workspaceDir, "runs");
        Directory.CreateDirectory(runsDir);
        string hash12 = corpusHash.Length > 12 ? corpusHash[..12] : corpusHash;
        string fileNameBase = $"verify-{unitId}-{criterion}-{hash12}";
        string jsonPath = Path.Combine(runsDir, fileNameBase + ".json");
        string mdPath = Path.Combine(runsDir, fileNameBase + ".md");
        string reportRelativePath = $"runs/{fileNameBase}.json";

        string rerunCommand = $"kp verify run --workspace {workspaceDir} --unit {unitId} --cases {casesFilePath} " +
            $"--source-cmd \"{sourceCmd}\" --target-cmd \"{targetCmd}\"";

        WriteJsonReport(jsonPath, unitId, criterion, corpusHash, sourceCmd, targetCmd, sourceBasisLabel, targetBasisLabel, results, verdict, independence);
        WriteMarkdownNotebook(mdPath, unitId, criterion, casesFilePath, sourceCmd, targetCmd, sourceBasisLabel, targetBasisLabel, results, verdict, rerunCommand, timestamp);

        return new VerifyRunResult(unitId, criterion, corpusHash, sourceCmd, targetCmd, sourceBasisLabel, targetBasisLabel, results, verdict, jsonPath, mdPath, reportRelativePath, independence);
    }

    /// <summary>
    /// Promotes the run as a kp.verification claim (proposed) and, when the verdict is pass AND
    /// policy.yaml allows the "kpVerification" claim class, auto-accepts it via the policy actor
    /// (CONTRACT.md §6). A fail verdict is always recorded as a claim but never auto-accepted.
    /// </summary>
    public static string PromoteResult(
        GneissBinding binding,
        PolicyDoc policy,
        VerifyRunResult run,
        IReadOnlyList<string>? evidenceAids,
        string actor,
        string reason)
    {
        var value = new VerificationClaimValue(
            run.Verdict,
            run.CorpusHash,
            run.SourceBasis,
            run.TargetBasis,
            run.Results.Count,
            run.Results.Where(r => !r.Match).Select(r => r.Name).ToList(),
            run.ReportRelativePath,
            run.Independence);

        string aid = binding.ProposeVerificationClaim(run.Unit, run.Criterion, value, evidenceAids, actor, reason);

        // CONTRACT-M15.md §1.6: policy.yaml's optional requiredIndependence floor gates
        // auto-accept alongside the existing autoAccept flag; absent -> no added constraint.
        if (run.Verdict == "pass" && PolicyEngine.AllowsAutoAccept(policy, "kpVerification", run.Independence))
        {
            binding.PolicyAutoAccept(aid, policy.Name, policy.Version, "auto-accept: verification green, policy allows kpVerification");
        }

        return aid;
    }

    private static List<string> ReadCaseNames(string casesContent)
    {
        var names = new List<string>();
        foreach (string line in SplitLines(casesContent))
        {
            using var doc = JsonDocument.Parse(line);
            names.Add(doc.RootElement.GetProperty("name").GetString()!);
        }
        return names;
    }

    /// <summary>The by-name results parsed from one command's stdout, plus the set of case names
    /// that appeared on more than one line (CONTRACT.md §6: a duplicated result line is a scoring
    /// failure for that case, never silently resolved to the last-seen line).</summary>
    private sealed record ParsedJsonlResults(Dictionary<string, string> ByName, HashSet<string> Duplicates);

    private static ParsedJsonlResults ParseJsonlResultsByName(string stdout)
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        var duplicates = new HashSet<string>(StringComparer.Ordinal);
        foreach (string line in SplitLines(stdout))
        {
            using var doc = JsonDocument.Parse(line);
            string name = doc.RootElement.GetProperty("name").GetString()!;
            string result = doc.RootElement.GetProperty("result").GetRawText();
            if (!map.TryAdd(name, result))
                duplicates.Add(name);
        }
        return new ParsedJsonlResults(map, duplicates);
    }

    private static IEnumerable<string> SplitLines(string content) =>
        content.Replace("\r\n", "\n").Split('\n').Where(l => !string.IsNullOrWhiteSpace(l));

    private static string RunCommand(string command, string stdin, string workingDirectory)
    {
        // NB: built as a raw command line (not ArgumentList) — the caller's command string may
        // already be quoted (e.g. a quoted path with spaces), and cmd.exe /c has its own rule for
        // stripping one matched pair of surrounding quotes from the remainder; double-quoting via
        // ArgumentList's auto-escaping would corrupt that.
        var psi = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/c {command}",
            WorkingDirectory = workingDirectory,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var process = Process.Start(psi) ?? throw new InvalidOperationException($"Failed to start process for command '{command}'.");
        process.StandardInput.Write(stdin);
        process.StandardInput.Close();
        string stdout = process.StandardOutput.ReadToEnd();
        string stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();
        if (process.ExitCode != 0)
            throw new InvalidOperationException($"Verify command '{command}' exited with code {process.ExitCode}. Stderr: {stderr}");
        return stdout;
    }

    private static void WriteJsonReport(
        string path, string unitId, string criterion, string corpusHash, string sourceCmd, string targetCmd,
        string sourceBasis, string targetBasis, IReadOnlyList<VerifyCaseResult> results, string verdict, string independence)
    {
        var resultsArray = new JsonArray();
        foreach (var r in results)
        {
            var obj = new JsonObject
            {
                ["name"] = r.Name,
                ["match"] = r.Match,
            };
            if (!r.Match)
            {
                obj["sourceResult"] = r.SourceResultJson is null ? null : JsonNode.Parse(r.SourceResultJson);
                obj["targetResult"] = r.TargetResultJson is null ? null : JsonNode.Parse(r.TargetResultJson);
                if (r.Reason is not null)
                    obj["reason"] = r.Reason;
            }
            resultsArray.Add(obj);
        }

        var report = new JsonObject
        {
            ["unit"] = unitId,
            ["criterion"] = criterion,
            ["corpusHash"] = corpusHash,
            ["sourceCmd"] = sourceCmd,
            ["targetCmd"] = targetCmd,
            ["sourceBasis"] = sourceBasis,
            ["targetBasis"] = targetBasis,
            ["results"] = resultsArray,
            ["verdict"] = verdict,
            ["independence"] = independence,
        };

        string json = report.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
        DomainFileIo.WriteLf(path, json);
    }

    private static void WriteMarkdownNotebook(
        string path, string unitId, string criterion, string casesFilePath, string sourceCmd, string targetCmd,
        string sourceBasis, string targetBasis, IReadOnlyList<VerifyCaseResult> results, string verdict,
        string rerunCommand, DateTimeOffset generatedAt)
    {
        int passCount = results.Count(r => r.Match);
        int failCount = results.Count - passCount;

        var sb = new StringBuilder();
        sb.Append("# Verify run: ").Append(unitId).Append(" / ").Append(criterion).Append("\n\n");
        // NB: this timestamp is display-only lab-notebook context — it is never embedded in the
        // JSON report, the report file names, or the claim value JSON, so it does not participate
        // in the content-addressed determinism guarantee (CONTRACT.md §8).
        sb.Append("- Generated: `").Append(generatedAt.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")).Append("`\n");
        sb.Append("- Cases file: `").Append(casesFilePath).Append("`\n");
        sb.Append("- Source basis: `").Append(sourceBasis).Append("`\n");
        sb.Append("- Target basis: `").Append(targetBasis).Append("`\n");
        sb.Append("- Source cmd: `").Append(sourceCmd).Append("`\n");
        sb.Append("- Target cmd: `").Append(targetCmd).Append("`\n");
        sb.Append("- Verdict: **").Append(verdict).Append("**\n");
        sb.Append("- Cases: ").Append(results.Count).Append(" total, ").Append(passCount).Append(" pass, ").Append(failCount).Append(" fail\n\n");

        if (failCount > 0)
        {
            sb.Append("## Mismatches\n\n");
            foreach (var r in results.Where(r => !r.Match))
            {
                sb.Append("- ").Append(r.Name);
                if (r.Reason is not null)
                    sb.Append(" (").Append(r.Reason).Append(')');
                sb.Append('\n');
            }
            sb.Append('\n');
        }

        sb.Append("## Rerun\n\n```\n").Append(rerunCommand).Append("\n```\n");
        DomainFileIo.WriteLf(path, sb.ToString());
    }
}
