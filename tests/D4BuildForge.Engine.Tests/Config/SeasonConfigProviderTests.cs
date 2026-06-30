using System.Collections.Generic;
using D4BuildForge.Engine.Config;
using D4BuildForge.Engine.Core;

namespace D4BuildForge.Engine.Tests.Config;

public class SeasonConfigProviderTests
{
    [Fact]
    public void Current_season_resolves_to_druid_tuning()
    {
        var cfg = new InMemorySeasonConfigProvider().Get(Season.Current);
        Assert.Equal(800, cfg.MainStatDivisor);
        Assert.Equal(0.2, cfg.GlobalSkillScalar);
        Assert.Equal(1.5, cfg.BaseCritMultiplier);
    }

    [Fact]
    public void Unknown_season_throws()
        => Assert.Throws<KeyNotFoundException>(
            () => new InMemorySeasonConfigProvider().Get(new Season("nope")));

    [Fact]
    public void Custom_map_is_honored()
    {
        var custom = new FormulaConfig(900, 0.25, 1.6, new Dictionary<string, double>());
        var provider = new InMemorySeasonConfigProvider(
            new Dictionary<string, FormulaConfig> { ["x"] = custom });
        Assert.Equal(900, provider.Get(new Season("x")).MainStatDivisor);
    }
}
