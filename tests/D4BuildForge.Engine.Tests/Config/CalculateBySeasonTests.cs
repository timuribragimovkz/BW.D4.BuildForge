using System.Collections.Generic;
using D4BuildForge.Engine;
using D4BuildForge.Engine.Core;
using D4BuildForge.Engine.Model;
using D4BuildForge.Engine.Sources;

namespace D4BuildForge.Engine.Tests.Config;

public class CalculateBySeasonTests
{
    private static Build SimpleBuild() => new(
        Level: 80,
        Sources: new List<IModifierSource>
        {
            new BaseStatsForLevel(80, 800, 100),
            // additive +50% via the existing Damage helper:
            new SingleMod(Modifier.Damage(BucketKey.Additive, 0.50, new SourceRef("Test", "x")))
        },
        Skill: new SkillSelection("Maul", 0.45, 1),
        ActiveState: new HashSet<Tag>(),
        Target: new Target(81, 0));

    private sealed class SingleMod(Modifier m) : IModifierSource
    {
        public IEnumerable<Modifier> GetModifiers() => new[] { m };
    }

    [Fact]
    public void Season_overload_matches_direct_formulaconfig()
    {
        var bySeason = BuildCalculator.Calculate(SimpleBuild(), Season.Current);
        var direct = BuildCalculator.Calculate(SimpleBuild(), FormulaConfig.Druid);
        Approx.Equal(direct.NonCrit, bySeason.NonCrit);
        Approx.Equal(27.0, bySeason.NonCrit); // 100*2.0*1.5*0.45*0.2
    }
}
