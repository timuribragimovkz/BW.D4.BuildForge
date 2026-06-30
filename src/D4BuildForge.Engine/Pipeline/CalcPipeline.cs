using System.Collections.Generic;

namespace D4BuildForge.Engine.Pipeline;

public sealed class CalcPipeline
{
    private readonly IReadOnlyList<IStage> _stages;
    public CalcPipeline(params IStage[] stages) => _stages = stages;

    public void Run(CalcContext ctx)
    {
        foreach (var stage in _stages)
        {
            foreach (var need in stage.Reads)
                if (!ctx.Has(need))
                    throw new InvalidOperationException(
                        $"Stage {stage.GetType().Name} reads '{need}' before any stage wrote it.");
            stage.Run(ctx);
        }
    }
}
