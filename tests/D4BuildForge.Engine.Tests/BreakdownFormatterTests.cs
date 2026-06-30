using D4BuildForge.Engine;
using D4BuildForge.Engine.Model;

namespace D4BuildForge.Engine.Tests;

public class BreakdownFormatterTests
{
    [Fact]
    public void Formats_lines_with_optional_detail()
    {
        var b = new Breakdown();
        b.Add("WeaponDamage", 100);
        b.Add("Additive", 1.5, "1 + Σ");
        var text = BreakdownFormatter.Format(b);
        Assert.Equal("WeaponDamage: 100\nAdditive: 1.5 (1 + Σ)", text);
    }
}
