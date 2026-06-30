# D4 Build Forge — Engine Design

**Date:** 2026-06-30
**Status:** Approved (brainstorming → spec)
**Author:** Timur + Claude

A Path-of-Building-class theorycrafting tool for **Diablo 4**: a faithful damage/defence/life/movement
calculation engine that reproduces in-game numbers to within ~1%, so a user can fully *tangibilize* a
build before playing it. Long-term it goes **beyond PoB** with an **AoE visualizer** widget.

**Online tool. Stack: .NET + DynamoDB (Docker-local) + web UI.** Engine first.

---

## 1. Goal & guiding principle

Build a calculation **framework**, not a hardcoded calculator. The framework is the product; the web UI is a
shell on top of it.

**Guiding principle — no game constants are hardcoded in formula logic.** The *bucket structure* (which
modifiers add vs. multiply) is formula logic and lives in code. Every *value* — base stats per level, affix
rolls, skill coefficients, main-stat divisors, global scalars, bucket bases — is **data**. This sidesteps
D4's seasonal re-tuning: when the game changes a number, the data changes, not the engine.

**Correctness oracle = the live game.** The user mirrors a real character (gear + passives + Paragon identical
to in-game), hits a training dummy, and we verify the engine reproduces the game's numbers. Each verified
scenario becomes a **golden fixture**. End state: a locked baseline where **Game = Our Expectation of it**.

---

## 2. Scope

### v1 (this spec)
- A **pure C# class library** (`D4BuildForge.Engine`, zero AWS/storage/web deps), fully test-driven.
- Replicate **Ava's community-validated D4 damage model** (see §6) as the reference, encoded as golden fixtures.
- Stand up the **storage substrate**: DynamoDB Local (Docker) for game content + BW.Libs.Config vessels for
  tuning constants (see §5). Engine consumes data through an adapter; it never talks to storage directly.
- Class = **Druid only**. Anchor skill = **Maul** (simple Werebear core skill), validated end-to-end vs. the game.
- **Build assembly flow:** class → level → items → passives (skill tree) → Paragon → **final passive-baseline
  stats** → buffs → conditions → pick Maul → compute.
- **Milestone 1:** match the **skill tooltip / character-sheet** numbers (pre-mitigation).
- **Milestone 2:** *splice in* a `Mitigation` stage (target armor + level-difference → damage reduction) to
  match the **actual on-dummy hit** — deliberately exercising the "insert a stage anywhere" architecture.

### Non-goals for v1 (roadmap, §8)
- Web UI front-end (the API contract is defined; the SPA/Blazor front-end is later).
- Defences/life/EHP and movement-speed stages (channels exist in the model; stages come later).
- Classes beyond Druid; skills beyond Maul. Expansion (skill-by-skill, item-by-item) is the explicit next phase.
- The AoE visualizer widget.

---

## 3. Architecture

### 3.1 Repo / solution layout
Standalone git repo at `~/gameSources/d4_build_forge`, **outside** any other project (project-separation rule).

```
d4_build_forge/
  D4BuildForge.sln
  src/
    D4BuildForge.Engine/      # PURE C# — modifier pool, bucket resolver, stages, Build, CalcResult.
                              # NO AWS, NO storage, NO web, NO BW.Libs dependency.
    D4BuildForge.Domain/      # game-content POCOs: Item, Affix, Skill, PassiveNode, ParagonNode,
                              # BaseStatTable, BuffDef, ConditionDef, FormulaConfig (tuning constants).
    D4BuildForge.Storage/     # DynamoDB single-table repo (game content) + BW.Libs.Config vessel
                              # wiring (tuning constants). THIS is the only project referencing
                              # BW.Libs.Config NuGet + AWS SDK.
    D4BuildForge.Assembly/    # BuildAssembler: turns a selection (class/level/items/passives/paragon/
                              # buffs/conditions/skill) + FormulaConfig into an engine `Build`.
    D4BuildForge.Api/         # web API (LATER) — thin; calls Assembly + Engine.
  tests/
    D4BuildForge.Engine.Tests/    # golden fixtures: Ava reference model + live-game Maul scenarios.
    D4BuildForge.Assembly.Tests/  # selection → Build mapping.
  docker/
    docker-compose.yml        # DynamoDB Local
  docs/
    superpowers/specs/        # this design doc
    reference/                # ava-damage-optimization.xlsx (the reference model)
```

