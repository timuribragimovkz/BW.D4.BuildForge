namespace D4BuildForge.Domain;

/// <summary>The dummy/enemy the build is measured against.</summary>
public sealed record TargetRef
{
    public int Level { get; init; }
    public double Armor { get; init; }
}
