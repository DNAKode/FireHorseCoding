using System.Text;

namespace KodePorter.Core.Domain;

/// <summary>The .kodeporter/project.yaml domain file (CONTRACT.md §4).</summary>
public sealed record ProjectYamlDoc(
    string Name,
    string Direction,
    string SourceRoot,
    string TargetRoot,
    string? PolicyVersion);

public static class ProjectYaml
{
    public static string KodeporterDir(string workspaceDir) => Path.Combine(workspaceDir, ".kodeporter");

    public static string FilePath(string workspaceDir) => Path.Combine(KodeporterDir(workspaceDir), "project.yaml");

    public static void Write(string workspaceDir, ProjectYamlDoc doc)
    {
        var sb = new StringBuilder();
        sb.Append("name: ").Append(YamlScalarCodec.Quote(doc.Name)).Append('\n');
        sb.Append("direction: ").Append(YamlScalarCodec.Quote(doc.Direction)).Append('\n');
        sb.Append("sourceRoot: ").Append(YamlScalarCodec.Quote(doc.SourceRoot)).Append('\n');
        sb.Append("targetRoot: ").Append(YamlScalarCodec.Quote(doc.TargetRoot)).Append('\n');
        sb.Append("policyVersion: ").Append(doc.PolicyVersion is null ? "null" : YamlScalarCodec.Quote(doc.PolicyVersion)).Append('\n');
        DomainFileIo.WriteLf(FilePath(workspaceDir), sb.ToString());
    }

    public static ProjectYamlDoc Read(string workspaceDir)
    {
        var map = ReadFlatMap(FilePath(workspaceDir));
        return new ProjectYamlDoc(
            map["name"]!,
            map["direction"]!,
            map["sourceRoot"]!,
            map["targetRoot"]!,
            map.GetValueOrDefault("policyVersion"));
    }

    private static Dictionary<string, string?> ReadFlatMap(string path)
    {
        var result = new Dictionary<string, string?>();
        foreach (var line in DomainFileIo.ReadLines(path))
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;
            var (key, value) = YamlLine.SplitKeyValue(line);
            result[key] = value == "null" ? null : YamlScalarCodec.Unquote(value);
        }
        return result;
    }
}
