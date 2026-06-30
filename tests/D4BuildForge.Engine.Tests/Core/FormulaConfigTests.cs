using D4BuildForge.Engine.Core;

namespace D4BuildForge.Engine.Tests.Core;

public class FormulaConfigTests
{
    [Fact]
    public void Druid_defaults_match_ava_model()
    {
        var c = FormulaConfig.Druid;
        Assert.Equal(800, c.MainStatDivisor);
        Assert.Equal(0.2, c.GlobalSkillScalar);
        Assert.Equal(1.5, c.BaseCritMultiplier);
    }

    [Fact]
    public void Unknown_bucket_base_defaults_to_one()
        => Assert.Equal(1.0, FormulaConfig.Druid.BucketBase(BucketKey.Additive));
}
