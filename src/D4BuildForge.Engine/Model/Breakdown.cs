using System.Collections.Generic;

namespace D4BuildForge.Engine.Model;

public record BreakdownLine(string Label, double Value, string Detail);

public sealed class Breakdown
{
    private readonly List<BreakdownLine> _lines = new();
    public void Add(string label, double value, string detail = "") => _lines.Add(new BreakdownLine(label, value, detail));
    public IReadOnlyList<BreakdownLine> Lines => _lines;
}
