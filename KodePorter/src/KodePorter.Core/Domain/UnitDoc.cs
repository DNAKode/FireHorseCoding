using System.Text;

namespace KodePorter.Core.Domain;

/// <summary>
/// A unit dossier: units/&lt;id&gt;.md, YAML front matter + markdown body (CONTRACT.md §4).
/// </summary>
/// <param name="Depth">CONTRACT-M15.md §1.4: `thin|dossiered`. A typed judgment, set explicitly
/// via `kp unit set-depth` — never inferred from prose length. Default `thin`.</param>
public sealed record UnitDoc(
    string Id,
    string Name,
    string Status, // mapped | in-progress | accepted
    IReadOnlyList<AnchorRef> SourceAnchors,
    IReadOnlyList<AnchorRef> TargetAnchors,
    IReadOnlyList<string> Claims,
    bool Stale,
    string Purpose,
    string Contract,
    string Questions,
    string Evidence,
    string Depth = "thin");

public static class UnitYaml
{
    public static string UnitsDir(string workspaceDir) => Path.Combine(workspaceDir, ".kodeporter", "units");

    public static string FilePath(string workspaceDir, string unitId) => Path.Combine(UnitsDir(workspaceDir), $"{unitId}.md");

    /// <summary>All unit ids present in .kodeporter/units/, sorted ordinally.</summary>
    public static List<string> ListUnitIds(string workspaceDir)
    {
        string dir = UnitsDir(workspaceDir);
        if (!Directory.Exists(dir))
            return [];
        return Directory.EnumerateFiles(dir, "*.md")
            .Select(Path.GetFileNameWithoutExtension)
            .Where(id => id is not null)
            .Select(id => id!)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>Reads every unit doc in the workspace, sorted by id.</summary>
    public static List<UnitDoc> ReadAll(string workspaceDir) =>
        ListUnitIds(workspaceDir).Select(id => Read(workspaceDir, id)).ToList();

    public static void Write(string workspaceDir, UnitDoc doc)
    {
        var sb = new StringBuilder();
        sb.Append("---\n");
        sb.Append("id: ").Append(YamlScalarCodec.Quote(doc.Id)).Append('\n');
        sb.Append("name: ").Append(YamlScalarCodec.Quote(doc.Name)).Append('\n');
        sb.Append("status: ").Append(YamlScalarCodec.Quote(doc.Status)).Append('\n');
        WriteAnchorList(sb, "sourceAnchors", doc.SourceAnchors);
        WriteAnchorList(sb, "targetAnchors", doc.TargetAnchors);
        sb.Append("claims: ").Append(YamlFlowList.Write(doc.Claims)).Append('\n');
        if (doc.Stale)
            sb.Append("stale: true\n");
        // CONTRACT-M15.md §1.4: only written when non-default, mirroring the `stale` field's
        // sparse-when-default convention; a reader defaults to "thin" when the key is absent.
        if (doc.Depth != "thin")
            sb.Append("depth: ").Append(YamlScalarCodec.Quote(doc.Depth)).Append('\n');
        sb.Append("---\n");
        sb.Append('\n');
        AppendSection(sb, "Purpose", doc.Purpose);
        AppendSection(sb, "Contract", doc.Contract);
        AppendSection(sb, "Questions", doc.Questions);
        AppendSection(sb, "Evidence", doc.Evidence);
        DomainFileIo.WriteLf(FilePath(workspaceDir, doc.Id), sb.ToString());
    }

    private static void WriteAnchorList(StringBuilder sb, string key, IReadOnlyList<AnchorRef> anchors)
    {
        if (anchors.Count == 0)
        {
            sb.Append(key).Append(": []\n");
            return;
        }
        sb.Append(key).Append(":\n");
        foreach (var a in anchors)
        {
            sb.Append("  - symbolPath: ").Append(YamlScalarCodec.Quote(a.SymbolPath)).Append('\n');
            sb.Append("    basisLabel: ").Append(YamlScalarCodec.Quote(a.BasisLabel)).Append('\n');
            sb.Append("    contentHash: ").Append(YamlScalarCodec.Quote(a.ContentHash)).Append('\n');
        }
    }

    private static void AppendSection(StringBuilder sb, string title, string body)
    {
        sb.Append("## ").Append(title).Append("\n\n");
        string trimmed = body.Trim('\n');
        if (trimmed.Length > 0)
            sb.Append(trimmed).Append('\n');
        sb.Append('\n');
    }

    public static UnitDoc Read(string workspaceDir, string unitId)
    {
        var lines = DomainFileIo.ReadLines(FilePath(workspaceDir, unitId));
        int i = 0;
        if (lines.Count == 0 || lines[i] != "---")
            throw new FormatException("Unit doc must start with a '---' front-matter delimiter.");
        i++;

        string? id = null, name = null, status = null;
        var sourceAnchors = new List<AnchorRef>();
        var targetAnchors = new List<AnchorRef>();
        var claims = new List<string>();
        bool stale = false;
        string depth = "thin";

        while (i < lines.Count && lines[i] != "---")
        {
            string line = lines[i];
            if (line.StartsWith("sourceAnchors"))
            {
                i = ParseAnchorList(lines, i, sourceAnchors);
                continue;
            }
            if (line.StartsWith("targetAnchors"))
            {
                i = ParseAnchorList(lines, i, targetAnchors);
                continue;
            }
            var (key, value) = YamlLine.SplitKeyValue(line);
            switch (key)
            {
                case "id": id = YamlScalarCodec.Unquote(value); break;
                case "name": name = YamlScalarCodec.Unquote(value); break;
                case "status": status = YamlScalarCodec.Unquote(value); break;
                case "claims": claims = YamlFlowList.Parse(value); break;
                case "stale": stale = value == "true"; break;
                case "depth": depth = YamlScalarCodec.Unquote(value); break;
            }
            i++;
        }
        i++; // past the closing '---'

        var body = string.Join('\n', lines.Skip(i));
        var sections = ParseSections(body);

        return new UnitDoc(
            id ?? throw new FormatException("Unit doc is missing 'id'."),
            name ?? throw new FormatException("Unit doc is missing 'name'."),
            status ?? throw new FormatException("Unit doc is missing 'status'."),
            sourceAnchors,
            targetAnchors,
            claims,
            stale,
            sections.GetValueOrDefault("Purpose", ""),
            sections.GetValueOrDefault("Contract", ""),
            sections.GetValueOrDefault("Questions", ""),
            sections.GetValueOrDefault("Evidence", ""),
            depth);
    }

    private static int ParseAnchorList(List<string> lines, int i, List<AnchorRef> target)
    {
        var (_, value) = YamlLine.SplitKeyValue(lines[i]);
        i++;
        if (value == "[]")
            return i;

        while (i < lines.Count && lines[i].StartsWith("  - "))
        {
            string symbolPath = YamlScalarCodec.Unquote(YamlLine.SplitKeyValue(lines[i][4..]).Value);
            i++;
            string basisLabel = YamlScalarCodec.Unquote(YamlLine.SplitKeyValue(lines[i].Trim()).Value);
            i++;
            string contentHash = YamlScalarCodec.Unquote(YamlLine.SplitKeyValue(lines[i].Trim()).Value);
            i++;
            target.Add(new AnchorRef(symbolPath, basisLabel, contentHash));
        }
        return i;
    }

    private static Dictionary<string, string> ParseSections(string body)
    {
        var result = new Dictionary<string, string>();
        var lines = body.Split('\n');
        string? current = null;
        var buffer = new List<string>();

        void Flush()
        {
            if (current != null)
                result[current] = string.Join('\n', buffer).Trim('\n');
        }

        foreach (var line in lines)
        {
            if (line.StartsWith("## "))
            {
                Flush();
                current = line[3..].Trim();
                buffer = [];
            }
            else if (current != null)
            {
                buffer.Add(line);
            }
        }
        Flush();
        return result;
    }
}
