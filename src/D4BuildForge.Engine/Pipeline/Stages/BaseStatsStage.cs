using System.Collections.Generic;
using D4BuildForge.Engine.Core;

namespace D4BuildForge.Engine.Pipeline.Stages;

public sealed class BaseStatsStage : IStage
{
    public IReadOnlySet<string> Reads { get; } = new HashSet<string>();
    public IReadOnlySet<string> Writes { get; } = new HashSet<string> { Keys.MainStatSum, Keys.CritChance, Keys.AttackSpeed };

    public void Run(CalcContext ctx)
    {
        ctx.Set(Keys.MainStatSum, ctx.Pool.FlatSum(StatChannel.MainStat));
        ctx.Set(Keys.CritChance, ctx.Pool.FlatSum(StatChannel.CritChance));
        ctx.Set(Keys.AttackSpeed, 1 + ctx.Pool.FlatSum(StatChannel.AttackSpeed));
        ctx.Breakdown.Add(Keys.MainStatSum, ctx.Get(Keys.MainStatSum));
        ctx.Breakdown.Add(Keys.CritChance, ctx.Get(Keys.CritChance));
    }
}
