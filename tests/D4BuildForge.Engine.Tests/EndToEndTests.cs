using System.Collections.Generic;
using D4BuildForge.Engine;
using D4BuildForge.Engine.Core;
using D4BuildForge.Engine.Model;

namespace D4BuildForge.Engine.Tests;

public class EndToEndTests
{
    private sealed class FixedSource(params Modifier[] mods) : IModifierSource
    {
        public IEnumerable<Modifier> GetModifiers() => mods;
    }

    private static readonly SourceRef Gear = new("Item", "TestGear");

    // A small synthetic Druid build with round numbers, computed by hand:
    //   weapon flat 100, mainStat 800 (divisor 800 -> mainStatMult 2.0),
    //   additive +50% (-> 1.5), no vuln/csdm/admg extras (-> 1.0), skill 0.45 @ 1 rank (-> 0.45),
    //   global scalar 0.2, crit chance 0.
    //   NonCrit = 100 * 2.0 * 1.5 * 1.0 * 1.0 * 0.45 * 0.2 = 27.0
    private static Build SyntheticBuild() => new(
        Level: 80,
        Sources: new List<IModifierSource>
        {
            new FixedSource(
                Modifier.Flat(StatChannel.WeaponDamage, 100, Gear),
                Modifier.Flat(StatChannel.MainStat, 800, Gear),
                Modifier.Damage(BucketKey.Additive, 0.50, Gear))
        },
        Skill: new SkillSelection("Maul", 0.45, 1),
        ActiveState: new HashSet<Tag>(),
        Target: new Target(81, 0));

    [Fact]
    public void Computes_noncrit_end_to_end()
        => Approx.Equal(27.0, BuildCalculator.Calculate(SyntheticBuild(), FormulaConfig.Druid).NonCrit);

    [Fact]
    public void Breakdown_is_populated()
        => Assert.NotEmpty(BuildCalculator.Calculate(SyntheticBuild(), FormulaConfig.Druid).Breakdown.Lines);
}
