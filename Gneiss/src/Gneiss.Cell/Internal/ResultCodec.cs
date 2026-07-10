namespace Gneiss.Cell.Internal;

/// <summary>Canonical serialization of a belief-view result, for ResultHash (CONTRACT.md section 3 step 9).</summary>
internal static class ResultCodec
{
    internal static string BuildResultJson(
        IReadOnlyList<BeliefEntry> accepted,
        IReadOnlyList<BeliefEntry> defeated,
        IReadOnlyList<ContestedGroup> contested,
        TypedMissing? missing) => CanonicalJsonWriter.ToJson(w => w.Obj(o => o
            .FieldRaw("accepted", w2 => w2.Arr(accepted, WriteEntry))
            .FieldRaw("defeated", w2 => w2.Arr(defeated, WriteEntry))
            .FieldRaw("contested", w2 => w2.Arr(contested, WriteContested))
            .FieldNullableObj("missing", missing, (w2, m) => w2.Field("kind", m.Kind))));

    private static void WriteEntry(CanonicalJsonWriter w, BeliefEntry e) => w.Obj(o => o
        .Field("aid", e.Aid)
        .Field("subj", e.Subject)
        .Field("pred", e.Predicate)
        .Field("valKind", e.Value.Kind)
        .Field("val", e.Value.Canonical)
        .Field("ckey", e.ClaimKey)
        .FieldBool("autoAdmitted", e.AutoAdmitted)
        .FieldBool("stale", e.StaleViaJustification)
        .FieldNullableString("defeatedBy", e.DefeatedBy)
        .FieldNullableString("defeatReason", e.DefeatReason));

    private static void WriteContested(CanonicalJsonWriter w, ContestedGroup c) => w.Obj(o => o
        .Field("ckey", c.ClaimKey)
        .FieldStringArray("aids", c.Aids)
        .FieldLong("stoppedAtRung", c.StoppedAtRung));
}
