namespace D4BuildForge.Domain;

/// <summary>One rolled stat on an item. Kind = explicit | implicit | tempered.</summary>
public sealed record AffixEntry
{
    public required string Kind { get; init; }
    public required string Stat { get; init; }
    public double Value { get; init; }
    public bool IsGreater { get; init; }
}
