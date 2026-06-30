using System.Collections.Generic;
using D4BuildForge.Engine.Core;

namespace D4BuildForge.Engine.Sources;

public sealed class BaseStatsForLevel(int level, double mainStat, double weaponDamage) : IModifierSource
{
    public IEnumerable<Modifier> GetModifiers()
    {
        var src = new SourceRef("BaseStats", $"L{level}");
        yield return Modifier.Flat(StatChannel.MainStat, mainStat, src);
        yield return Modifier.Flat(StatChannel.WeaponDamage, weaponDamage, src);
    }
}
