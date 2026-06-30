namespace D4BuildForge.Domain;

/// <summary>A single D4 item — general enough for every slot/rarity. Frozen contract (spec §3.1).</summary>
public sealed record ItemRecord
{
    public string Id { get; init; } = "";
    public string? SourceId { get; init; }
    public required string Name { get; init; }
    public required string Slot { get; init; }
    public string? ItemType { get; init; }
    public required string Rarity { get; init; }
    public int ItemPower { get; init; }
    public string? ClassRestriction { get; init; }
    public IReadOnlyList<AffixEntry> Affixes { get; init; } = [];
    public AspectRef? Aspect { get; init; }
    public IReadOnlyList<string?> Sockets { get; init; } = [];
}
