using System.Collections.Generic;

namespace D4BuildForge.Engine.Pipeline.Stages;

public sealed class DpsStage : IStage
{
    public IReadOnlySet<string> Reads { get; } = new HashSet<string> { Keys.Average, Keys.AttackSpeed };
    public IReadOnlySet<string> Writes { get; } = new HashSet<string> { Keys.Dps };

    public void Run(CalcContext ctx)
    {
        double dps = ctx.Get(Keys.Average) * ctx.Get(Keys.AttackSpeed);
        ctx.Set(Keys.Dps, dps);
        ctx.Breakdown.Add(Keys.Dps, dps);
    }
}
