# Live-Maul Validation Harness + Season Seam — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a `Season` config seam and a hand-fed `MaulScenario` validation harness so the user can express a real Druid Maul build, run it through the existing engine, and compare the predicted hit + bucket breakdown against the live game — each passing scenario becoming a golden fixture.

**Architecture:** A `Season` value + `ISeasonConfigProvider` seam resolves a `FormulaConfig` (in-memory presets now; `seasonId`-keyed vessels later). A new `BuildCalculator.Calculate(Build, Season)` overload uses it. A test-side `MaulScenario` assembles a `Build` from real Maul values via the existing `IModifierSource` bricks; a `BreakdownFormatter` renders the breakdown; data-driven xUnit fixtures assert engine ≈ game (≤1%) and print the breakdown on mismatch.

**Tech Stack:** .NET 10 (`net10.0`), C#, xUnit. Pure `D4BuildForge.Engine` library — no AWS/web/storage.

## Global Constraints

- **Target framework:** `net10.0`. Test framework: **xUnit**. `D4BuildForge.Engine` keeps ZERO deps on AWS/web/BW.Libs.Config/DynamoDB.
- **No hardcoded game constants in formula logic** — season presets hold the tuning (`FormulaConfig`); the current preset reuses the existing `FormulaConfig.Druid` (divisor 800, scalar 0.2, crit 1.5).
- **`Season`/`BucketKey`/`Tag` are open string-based value types.** No `switch(class)`/`switch(season)` in formulas.
- **Numerics:** `double`; assert with the existing `Approx.Equal` helper. Live-game fixtures use relTol `1e-2` (≤1%); hand-computed seed fixtures use the default `1e-6`.
- **Test files must NOT include `using Xunit;`** — the test project has a global Xunit using.
- **Breakdown-first** — divergence is diagnosed via the breakdown.
- **Repo:** `~/gameSources/d4_build_forge`. Spec: `docs/superpowers/specs/2026-06-30-validation-harness-season-design.md`. Build on a worktree off `master`.

**Out of scope (later plans):** UI / `dotnet run` console printer; real storage/vessels; build scraping/importer; enemy mitigation (Milestone 2); per-affix provenance (item-by-item phase).

**Existing engine entry points this plan builds on:** `BuildCalculator.Calculate(Build, FormulaConfig) -> CalcResult`; `FormulaConfig.Druid`; `Modifier.Flat(StatChannel, double, SourceRef, params Tag[])`, `Modifier.Damage(BucketKey, double, SourceRef, params Tag[])`; sources `BaseStatsForLevel(int,double,double)`; model `Build(int Level, IReadOnlyList<IModifierSource> Sources, SkillSelection Skill, IReadOnlySet<Tag> ActiveState, Target Target)`, `SkillSelection(string,double,int)`, `Target(int,double)`, `CalcResult(double NonCrit,double Crit,double Average,double Dps,Breakdown Breakdown)`, `Breakdown.Lines : IReadOnlyList<BreakdownLine(string Label,double Value,string Detail)>`; `StatChannel.{MainStat,WeaponDamage,CritChance,AttackSpeed}`; `BucketKey.{Additive,Vulnerable,CritDamage,AllDamage}`.

---

### Task 1: Season + ISeasonConfigProvider + in-memory provider

**Files:**
- Create: `src/D4BuildForge.Engine/Core/Season.cs`
- Create: `src/D4BuildForge.Engine/Config/ISeasonConfigProvider.cs`
- Create: `src/D4BuildForge.Engine/Config/InMemorySeasonConfigProvider.cs`
- Test: `tests/D4BuildForge.Engine.Tests/Config/SeasonConfigProviderTests.cs`

**Interfaces:**
- Produces:
  - `readonly record struct Season(string Id)` with `static readonly Season Current = new("s13")`.
  - `interface ISeasonConfigProvider { FormulaConfig Get(Season season); }`
  - `sealed class InMemorySeasonConfigProvider : ISeasonConfigProvider` — default ctor seeds `{ "s13": FormulaConfig.Druid }`; an overload ctor takes a custom `IReadOnlyDictionary<string, FormulaConfig>`. `Get` returns the mapped config or throws `KeyNotFoundException` naming the season.

- [ ] **Step 1: Write the failing test**

