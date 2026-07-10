using System.Text;

namespace KodePorter.Core.Domain;

/// <summary>One correspondences.yaml entry (CONTRACT.md §4).</summary>
public sealed record Correspondence(
    string Id,
    string Type, // implements | maps-to | adapts | diverges | covers
    string? DivergenceKind, // adaptation | exception | intended | observed | unresolved; only when Type is diverges/adapts
    string Unit,
    AnchorRef? Source,
    AnchorRef? Target,
    string? Criterion, // io-agreement-v1 | api-shape-v1 | error-semantics-v1 | null
    string? Note,
    string? ClaimAid,
    bool Stale = false);

public static class CorrespondencesYaml
{
    public static string FilePath(string workspaceDir) => Path.Combine(workspaceDir, ".kodeporter", "correspondences.yaml");

    public static void Write(string workspaceDir, IReadOnlyList<Correspondence> items)
    {
        var sb = new StringBuilder();
        var ordered = items.OrderBy(c => c.Id, StringComparer.Ordinal).ToList();
        if (ordered.Count == 0)
        {
            sb.Append("[]\n");
        }
        else
        {
            foreach (var c in ordered)
            {
                sb.Append("- id: ").Append(YamlScalarCodec.Quote(c.Id)).Append('\n');
                sb.Append("  type: ").Append(YamlScalarCodec.Quote(c.Type)).Append('\n');
                if (c.DivergenceKind is not null)
                    sb.Append("  divergenceKind: ").Append(YamlScalarCodec.Quote(c.DivergenceKind)).Append('\n');
                sb.Append("  unit: ").Append(YamlScalarCodec.Quote(c.Unit)).Append('\n');
                WriteAnchorField(sb, "source", c.Source);
                WriteAnchorField(sb, "target", c.Target);
                sb.Append("  criterion: ").Append(c.Criterion is null ? "null" : YamlScalarCodec.Quote(c.Criterion)).Append('\n');
                sb.Append("  note: ").Append(c.Note is null ? "null" : YamlScalarCodec.Quote(c.Note)).Append('\n');
                sb.Append("  claimAid: ").Append(c.ClaimAid is null ? "null" : YamlScalarCodec.Quote(c.ClaimAid)).Append('\n');
                if (c.Stale)
                    sb.Append("  stale: true\n");
            }
        }
        DomainFileIo.WriteLf(FilePath(workspaceDir), sb.ToString());
    }

    private static void WriteAnchorField(StringBuilder sb, string key, AnchorRef? anchor)
    {
        if (anchor is null)
        {
            sb.Append("  ").Append(key).Append(": null\n");
            return;
        }
        sb.Append("  ").Append(key).Append(":\n");
        sb.Append("    symbolPath: ").Append(YamlScalarCodec.Quote(anchor.SymbolPath)).Append('\n');
        sb.Append("    basisLabel: ").Append(YamlScalarCodec.Quote(anchor.BasisLabel)).Append('\n');
        sb.Append("    contentHash: ").Append(YamlScalarCodec.Quote(anchor.ContentHash)).Append('\n');
    }

    public static List<Correspondence> Read(string workspaceDir)
    {
        string path = FilePath(workspaceDir);
        if (!File.Exists(path))
            return [];

        var lines = DomainFileIo.ReadLines(path);
        var result = new List<Correspondence>();

        int i = 0;
        while (i < lines.Count && string.IsNullOrWhiteSpace(lines[i]))
            i++;
        if (i >= lines.Count || lines[i] == "[]")
            return result;

        while (i < lines.Count)
        {
            if (string.IsNullOrWhiteSpace(lines[i]))
            {
                i++;
                continue;
            }
            if (!lines[i].StartsWith("- "))
                throw new FormatException($"Expected a correspondence entry starting with '- '; got '{lines[i]}'.");

            string id = "", type = "", unit = "";
            string? divergenceKind = null, criterion = null, note = null, claimAid = null;
            AnchorRef? source = null, target = null;
            bool stale = false;

            var (firstKey, firstVal) = YamlLine.SplitKeyValue(lines[i][2..]);
            ApplyField(firstKey, firstVal, ref id, ref type, ref unit, ref divergenceKind, ref criterion, ref note, ref claimAid, ref stale);
            i++;

            while (i < lines.Count && lines[i].StartsWith("  ") && !lines[i].StartsWith("- "))
            {
                var (key, value) = YamlLine.SplitKeyValue(lines[i].TrimStart());
                if (key is "source" or "target")
                {
                    if (value.Length == 0)
                    {
                        i++;
                        string sp = YamlScalarCodec.Unquote(YamlLine.SplitKeyValue(lines[i].Trim()).Value);
                        i++;
                        string bl = YamlScalarCodec.Unquote(YamlLine.SplitKeyValue(lines[i].Trim()).Value);
                        i++;
                        string ch = YamlScalarCodec.Unquote(YamlLine.SplitKeyValue(lines[i].Trim()).Value);
                        i++;
                        var anchor = new AnchorRef(sp, bl, ch);
                        if (key == "source") source = anchor; else target = anchor;
                    }
                    else
                    {
                        // "source: null" / "target: null" — fields already default to null.
                        i++;
                    }
                    continue;
                }

                ApplyField(key, value, ref id, ref type, ref unit, ref divergenceKind, ref criterion, ref note, ref claimAid, ref stale);
                i++;
            }

            result.Add(new Correspondence(id, type, divergenceKind, unit, source, target, criterion, note, claimAid, stale));
        }

        return result;
    }

    private static void ApplyField(
        string key, string rawValue,
        ref string id, ref string type, ref string unit,
        ref string? divergenceKind, ref string? criterion, ref string? note, ref string? claimAid, ref bool stale)
    {
        string? val = rawValue == "null" ? null : YamlScalarCodec.Unquote(rawValue);
        switch (key)
        {
            case "id": id = val!; break;
            case "type": type = val!; break;
            case "unit": unit = val!; break;
            case "divergenceKind": divergenceKind = val; break;
            case "criterion": criterion = val; break;
            case "note": note = val; break;
            case "claimAid": claimAid = val; break;
            case "stale": stale = rawValue == "true"; break;
        }
    }
}
