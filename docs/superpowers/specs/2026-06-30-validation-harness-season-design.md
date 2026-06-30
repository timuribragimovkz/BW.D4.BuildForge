# D4 Build Forge — Live-Maul Validation Harness + Season Seam (Design)

**Date:** 2026-06-30
**Status:** Approved (brainstorming → spec)
**Phase:** 0 (local-only; no UI, no storage, no cloud)

## 1. Goal

Give the user a place to express a real **Druid Maul** character (the exact values read in-game) and get the
engine's predicted per-hit damage (non-crit / crit / average) **plus the full bucket breakdown**, so the user
can compare to the live game, find divergence, fix it, and **freeze each verified setup as a golden fixture**.
Thread a **`Season`** dimension through config selection. This unblocks "go test against the game" — today
there is no surface to enter a real build and read the engine's prediction.

## 2. Correctness oracle & data sourcing (decided)

- **Oracle = the live game.** The expected number is what the game's Maul tooltip shows — supplied by the user.
- **Milestone 1 = hand-fed values** off the user's own character (~10 numbers + the Maul tooltip). Single
  source isolates engine math from data-mapping error.
- **Scraping build inputs** (Maxroll = curl-able JSON) is deferred to a later bulk-corpus / importer plan; it
  supplies inputs only, never the game's ground-truth output. Matching a planner's *computed* number is an
  optional sanity cross-check, not the oracle.

## 3. Season seam (decided)

- `Season` is an open value (string-based, like `Tag`). A `SeasonRegistry` maps `seasonId → FormulaConfig`,
  behind an **`ISeasonConfigProvider`** seam. Phase 0 = in-memory presets; later swaps to BW.Libs.Config
  vessels with **`seasonId` in the Vessel PK** — zero change to callers.
- **New-season provisioning:** PTR-test, then copy the prior season's vessel wholesale or adjust the copy
  (versioned config snapshots; clone-and-tweak).
- New API: `BuildCalculator.Calculate(Build build, Season season)` resolves `FormulaConfig` via the provider.
  The existing `Calculate(Build, FormulaConfig)` stays as the core (the new overload delegates to it).

## 4. Components (all in `D4BuildForge.Engine` + tests; pure C#, zero AWS/web/storage deps)

1. **`Season`** — `readonly record struct Season(string Id)` with a constant for the current season preset.
2. **`ISeasonConfigProvider`** — `FormulaConfig Get(Season season)`. Phase-0 impl `InMemorySeasonConfigProvider`
   backed by a `seasonId → FormulaConfig` map (default entry = the existing Druid tuning: divisor 800, scalar
   0.2, crit 1.5).
3. **`BuildCalculator.Calculate(Build, Season)`** overload (uses a default provider; provider injectable for tests).
4. **`MaulScenario`** (test-side builder) — an ergonomic, readable way to express a real Druid Maul build as
   data: base stats for level (Willpower sum → MainStat, weapon damage), Maul base coeff + total ranks, each
   additive-bucket source (+%damage, vs-close, core, etc.), vulnerable / crit-damage / all-damage
   contributions, crit chance, attack speed, and a Werebear toggle. Produces a `Build` (+ `Season`). It is a
   thin assembler over the existing `IModifierSource` bricks — no new engine concepts.
5. **`BreakdownFormatter`** — `Breakdown → string`: each line as `Label: value (detail)`, readable, so when
   engine ≠ game the off-bucket is obvious. Lives in the engine (useful beyond tests).
6. **Data-driven xUnit validation fixtures** — each real scenario asserts the engine's chosen output ≈ the
   user's observed game number within **≤1%** (spec §7 of the engine spec; `Approx.Equal` relTol `1e-2`); on
   mismatch the test message includes the formatted breakdown (engine vs expected). **A passing scenario is a
   locked golden fixture** — this is the "test → compare → lock baseline" loop in one mechanism.

## 5. Data flow

`MaulScenario` (hand-fed real values) → `Build` + `Season` → `BuildCalculator.Calculate` (resolves
`FormulaConfig` via `ISeasonConfigProvider`) → `CalcResult` (NonCrit/Crit/Average + `Breakdown`) → fixture
asserts vs the user's game tooltip; on failure prints the breakdown.

## 6. Out of scope (YAGNI / later plans)

- No UI; no `dotnet run` console printer (trivial fast-follow if hand-editing tests feels clunky live).
- No storage / DynamoDB / actual vessels (the seam is in-memory; vessels are the Storage plan).
- No build scraping / importer (later bulk-corpus plan).
- No enemy mitigation (Milestone 2). The harness matches the **tooltip**, pre-mitigation.

## 7. Testing

- TDD. The `ISeasonConfigProvider` resolution, the `MaulScenario → Build` assembly, the `BreakdownFormatter`
  output, and the `Calculate(Build, Season)` overload each get focused unit tests.
- One seeded illustrative Maul scenario with **representative** numbers proves the harness end-to-end (asserts
  a hand-computed expected value, not a live one). Real live-game scenarios are added by the user as fixtures.

## 8. Risks / open

- **Which engine output equals the tooltip number** (avg? non-crit?) is calibrated when the user supplies the
  first real numbers; the fixture exposes all outputs so the mapping is chosen from evidence.
- Vulnerable `×1.2` base and Ava's `0.2` scalar remain data in `FormulaConfig` (engine spec §10); a correction
  is a value change in the season preset, not code.