`tests/D4BuildForge.Engine.Tests/Config/SeasonConfigProviderTests.cs`:
```csharp
using System.Collections.Generic;
using D4BuildForge.Engine.Config;
using D4BuildForge.Engine.Core;

namespace D4BuildForge.Engine.Tests.Config;

public class SeasonConfigProviderTests
{
    [Fact]
    public void Current_season_resolves_to_druid_tuning()
    {
        var cfg = new InMemorySeasonConfigProvider().Get(Season.Current);
        Assert.Equal(800, cfg.MainStatDivisor);
        Assert.Equal(0.2, cfg.GlobalSkillScalar);
        Assert.Equal(1.5, cfg.BaseCritMultiplier);
    }

    [Fact]
    public void Unknown_season_throws()
        => Assert.Throws<KeyNotFoundException>(
            () => new InMemorySeasonConfigProvider().Get(new Season("nope")));

    [Fact]
    public void Custom_map_is_honored()
    {
        var custom = new FormulaConfig(900, 0.25, 1.6, new Dictionary<string, double>());
        var provider = new InMemorySeasonConfigProvider(
            new Dictionary<string, FormulaConfig> { ["x"] = custom });
        Assert.Equal(900, provider.Get(new Season("x")).MainStatDivisor);
    }
}
```

- [ ] **Step 2: Run to verify it fails**

Run: `dotnet test --filter SeasonConfigProviderTests`
Expected: FAIL — `Season`/`ISeasonConfigProvider`/`InMemorySeasonConfigProvider` not found.

- [ ] **Step 3: Implement**

`Core/Season.cs`:
```csharp
namespace D4BuildForge.Engine.Core;

public readonly record struct Season(string Id)
{
    public static readonly Season Current = new("s13");
    public override string ToString() => Id;
}
```

`Config/ISeasonConfigProvider.cs`:
```csharp
using D4BuildForge.Engine.Core;

namespace D4BuildForge.Engine.Config;

public interface ISeasonConfigProvider
{
    FormulaConfig Get(Season season);
}
```

`Config/InMemorySeasonConfigProvider.cs`:
```csharp
using System.Collections.Generic;
using D4BuildForge.Engine.Core;

namespace D4BuildForge.Engine.Config;

public sealed class InMemorySeasonConfigProvider : ISeasonConfigProvider
{
    private readonly IReadOnlyDictionary<string, FormulaConfig> _bySeason;

    public InMemorySeasonConfigProvider()
        : this(new Dictionary<string, FormulaConfig> { [Season.Current.Id] = FormulaConfig.Druid }) { }

    public InMemorySeasonConfigProvider(IReadOnlyDictionary<string, FormulaConfig> bySeason)
        => _bySeason = bySeason;

    public FormulaConfig Get(Season season)
        => _bySeason.TryGetValue(season.Id, out var cfg)
            ? cfg
            : throw new KeyNotFoundException($"No config for season '{season.Id}'.");
}
```

- [ ] **Step 4: Run to verify it passes**

Run: `dotnet test --filter SeasonConfigProviderTests`
Expected: PASS (3).

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "feat(engine): Season + ISeasonConfigProvider + in-memory provider (seasonId-keyed)"
```

---

### Task 2: BuildCalculator.Calculate(Build, Season) overloads

**Files:**
- Modify: `src/D4BuildForge.Engine/BuildCalculator.cs`
- Test: `tests/D4BuildForge.Engine.Tests/Config/CalculateBySeasonTests.cs`

**Interfaces:**
- Consumes: Task 1 (`Season`, `ISeasonConfigProvider`, `InMemorySeasonConfigProvider`); existing `Calculate(Build, FormulaConfig)`.
- Produces:
  - `static CalcResult BuildCalculator.Calculate(Build build, Season season)` — resolves via a private default `InMemorySeasonConfigProvider`.
  - `static CalcResult BuildCalculator.Calculate(Build build, Season season, ISeasonConfigProvider provider)` — `Calculate(build, provider.Get(season))`.

- [ ] **Step 1: Write the failing test**

`tests/D4BuildForge.Engine.Tests/Config/CalculateBySeasonTests.cs`:
```csharp
using System.Collections.Generic;
using D4BuildForge.Engine;
using D4BuildForge.Engine.Core;
using D4BuildForge.Engine.Model;
using D4BuildForge.Engine.Sources;

namespace D4BuildForge.Engine.Tests.Config;

public class CalculateBySeasonTests
{
    private static Build SimpleBuild() => new(
        Level: 80,
        Sources: new List<IModifierSource>
        {
            new BaseStatsForLevel(80, 800, 100),
            // additive +50% via the existing Damage helper:
            new SingleMod(Modifier.Damage(BucketKey.Additive, 0.50, new SourceRef("Test", "x")))
        },
        Skill: new SkillSelection("Maul", 0.45, 1),
        ActiveState: new HashSet<Tag>(),
        Target: new Target(81, 0));

