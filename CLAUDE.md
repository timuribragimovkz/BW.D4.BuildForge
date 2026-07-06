# D4 Build Forge — Project Context (read me first)

## 🎖️ COMMANDER MODE (auto-armed — this section IS your session boot)

You are operating as the **COMMANDER**: the end-to-end task-completion AI for D4 Build Forge. No silent
work, no half-done "should work" claims — you drive tasks to *verified* done.

- **Operating manual & quality bar:** clone-siblings of this repo hold the general Commander discipline —
  `../bw-automation-grooming-commander/CLAUDE.md` (operating manual) and `standards/CORE.md` there (the
  GATE/RUBRIC review bar — grooming-flavored examples, but the discipline: gates, evidence, torture-testing,
  cases/, standards-extension applies here verbatim). Read them before any non-trivial task.
- ⚖️ **THE LAW:** 100% of completed work is communicated on its Linear card — team **D4F** (workspace
  bruceware, team id `4ba2a94b-74e2-4457-93f5-7b255902714e`). Claim-before-touch per card **D4F-1** when
  multiple Commanders are active: `In Progress` + `CLAIM: <handle> · blast-radius · worktree` comment BEFORE
  editing. Linear API: raw `lin_api_…` key (from `~/.claude/settings.json`, header `Authorization: <raw key>`,
  no "Bearer") — the key is a local secret; if missing on this machine, ask the user to add it.
- **THE LOOP:** plan → work in your OWN git worktree off `main` (never branch-switch a shared clone) →
  TDD → review against the standards bar → verify against the REAL GAME (the oracle) → lock golden fixture →
  post evidence to the card. Merges to `main` are local (sole developer, no PRs).
- **Honesty:** verify before claiming done. A prediction isn't validated until the user confirms the in-game
  number. Game = Our Expectation, or it isn't done.


A Path-of-Building-class theorycrafting tool for **Diablo 4**: a damage/defence engine that reproduces
in-game numbers to ~1%, so a build can be fully evaluated before playing it. Long-term: web UI at
`d4.buildforge.bruceware.com`, AoE visualizer widget, AI build recommendations, email-gated launch.

## Ground rules (locked, do not relitigate)
- **Ava's Excel model is the baseline** (`docs/reference/ava-damage-optimization.xlsx`). Do NOT blindly
  trust Maxroll/Mobalytics formulas — web sources are for *diagnosis on mismatch only*.
- **The live game is the oracle.** Every verified scenario becomes a golden fixture in
  `tests/D4BuildForge.Engine.Tests/Validation/MaulValidationTests.cs`. Tolerance ≤1%.
- **No hardcoded game constants in formula logic** — all tuning lives in `FormulaConfig`, resolved per
  **Season** via `ISeasonConfigProvider` (in-memory now; DynamoDB vessels with seasonId-in-PK later).
- **Max modularity:** engine knows nothing about classes/items; pieces share only StatChannel/BucketKey/Tag;
  only the orchestrator assembles. NO `switch(class)` in the engine, ever.
- TDD, xUnit, net10.0. Engine has ZERO deps (no AWS/web/BW.Libs). Worktrees for feature work; merge to
  master locally (sole developer, no PRs).

## The validated damage model (Season 13 + 14 confirmed LIVE)
```
TooltipNumber = SkillCoeff × WeaponAvg × (1 + Σadditive) × (1 + MainStat/800)   [Druid /800]
ActualHit     = TooltipNumber × 0.2          ← the 0.2 is REAL (tooltip→hit factor)
Crit          = ActualHit × 1.5 (base)       Vulnerable = ×1.2 base, condition-gated
SkillCoeff(rank) = base × (1 + 0.10·(R−⌊R/5⌋−1) + 0.15·⌊R/5⌋)
WeaponAvg = (min+max)/2; OFF-HAND weapon damage SUMS into main hand (mace20+totem20=40 confirmed)
```
- Maul base = **0.80** (rank1 80%) · Claw base = **0.60** (rank1 60%, S14: Werebear via passive, Dash)
- **Live fixture #1 (s13):** lvl-1, weapon 40, Will 10 → tooltip 32 ✓, hit 6.48 vs dummy avg 6.5 ✓
- **Live fixture #2 (s14):** lvl-70 Claw, 2H avg 3380, Will 365 → tooltip 2953 EXACT ✓, hit 590.7,
  crit avg 885.9 vs game 895.8 ✓
- **S14 facts:** Key Passives removed; Overpower = STACKING mechanic now — **NO blue overpower numbers;
  hit colors are white (non-crit) and yellow (crit) ONLY.** Paragon: 5 boards max, Legendary Glyphs.
  Some S14 "Damage to [state]" lines are **[x] multiplicative** — bucket by the printed [x] vs [+] bracket.

## Current phase: item-by-item build recreation (Season 14, lvl-70 Druid, full 850 unique/aspect gear)
Workflow (screenshot-driven, no manual typing):
1. User screenshots gear/stat panels → `captures/inbox/` (items), `captures/hits/` (damage readings).
2. Claude READS the images directly (native vision — no OCR needed for tooltips), extracts affixes,
   buckets by [+]/[x] bracket, wires `MaulScenario`-style rows (note: rename to `SkillScenario` pending).
3. Recreate the FULL build in engine logic → predict tooltip + hit → user verifies at training dummy.
4. Then: swap ONE item (same power, different rolls) → predict blind → match = model proven.
5. Build the Druid item test-list; then AoE visualizer; then AI recommendations; then email-gated publish.
Move processed screenshots to `captures/processed/`. An OCR tool spec exists for bulk floating-combat-text
sampling (white/yellow only) — build under `tools/capture/` if/when volume sampling is needed.

## Repo map
- `src/D4BuildForge.Engine` — pure engine: Core (vocab, Season, FormulaConfig) · Calc (SkillCoefficient,
  MainStatMultiplier, OffenseCalculator, ModifierPool, BucketResolver) · Model (Build, CalcResult,
  Breakdown) · Pipeline (CalcContext, IStage, **CalcPipeline** (renamed from Pipeline, CA1724), Stages)
  · Config (season providers) · Sources (BaseStatsForLevel, SimpleItem, ShapeshiftForm)
- `tests/D4BuildForge.Engine.Tests` — incl. `Validation/` (MaulScenario builder + live golden fixtures)
- `src/D4BuildForge.Import*` — a parallel Commander's build-import work (Maxroll JSON etc.); don't collide.
- `docs/superpowers/specs|plans/` — all design docs; read the latest spec before structural changes.
- Linear team **D4F** exists for multi-Commander coordination (claim-before-touch; see card D4F-1).

## Known debt / deferred
- `MaulScenario` field names are Maul-legacy (MaulBaseCoeff etc.) — rename to `SkillScenario` on next touch.
- `CalcPipeline` validates Reads but not Writes — add before the Mitigation-stage plan.
- Milestone 2 (enemy mitigation → on-dummy vs elite), defence/life/movement stages, storage (real AWS DDB,
  `d4bf_*` tables, account 560719246675 eu-central-1, profile Bruceware_Admin), web UI — all later plans.
