# Druid Uniques Catalog — Season 14 DRAFT (needs live verification)

> **Status: DRAFT.** Compiled 2026-07-08 from Maxroll boss-loot cheat-sheet (drop sources) + game8 + Fextralife
> (power texts). **The wikis CONTRADICT each other on several items** (game8 carries pre-Lord-of-Hatred texts;
> some entries reference Key Passives, which S14 REMOVED — provably stale). Per project doctrine the GAME is
> the oracle: every entry must be **verified from an in-game screenshot** (`captures/inbox/`) before its math
> is trusted. `verify` column: ☐ = unverified draft, ✅ = screenshot-confirmed (update text verbatim then).
>
> **Math `kind`:** `modifier` = expressible in the bucket primitive today (bucket + [x]/[+] + condition tags) ·
> `mechanic` = behavior/proc/summon, NOT a static multiplier (engine flags, doesn't fake) · `hybrid` = both.

## Vessel schema (locked with user 2026-07-08)
- **Catalog vessels (fixed, shared, seasonId in PK):** skills (1 vessel per tree branch, node vessels inside),
  passives, unique powers (this file's content), paragon nodes/glyphs. Immutable per season; new season =
  clone-and-tweak.
- **Build rows (per user):** pointers into the catalog + `IsSelected` state + the user's ROLLED stats per
  equipped item (rolls vary; ranges live in catalog). Client hydrates catalog+build into the selectable view;
  calc iterates selected → each selected node/power emits its modifiers (`IModifierSource` pattern) → pool.
- Unique power entry shape: `{ id, name, slot, dropSource, textVerbatim, rollRange, kind, mathMapping
  (bucket/op/value/conditions or mechanic-note), verifyStatus, sources[] }`.

## Weapons
| # | Item | Slot | Drops from | Power (primary text ⌇ conflicting alt) | kind | verify |
|---|---|---|---|---|---|---|
| 1 | Fleshrender | 1H Mace | Harbinger of Hatred | G8: Defensive Skill cast deals (4050–8100) dmg to Nearby Poisoned, +50% per 100 Willpower ⌇ FX: expiring Tornadoes return, [x40–50%] bonus | mechanic | ☐ CONFLICT |
| 2 | Greatstaff of the Crone | 2H Staff | Varshan | FX(S14): Claw gains ALL Variants, [x15–20%] bonus dmg ⌇ G8(old): Claw is a Storm Skill + casts Storm Strike at (170–250)% | hybrid | ☐ CONFLICT |
| 3 | Ifeh's Dire Totem | Totem | Harbinger of Hatred | Grizzly Rage → Werewolf Skill; Dire Werewolf: [x110–125%] crit dmg, [x100–125%] poisoning, spirit-cost-red. instead of Armor; kills heal 5% max life | hybrid | ☐ |
| 4 | Purified Lightbringer | 2H Mace | The Butcher | G8: Pulverize pulls Distant; [x100–125%] → [x200–250%] if pulled/Unstoppable/Boss ⌇ FX: [50–63%] | hybrid | ☐ CONFLICT |
| 5 | Rotting Lightbringer | 2H Mace | (world?) | Pulverize puddle guarantees own Overpowers, (200–600%) as Poisoning over 7s; close puddles splash (20–60)% | mechanic | ☐ |
| 6 | Stone of Vehemen | Totem | Varshan | G8: +15% DR channeling; final explosion +(15–20)%, +(15–20)% per size ⌇ FX: x40% AllRes; final explosion [x250–300%] split | hybrid | ☐ CONFLICT |
| 7 | The Basilisk | Staff | Grigoire | FX: Petrified enemies [+80–100%] chance to be crit; Earth Skills Petrify Healthy 3s ⌇ G8: first Earth hit Petrifies 3s + (4412–26472) phys | mechanic | ☐ CONFLICT |
| 8 | Waxing Gibbous | 1H Axe | Lord Zir | S14: Shred [x20–25%] more dmg; 25% chance extra strike, up to 4 ⌇ G8(old): Shred kill → Stealth → guaranteed crits | hybrid | ☐ CONFLICT |
| 9 | The Butcher's Cleaver | 1H Axe | The Butcher | Attacks crit Injured; Elite kills Fear+Slow (85–95%) 2s | mechanic | ☐ |

## Armor
| # | Item | Slot | Drops from | Power | kind | verify |
|---|---|---|---|---|---|---|
| 10 | Autumnal Crown | Helm | Varshan | Wind Shear deals poisoning DoT over 4s (numbers differ by source: 6017–9022 ⌇ 225–300 — level-scaled); Lucky Hit 20% restore 100 Spirit | mechanic | ☐ |
| 11 | Dark Howl | Gloves | Urivar | Debilitating Roar → Werewolf Skill; [x60–80%] dmg in Werewolf form for duration | modifier (buff-window) | ☐ |
| 12 | Gathlen's Birthright | Helm | Lord Zir | G8: Nature-Magic crits grant Anima of the Forest → grants Perfect Storm + Earthen Might **KEY PASSIVES — REMOVED in S14 → text STALE** | mechanic | ☐ STALE? |
| 13 | Greenwalker's Oath | Boots | Duriel | Poison Creeper gains Germinate Variant free, [x30–40%] bonus dmg | hybrid | ☐ |
| 14 | Heart of Azgar | Chest | The Butcher | Maul hit: +3% AS, [x20–25%] dmg 5s, stacks ×5 | modifier (stacking) | ☐ |
| 15 | Insatiable Fury | Chest | Andariel | G8: Werebear true form, +(2–5) Werebear ranks ⌇ alt-S14: Trample recast 2s window + [x150–200%] Trample | hybrid | ☐ CONFLICT |
| 16 | Khamsin Steppewalkers | Boots | Bartuc | Damaging (150–100) enemies w/ Nature Magic → Max MoveSpeed + Unhindered (4–8)s; run-into Immobilize 1s | mechanic | ☐ |
| 17 | Kilt of Blackwing | Pants | Urivar | Ravens [x60–100%]; shapeshift/companion-cast summons Raven; chance of Unkindness (3 ravens 10s) | hybrid | ☐ |
| 18 | Mad Wolf's Glee | Chest | Astaroth | G8: Werewolf true form, +(4–7) Werewolf ranks ⌇ alt-S14: Lacerate no CD, costs 100 Spirit, [x80–100%] dmg, no Immunity | hybrid | ☐ CONFLICT |
| 19 | Storm's Companion | Pants | Andariel | Wolves deal Lightning dmg + gain Storm Howl | mechanic | ☐ |
| 20 | Tempest Roar | Helm | (general) | Lucky Hit: Storm Skills (15–35%) grant 4 Spirit; base Storm Skills are also Werewolf Skills | hybrid | ☐ |
| 21 | Unsung Ascetic's Wraps | Gloves | Lord Zir | Lightning Storm +1 strike per growth; LS crits strike twice, (10–40)% increased | hybrid | ☐ |
| 22 | Vasily's Prayer | Helm | (general) | Earth Skills are also Werebear Skills; Fortify 2–5% max life | hybrid | ☐ |
| 23 | Wildheart Hunger | Boots | Astaroth | Shapeshift → Bestial Rampage bonuses value +(2–5)%, cap (20–50)%, decays 0.5%/s — **references Key Passive: check S14 form** | modifier (stacking) | ☐ STALE? |
| 24 | Will of Stone | Helm | Grigoire | Earth Spike is a projectile; impact (8717–11622) dmg + summons 2–4 Earth Spikes | mechanic | ☐ |

## Jewelry
| # | Item | Slot | Drops from | Power | kind | verify |
|---|---|---|---|---|---|---|
| 25 | Accord of the Wilds | Ring | Beast in the Ice | Companion Skills +1 companion each, [x40–50%]; free passive effect of Ravens/Wolves/Poison Creeper | hybrid | ☐ |
| 26 | Airidah's Inexorable Will | Ring | Beast in the Ice | Ultimate cast (+again 5s later): Pull Distant + (2298–9193) phys, +10% per 1(?) Willpower (verify scaling) | mechanic | ☐ |
| 27 | Dirge of Airidah | Ring | Astaroth | Storm Skills cast grant 1 Spirit, [x25–35%] bonus; DOUBLED vs Vulnerable/Immobilized/Slowed | modifier | ☐ |
| 28 | Dolmen Stone | Amulet | Urivar | Boulder orbits during Hurricane; Boulder dmg +(20–40)% per rotating Boulder | hybrid | ☐ |
| 29 | Earthbreaker | Ring | Bartuc | Landslide leaves Tectonic Spikes (1215–2430) phys over 2s; pillar in spikes (20–30)% chance ×2 | mechanic | ☐ |
| 30 | Fractured Runestone | Ring | Duriel | Earth Skills [x40–50%] crit dmg → [x80–100%] at ≥4 Overpower stacks; +2 OP stacks/10s (S14 stacking OP!) | modifier (conditional) | ☐ |
| 31 | Fury of the Wilds | Ring | The Butcher | Shapeshift → Berserking (6–8)s; +1 Ferocity/s while Berserking; Berserking dmg +10(%?) | hybrid | ☐ |
| 32 | Greenwalker's Signet | Ring | Andariel | Human Skill cast: 15% trigger another equipped non-Ult Human Skill; Human Skills [x40–50%] | hybrid | ☐ |
| 33 | Hunter's Zenith | Ring | Grigoire | Every 30s in animal form: next Core Skill guaranteed Overpower+Crit, (30–60)% increased; shapeshift casts −1s (−2s new form) | mechanic | ☐ |
| 34 | Malefic Crescent | Amulet | Beast in the Ice | Lupine Ferocity consecutive-crit value → (150–200)% — **Lupine Ferocity was a Key Passive; verify S14 wording** | modifier | ☐ STALE? |
| 35 | Mark of the Old Wolf | Ring | Duriel | [x15–20%] Poisoning; WW direct dmg gains +x50% of your poisoning bonus (cap x50%); WW poison gains x100% of AS+CritChance (cap x50%) — conversion formulas | modifier (formula) | ☐ |
| 36 | Might of the Ursine | Ring | Harbinger of Hatred | [x10–13%] per Resolve stack in Werebear; +1 Resolve/1s in Werebear | modifier (stacking) | ☐ |
| 37 | Mjölnic Ryng | Ring | Bartuc | While Cataclysm active: unlimited Spirit + (40–100)% increased dmg | modifier (buff-window) | ☐ |

## Mythic (Druid-eligible)
| # | Item | Slot | Drops from | Power | kind | verify |
|---|---|---|---|---|---|---|
| M1 | Ahavarion, Spear of Lycander | Staff | (Mythic pool) | (Druid/Sorc) — text TBD from game | ? | ☐ |
| M2 | Shattered Vow | 2H | (Mythic pool) | (Barb/Druid/Spiritborn) — text TBD from game | ? | ☐ |

## Verification protocol (PC session)
1. User screenshots the item tooltip → `captures/inbox/`.
2. Replace the entry's power text with the **verbatim in-game text**, set verify ✅, record roll range AND the
   user's actual roll (roll → build row, range → catalog).
3. Write the math mapping: bucket + op ([x]/[+]) + condition tags for `modifier` parts; a one-line mechanic
   note for the rest. CONFLICT/STALE rows get priority — they're where the wikis failed.

Sources: Maxroll boss-loot cheat-sheet · game8 archives/410081 · Fextralife Druid+Unique+Equipment (fetched 2026-07-08).
