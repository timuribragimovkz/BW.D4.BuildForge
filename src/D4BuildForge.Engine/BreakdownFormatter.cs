using System.Globalization;
using System.Linq;
using D4BuildForge.Engine.Model;

namespace D4BuildForge.Engine;

public static class BreakdownFormatter
{
    public static string Format(Breakdown breakdown)
        => string.Join("\n", breakdown.Lines.Select(FormatLine));

    private static string FormatLine(BreakdownLine line)
    {
        var value = line.Value.ToString(CultureInfo.InvariantCulture);
        return string.IsNullOrEmpty(line.Detail)
            ? $"{line.Label}: {value}"
            : $"{line.Label}: {value} ({line.Detail})";
    }
}
