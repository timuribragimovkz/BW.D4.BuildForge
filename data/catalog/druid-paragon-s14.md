# Druid Paragon Catalog — Season 14 DRAFT (needs live verification)

> Compiled 2026-07-08 (fextralife boards+glyphs, S14 patch notes, icy-veins glyph guide). DRAFT until
> screenshot-verified. Note the printed `[+]`/`[x]` brackets below — they came through the wiki and match the
> game's bucket convention; still verify on real tooltips.

## Structure (= the vessel hierarchy)
- **9 boards per class** (Starter + 8 legendary-named). **Max 5 equipped** (Starter + 4). Each board:
  **1 Legendary Node** (board's name) + **6 rare nodes** + magic/normal stat nodes + **1 glyph socket**.
- **Glyphs:** level 1–100 (S14 raised from 21). **Legendary Glyph** upgrade at glyph level 45 (Gem
  Fragments): radius 4 → 5 + one additional affix. Up to 5 active glyphs (one per equipped board).
- Vessel mapping: board = branch vessel; legendary node / rare nodes / glyph socket = node vessels
  (IsSelected on inherited type); glyph bonus buckets by its printed bracket → `IXXXBucket` → `BucketKey`.
  Conditions (in-form, vs-Poisoned, CC'd…) = condition tags — ALL-ON by default, toggleable.

## Legendary Nodes (9 boards)
| Board | Legendary Node effect (draft text) | Bucket/kind | verify |
|---|---|---|---|
| Ancestral Guidance | After spending 75 Spirit: [x40%] dmg 5s. Ultimates [x1%]/Spirit spent on cast | [x] cond. | ☐ |
| Thunderstruck | Storm Skills deal [x] = 20% of your (Close+Distant dmg bonuses), cap [x60%] | [x] formula | ☐ |
| Constricting Tendrils | [x40%→**S14: 60%**] vs Poisoned; Lucky Hit: Nature Magic 40% chance Poison 1000%/4s | [x] cond. + proc | ☐ S14-BUFFED |
| Survival Instincts | Werebear: [x30%] (or [x45%] full Life); +[x1%] Overpower dmg per 1% Life-vs-Fortify gap, cap [x25%] | [x] cond. formula | ☐ |
| Earthen Devastation | Earth Skills [x70%] Critical Strike Damage | [x] crit bucket | ☐ |
| Heightened Malice | Poisoned enemy Nearby: [x35%] +[x5%] per extra Poisoned, cap +4 | [x] cond. stacking | ☐ |
| Lust for Carnage | Werewolf Skill crits: +2 Spirit and [x50%] increased damage | [x] cond. | ☐ |
| Inner Beast | After Shapeshift: −4.5% Spirit cost/stack 10s, cap 45%; at 10 stacks reset → −5s Ultimate CD | resource mech | ☐ |
| Untamed | Companion Skill cast: [x20%] Companion dmg 5s, stacks to [x80%] | [x] stacking | ☐ |

## Druid Glyphs (22) — bonus scales with stat purchased in radius; Additional = the secondary effect
| Glyph | Bonus (per 5 stat in range) | Additional effect | verify |
|---|---|---|---|
| Bane | +1.9% Poison dmg (per 5 Int) | Poisoning 10% chance ×2 over duration | ☐ |
| Dominate | +5.9% Overpower dmg (Int) | On Overpower: enemy takes [x12%] from you, 5s | ☐ |
| Earth and Sky | +30% to Magic nodes in range | Nature Magic [x10%] vs CC'd or Vulnerable | ☐ |
| Electrocution | +13.2% Lightning/Dancing Bolts (Will) | Bolt-damaged enemies take [x20%] from you 5s | ☐ |
| Exploit | +0.9% dmg vs Vulnerable (Dex) | Damaged enemy → Vulnerable 3s (per-enemy ICD 20s) | ☐ |
| Fang and Claw | +30% to Magic nodes | In WW/WB form: Close enemies take [x12%] | ☐ |
| Fulminate | +1.9% Lightning dmg (Dex) | [x12%] Lightning vs Healthy AND Injured | ☐ |
| Guzzler | +2.6% dmg while Healthy (Int) | +30%[+] Potion Healing | ☐ |
| Human | +1.3% dmg in Human form (Will) | 10% DR in Human form | ☐ |
| Keeper | +25% to Rare nodes | You + Companions [x10%] Non-Physical dmg | ☐ |
| Outmatch | +25% to Rare nodes | [x16%] Physical vs Non-Elites and Bosses | ☐ |
| Poise | +25% to Rare nodes | 30% chance on Shapeshift: Barrier 5% maxlife 4s | ☐ |
| Protector | +25% to Rare nodes | 10% DR while Barrier active | ☐ |
| Shapeshifter | +25% to Rare nodes | Shapeshift: 20% chance skill's dmg crits | ☐ |
| Spirit | +1.9% Crit Strike Damage (Dex) | Crits: enemy takes +[x2%] 20s, cap [x12%] | ☐ |
| Tectonic | +3.0% Earth-skill CSD (Dex) | +15% Lucky Hit Chance | ☐ |
| Territorial | +1.9% dmg vs Close (Dex) | 10% DR vs Close | ☐ |
| Tracker | +1.9% dmg vs Poisoned (Dex) | Poisoning lasts [x33%] longer | ☐ |
| Undaunted | +1.9% dmg while Fortified (Int) | up to 10% DR scaling with Fortify | ☐ |
| Werebear | +1.3% dmg in Werebear form (Will) | 10% DR in Werebear form | ☐ |
| Werewolf | +1.3% dmg in Werewolf form (Will) | 10% DR (wiki text says "in Werebear" — likely typo, verify) | ☐ TYPO? |
| Wilds | +4.9% Companion dmg (Int) | Companion passive portion [x80%] | ☐ |

> **Not yet gathered:** each board's 6 rare-node effects + node grid layouts (screenshot pass or follow-up),
> the Starter board contents, and which board is the S13/S14 NEW one. Glyph leveling curve (bonus per level)
> also TBD. Sources: fextralife Druid+Paragon+Boards & Glyphs, S14 patch notes, icy-veins glyph guide.
