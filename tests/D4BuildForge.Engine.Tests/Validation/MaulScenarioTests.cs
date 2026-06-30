using D4BuildForge.Engine;
using D4BuildForge.Engine.Core;

namespace D4BuildForge.Engine.Tests.Validation;

public class MaulScenarioTests
{
    [Fact]
    public void Seed_scenario_computes_hand_value()
    {
        // Seed(): Level 80, MainStat 800 (->mainStatMult 2.0), WeaponDamage 100,
        // Maul 0.45 @ 1 rank (->0.45), AdditivePct 0.30 (->1.30), everything else 0,
        // not vulnerable, in Werebear. NonCrit = 100*2.0*1.30*0.45*0.2 = 23.4
        var result = BuildCalculator.Calculate(MaulScenario.Seed().ToBuild(), Season.Current);
        Approx.Equal(23.4, result.NonCrit);
    }

    [Fact]
    public void Vulnerable_modifier_is_gated_off_when_target_not_vulnerable()
    {
        var notVuln = MaulScenario.Seed() with { VulnerablePct = 1.0, TargetVulnerable = false };
        var vuln = MaulScenario.Seed() with { VulnerablePct = 1.0, TargetVulnerable = true };
        Approx.Equal(23.4, BuildCalculator.Calculate(notVuln.ToBuild(), Season.Current).NonCrit);   // VDM gated off
        Approx.Equal(46.8, BuildCalculator.Calculate(vuln.ToBuild(), Season.Current).NonCrit);      // ×(1+1.0)=2.0
    }
}
