using System.Collections.Generic;

namespace D4BuildForge.Engine.Core;

public record FormulaConfig(
    double MainStatDivisor,
    double GlobalSkillScalar,
    double BaseCritMultiplier,
    IReadOnlyDictionary<string, double> BucketBases)
{
    public static FormulaConfig Druid { get; } = new(
        MainStatDivisor: 800,
        GlobalSkillScalar: 0.2,
        BaseCritMultiplier: 1.5,
        BucketBases: new Dictionary<string, double>());

    public double BucketBase(BucketKey key)
        => BucketBases.TryGetValue(key.Name, out var b) ? b : 1.0;
}
