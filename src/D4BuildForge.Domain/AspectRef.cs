namespace D4BuildForge.Domain;

/// <summary>A legendary aspect / unique power: a named bundle of modifiers.</summary>
public sealed record AspectRef
{
    public required string Name { get; init; }
    public IReadOnlyList<AffixEntry> Modifiers { get; init; } = [];
}
