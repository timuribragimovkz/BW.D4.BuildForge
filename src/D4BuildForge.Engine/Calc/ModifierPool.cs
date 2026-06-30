using System.Collections.Generic;
using System.Linq;
using D4BuildForge.Engine.Core;

namespace D4BuildForge.Engine.Calc;

public sealed class ModifierPool
{
    private readonly IReadOnlyList<Modifier> _active;

    private ModifierPool(IReadOnlyList<Modifier> active) => _active = active;

    public static ModifierPool From(IEnumerable<IModifierSource> sources, IReadOnlySet<Tag> activeState)
    {
        var active = sources
            .SelectMany(s => s.GetModifiers())
            .Where(m => m.Conditions.All(activeState.Contains))
            .ToList();
        return new ModifierPool(active);
    }

    public double BucketSum(BucketKey key)
        => _active.Where(m => m.Op == ModOp.AdditivePercent && m.Bucket == key).Sum(m => m.Value);

    public double FlatSum(StatChannel channel)
        => _active.Where(m => m.Op == ModOp.Flat && m.Channel == channel).Sum(m => m.Value);

    public IReadOnlyList<Modifier> ActiveInBucket(BucketKey key)
        => _active.Where(m => m.Op == ModOp.AdditivePercent && m.Bucket == key).ToList();
}
