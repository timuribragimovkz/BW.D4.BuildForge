using D4BuildForge.Engine.Calc;
using D4BuildForge.Engine.Core;
using D4BuildForge.Engine.Model;
using D4BuildForge.Engine.Pipeline;
using D4BuildForge.Engine.Pipeline.Stages;

namespace D4BuildForge.Engine;

public static class BuildCalculator
{
    public static CalcResult Calculate(Build build, FormulaConfig cfg)
    {
        var pool = ModifierPool.From(build.Sources, build.ActiveState);
        var ctx = new CalcContext(build, cfg, pool, new Breakdown());
        new CalcPipeline(new BaseStatsStage(), new OffenseBucketsStage(), new DpsStage()).Run(ctx);
        return new CalcResult(
            NonCrit: ctx.Get(Keys.NonCrit),
            Crit: ctx.Get(Keys.Crit),
            Average: ctx.Get(Keys.Average),
            Dps: ctx.Get(Keys.Dps),
            Breakdown: ctx.Breakdown);
    }
}
