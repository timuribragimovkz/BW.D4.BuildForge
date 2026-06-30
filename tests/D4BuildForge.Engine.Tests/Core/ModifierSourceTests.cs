using D4BuildForge.Engine.Core;

namespace D4BuildForge.Engine.Tests.Core;

public class ModifierSourceTests
{
    private sealed class FixedSource(params Modifier[] mods) : IModifierSource
    {
        public System.Collections.Generic.IEnumerable<Modifier> GetModifiers() => mods;
    }

    [Fact]
    public void Damage_helper_sets_bucket_and_conditions()
    {
        var src = new SourceRef("Item", "Ring");
        var m = Modifier.Damage(BucketKey.Additive, 0.18, src, new Tag("Close"));
        Assert.Equal(BucketKey.Additive, m.Bucket);
        Assert.Equal(ModOp.AdditivePercent, m.Op);
        Assert.Contains(new Tag("Close"), m.Conditions);
    }

    [Fact]
    public void Source_emits_its_modifiers()
    {
        var src = new SourceRef("Item", "Amulet");
        var source = new FixedSource(Modifier.Flat(StatChannel.CritChance, 0.05, src));
        Assert.Single(source.GetModifiers());
    }
}
