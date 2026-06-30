using System.Collections.Generic;
using D4BuildForge.Engine;
using D4BuildForge.Engine.Core;
using D4BuildForge.Engine.Model;
using D4BuildForge.Engine.Sources;

namespace D4BuildForge.Engine.Tests;

public class DruidMaulAssemblyTests
{
    private static readonly Tag Werebear = new("Werebear");

    // LEGO bricks assembled by the test (the orchestrator's job): base stats + an item + Werebear form.
    private static Build AssembleMaulBuild(bool inWerebear)
    {
        var item = new SimpleItem("Two-Hander", new List<Modifier>
        {
            Modifier.Damage(BucketKey.Additive, 0.30, new SourceRef("Item", "Two-Hander")),
        });
        var form = new ShapeshiftForm(Werebear, new List<Modifier>
        {
            // Werebear-only +50% additive damage (representative number).
            Modifier.Damage(BucketKey.Additive, 0.50, new SourceRef("Form", "Werebear"), Werebear),
        });

        return new Build(
            Level: 80,
            Sources: new List<IModifierSource> { new BaseStatsForLevel(80, 800, 100), item, form },
            Skill: new SkillSelection("Maul", 0.45, 1),
            ActiveState: inWerebear ? new HashSet<Tag> { Werebear } : new HashSet<Tag>(),
            Target: new Target(81, 0));
    }

    [Fact]
    public void Werebear_form_modifier_applies_only_in_werebear()
    {
        double human = BuildCalculator.Calculate(AssembleMaulBuild(false), FormulaConfig.Druid).NonCrit;
        double bear = BuildCalculator.Calculate(AssembleMaulBuild(true), FormulaConfig.Druid).NonCrit;

        // Human: weapon 100 * mainStatMult 2.0 * additive(1+0.30=1.30) * 0.45 * 0.2 = 23.4
        Approx.Equal(23.4, human);
        // Werebear adds +0.50 to the additive bucket -> 1.80: 100*2.0*1.80*0.45*0.2 = 32.4
        Approx.Equal(32.4, bear);
    }
}