**Dependency direction:** `Engine` depends on nothing app-specific. `Domain` is POCOs. `Assembly` depends on
`Engine` + `Domain`. `Storage` depends on `Domain` + BW.Libs.Config + AWS SDK. `Api` composes them. The pure
engine is therefore unit-testable in milliseconds with no Docker/AWS.

### 3.2 Pattern — modifier pool + bucket resolver + calc passes
Mirrors PoB's proven pipeline (`ModParser → ModDB/ModStore → calc passes → CalcBreakdown`) — the same
"injectable sources, agnostic calculators" idea. Three small, independently testable units inside `Engine`:

1. **Sources → Modifiers** — every contributor emits modifiers; knows nothing about other sources.
2. **Resolver** — aggregates the modifier pool and resolves any channel via the bucket primitive (§4.3).
3. **Pipeline of Stages** — ordered, **insertable** consumers that read resolved values and write results +
   a full breakdown.

---

## 4. Core model (in `D4BuildForge.Engine`)

### 4.1 `StatChannel`
A named quantity a modifier can touch. v1 set (extensible): `WeaponDamage`, `MainStat`, `CritChance`,
`CritDamage`, `AttackSpeed`, `SkillRank`, and damage **bucket channels** (`AdditiveDamage`, `Vulnerable`,
`CritDamageBucket`, `AllDamage`/type-multipliers, `DamageOverTime`). Reserved-but-unused-in-v1 for later
stages: `Armor`, `MaxLife`, `Resist*`, `DamageReduction`, `MovementSpeed`.

### 4.2 `Modifier`
```
record Modifier(
  StatChannel Channel,                       // what it touches
  ModOp Op,                                  // Flat | AdditivePercent | Multiplicative
  double Value,
  string BucketKey,                          // which multiplier group it pools into (damage mods)
  IReadOnlySet<Tag> Conditions,              // gating: Close, Distant, CrowdControlled, Vulnerable,
                                             //         Healthy, Werebear, Core, Fortified, ...
  SourceRef Source);                         // provenance, for the breakdown
```

A **`IModifierSource`** is anything that emits modifiers: `BaseStatsForLevel`, an `Item` (its affixes), a
`PassiveNode`, a `ParagonNode`, a `BuffDef`, a `SkillRankSource`. Sources are injectable and mutually decoupled.

### 4.3 The bucket primitive (the heart of the engine)
Ava's sheet proves there are exactly two kinds of damage multiplier, and **both reduce to one rule**:

