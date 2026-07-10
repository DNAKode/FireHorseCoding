using KodePorter.Core.Model;

namespace KodePorter.Core.Tests.Support;

internal static class EntityAssertions
{
    public static Entity AssertEntity(
        IReadOnlyDictionary<string, Entity> entities,
        string symbolPath,
        string kind,
        string name,
        int startLine,
        int endLine,
        Entity? parent)
    {
        Assert.True(entities.TryGetValue(symbolPath, out var entity), $"Expected an entity with symbolPath '{symbolPath}'.");
        Assert.Equal(kind, entity!.Kind);
        Assert.Equal(name, entity.Name);
        Assert.Equal(startLine, entity.StartLine);
        Assert.Equal(endLine, entity.EndLine);
        Assert.Equal(parent?.Id, entity.ParentId);
        return entity;
    }
}
