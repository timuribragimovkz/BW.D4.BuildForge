namespace D4BuildForge.Domain;

/// <summary>The selected skill + its base coefficient and rank count.</summary>
public sealed record SkillRef
{
    public required string Name { get; init; }
    public double BaseCoeff { get; init; }
    public int Ranks { get; init; }
}
