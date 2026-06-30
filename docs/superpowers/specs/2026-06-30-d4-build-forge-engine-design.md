# D4 Build Forge — Engine Design

**Date:** 2026-06-30
**Status:** Approved (brainstorming → spec)
**Author:** Timur + Claude

A Path-of-Building-class theorycrafting tool for **Diablo 4**: a faithful damage/defence/life/movement
calculation engine that reproduces in-game numbers to within ~1%, so a user can fully *tangibilize* a
build before playing it. Long-term it goes **beyond PoB** with an **AoE visualizer** widget. Multiplatform
via Flutter; **macOS first**.

---

## 1. Goal & guiding principle

Build a calculation **framework**, not a hardcoded calculator. The framework is the product; the UI is a
shell on top of it.

**Guiding principle — no game constants are hardcoded in formula logic.** The *bucket structure* (which
modifiers add vs. multiply) is formula logic and lives in code. Every *value* (base stats per level, affix
rolls, skill coefficients, main-stat divisors, etc.) is **data the user supplies**. This sidesteps D4's
seasonal re-tuning entirely: when the game changes a number, the user changes the data, not the engine.

**Correctness oracle = the live game.** The user mirrors a real character (gear + stats identical to in-game),
hits a training dummy, and we verify the engine reproduces the game's numbers. Each verified scenario becomes
a **golden fixture**. The end state: a locked baseline where **Game = Our Expectation of it**.

---

## 2. Scope

### v1 (this spec)
- A **pure-Dart** calculation engine package (zero Flutter deps), fully test-driven.
- Replicate **Ava's community-validated D4 damage model** (see §5) as the reference, encoded as golden fixtures.
- Anchor skill: **Druid Maul** (simple Werebear core skill), validated end-to-end against the live game.
- **Milestone 1:** match the **skill tooltip / character-sheet** numbers (pre-mitigation: per-hit damage,
  crit chance, attack speed, main stat).
- **Milestone 2:** *splice in* a `Mitigation` pipeline stage (target armor + level-difference → damage
  reduction) to match the **actual on-dummy hit** number. This deliberately exercises the
  "insert a stage anywhere" architecture.

### Non-goals for v1 (roadmap, §7)
- Flutter UI / macOS app shell, item-creator UI, AoE visualizer.
- Defences/life/EHP, movement speed (channels exist in the model but stages come later).
- Full skill/paragon/aspect databases. Data is user-supplied per scenario; no datamined tables.
- Multiple classes/skills beyond Maul (expansion is the explicit next phase, not v1).

---

## 3. Architecture

### 3.1 Repo layout
Standalone git repo at `~/gameSources/d4_build_forge`, **outside** any other project (project-separation rule).

```
d4_build_forge/
  packages/
    engine/                 # pure Dart — the product (this spec)
      lib/
      test/
  apps/
    macos/                  # Flutter shell — LATER, depends on packages/engine
  docs/
    superpowers/specs/      # this design doc
    reference/              # ava-damage-optimization.xlsx (the reference model)
```

### 3.2 Pattern — modifier pool + bucket resolver + calc passes
This mirrors PoB's proven pipeline (`ModParser → ModDB/ModStore → calc passes → CalcBreakdown`), which is
the same "injectable sources, agnostic calculators" idea the user described. Three small, independently
testable units:

1. **Sources → Modifiers** — every contributor emits modifiers; knows nothing about other sources.
2. **Resolver** — aggregates the modifier pool and resolves any channel via the bucket primitive (§4.3).
3. **Pipeline of Stages** — ordered, **insertable** consumers that read resolved values and write results +
   a full breakdown.

---

## 4. Core model

### 4.1 `StatChannel`
A named quantity a modifier can touch. v1 set (extensible): `weaponDamage`, `mainStat`, `critChance`,
`critDamage`, `attackSpeed`, `skillRank`, and the damage **bucket channels** (`additiveDamage`, `vulnerable`,
`critDamageBucket`, `allDamage`/type-multipliers, `damageOverTime`). Reserved-but-unused-in-v1 channels exist
for later stages: `armor`, `maxLife`, `resist*`, `damageReduction`, `movementSpeed`.

