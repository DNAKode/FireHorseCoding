using System.Text;

namespace KodePorter.Core.Domain;

/// <summary>
/// One manually-recorded override in .kodeporter/absences.yaml (CONTRACT-M15.md §1.5). This file
/// holds ONLY overrides — the computed default classification for every eligible, uncovered
/// entity (source: `unknown`; target-only: `unexplained`) is never written here; it is computed
/// at read time by <see cref="Absence.AbsenceCalculator"/> and only overridden when a matching
/// row exists in this file. Recorded via `kp absence set --symbol &lt;sp&gt; --kind &lt;k&gt;
/// [--note &lt;s&gt;]`.
/// </summary>
/// <param name="SymbolPath">The source (or, when <paramref name="Side"/> is "target", target)
/// entity's symbolPath.</param>
/// <param name="Kind">Source side: `not-yet-ported | deliberately-dropped | unknown`. Target
/// side: `intentional | unexplained`.</param>
/// <param name="Note">Optional free-form note.</param>
/// <param name="Side">`source` (default) or `target`.</param>
public sealed record AbsenceOverride(string SymbolPath, string Kind, string? Note, string Side = "source");

public static class AbsencesYaml
{
    public static string FilePath(string workspaceDir) => Path.Combine(workspaceDir, ".kodeporter", "absences.yaml");

    public static void Write(string workspaceDir, IReadOnlyList<AbsenceOverride> overrides)
    {
        var sb = new StringBuilder();
        var ordered = overrides
            .OrderBy(o => o.Side, StringComparer.Ordinal)
            .ThenBy(o => o.SymbolPath, StringComparer.Ordinal)
            .ToList();
        if (ordered.Count == 0)
        {
            sb.Append("[]\n");
        }
        else
        {
            foreach (var o in ordered)
            {
                sb.Append("- symbolPath: ").Append(YamlScalarCodec.Quote(o.SymbolPath)).Append('\n');
                sb.Append("  side: ").Append(YamlScalarCodec.Quote(o.Side)).Append('\n');
                sb.Append("  kind: ").Append(YamlScalarCodec.Quote(o.Kind)).Append('\n');
                sb.Append("  note: ").Append(o.Note is null ? "null" : YamlScalarCodec.Quote(o.Note)).Append('\n');
            }
        }
        DomainFileIo.WriteLf(FilePath(workspaceDir), sb.ToString());
    }

    /// <summary>Returns an empty list when the file does not exist (no overrides recorded yet).</summary>
    public static List<AbsenceOverride> Read(string workspaceDir)
    {
        string path = FilePath(workspaceDir);
        if (!File.Exists(path))
            return [];

        var lines = DomainFileIo.ReadLines(path);
        var result = new List<AbsenceOverride>();

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
                throw new FormatException($"Expected an absence entry starting with '- '; got '{lines[i]}'.");

            string symbolPath = "", kind = "";
            string? note = null;
            string side = "source"; // CONTRACT-M15.md §1.5: absent -> source (target rows always carry side: target).

            var (firstKey, firstVal) = YamlLine.SplitKeyValue(lines[i][2..]);
            ApplyField(firstKey, firstVal, ref symbolPath, ref kind, ref note, ref side);
            i++;

            while (i < lines.Count && lines[i].StartsWith("  ") && !lines[i].StartsWith("- "))
            {
                var (key, value) = YamlLine.SplitKeyValue(lines[i].Trim());
                ApplyField(key, value, ref symbolPath, ref kind, ref note, ref side);
                i++;
            }

            result.Add(new AbsenceOverride(symbolPath, kind, note, side));
        }

        return result;
    }

    private static void ApplyField(string key, string rawValue, ref string symbolPath, ref string kind, ref string? note, ref string side)
    {
        string? val = rawValue == "null" ? null : YamlScalarCodec.Unquote(rawValue);
        switch (key)
        {
            case "symbolPath": symbolPath = val!; break;
            case "kind": kind = val!; break;
            case "note": note = val; break;
            case "side": side = val ?? "source"; break;
        }
    }
}
