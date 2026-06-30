using D4BuildForge.Engine.Core;

namespace D4BuildForge.Assembly;

/// <summary>
/// The single source of truth for "what does this D4 affix stat mean to the engine". Extends
/// per-affix as item-by-item validation proceeds (spec §10). A skill-rank affix ("&lt;Skill&gt;Ranks")
/// is handled by <see cref="BuildAssembler"/> (it maps to <see cref="StatChannel.SkillRank"/>), not here.
/// </summary>
public static class AffixStatMap
{
    /// <summary>Stats that enter as flat additions on a stat channel.</summary>
    public static readonly IReadOnlyDictionary<string, StatChannel> FlatChannels =
        new Dictionary<string, StatChannel>(StringComparer.OrdinalIgnoreCase)
        {
            ["MainStat"]     = StatChannel.MainStat,
            ["WeaponDamage"] = StatChannel.WeaponDamage,
            ["CritChance"]   = StatChannel.CritChance,
            ["AttackSpeed"]  = StatChannel.AttackSpeed,
        };

    /// <summary>Stats that enter as additive-percent damage modifiers in a bucket (optionally condition-gated).</summary>
    public static readonly IReadOnlyDictionary<string, DamageBucketMap> DamageBuckets =
        new Dictionary<string, DamageBucketMap>(StringComparer.OrdinalIgnoreCase)
        {
            ["Additive"]   = new(BucketKey.Additive, null),
            ["AllDamage"]  = new(BucketKey.AllDamage, null),
            ["CritDamage"] = new(BucketKey.CritDamage, null),
            ["Vulnerable"] = new(BucketKey.Vulnerable, "Vulnerable"),
        };
}

/// <summary>A damage-bucket target plus the optional condition tag that gates it.</summary>
public readonly record struct DamageBucketMap(BucketKey Bucket, string? ConditionTag);
