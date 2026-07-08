# Druid Skill Tree Catalog — Season 14 DRAFT (needs live verification)

> Compiled 2026-07-08 (icy-veins S14 guide + LoH rework docs). Same doctrine as uniques: DRAFT until
> screenshot-verified. **Two entries already CROSS-VALIDATED live:** Maul base 80% (fixture #1) and Claw's
> Dash variant 60% (fixture #2 — proving **variants REPLACE the base coefficient**).

## Structure (= the vessel hierarchy)
- **26 skills, 6 clusters** (Basic 5 · Core 6 · Defensive 4 · Companion 3 · Wrath 4 · Ultimate 4). Cluster =
  branch vessel; skill = node vessel inside it (inherits base Skill-Tree-Vessel type, carries `IsSelected`).
- **Per skill:** 2 pairs of side-upgrades (pick per pair) + **3 Variants** (pick 1; 3rd = LoH-exclusive;
  variant may replace coefficient/damage type/cluster) + **Form Node** (keep base form bonus, or change cast
  form → adds form Tag: Werebear/Werewolf/Human/Versatile).
- **Key Passives: REMOVED in S14** (replaced by expanded branching). Passive nodes per cluster still exist —
  NOT yet gathered (screenshot pass or follow-up fetch).
- Rank scaling (engine-validated): `base × (1 + 0.10(R−⌊R/5⌋−1) + 0.15⌊R/5⌋)`.

## Basic (generate Spirit)
| Skill | Coeff | Type | Spirit | Forms | Variants (coeff/effect) |
|---|---|---|---|---|---|
| Earth Spike | 90% | Physical | +16 | H/WW/WB | Megalith Stone (25% stacking) · Seismic Shift (70% line+slow) · Aftershock (80% 2nd spike) |
| Claw ✅ | 80% | Physical | +15 | WW/WB | Viper Strikes (dual, 28% poison) · Lightning Claw (120% lightning) · **Dash (60%) ✅ live-verified** |
| Storm Strike | 60% | Lightning, chains 2 | +15 | WB/WW/H | Chain Reaction (+3 chains, 20%/chain) · Storm Hammer (96% proj + immobilize) · Focused Strike (150% same-target) |
| Maul ✅ | **80% ✅ live-verified** | Physical | +20 | WB/WW | Wide Sweeps (120%, +50% size) · Reckless Rage (250%, −10 spirit) · Stone Fists (160% Earth) |
| Wind Shear | 60% | Physical | +16 | WB/WW/H | Boomerang (150% return) · Wind Swept (2 proj, +15% MS) · Solar Wind (auto-seek) |

## Core (spend Spirit)
| Skill | Coeff | Type | Cost | Forms | Variants |
|---|---|---|---|---|---|
| Pulverize | 175% | Physical | 35 | WB | Greater (400% split, +30% size) · Shockwave (Earth, 210% spikes) · Mega Bear Punch (1620%, 50 cost) |
| Landslide | 55%/pillar ×3 (+100% first) | Earth | 30 | WB/H | Continental Shift (74%, 2 pillars +50% size) · Vengeance of Earth (120→30% scaling) · Land Mine (500% trap) |
| Tornado | 35%/hit, 4s | Physical | 35 | WW/H | Swelling Storm (30% stack, 100% crit @3s) · Explosive Tornado (4 minis, 14%) · Greater Tornado (+50% size, 100% merge) |
| Lightning Storm | 65%/strike ≤10, 5 growths | Lightning | 15/period (channel) | WW/H | Supercell (+1 growth, +3 strikes) · Omnibolt (50%/strike single) · Hero of the Storm (259%, self-traveling) |
| Shred | 100→150→300% combo | Physical | 35 | WW | Critical Shred (100% crit first, +50% CD) · Roundhouse (1620% 360° final) · Storm Shred (+80% lightning, Storm tag) |
| Stone Burst | 130% + 20%/tick | Earth | 30 | WB/H | Epicenter (80% slow, 40% stack) · Rumbling Stones (∞ channel, 10/tick) · Living Stones (auto-grow 3s) |

## Defensive
| Skill | Numbers | CD | Forms | Variants |
|---|---|---|---|---|
| Cyclone Armor | 30% active; passive +10% AllRes | 12s | Versatile | Greater (+100% all) · Slipstream (knockdown 3s) · Reversal (pull, +30% AoE) |
| Earthen Bulwark | Barrier 48% maxlife, 6s | 16s | Versatile (−5% cost) | Reconstruction (15%/s regen) · Earthen Shrapnel (275% on break) · Travertine (3 Resolve, 150% spikes) |
| Debilitating Roar | −25% enemy dmg, 3.4s | 22s | WB | Booming (+50% all) · Survival Instincts (no CD, 50 spirit) · Roar of Resolve (+1 Resolve/s) |
| Blood Howl | Heal 20% maxlife | 15s | WW | Hungering (+50% healing recv) · Spirit Howl (1 spirit/1% healed) · Berserking Howl (5s zerk, +80% poison) |

## Companion (passive summon + active)
| Skill | Passive | Active | CD | Variants |
|---|---|---|---|---|
| Poison Creeper | 65% poison/6s | Immobilize 2s + 180%/2s | 20s | Germinate (20% free recast on kill) · Carnivorous (144% immediate) · Toxic Vegetation (324% aura, 80% slow) |
| Wolves ×2 | 14% bites | Feral Dash 200%, Unstoppable | 11s | Thrill of the Hunt (+100% upgrades) · Werewolf Pack (WW form, +50%) · Berserking (4s on cast) |
| Ravens | 90%/5s | 300% swarm/6s | 15s | Raven Flock (+100% speed, +2 ravens) · Stormcrow (lightning + 3s stun) · Migration (+50% size, retarget) |

## Wrath
| Skill | Coeff | CD | Forms | Variants |
|---|---|---|---|---|
| Hurricane | 360%/6s | 20s | Versatile | Rapid Intensification (30% stack/s) · Twin Storms (2 charges + vuln) · Derecho (no CD, 15 spirit/s, becomes CORE) |
| Trample | 75% (+20 spirit) | 14s | WB charge / WW leap | Brunt Force (500% first, −50% next) · Trampled Earth (120% pillars) · Exsanguination (execute + 150% burst + 10% heal) |
| Boulder | 60%/hit | 10s | WB/H | Crushing Impact (300% first, −50% ea) · Dolmen's Will (CORE, 60 spirit, 5 orbiting) · Landslip (placed, 200% landing) |
| Rabies | 28% + 300% poison/6s | 12s | WB/WW | Malaise (direct full + 150/300% crit) · Superinfection (CORE 30 spirit, 1500% single) · Relapse (CORE 45, reinfect ×3) |

## Ultimate
| Skill | Numbers | CD | Forms | Variants |
|---|---|---|---|---|
| Cataclysm | 46% twisters + 115% lightning, 8s | 50s | WW/H | Severe Storm (double) · Terrible Twisters (orbit, 200%) · Tectonic Spikes (Earth, 165%) |
| Petrify | Stun 3s, +15% crit dmg taken | 40s | H/WB | Greater (+50% dur, −15 CD) · Seismic Shift (1000% while petrified) · Enduring Consequences (10s, auto-petrify) |
| Grizzly Rage | Dire Werebear, Berserk, +20% armor, 20s | 120s | WB | Immunity (2s) · Cornered Beast (5% stack, cap 200%) · Avatar of Nature (Earth/Storm castable, +35%) |
| Lacerate | ≤660% total, 10 dashes, Immune | 35s | WW | Hunter Killer (+7 strikes, 50% faster) · Toxin Outburst (70% poison burst) · Temperance Training (+30% MS, pull) |

> Side-upgrades (2 pairs/skill) captured in source but abbreviated here — full effects land as node vessels
> when this file becomes JSON. Passive nodes per cluster: NOT YET GATHERED. Source: icy-veins Druid Skills
> (S14), fetched 2026-07-08. Verify-live per skill on first use in a build.
