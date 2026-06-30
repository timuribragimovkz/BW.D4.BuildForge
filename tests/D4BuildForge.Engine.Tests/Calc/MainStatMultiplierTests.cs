using D4BuildForge.Engine.Calc;

namespace D4BuildForge.Engine.Tests.Calc;

public class MainStatMultiplierTests
{
    [Fact] // Ava: 2428 main stat, divisor 800, no pooled mult -> 4.035
    public void Ava_example() => Approx.Equal(4.035, MainStatMultiplier.Compute(2428, 800, 0));

    [Fact] // 800/800 with +10% pooled -> 1 + 800*1.1/800 = 2.1
    public void With_pooled_multiplier() => Approx.Equal(2.1, MainStatMultiplier.Compute(800, 800, 0.10));
}
