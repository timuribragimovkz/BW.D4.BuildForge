using D4BuildForge.Domain;
using D4BuildForge.Engine.Core;
using D4BuildForge.Engine.Model;

namespace D4BuildForge.Assembly;

/// <summary>
/// Maps a <see cref="BuildRecord"/> + its referenced <see cref="ItemRecord"/>s into an engine
/// <see cref="Build"/>. The ONLY place D4 data becomes engine input (modularity contract): every
/// affix becomes a <see cref="Modifier"/>; no class/skill conditionals live in the engine.
/// </summary>
public static class BuildAssembler
{
    public static Build Assemble(BuildRecord build, IReadOnlyList<ItemRecord> items)
    {
        ArgumentNullException.ThrowIfNull(build);
        ArgumentNullException.ThrowIfNull(items);

        var rankStat = build.Skill.Name + "Ranks";
        var mods = new List<Modifier>();
        foreach (var item in items)
        {
            var src = new SourceRef("Item", item.Name);
            foreach (var affix in EnumerateAffixes(item))
                mods.Add(MapAffix(affix, rankStat, src, item.Name));
        }

        var state = new HashSet<Tag>(build.ActiveState.Select(s => new Tag(s)));

        return new Build(
            Level: build.Level,
            Sources: new List<IModifierSource> { new ModifierListSource(mods) },
            Skill: new SkillSelection(build.Skill.Name, build.Skill.BaseCoeff, build.Skill.Ranks),
            ActiveState: state,
            Target: new Target(build.Target.Level, build.Target.Armor));
    }

    private static Modifier MapAffix(AffixEntry affix, string rankStat, SourceRef src, string itemName)
    {
        // "+X ranks to the active skill" -> a SkillRank flat (engine adds it to totalRanks).
        if (string.Equals(affix.Stat, rankStat, StringComparison.OrdinalIgnoreCase))
            return Modifier.Flat(StatChannel.SkillRank, affix.Value, src);

        if (AffixStatMap.FlatChannels.TryGetValue(affix.Stat, out var channel))
            return Modifier.Flat(channel, affix.Value, src);

        if (AffixStatMap.DamageBuckets.TryGetValue(affix.Stat, out var dmg))
            return dmg.ConditionTag is null
                ? Modifier.Damage(dmg.Bucket, affix.Value, src)
                : Modifier.Damage(dmg.Bucket, affix.Value, src, new Tag(dmg.ConditionTag));

        throw new AssemblyException(
            $"Unknown affix stat '{affix.Stat}' on item '{itemName}'. Add it to AffixStatMap " +
            "(or it is the wrong skill's \"<Skill>Ranks\").");
    }

    private static IEnumerable<AffixEntry> EnumerateAffixes(ItemRecord item)
    {
        foreach (var a in item.Affixes) yield return a;
        if (item.Aspect is { } aspect)
            foreach (var a in aspect.Modifiers) yield return a;
    }
}
