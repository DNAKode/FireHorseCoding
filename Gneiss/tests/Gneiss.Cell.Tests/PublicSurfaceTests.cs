using System.Reflection;
using Gneiss.Cell;

namespace Gneiss.Cell.Tests;

/// <summary>CONTRACT.md section 2 / kb/32 solo-maintainer tripwire: keep the public surface small.</summary>
public sealed class PublicSurfaceTests
{
    [Fact]
    public void Public_Type_Count_Is_At_Most_Twenty()
    {
        var assembly = typeof(GneissLedger).Assembly;
        var publicTypes = assembly.GetTypes()
            .Where(t => t.IsPublic)
            .ToList();

        Assert.True(publicTypes.Count <= 20,
            $"Public type budget exceeded ({publicTypes.Count} > 20): {string.Join(", ", publicTypes.Select(t => t.Name))}");
    }
}