    private sealed class SingleMod(Modifier m) : IModifierSource
    {
        public IEnumerable<Modifier> GetModifiers() => new[] { m };
    }

    [Fact]
    public void Season_overload_matches_direct_formulaconfig()
    {
        var bySeason = BuildCalculator.Calculate(SimpleBuild(), Season.Current);
        var direct = BuildCalculator.Calculate(SimpleBuild(), FormulaConfig.Druid);
        Approx.Equal(direct.NonCrit, bySeason.NonCrit);
        Approx.Equal(27.0, bySeason.NonCrit); // 100*2.0*1.5*0.45*0.2
    }
}
```

- [ ] **Step 2: Run to verify it fails**

Run: `dotnet test --filter CalculateBySeasonTests`
Expected: FAIL — no `Calculate(Build, Season)` overload.

- [ ] **Step 3: Implement** — add to `BuildCalculator.cs` (keep the existing `Calculate(Build, FormulaConfig)` unchanged):
```csharp
using D4BuildForge.Engine.Config;
using D4BuildForge.Engine.Core;
// ... existing usings ...

// inside `public static class BuildCalculator`:
private static readonly ISeasonConfigProvider DefaultSeasonConfig = new InMemorySeasonConfigProvider();

public static CalcResult Calculate(Build build, Season season)
    => Calculate(build, season, DefaultSeasonConfig);

public static CalcResult Calculate(Build build, Season season, ISeasonConfigProvider provider)
    => Calculate(build, provider.Get(season));
```

- [ ] **Step 4: Run to verify it passes**

Run: `dotnet test --filter CalculateBySeasonTests`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "feat(engine): BuildCalculator.Calculate(Build, Season) overloads via config provider"
```

---

### Task 3: BreakdownFormatter

**Files:**
- Create: `src/D4BuildForge.Engine/BreakdownFormatter.cs`
- Test: `tests/D4BuildForge.Engine.Tests/BreakdownFormatterTests.cs`

**Interfaces:**
- Consumes: `Breakdown`, `BreakdownLine`.
- Produces: `static string BreakdownFormatter.Format(Breakdown breakdown)` — one line per entry, `"{Label}: {Value}"`, with ` ({Detail})` appended when `Detail` is non-empty, joined by `\n`.

- [ ] **Step 1: Write the failing test**

`tests/D4BuildForge.Engine.Tests/BreakdownFormatterTests.cs`:
```csharp
using D4BuildForge.Engine;
using D4BuildForge.Engine.Model;

namespace D4BuildForge.Engine.Tests;

public class BreakdownFormatterTests
{
    [Fact]
    public void Formats_lines_with_optional_detail()
    {
        var b = new Breakdown();
        b.Add("WeaponDamage", 100);
        b.Add("Additive", 1.5, "1 + Σ");
        var text = BreakdownFormatter.Format(b);
        Assert.Equal("WeaponDamage: 100\nAdditive: 1.5 (1 + Σ)", text);
    }
}
```

- [ ] **Step 2: Run to verify it fails**

Run: `dotnet test --filter BreakdownFormatterTests`
Expected: FAIL — `BreakdownFormatter` not found.

- [ ] **Step 3: Implement**

`BreakdownFormatter.cs`:
```csharp
using System.Globalization;
using System.Linq;
using D4BuildForge.Engine.Model;

namespace D4BuildForge.Engine;

public static class BreakdownFormatter
{
    public static string Format(Breakdown breakdown)
        => string.Join("\n", breakdown.Lines.Select(FormatLine));

    private static string FormatLine(BreakdownLine line)
    {
        var value = line.Value.ToString(CultureInfo.InvariantCulture);
        return string.IsNullOrEmpty(line.Detail)
            ? $"{line.Label}: {value}"
            : $"{line.Label}: {value} ({line.Detail})";
    }
}
```

- [ ] **Step 4: Run to verify it passes**

