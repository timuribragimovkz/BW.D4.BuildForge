using D4BuildForge.Assembly;
using D4BuildForge.Domain;
using D4BuildForge.Engine;
using D4BuildForge.Engine.Core;

namespace D4BuildForge.Assembly.Tests;

public class BuildAssemblerTests
{
    private static ItemRecord Gear(params AffixEntry[] affixes) => new()
    {
        Id = "item_seed",
        Name = "Seed Gear",
        Slot = "Weapon",
        Rarity = "Legendary",
        Affixes = affixes,
    };

    private static BuildRecord Build(string itemId, IReadOnlyList<string>? state = null, int ranks = 1) => new()
    {
        Id = "build_seed",
        Name = "Seed",
        Season = "s13",
        Class = "Druid",
        Level = 80,
        Skill = new SkillRef { Name = "Maul", BaseCoeff = 0.45, Ranks = ranks },
        ItemIds = [itemId],
        ActiveState = state ?? ["Werebear"],
        Target = new TargetRef { Level = 81, Armor = 0 },
    };

    private static readonly AffixEntry MainStat800 = new() { Kind = "explicit", Stat = "MainStat", Value = 800 };
    private static readonly AffixEntry Weapon100   = new() { Kind = "implicit", Stat = "WeaponDamage", Value = 100 };
    private static readonly AffixEntry Additive30  = new() { Kind = "explicit", Stat = "Additive", Value = 0.30 };

    [Fact]
    public void Assembles_seed_build_to_known_engine_value()
    {
        var gear = Gear(MainStat800, Weapon100, Additive30);
        var assembled = BuildAssembler.Assemble(Build(gear.Id), [gear]);
        var result = BuildCalculator.Calculate(assembled, Season.Current);

        // 100 * (1 + 800/800)=2.0 * (1 + 0.30)=1.30 * baseCoeff 0.45 @ rank1 (x1.0) * scalar 0.2 = 23.4
        Assert.Equal(23.4, result.NonCrit, 3);
    }

    [Fact]
    public void Vulnerable_affix_is_gated_by_active_state()
    {
        var gear = Gear(MainStat800, Weapon100, Additive30,
            new AffixEntry { Kind = "tempered", Stat = "Vulnerable", Value = 1.0 });

        var notVuln = BuildAssembler.Assemble(Build(gear.Id, ["Werebear"]), [gear]);
        var vuln    = BuildAssembler.Assemble(Build(gear.Id, ["Werebear", "Vulnerable"]), [gear]);

        Assert.Equal(23.4, BuildCalculator.Calculate(notVuln, Season.Current).NonCrit, 3); // gated OFF
        Assert.Equal(46.8, BuildCalculator.Calculate(vuln, Season.Current).NonCrit, 3);    // x(1+1.0)=2.0
    }

    [Fact]
    public void Skill_rank_affix_increments_total_ranks()
    {
        var r1Gear = Gear(MainStat800, Weapon100, Additive30);
        var r2Gear = Gear(MainStat800, Weapon100, Additive30,
            new AffixEntry { Kind = "tempered", Stat = "MaulRanks", Value = 1 });

        var r1 = BuildCalculator.Calculate(BuildAssembler.Assemble(Build(r1Gear.Id), [r1Gear]), Season.Current).NonCrit;
        var r2 = BuildCalculator.Calculate(BuildAssembler.Assemble(Build(r2Gear.Id), [r2Gear]), Season.Current).NonCrit;

        // rank1 -> coeff x1.0 (=23.4); rank2 -> coeff x1.10 -> 25.74
        Assert.Equal(23.4, r1, 3);
        Assert.Equal(25.74, r2, 3);
    }

    [Fact]
    public void Aspect_modifiers_are_assembled_too()
    {
        var gear = Gear(MainStat800, Weapon100) with
        {
            Aspect = new AspectRef { Name = "Aspect of Wrath", Modifiers = [Additive30] },
        };
        var result = BuildCalculator.Calculate(BuildAssembler.Assemble(Build(gear.Id), [gear]), Season.Current);
        Assert.Equal(23.4, result.NonCrit, 3); // aspect's Additive 0.30 counted
    }

    [Fact]
    public void Unknown_affix_stat_throws_naming_the_stat()
    {
        var gear = Gear(new AffixEntry { Kind = "explicit", Stat = "Nonsense", Value = 1 });
        var ex = Assert.Throws<AssemblyException>(() => BuildAssembler.Assemble(Build(gear.Id), [gear]));
        Assert.Contains("Nonsense", ex.Message);
    }
}
