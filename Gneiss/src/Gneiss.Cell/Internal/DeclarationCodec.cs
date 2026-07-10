using System.Globalization;
using System.Text.Json;

namespace Gneiss.Cell.Internal;

/// <summary>
/// Encodes/decodes the small set of JSON payloads Gneiss.Cell writes into `assrt.val` for its own
/// sugar (gneiss.context, gneiss.predicate, gneiss.decision). Encoding uses the hand-rolled
/// <see cref="CanonicalJsonWriter"/> (determinism-sensitive, per CONTRACT.md section 5). Decoding
/// uses System.Text.Json (part of the BCL, not a package) since only content we ourselves wrote in
/// a known fixed shape is ever parsed back — parsing is not the hash-sensitive direction.
/// </summary>
internal static class DeclarationCodec
{
    internal static string EncodeContextDecl(ContextDecl d) => CanonicalJsonWriter.ToJson(w => w.Obj(o => o
        .Field("name", d.Name)
        .FieldNullableLong("dataCut", d.DataCut)
        .FieldNullableLong("defCut", d.DefCut)
        .Field("admit", d.Admit)
        .FieldNullableInt("admitThresholdBp", d.AdmitThresholdBp)
        .Field("confPolicy", d.ConfPolicy)));

    internal static ContextDecl DecodeContextDecl(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var r = doc.RootElement;
        return new ContextDecl(
            Name: r.GetProperty("name").GetString()!,
            DataCut: r.GetProperty("dataCut").ValueKind == JsonValueKind.Null ? null : r.GetProperty("dataCut").GetInt64(),
            DefCut: r.GetProperty("defCut").ValueKind == JsonValueKind.Null ? null : r.GetProperty("defCut").GetInt64(),
            Admit: r.GetProperty("admit").GetString()!,
            AdmitThresholdBp: r.GetProperty("admitThresholdBp").ValueKind == JsonValueKind.Null ? null : r.GetProperty("admitThresholdBp").GetInt32(),
            ConfPolicy: r.GetProperty("confPolicy").GetString()!);
    }

    internal static string EncodePredicateDecl(PredicateDecl d) => CanonicalJsonWriter.ToJson(w => w.Obj(o => o
        .Field("name", d.Name)
        .Field("comparator", d.Comparator)
        .FieldNullableString("tolAbs", d.TolAbs?.ToString(CultureInfo.InvariantCulture))
        .FieldNullableString("tolRel", d.TolRel?.ToString(CultureInfo.InvariantCulture))
        .FieldLong("stopRung", d.StopRung)
        .FieldBool("instantSampled", d.InstantSampled)
        .FieldNullableStringArray("sourcePrecedence", d.SourcePrecedence)));

    internal static PredicateDecl DecodePredicateDecl(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var r = doc.RootElement;
        var tolAbsEl = r.GetProperty("tolAbs");
        var tolRelEl = r.GetProperty("tolRel");
        var spEl = r.GetProperty("sourcePrecedence");
        List<string>? sp = null;
        if (spEl.ValueKind == JsonValueKind.Array)
        {
            sp = new List<string>();
            foreach (var item in spEl.EnumerateArray())
            {
                sp.Add(item.GetString()!);
            }
        }
        return new PredicateDecl(
            Name: r.GetProperty("name").GetString()!,
            Comparator: r.GetProperty("comparator").GetString()!,
            TolAbs: tolAbsEl.ValueKind == JsonValueKind.Null ? null : decimal.Parse(tolAbsEl.GetString()!, CultureInfo.InvariantCulture),
            TolRel: tolRelEl.ValueKind == JsonValueKind.Null ? null : decimal.Parse(tolRelEl.GetString()!, CultureInfo.InvariantCulture),
            StopRung: r.GetProperty("stopRung").GetInt32(),
            InstantSampled: r.GetProperty("instantSampled").GetBoolean(),
            SourcePrecedence: sp);
    }

    internal static string EncodeDecision(DecisionKind kind, string? tgtAid, string? tgtCKey) => CanonicalJsonWriter.ToJson(w => w.Obj(o => o
        .Field("kind", KindToWire(kind))
        .FieldNullableString("tgtAid", tgtAid)
        .FieldNullableString("tgtCKey", tgtCKey)));

    internal static string KindToWire(DecisionKind kind) => kind switch
    {
        DecisionKind.Accepts => "accepts",
        DecisionKind.Rejects => "rejects",
        DecisionKind.Retracts => "retracts",
        DecisionKind.Supersedes => "supersedes",
        _ => throw new GneissException("InvalidArgument", $"Unknown decision kind {kind}"),
    };

    internal static DecisionKind KindFromWire(string wire) => wire switch
    {
        "accepts" => DecisionKind.Accepts,
        "rejects" => DecisionKind.Rejects,
        "retracts" => DecisionKind.Retracts,
        "supersedes" => DecisionKind.Supersedes,
        _ => throw new GneissException("InvalidArgument", $"Unknown decision kind '{wire}'"),
    };
}