Run: `dotnet test --filter BreakdownFormatterTests`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "feat(engine): BreakdownFormatter (readable breakdown text)"
```

---

### Task 4: MaulScenario builder (test-side)

**Files:**
- Create: `tests/D4BuildForge.Engine.Tests/Validation/MaulScenario.cs`
- Test: `tests/D4BuildForge.Engine.Tests/Validation/MaulScenarioTests.cs`

**Interfaces:**
- Consumes: `Build`, `SkillSelection`, `Target`, `Modifier`, `BaseStatsForLevel`, `StatChannel`, `BucketKey`, `Tag`, `Season`.
- Produces (in namespace `D4BuildForge.Engine.Tests.Validation`):
  - `record MaulScenario(int Level, double MainStat, double WeaponDamage, double MaulBaseCoeff, int MaulRanks, double AdditivePct, double AllDamagePct, double CritChance, double CritDamagePct, double AttackSpeedPct, double VulnerablePct, bool TargetVulnerable, bool InWerebear, Season Season)`
  - `Build ToBuild()` — assembles base stats + a `ScenarioStats` source (flats for CritChance/AttackSpeed; damage mods for the four buckets; the vulnerable mod carries a `Vulnerable` condition tag) + the Maul `SkillSelection`; `ActiveState` = `{Werebear}` if `InWerebear`, plus `{Vulnerable}` if `TargetVulnerable`.
  - `static MaulScenario Seed()` — a representative hand-computable scenario (see test).

- [ ] **Step 1: Write the failing test**

`tests/D4BuildForge.Engine.Tests/Validation/MaulScenarioTests.cs`:
```csharp
using D4BuildForge.Engine;
using D4BuildForge.Engine.Core;

namespace D4BuildForge.Engine.Tests.Validation;

public class MaulScenarioTests
{
    [Fact]
    public void Seed_scenario_computes_hand_value()
    {
        // Seed(): Level 80, MainStat 800 (->mainStatMult 2.0), WeaponDamage 100,
        // Maul 0.45 @ 1 rank (->0.45), AdditivePct 0.30 (->1.30), everything else 0,
        // not vulnerable, in Werebear. NonCrit = 100*2.0*1.30*0.45*0.2 = 23.4
        var result = BuildCalculator.Calculate(MaulScenario.Seed().ToBuild(), Season.Current);
        Approx.Equal(23.4, result.NonCrit);
    }

    [Fact]
    public void Vulnerable_modifier_is_gated_off_when_target_not_vulnerable()
    {
        var notVuln = MaulScenario.Seed() with { VulnerablePct = 1.0, TargetVulnerable = false };
        var vuln = MaulScenario.Seed() with { VulnerablePct = 1.0, TargetVulnerable = true };
        Approx.Equal(23.4, BuildCalculator.Calculate(notVuln.ToBuild(), Season.Current).NonCrit);   // VDM gated off
        Approx.Equal(46.8, BuildCalculator.Calculate(vuln.ToBuild(), Season.Current).NonCrit);      // ×(1+1.0)=2.0
    }
}
```

- [ ] **Step 2: Run to verify it fails**

Run: `dotnet test --filter MaulScenarioTests`
Expected: FAIL — `MaulScenario` not found.

- [ ] **Step 3: Implement**

`tests/D4BuildForge.Engine.Tests/Validation/MaulScenario.cs`:
```csharp
using System.Collections.Generic;
using D4BuildForge.Engine.Core;
using D4BuildForge.Engine.Model;
using D4BuildForge.Engine.Sources;

namespace D4BuildForge.Engine.Tests.Validation;

public record MaulScenario(
    int Level,
    double MainStat,
    double WeaponDamage,
    double MaulBaseCoeff,
    int MaulRanks,
    double AdditivePct,
    double AllDamagePct,
    double CritChance,
    double CritDamagePct,
    double AttackSpeedPct,
    double VulnerablePct,
    bool TargetVulnerable,
    bool InWerebear,
    Season Season)
{
    public static MaulScenario Seed() => new(
        Level: 80, MainStat: 800, WeaponDamage: 100,
        MaulBaseCoeff: 0.45, MaulRanks: 1,
        AdditivePct: 0.30, AllDamagePct: 0, CritChance: 0, CritDamagePct: 0,
        AttackSpeedPct: 0, VulnerablePct: 0, TargetVulnerable: false, InWerebear: true,
        Season: Season.Current);

    public Build ToBuild()
    {
        var state = new HashSet<Tag>();
        if (InWerebear) state.Add(new Tag("Werebear"));
        if (TargetVulnerable) state.Add(new Tag("Vulnerable"));

        return new Build(
            Level: Level,
            Sources: new List<IModifierSource>
            {
                new BaseStatsForLevel(Level, MainStat, WeaponDamage),
                new ScenarioStats(this),
            },
            Skill: new SkillSelection("Maul", MaulBaseCoeff, MaulRanks),
            ActiveState: state,
            Target: new Target(Level + 1, 0));
    }

    private sealed class ScenarioStats(MaulScenario s) : IModifierSource
    {
        public IEnumerable<Modifier> GetModifiers()
        {
            var src = new SourceRef("Scenario", "Maul");
            yield return Modifier.Flat(StatChannel.CritChance, s.CritChance, src);
            yield return Modifier.Flat(StatChannel.AttackSpeed, s.AttackSpeedPct, src);
            yield return Modifier.Damage(BucketKey.Additive, s.AdditivePct, src);
            yield return Modifier.Damage(BucketKey.AllDamage, s.AllDamagePct, src);
            yield return Modifier.Damage(BucketKey.CritDamage, s.CritDamagePct, src);
            yield return Modifier.Damage(BucketKey.Vulnerable, s.VulnerablePct, src, new Tag("Vulnerable"));
        }
    }
}
```

- [ ] **Step 4: Run to verify it passes**

Run: `dotnet test --filter MaulScenarioTests`
Expected: PASS (2).

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "test(engine): MaulScenario builder — hand-fed Druid Maul build assembly"
```