> Every damage modifier carries a `BucketKey`. Within a bucket, sources **add**: `base + Σ value` (active
> conditions only). Buckets **multiply** together. A "separate `[x]` multiplier" (Paragon/seal) is just its
> own single-member bucket. Crit and Vulnerable are buckets **gated by a condition** (crit's base = 1.5).

```
bucketValue(B) = base_B + Σ { m.Value : m ∈ pool, m.BucketKey == B, conditionsActive(m) }
hitMultiplier  = Π over buckets B of bucketValue(B)
```

Bucket **bases** (`base_B`, e.g. crit = 1.5) come from `FormulaConfig` (tuning data, §5), not hardcoded.

### 4.4 `IStage` and the pipeline
```
interface IStage {
  IReadOnlySet<...> Reads;    // declared inputs  (for ordering validation)
  IReadOnlySet<...> Writes;   // declared outputs
  void Run(CalcContext ctx);  // reads resolved values, writes results + breakdown
}
```
v1 pipeline: `BaseStats → PassiveBaseline → OffenseBuckets → AttackSpeed/DPS`. "Buffs" and "conditions" toggle
which modifiers/buckets are active. Milestone 2 inserts `Mitigation` after `OffenseBuckets`. Ordering is
validated from `Reads`/`Writes` so a spliced-in stage can't silently run out of order.

### 4.5 Engine inputs — `Build` and `FormulaConfig`
`Build` is produced by `BuildAssembler` (it is **not** the storage shape):
```
record Build(
  int Level,
  IReadOnlyList<IModifierSource> Sources,    // base-stats-for-level, items, passives, paragon, buffs
  SkillSelection Skill,                       // Maul: baseCoeff, ranks
  IReadOnlySet<Tag> ActiveState,              // inWerebear, targetVulnerable, targetCC'd, healthy, ...
  Target Target);                             // level, armor, ... (for the Mitigation stage)

record FormulaConfig(...);                     // tuning constants from BW.Libs.Config vessels (§5)
```

### 4.6 Output — `CalcResult` + breakdown
- Resolved stats: crit chance, attack speed, main stat, skill coefficient, …
- Per-hit **variants**: non-crit, crit, crit+vulnerable, overpower (avg = crit·p + noncrit·(1−p)).
- **DPS** (per-hit × attack speed × hits-per-cast).
- **Full bucket breakdown**: for each variant, every modifier, the bucket it landed in, each bucket's
  `(base + Σ)`, and the running product — *exactly where* any number comes from. This is the primary tool for
  the user's "find where we diverge from the game" loop.

---

## 5. Storage substrate (split: vessels + entities)

Two distinct kinds of data, two stores — both DynamoDB Local via Docker in dev:

1. **Tuning constants → BW.Libs.Config vessels.** Bucket bases (crit 1.5, vulnerable base, …), main-stat
   divisors (800; 900 Barbarian), the global skill scalar (Ava's `0.2`), etc. Loaded as a `FormulaConfig`
   and handed to the engine. We **reference the published BW.Libs.Config NuGet** (a generic config library —
   not Bruceware domain code, so it doesn't breach project separation); it lives **only** in `Storage`.
2. **Game content → DynamoDB single-table entities.** Items/affixes, skills (incl. Maul's coefficient),
   base-stat-per-level tables, passives, Paragon nodes, buff/condition definitions. Single-table design with a
   hardcoded GSI1 (mirroring the user's established convention, in this project's own table — not Bruceware's).

`Storage` + `Assembly` are the only projects that know storage exists. The engine receives a `Build` +
`FormulaConfig` and computes. Tests construct these literally, no Docker required.

---

## 6. Canonical damage formula (Ava's model — golden fixture #1)

Decoded directly from `docs/reference/ava-damage-optimization.xlsx`, **pre-mitigation** (character-sheet level):

```
NonCrit = WeaponDamage × MainStatMult × AdditiveBucket × VDM × ADMG × SkillCoeff × 0.2 × (1 + xDmgSeal)
Crit    = WeaponDamage × MainStatMult × AdditiveBucket × VDM × CSDM × ADMG × SkillCoeff
          × 1.5 × 0.2 × (1 + xCritDmgSeal + xDmgSeal)
Avg     = Crit · CritChance + NonCrit · (1 − CritChance)
```

Components (each expressed via the bucket primitive of §4.3):
- **MainStatMult** = `1 + MainStatSum · (1 + xAllStat% + xMainStat%) / divisor` — `divisor = 800` general,
  **`900` Barbarian**. Druid → 800, main stat = **Willpower**.
- **AdditiveBucket** = `1 + Σ([+]% damage)`: +%damage, vs close/distant/elites/CC/healthy, skill-based
  additives, tempers, etc.
- **VDM** = `1 + Σ vulnerable-damage %`.
- **CSDM** = `1 + Σ crit-strike-damage %` (crit-only; the `×1.5` base is separate).
- **ADMG** = `1 + Σ "[x]" type-damage multipliers that pool additively` (all/elemental/non-phys + weapon gem %).
- **xDmgSeal / xCritDmgSeal / xAllStat / xMainStat** = truly-separate `[x]` multipliers (Paragon/seal), each `(1+x)`.
- **SkillCoeff** = `baseCoeff · (1 + 0.10·(R − ⌊R/5⌋ − 1) + 0.15·⌊R/5⌋)`, `R = total skill ranks` — every 5th
  rank contributes 0.15 instead of 0.10.
- Constants captured as **data** (FormulaConfig): base crit `×1.5`, global skill scalar `×0.2`.
- **DoT variant** (poison/bleed): same product with **DOTM** in place of crit, no `×1.5`.

Ava's worked example values become the first golden test asserting `Avg`, `NonCrit`, `Crit`.

---

## 7. Validation strategy

- **TDD throughout.** Every formula behavior is driven by a failing test first.
- **Golden fixtures**, two kinds:
  1. **Reference fixture** — Ava's worked example locks the bucket math in isolation.
  2. **Live-game fixtures** — the user mirrors a real Druid Maul character, reads the game's tooltip
     (Milestone 1) and on-dummy hit (Milestone 2); each becomes a `Build` + `FormulaConfig` + expected output,
     ≤1% tolerance.
- **Expansion protocol** (post-baseline): add one skill → add its fixtures → green; then validate
  **item-by-item** (each affix's contribution vs. the game). The fixture suite is the permanent record of
  "Game = Our Expectation."

---

## 8. Roadmap (after the v1 baseline)

Each its own spec → plan → implementation cycle, rough order:
1. **Skill-by-skill expansion** — more Druid skills; later other classes (class differences = data).
2. **Item-by-item validation** — affix-level confidence.
3. **Defence/life stage** — Armor→DR vs target level, resistances, %DR, max-life, Fortify/Barrier, EHP.
4. **Movement stage** — movement speed.
5. **Web UI** — build editor (class/level/item creator + selector, passive & Paragon pickers, buffs/conditions),
   character sheet, save/load. .NET API + SPA/Blazor (front-end choice deferred).
6. **AoE visualizer widget** — skill shape/range/size drawn to scale, reacting to +Size affixes (the
   beyond-PoB differentiator), as a dedicated pipeline output rendered in the web UI.
7. **Stat-weighting / "what upgrades me most"** — Ava's sheet already computes relative-gain/weight per stat;
   port that as an optimization view.

---

## 9. Conventions & tech

- **Language/runtime:** .NET (current LTS), C#. `Engine` is a pure library (no AWS/web/BW.Libs deps).
- **Storage:** DynamoDB single-table (game content) + BW.Libs.Config vessels (tuning); DynamoDB Local via Docker in dev.
- **Numerics:** `double`; assert with explicit tolerance (≤1%) in fixture tests.
- **Testing:** xUnit; golden fixtures stored as data alongside tests; engine/assembly tests need no Docker.
- **No hardcoded game constants in formula logic** — all values are data (§1, §5).
- **Breakdown-first** — any computed number must be explainable via the breakdown (§4.6).
- **Project separation** — own repo, own DynamoDB table; only the generic BW.Libs.Config NuGet is shared, and
  only in `Storage`.

---

## 10. Risks & open questions

- **Ava's `0.2` global scalar and bucket bases** — confirmed against the live game during Milestone 1; held in
  `FormulaConfig`, so a correction is a value change, not a code change.
- **Vulnerable base (×1.2 / 20%)** — confirm whether the 20% lives in the bucket base or the additive list.
- **Overpower** — modeled as a conditional bucket; values/availability for Maul confirmed during validation.
- **Maul specifics** — Werebear/Fortify/Overpower interactions and Maul's exact `baseCoeff` are supplied from
  the live game as data; the engine stays agnostic.
- **Single-table key design for game content** — PK/SK + GSI1 access patterns to be finalized in the Storage plan.
- **BW.Libs.Config NuGet surface** — confirm the published API for loading a vessel/KVP collection when wiring `Storage`.
