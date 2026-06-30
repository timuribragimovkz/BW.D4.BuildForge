using System.Text.Json.Serialization;

namespace D4BuildForge.Domain;

/// <summary>A full build to compute + validate against the live game. Frozen contract (spec §3.2).</summary>
public sealed record BuildRecord
{
    public string Id { get; init; } = "";
    public required string Name { get; init; }
    public required string Season { get; init; }

    [JsonPropertyName("class")]
    public required string Class { get; init; }

    public int Level { get; init; }
    public required SkillRef Skill { get; init; }
    public IReadOnlyList<string> ItemIds { get; init; } = [];
    public IReadOnlyList<string> ActiveState { get; init; } = [];
    public required TargetRef Target { get; init; }

    /// <summary>The live-game tooltip number; null until measured.</summary>
    public double? ExpectedNonCrit { get; init; }
}
