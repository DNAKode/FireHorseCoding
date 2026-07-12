using System.Reflection;
using Gneiss.Cell;

namespace Gneiss.Cell.Tests;

/// <summary>
/// CONTRACT.md section 2 / kb/32 solo-maintainer tripwire: keep the public surface small. Budget
/// raised from 20 to 22 by CONTRACT-V01.md section 4 (AppendResult and AssertionInfo), then to 23
/// by the CONTRACT-V01.md section 6 addendum (NoteInfo) — each a conscious, recorded adjustment;
/// see that file for the rationale.
/// </summary>
public sealed class PublicSurfaceTests
{
    [Fact]
    public void Public_Type_Count_Is_At_Most_TwentyThree()
    {
        var assembly = typeof(GneissLedger).Assembly;
        var publicTypes = assembly.GetTypes()
            .Where(t => t.IsPublic)
            .ToList();

        Assert.True(publicTypes.Count <= 23,
            $"Public type budget exceeded ({publicTypes.Count} > 23): {string.Join(", ", publicTypes.Select(t => t.Name))}");
    }
}
