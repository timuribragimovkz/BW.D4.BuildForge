// D4BuildForge.Import — Internal Build Contract (the source-agnostic IR).
// Pure POCOs/records: no engine, storage, web, or AWS dependency. This is the single
// shape every external source (Maxroll, Mobalytics, future sites) is mapped INTO.
//
// Design rules:
//  - Identifiers are preserved VERBATIM inside ExternalRef (no game-knowledge resolution here).
//  - Fields a given source cannot supply are nullable; the importer records why in Warnings.
namespace D4BuildForge.Import.Contracts;

public enum BuildSource { Maxroll, Mobalytics }

/// A reference to an external game entity, kept verbatim for a later content-resolver to map.
/// Kind is an open vocabulary: item, affix, aspect, gem, rune, skill, paragonBoard,
/// paragonNode, glyph, boon, mercenary, mercSkill, ...
public sealed record ExternalRef(
    BuildSource Source,
    string Kind,
    string Id,
    string? Name = null);

/// One rolled (or flagged) stat line on an item.
/// Maxroll supplies Values; Mobalytics leaves Values null and only sets Greater/Masterwork.
public sealed record ImportedAffix(
    ExternalRef Ref,
    IReadOnlyList<double>? Values,
    bool Greater,
    bool Masterwork);

public sealed record ImportedAspect(
    ExternalRef Ref,
    IReadOnlyList<double>? Values = null);

public sealed record ImportedSocket(
    ExternalRef Ref,
    string? Sub = null);   // e.g. rune class "Ritual"/"Invocation"

public sealed record GearItem(
    string Slot,            // normalized: Helm/Chest/.../Weapon/Ring/Seasonal/ChaosPerk/Charm/Seal
    string? RawSlot,        // the source's own slot id (Maxroll slot index, Mobalytics slug)
    ExternalRef Ref,
    string? Rarity,
    string? ItemKind,
    bool Mythic,
    IReadOnlyList<ImportedAffix> Implicits,
    IReadOnlyList<ImportedAffix> Explicits,
    IReadOnlyList<ImportedAffix> Tempers,
    IReadOnlyList<ImportedAspect> Aspects,
    IReadOnlyList<ImportedSocket> Sockets);

public sealed record SkillRank(
    ExternalRef Ref,
    int Rank);

public sealed record ParagonNode(
    ExternalRef Ref,
    int Rank);

public sealed record GridPosition(int X, int Y);

public sealed record ParagonBoard(
    ExternalRef Ref,
    IReadOnlyList<ParagonNode> Nodes,
    int? Rotation,             // Mobalytics: null
    GridPosition? Position,    // Mobalytics: null
    ExternalRef? Glyph,        // Mobalytics: null
    int? GlyphLevel);          // Mobalytics: null

public sealed record ParagonLoadout(
    IReadOnlyList<ParagonBoard> Boards);

public sealed record MercSkill(
    ExternalRef Ref,
    int? Rank);

public sealed record ImportedMercenary(
    ExternalRef? Primary,
    ExternalRef? Reinforcement,
    ExternalRef? Skill,
    ExternalRef? Opportunity,
    IReadOnlyList<MercSkill> SkillTree);

/// Class-specific extras kept generic so no class is baked into the IR
/// (Druid spirit boons today; Sorcerer enchantments, Rogue specialization, ... tomorrow).
public sealed record ClassExtra(
    string Group,              // e.g. "spiritBoon"
    ExternalRef Ref,
    string? Animal = null);    // Mobalytics tags boons with the animal (DEER/EAGLE/...)

public sealed record BuildVariant(
    string? Name,              // "Endgame" / "End Game" / "Starter" ...
    int? Level,                // Mobalytics: null
    int? WorldTier,            // Mobalytics: null
    IReadOnlyList<GearItem> Gear,
    IReadOnlyList<SkillRank> SkillRanks,
    IReadOnlyList<ExternalRef>? SkillBar,   // Mobalytics: null (action bar not separable)
    ParagonLoadout Paragon,
    ImportedMercenary? Mercenary,
    IReadOnlyList<ClassExtra> ClassExtras);

public sealed record ImportedBuild(
    string SchemaVersion,
    BuildSource Source,
    string? SourceId,
    string? SourceUrl,
    string Clazz,              // "druid" (lower-cased class)
    string? Name,
    DateTime CapturedAtUtc,
    IReadOnlyList<BuildVariant> Variants,
    IReadOnlyList<string> Warnings);