### 4.2 `Modifier`
```
Modifier {
  channel:    StatChannel        // what it touches
  op:         flat | additivePercent | multiplicative
  value:      double
  bucketKey:  String             // which multiplier group it pools into (damage mods)
  conditions: Set<Tag>           // gating: close, distant, crowdControlled, vulnerable,
                                 //         healthy, werebear, core, fortified, ...
  source:     SourceRef          // provenance, for the breakdown
}
```

A **`ModifierSource`** is anything that emits `Modifier`s: `BaseStatsForLevel`, an `Item` (its affixes),
later `Aspect`/`ParagonNode`/`Temper`/`SkillRank`. Sources are injectable and mutually decoupled.

### 4.3 The bucket primitive (the heart of the engine)
Ava's sheet proves there are exactly two kinds of damage multiplier, and **both reduce to one rule**:

> Every damage modifier carries a `bucketKey`. Within a bucket, sources **add**: `base + Σ value` (active
> conditions only). Buckets **multiply** together. A "separate `[x]` multiplier" (Paragon/seal) is just its
> own single-member bucket. Crit and Vulnerable are buckets **gated by a condition** (crit's base = 1.5).

```
bucketValue(B) = base_B + Σ { m.value : m ∈ pool, m.bucketKey == B, conditionsActive(m) }
hitMultiplier  = Π over buckets B of bucketValue(B)
```

Bucket bases: most buckets `base = 1.0`; **crit** `base = 1.5`; vulnerable/others per data. This single
primitive expresses the entire pre-mitigation damage formula in §5.

### 4.4 `Stage` and the pipeline
```
Stage {
  reads:  Set<...>     // declared inputs  (for ordering checks)
  writes: Set<...>     // declared outputs
  run(ctx)             // reads resolved values, writes results + breakdown into CalcResult
}
```
v1 pipeline: `BaseStats → OffenseBuckets → AttackSpeed/DPS`. Milestone 2 inserts `Mitigation` after
`OffenseBuckets`. Each stage records its contribution to the breakdown. Ordering is validated from
`reads`/`writes` so a spliced-in stage can't silently run out of order.

### 4.5 Inputs — `Build`
```
Build {
  level
  baseStatsForLevel        // user-supplied table/entry
  equippedItems[]          // each item → affixes (ModifierSources)
  skill { id, baseCoeff, ranks }   // Druid Maul; coeff + ranks user-supplied
  state: Set<Tag>          // active conditions: inWerebear, targetVulnerable, targetCC'd, healthy, ...
  target { level, armor, ... }     // for the Mitigation stage (Milestone 2)
}
```

### 4.6 Output — `CalcResult` + breakdown
- Resolved stats: crit chance, attack speed, main stat, skill coefficient, …
- Per-hit **variants**: non-crit, crit, crit+vulnerable, overpower (and avg = crit·p + noncrit·(1−p)).
- **DPS** (per-hit × attack speed × hits-per-cast).
- **Full bucket breakdown**: for each variant, every modifier, the bucket it landed in, each bucket's
  `(base + Σ)`, and the running product — i.e. *exactly where* any number comes from. This breakdown is the
  primary tool for the user's "find out where we diverge from the game" loop.

---

## 5. Canonical damage formula (Ava's model — golden fixture #1)

Decoded directly from `docs/reference/ava-damage-optimization.xlsx`, **pre-mitigation** (character-sheet level):

```
NonCrit = WeaponDamage × MainStatMult × AdditiveBucket × VDM × ADMG × SkillCoeff × 0.2 × (1 + xDmgSeal)
Crit    = WeaponDamage × MainStatMult × AdditiveBucket × VDM × CSDM × ADMG × SkillCoeff
          × 1.5 × 0.2 × (1 + xCritDmgSeal + xDmgSeal)
Avg     = Crit · CritChance + NonCrit · (1 − CritChance)
```

Components (each expressed via the bucket primitive of §4.3):
- **MainStatMult** = `1 + MainStatSum · (1 + xAllStat% + xMainStat%) / divisor`
  — `divisor = 800` (general), **`900` for Barbarian**. Druid → 800, main stat = **Willpower**.
- **AdditiveBucket** = `1 + Σ([+]% damage)`: +%damage, vs close/distant/elites/CC/healthy, skill-based
  additives, tempers, etc. (the big additive pool).
- **VDM** = `1 + Σ vulnerable damage %`.
- **CSDM** = `1 + Σ critical-strike-damage %` (crit-only bucket; the `×1.5` base is separate).
- **ADMG** = `1 + Σ "[x]" type-damage multipliers that pool additively` (all/elemental/non-phys + weapon gem %).
- **xDmgSeal / xCritDmgSeal / xAllStat / xMainStat** = truly-separate `[x]` multipliers (Paragon/seal), each `(1+x)`.
- **SkillCoeff** = `baseCoeff · (1 + 0.10·(R − ⌊R/5⌋ − 1) + 0.15·⌊R/5⌋)`, `R = total skill ranks`
  — every 5th rank contributes 0.15 instead of 0.10.
- Constants captured as **data**: base crit `×1.5`, global skill scalar `×0.2`.
- **DoT variant** (poison/bleed): same product but with **DOTM** in place of crit, no `×1.5`.

Mapping to the engine: `WeaponDamage`, `SkillCoeff`, and the global scalar are the **base damage**;
`MainStatMult`, `AdditiveBucket`, `VDM`, `CSDM`/`1.5`, `ADMG`, and each seal are **buckets** multiplied
per §4.3. Ava's worked example values become the first golden test asserting `Avg`, `NonCrit`, `Crit`.

---

## 6. Validation strategy

- **TDD throughout.** Every formula behavior is driven by a failing test first.
- **Golden fixtures.** Two kinds:
  1. **Reference fixture** — Ava's worked example (numbers from the sheet) locks the bucket math in isolation.
  2. **Live-game fixtures** — the user mirrors a real Druid Maul character, reads the game's tooltip
     (Milestone 1) and on-dummy hit (Milestone 2); these become `Build` input + expected output, ≤1% tolerance.
- **Expansion protocol** (post-baseline): add one skill → add its fixtures → green; then validate
  **item-by-item** (each affix's contribution checked against the game). The fixture suite is the
  permanent record of "Game = Our Expectation."

---

## 7. Roadmap (after the v1 baseline)

In rough order, each its own spec → plan → implementation cycle:
1. **Skill-by-skill expansion** — more Druid skills, then other classes; class differences (main-stat divisor,
   dual-wield, etc.) as data.
2. **Item-by-item validation** — affix-level confidence.
3. **Defence/life stage** — Armor→DR vs target level, resistances, %DR, max-life, Fortify/Barrier, EHP.
4. **Movement stage** — movement speed.
5. **Flutter macOS shell** — item creator/selector, character sheet, build save/load.
6. **AoE visualizer widget** — skill shape/range/size drawn to scale, reacting to +Size affixes (the
   beyond-PoB differentiator), as a dedicated pipeline output.
7. **Stat-weighting / "what upgrades me most"** — Ava's sheet already computes relative-gain/weight per stat;
   port that as an optimization view.

---

## 8. Conventions & tech

- **Language:** Dart (pure for `engine`; Flutter only in `apps/macos`).
- **Numerics:** `double`; assert with explicit tolerance (≤1%) in fixture tests.
- **Testing:** `package:test`; golden fixtures stored as data alongside tests.
- **No hardcoded game constants in formula logic** — all values are data (§1).
- **Breakdown-first** — any computed number must be explainable via the breakdown (§4.6).

---

## 9. Risks & open questions

- **Ava's `0.2` global scalar and bucket-base details** — meaning to be confirmed against the live game during
  Milestone 1; treated as data so a correction is a value change, not a code change.
- **Vulnerable base (×1.2 / 20%)** — confirm whether the 20% base lives in the bucket base or the data list.
- **Overpower** — modeled as a conditional bucket; values/availability for Maul confirmed during validation.
- **Maul specifics** — Werebear/Fortify/Overpower interactions and Maul's exact `baseCoeff` are user-supplied
  from the live game; the engine stays agnostic.
- **Class coverage of Ava's sheet** — sheet enumerates several classes incl. non-base entries; we only rely on
  the divisor/main-stat logic, which is parameterized.
