using System.Text.Json.Serialization;

namespace KodePorter.Core.Workspace;

/// <summary>The kp.json project descriptor (CONTRACT.md §1).</summary>
/// <param name="Name">Project name.</param>
/// <param name="Direction">e.g. "rust-&gt;csharp".</param>
/// <param name="SourceRoot">Source root as given (not normalized).</param>
/// <param name="TargetRoot">Target root as given (not normalized).</param>
/// <param name="PolicyRef">Optional reference to the governing policy (policy.yaml name/version).</param>
public sealed record ProjectDescriptor(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("direction")] string Direction,
    [property: JsonPropertyName("sourceRoot")] string SourceRoot,
    [property: JsonPropertyName("targetRoot")] string TargetRoot,
    [property: JsonPropertyName("policyRef")] string? PolicyRef = null);
