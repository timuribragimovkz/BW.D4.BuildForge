using System.Collections.Generic;
using D4BuildForge.Engine.Calc;
using D4BuildForge.Engine.Core;

namespace D4BuildForge.Engine.Tests.Calc;

public class BucketResolverTests
{
    private sealed class FixedSource(params Modifier[] mods) : IModifierSource
    {
        public IEnumerable<Modifier> GetModifiers() => mods;
    }

    private static readonly SourceRef Src = new("Test", "x");

    [Fact]
    public void Additive_sources_in_a_bucket_sum()
    {
        var pool = ModifierPool.From(new[] { new FixedSource(
            Modifier.Damage(BucketKey.Additive, 0.30, Src),
            Modifier.Damage(BucketKey.Additive, 0.18, Src)) }, new HashSet<Tag>());
        // base 1.0 + 0.30 + 0.18 = 1.48
        Approx.Equal(1.48, BucketResolver.BucketValue(pool, FormulaConfig.Druid, BucketKey.Additive));
    }

    [Fact]
    public void Conditional_modifier_only_counts_when_tag_active()
    {
        var werebear = new Tag("Werebear");
        var src = new FixedSource(Modifier.Damage(BucketKey.Additive, 0.50, Src, werebear));

        var inactive = ModifierPool.From(new[] { src }, new HashSet<Tag>());
        Approx.Equal(1.0, BucketResolver.BucketValue(inactive, FormulaConfig.Druid, BucketKey.Additive));

        var active = ModifierPool.From(new[] { src }, new HashSet<Tag> { werebear });
        Approx.Equal(1.5, BucketResolver.BucketValue(active, FormulaConfig.Druid, BucketKey.Additive));
    }

    [Fact]
    public void HitMultiplier_multiplies_buckets()
    {
        var pool = ModifierPool.From(new[] { new FixedSource(
            Modifier.Damage(BucketKey.Additive, 0.50, Src),       // -> 1.5
            Modifier.Damage(BucketKey.Vulnerable, 1.0, Src)) },   // -> 2.0
            new HashSet<Tag>());
        Approx.Equal(3.0, BucketResolver.HitMultiplier(pool, FormulaConfig.Druid,
            new[] { BucketKey.Additive, BucketKey.Vulnerable }));
    }
}
