using D4BuildForge.Engine.Calc;

namespace D4BuildForge.Engine.Tests.Calc;

public class SkillCoefficientTests
{
    [Fact] // Ava worked example: baseCoeff 0.45, 29 ranks -> 1.8225
    public void Ava_example_29_ranks() => Approx.Equal(1.8225, SkillCoefficient.Compute(0.45, 29));

    [Fact] // 1 rank -> base coefficient unchanged
    public void One_rank_is_base() => Approx.Equal(0.45, SkillCoefficient.Compute(0.45, 1));

    [Fact] // 5 ranks: 0.45*(1 + 0.1*3 + 0.15*1) = 0.6525
    public void Five_ranks() => Approx.Equal(0.6525, SkillCoefficient.Compute(0.45, 5));
}
