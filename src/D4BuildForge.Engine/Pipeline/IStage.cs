using System.Collections.Generic;

namespace D4BuildForge.Engine.Pipeline;

public interface IStage
{
    IReadOnlySet<string> Reads { get; }
    IReadOnlySet<string> Writes { get; }
    void Run(CalcContext ctx);
}