---

### Task 5: Validation fixture (compare + breakdown-on-mismatch)

**Files:**
- Create: `tests/D4BuildForge.Engine.Tests/Validation/MaulValidationTests.cs`

**Interfaces:**
- Consumes: `MaulScenario`, `BuildCalculator.Calculate(Build, Season)`, `BreakdownFormatter`, `Approx`.
- Produces: a reusable assertion `AssertMatchesGame(MaulScenario scenario, double expectedNonCrit)` that runs the engine, asserts `NonCrit` within ≤1% (`Approx.Equal(..., 1e-2)`), and on failure surfaces the formatted breakdown. The seeded scenario is wired as the first passing fixture; real live-game scenarios get added here later.

- [ ] **Step 1: Write the failing test**

`tests/D4BuildForge.Engine.Tests/Validation/MaulValidationTests.cs`:
```csharp
using System;
using D4BuildForge.Engine;
using D4BuildForge.Engine.Model;

namespace D4BuildForge.Engine.Tests.Validation;

public class MaulValidationTests
{
    // === GOLDEN FIXTURES: one row per verified-against-game Druid Maul scenario. ===
    // Seed row uses representative numbers (NonCrit 23.4). Replace/add rows with REAL
    // in-game values + the tooltip number you read; a passing row is a locked baseline.

    [Fact]
    public void Seed_scenario_matches_expected()
        => AssertMatchesGame(MaulScenario.Seed(), expectedNonCrit: 23.4);

    private static void AssertMatchesGame(MaulScenario scenario, double expectedNonCrit)
    {
        CalcResult result = BuildCalculator.Calculate(scenario.ToBuild(), scenario.Season);
        double diff = Math.Abs(expectedNonCrit - result.NonCrit);
        double tol = Math.Max(1e-2 * Math.Abs(expectedNonCrit), 1e-9); // ≤1% per spec
        Assert.True(diff <= tol,
            $"Engine NonCrit {result.NonCrit} != expected {expectedNonCrit} (diff {diff} > tol {tol}).\n" +
            $"Breakdown:\n{BreakdownFormatter.Format(result.Breakdown)}");
    }
}
```

- [ ] **Step 2: Run to verify it fails**

Run: `dotnet test --filter MaulValidationTests`
Expected: FAIL to compile — `MaulValidationTests` references nothing missing, but the file is new; after it compiles, the seed row PASSES (23.4 matches the engine). If the row is red because the seed expectation is wrong, fix the expectation, not the engine. (To prove the breakdown-on-mismatch path once, temporarily pass `expectedNonCrit: 999`, confirm the failure message contains the formatted breakdown, then revert to 23.4.)

- [ ] **Step 3: Verify (test-only — no production code)**

This fixture is test-only. Confirm the seed row passes and the failure message wiring compiles. Keep the seed row asserting 23.4.

- [ ] **Step 4: Run to verify it passes + full suite**

Run: `dotnet test --filter MaulValidationTests` then `dotnet test`
Expected: validation fixture PASSES; full suite stays green.

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "test(engine): Maul validation fixture — compare engine vs game, breakdown on mismatch"
```

---

## Done criteria
- `dotnet test` green; new tests cover season resolution, the `Calculate(Build, Season)` overloads, the formatter, the `MaulScenario` assembly (incl. vulnerable gating), and the validation fixture.
- The user can add a real Druid Maul row to `MaulValidationTests` (real inputs via `MaulScenario` + the in-game tooltip number) and immediately see pass/fail with a breakdown.

## Next (not this plan)
1. User supplies real Maul numbers → first live golden fixture (calibrate which output = the tooltip).
2. Mitigation stage (Milestone 2). 3. Storage: swap `InMemorySeasonConfigProvider` for the vessel-backed (`seasonId` PK) provider.
