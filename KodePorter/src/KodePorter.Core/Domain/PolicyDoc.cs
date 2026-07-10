using System.Text;

namespace KodePorter.Core.Domain;

/// <summary>
/// The .kodeporter/policy.yaml domain file (CONTRACT.md §4) — the autonomy dial (K-A10). Claim
/// classes (e.g. "kpVerification", "kpBehavior") map to whether a green mechanical result may be
/// accepted by the policy actor without a human decision.
/// </summary>
public sealed record PolicyDoc(
    string Name,
    string Version,
    IReadOnlyDictionary<string, bool> AutoAccept,
    IReadOnlyDictionary<string, IReadOnlyList<string>> RequiredEvidence)
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
        DomainFileIo.WriteLf(FilePath(workspaceDir), sb.ToString());
    }

    public static PolicyDoc Read(string workspaceDir)
    {
        var lines = DomainFileIo.ReadLines(FilePath(workspaceDir));
        string name = "", version = "";
        var autoAccept = new Dictionary<string, bool>();
        var requiredEvidence = new Dictionary<string, IReadOnlyList<string>>();

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
            i++;
        }

        return new PolicyDoc(name, version, autoAccept, requiredEvidence);
    }
}

/// <summary>The autonomy dial evaluation (CONTRACT.md §5/§6): whether a claim class may be
/// accepted by the policy actor given the current mechanical evidence.</summary>
public static class PolicyEngine
{
    public static bool AllowsAutoAccept(PolicyDoc policy, string claimClass) =>
        policy.AutoAccept.TryGetValue(claimClass, out bool allowed) && allowed;
}
