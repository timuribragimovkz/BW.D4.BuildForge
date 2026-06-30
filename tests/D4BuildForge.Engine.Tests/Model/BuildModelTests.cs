using System.Collections.Generic;
using D4BuildForge.Engine.Core;
using D4BuildForge.Engine.Model;

namespace D4BuildForge.Engine.Tests.Model;

public class BuildModelTests
{
    [Fact]
    public void Breakdown_accumulates_lines_in_order()
    {
        var b = new Breakdown();
        b.Add("WeaponDamage", 4607);
        b.Add("Additive", 16.602, "1 + Σ");
        Assert.Equal(2, b.Lines.Count);
        Assert.Equal("WeaponDamage", b.Lines[0].Label);
        Assert.Equal(16.602, b.Lines[1].Value);
    }

    [Fact]
    public void Build_holds_its_selection()
    {
        var build = new Build(80, new List<IModifierSource>(),
            new SkillSelection("Maul", 0.45, 29), new HashSet<Tag>(), new Target(81, 0));
        Assert.Equal("Maul", build.Skill.SkillId);
        Assert.Equal(29, build.Skill.Ranks);
    }
}
