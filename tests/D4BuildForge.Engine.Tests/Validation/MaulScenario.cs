using System.Collections.Generic;
using D4BuildForge.Engine.Core;
using D4BuildForge.Engine.Model;
using D4BuildForge.Engine.Sources;

namespace D4BuildForge.Engine.Tests.Validation;

public record MaulScenario(
    int Level,
    double MainStat,
    double WeaponDamage,
    double MaulBaseCoeff,
    int MaulRanks,
    double AdditivePct,
    double AllDamagePct,
    double CritChance,
    double CritDamagePct,
    double AttackSpeedPct,
    double VulnerablePct,
    bool TargetVulnerable,
    bool InWerebear,
    Season Season)
{
    public static MaulScenario Seed() => new(
        Level: 80, MainStat: 800, WeaponDamage: 100,
        MaulBaseCoeff: 0.45, MaulRanks: 1,
        AdditivePct: 0.30, AllDamagePct: 0, CritChance: 0, CritDamagePct: 0,
        AttackSpeedPct: 0, VulnerablePct: 0, TargetVulnerable: false, InWerebear: true,
        Season: Season.Current);

    public Build ToBuild()
    {
        var state = new HashSet<Tag>();
        if (InWerebear) state.Add(new Tag("Werebear"));
        if (TargetVulnerable) state.Add(new Tag("Vulnerable"));

        return new Build(
            Level: Level,
            Sources: new List<IModifierSource>
            {
                new BaseStatsForLevel(Level, MainStat, WeaponDamage),
                new ScenarioStats(this),
            },
            Skill: new SkillSelection("Maul", MaulBaseCoeff, MaulRanks),
            ActiveState: state,
            Target: new Target(Level + 1, 0));
    }

    private sealed class ScenarioStats(MaulScenario s) : IModifierSource
    {
        public IEnumerable<Modifier> GetModifiers()
        {
            var src = new SourceRef("Scenario", "Maul");
            yield return Modifier.Flat(StatChannel.CritChance, s.CritChance, src);
            yield return Modifier.Flat(StatChannel.AttackSpeed, s.AttackSpeedPct, src);
            yield return Modifier.Damage(BucketKey.Additive, s.AdditivePct, src);
            yield return Modifier.Damage(BucketKey.AllDamage, s.AllDamagePct, src);
            yield return Modifier.Damage(BucketKey.CritDamage, s.CritDamagePct, src);
            yield return Modifier.Damage(BucketKey.Vulnerable, s.VulnerablePct, src, new Tag("Vulnerable"));
        }
    }
}
