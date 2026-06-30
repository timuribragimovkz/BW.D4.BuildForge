using D4BuildForge.Engine.Calc;

namespace D4BuildForge.Engine.Tests.Calc;

public class OffenseCalculatorTests
{
    // Ava ALL CLASSES worked example -> NonCrit 512128.786, Crit 3264821.011, Avg 2543615.648
    private static OffenseInputs AvaExample() => new(
        WeaponDamage: 4607, MainStatMult: 4.035,
        AdditiveNonCrit: 16.602, AdditiveCrit: 16.602,
        Vdm: 2.06, Csdm: 4.25, Admg: 2.21,
        SkillCoeff: 1.8225, GlobalScalar: 0.2, BaseCritMult: 1.5,
        CritChance: 0.738, SealDmg: 0, SealCritDmg: 0);

    [Fact]
    public void Reproduces_ava_noncrit() => Approx.Equal(512128.786, OffenseCalculator.Compute(AvaExample()).NonCrit, 1e-4);

    [Fact]
    public void Reproduces_ava_crit() => Approx.Equal(3264821.011, OffenseCalculator.Compute(AvaExample()).Crit, 1e-4);

    [Fact]
    public void Reproduces_ava_average() => Approx.Equal(2543615.648, OffenseCalculator.Compute(AvaExample()).Average, 1e-4);
}
