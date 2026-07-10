using System.Globalization;

namespace Gneiss.Cell.Internal;

/// <summary>
/// The "hidden 40%" (THE-PAGE exclusions): typed-value comparability behind the strainer pipeline.
/// v0 per CONTRACT.md section 3 step 6 / D-R4: exact, numberTol, stringNorm.
/// </summary>
internal static class Comparators
{
    /// <summary>True iff two values are INCOMPATIBLE (i.e., a genuine conflict) under the predicate's comparator.</summary>
    internal static bool Incompatible(ResolvedPredicate pred, AssrtRow a, AssrtRow b)
    {
        switch (pred.Comparator)
        {
            case "numberTol":
                {
                    // DIVERGENCE: values that fail to parse as decimal under numberTol are treated as
                    // incompatible (a data-quality signal), rather than silently falling back to exact.
                    if (!decimal.TryParse(a.Val, NumberStyles.Number, CultureInfo.InvariantCulture, out var da) ||
                        !decimal.TryParse(b.Val, NumberStyles.Number, CultureInfo.InvariantCulture, out var db))
                    {
                        return true;
                    }
                    var absDiff = Math.Abs(da - db);
                    var tolAbs = pred.TolAbs ?? 0m;
                    if (absDiff <= tolAbs)
                    {
                        return false;
                    }
                    var tolRel = pred.TolRel ?? 0m;
                    var denom = Math.Max(Math.Abs(da), Math.Abs(db));
                    var relDiff = denom == 0m ? 0m : absDiff / denom;
                    return relDiff > tolRel;
                }
            case "stringNorm":
                {
                    var na = a.Val.Trim().ToLowerInvariant();
                    var nb = b.Val.Trim().ToLowerInvariant();
                    return na != nb;
                }
            case "exact":
            default:
                return a.Val != b.Val;
        }
    }

    /// <summary>Half-open interval overlap; NULL vfrom = -infinity, NULL vto = +infinity.</summary>
    internal static bool IntervalsOverlap(AssrtRow a, AssrtRow b)
    {
        // aFrom < bTo && bFrom < aTo
        bool aFromLtBTo = a.VFrom is null || b.VTo is null || string.CompareOrdinal(a.VFrom, b.VTo) < 0;
        bool bFromLtATo = b.VFrom is null || a.VTo is null || string.CompareOrdinal(b.VFrom, a.VTo) < 0;
        return aFromLtBTo && bFromLtATo;
    }

    /// <summary>True iff a's interval strictly contains b's interval (b is narrower, b properly inside a) and they are not equal.</summary>
    internal static bool StrictlyContains(AssrtRow a, AssrtRow b)
    {
        bool aLeLeqBLe = a.VFrom is null || (b.VFrom is not null && string.CompareOrdinal(a.VFrom, b.VFrom) <= 0);
        bool bReLeqARe = a.VTo is null || (b.VTo is not null && string.CompareOrdinal(b.VTo, a.VTo) <= 0);
        bool notEqual = !(string.Equals(a.VFrom, b.VFrom, StringComparison.Ordinal) && string.Equals(a.VTo, b.VTo, StringComparison.Ordinal));
        return aLeLeqBLe && bReLeqARe && notEqual;
    }
}
