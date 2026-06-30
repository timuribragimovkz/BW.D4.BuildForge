using System.Collections.Generic;
using D4BuildForge.Engine.Core;
using D4BuildForge.Engine.Model;
using D4BuildForge.Engine.Pipeline;
using D4BuildForge.Engine.Calc;

namespace D4BuildForge.Engine.Tests.Pipeline;

public class PipelineTests
{
    private sealed class WriteStage(string key, double val) : IStage
    {
        public IReadOnlySet<string> Reads { get; } = new HashSet<string>();
        public IReadOnlySet<string> Writes { get; } = new HashSet<string> { key };
        public void Run(CalcContext ctx) => ctx.Set(key, val);
    }

    private sealed class ReadStage(string needs, string produces) : IStage
    {
        public IReadOnlySet<string> Reads { get; } = new HashSet<string> { needs };
        public IReadOnlySet<string> Writes { get; } = new HashSet<string> { produces };
        public void Run(CalcContext ctx) => ctx.Set(produces, ctx.Get(needs) * 2);
    }

    private static CalcContext NewContext()
    {
        var build = new Build(80, new List<IModifierSource>(),
            new SkillSelection("Maul", 0.45, 29), new HashSet<Tag>(), new Target(81, 0));
        var pool = ModifierPool.From(build.Sources, build.ActiveState);
        return new CalcContext(build, FormulaConfig.Druid, pool, new Breakdown());
    }

    [Fact]
    public void Stages_run_in_order_and_share_values()
    {
        var ctx = NewContext();
        new CalcPipeline(new WriteStage("a", 10), new ReadStage("a", "b")).Run(ctx);
        Approx.Equal(20, ctx.Get("b"));
    }

    [Fact]
    public void Reading_an_unwritten_key_throws()
    {
        var ctx = NewContext();
        Assert.Throws<InvalidOperationException>(
            () => new CalcPipeline(new ReadStage("missing", "b")).Run(ctx));
    }
}
