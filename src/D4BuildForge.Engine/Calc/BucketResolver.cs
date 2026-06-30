using System.Collections.Generic;
using System.Linq;
using D4BuildForge.Engine.Core;

namespace D4BuildForge.Engine.Calc;

public static class BucketResolver
{
    public static double BucketValue(ModifierPool pool, FormulaConfig cfg, BucketKey key)
        => cfg.BucketBase(key) + pool.BucketSum(key);

    public static double HitMultiplier(ModifierPool pool, FormulaConfig cfg, IEnumerable<BucketKey> buckets)
        => buckets.Aggregate(1.0, (acc, b) => acc * BucketValue(pool, cfg, b));
}
