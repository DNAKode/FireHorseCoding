using System.Text;

namespace KodePorter.Core.Domain;

/// <summary>
/// The .kodeporter/policy.yaml domain file (CONTRACT.md §4) — the autonomy dial (K-A10). Claim
/// classes (e.g. "kpVerification", "kpBehavior") map to whether a green mechanical result may be
/// accepted by the policy actor without a human decision.
/// </summary>
/// <param name="RequiredIndependence">CONTRACT-M15.md §1.6: optional claim-class -> minimum
/// evidence-independence level (e.g. `{kpVerification: independently-derived}`). Null/absent ->
/// no constraint (existing behavior, back-compat with policy.yaml files written before this
/// field existed).</param>
public sealed record PolicyDoc(
    string Name,
    string Version,
    IReadOnlyDictionary<string, bool> AutoAccept,
    IReadOnlyDictionary<string, IReadOnlyList<string>> RequiredEvidence,
    IReadOnlyDictionary<string, string>? RequiredIndependence = null)
{
    /// <summary>The actor string used for policy-driven decisions (CONTRACT.md §5).</summary>
    public string ActorName => $"policy:{Name}@{Version}";
}

public static class PolicyYaml
{
    public static string FilePath(string workspaceDir) => Path.Combine(workspaceDir, ".kodeporter", "policy.yaml");

    public static void Write(string workspaceDir, PolicyDoc doc)
    {
        var sb = new StringBuilder();
        sb.Append("name: ").Append(YamlScalarCodec.Quote(doc.Name)).Append('\n');
        sb.Append("version: ").Append(YamlScalarCodec.Quote(doc.Version)).Append('\n');
        sb.Append("autoAccept:\n");
        foreach (var kv in doc.AutoAccept.OrderBy(k => k.Key, StringComparer.Ordinal))
            sb.Append("  ").Append(kv.Key).Append(": ").Append(kv.Value ? "true" : "false").Append('\n');
        sb.Append("requiredEvidence:\n");
        foreach (var kv in doc.RequiredEvidence.OrderBy(k => k.Key, StringComparer.Ordinal))
            sb.Append("  ").Append(kv.Key).Append(": ").Append(YamlFlowList.Write(kv.Value)).Append('\n');
        // CONTRACT-M15.md §1.6: optional section — omitted entirely (not even an empty block)
        // when absent, so policy.yaml files written before this field existed stay untouched by
        // a re-Write and Read() correctly reports null (no constraint) rather than an empty dict.
        if (doc.RequiredIndependence is not null)
        {
            sb.Append("requiredIndependence:\n");
            foreach (var kv in doc.RequiredIndependence.OrderBy(k => k.Key, StringComparer.Ordinal))
                sb.Append("  ").Append(kv.Key).Append(": ").Append(YamlScalarCodec.Quote(kv.Value)).Append('\n');
        }
        DomainFileIo.WriteLf(FilePath(workspaceDir), sb.ToString());
    }

    public static PolicyDoc Read(string workspaceDir)
    {
        var lines = DomainFileIo.ReadLines(FilePath(workspaceDir));
        string name = "", version = "";
        var autoAccept = new Dictionary<string, bool>();
        var requiredEvidence = new Dictionary<string, IReadOnlyList<string>>();
        Dictionary<string, string>? requiredIndependence = null;

        int i = 0;
        while (i < lines.Count)
        {
            if (string.IsNullOrWhiteSpace(lines[i]))
            {
                i++;
                continue;
            }
            var (key, value) = YamlLine.SplitKeyValue(lines[i]);
            if (key == "name")
            {
                name = YamlScalarCodec.Unquote(value);
                i++;
                continue;
            }
            if (key == "version")
            {
                version = YamlScalarCodec.Unquote(value);
                i++;
                continue;
            }
            if (key == "autoAccept")
            {
                i++;
                while (i < lines.Count && lines[i].StartsWith("  "))
                {
                    var (k2, v2) = YamlLine.SplitKeyValue(lines[i].Trim());
                    autoAccept[k2] = v2 == "true";
                    i++;
                }
                continue;
            }
            if (key == "requiredEvidence")
            {
                i++;
                while (i < lines.Count && lines[i].StartsWith("  "))
                {
                    var (k2, v2) = YamlLine.SplitKeyValue(lines[i].Trim());
                    requiredEvidence[k2] = YamlFlowList.Parse(v2);
                    i++;
                }
                continue;
            }
            if (key == "requiredIndependence")
            {
                requiredIndependence = [];
                i++;
                while (i < lines.Count && lines[i].StartsWith("  "))
                {
                    var (k2, v2) = YamlLine.SplitKeyValue(lines[i].Trim());
                    requiredIndependence[k2] = YamlScalarCodec.Unquote(v2);
                    i++;
                }
                continue;
            }
            i++;
        }

        return new PolicyDoc(name, version, autoAccept, requiredEvidence, requiredIndependence);
    }
}

/// <summary>The autonomy dial evaluation (CONTRACT.md §5/§6): whether a claim class may be
/// accepted by the policy actor given the current mechanical evidence.</summary>
public static class PolicyEngine
{
    // CONTRACT-M15.md §1.6: independently-derived is the strongest evidence, unknown the weakest
    // (caller did not attest independence at all).
    private static readonly IReadOnlyDictionary<string, int> IndependenceRank = new Dictionary<string, int>(StringComparer.Ordinal)
    {
        ["unknown"] = 0,
        ["implementation-coupled"] = 1,
        ["independently-derived"] = 2,
    };

    public static bool AllowsAutoAccept(PolicyDoc policy, string claimClass) =>
        policy.AutoAccept.TryGetValue(claimClass, out bool allowed) && allowed;

    /// <summary>
    /// CONTRACT-M15.md §1.6: whether <paramref name="actualIndependence"/> meets policy.yaml's
    /// optional `requiredIndependence` floor for <paramref name="claimClass"/>. Absent constraint
    /// (no `requiredIndependence` section, or no entry for this claim class) -> no constraint,
    /// i.e. always true (existing behavior preserved).
    /// </summary>
    public static bool MeetsIndependence(PolicyDoc policy, string claimClass, string actualIndependence)
    {
        if (policy.RequiredIndependence is null || !policy.RequiredIndependence.TryGetValue(claimClass, out string? required))
            return true;
        return RankOf(actualIndependence) >= RankOf(required);
    }

    /// <summary>Combines <see cref="AllowsAutoAccept(PolicyDoc,string)"/> with
    /// <see cref="MeetsIndependence"/> — the gate a caller with an independence-bearing claim
    /// (currently only kp.verification) should check before auto-accepting.</summary>
    public static bool AllowsAutoAccept(PolicyDoc policy, string claimClass, string actualIndependence) =>
        AllowsAutoAccept(policy, claimClass) && MeetsIndependence(policy, claimClass, actualIndependence);

    private static int RankOf(string level) => IndependenceRank.GetValueOrDefault(level, 0);
}
