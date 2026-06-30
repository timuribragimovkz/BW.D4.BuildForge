# D4 Build Forge — Engine Core Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the pure-C# `D4BuildForge.Engine` library that reproduces D4's damage math (Ava's
community-validated model) to floating-point exactness, end-to-end, with no storage/cloud — runnable and
testable locally.

**Architecture:** Modifier pool → bucket resolver (the bucket primitive: within a bucket sources add
`base+Σ`, buckets multiply) → insertable calc-pipeline stages → `CalcResult` with a full breakdown. Every
piece is independent and agnostic; only an orchestrator assembles them (here, tests construct `Build`s
directly — the DB-backed `BuildAssembler` is a later plan).

**Tech Stack:** .NET 10 (`net10.0`), C#, xUnit. Pure library — no AWS, no web, no BW.Libs, no DynamoDB.

## Global Constraints

- **Target framework:** `net10.0` (SDK 10.0.100 confirmed installed). Test framework: **xUnit**.
- **`D4BuildForge.Engine` has ZERO dependencies** on AWS SDK, web, BW.Libs.Config, or DynamoDB. Pure C#.
- **No hardcoded game constants in formula logic.** All tunable values (main-stat divisor, global skill
  scalar, base crit multiplier, bucket bases) live in `FormulaConfig` (data passed in).
- **No `switch(class)` / class conditionals in the engine.** Class differences are data + sources + tags.
- **`Tag` and `BucketKey` are open string-based value types** (new game content never edits a central enum).
  `StatChannel` is the one stable enum (the engine's computed channels).
- **Numerics:** `double`; assert with the `Approx.Equal` relative-tolerance helper (Task 1). Engine-internal
  reproductions are near-exact (relTol `1e-6`); live-game fixtures use ≤1% (relTol `1e-2`).
- **Breakdown-first:** every computed damage number must be explainable via the breakdown.
- **Repo:** `~/gameSources/d4_build_forge`. Spec: `docs/superpowers/specs/2026-06-30-d4-build-forge-engine-design.md`.

**Out of scope for this plan (follow-up plans):** the per-concept DynamoDB tables + config vessels (spec §5),
the DB-backed `BuildAssembler` (spec §4.5/§5), Milestone 2 mitigation stage (spec §2), defences/movement
stages, and the web UI (spec §8).

---

### Task 1: Solution + Engine + Engine.Tests scaffold

**Files:**
- Create: `D4BuildForge.sln`
- Create: `src/D4BuildForge.Engine/D4BuildForge.Engine.csproj`
- Create: `tests/D4BuildForge.Engine.Tests/D4BuildForge.Engine.Tests.csproj`
- Create: `tests/D4BuildForge.Engine.Tests/Approx.cs`
- Create: `tests/D4BuildForge.Engine.Tests/SmokeTest.cs`

**Interfaces:**
- Consumes: nothing.
- Produces: `Approx.Equal(double expected, double actual, double relTol = 1e-6)` test helper used by all later tasks.

- [ ] **Step 1: Create the solution and projects**

```bash
cd ~/gameSources/d4_build_forge
dotnet new sln -n D4BuildForge
dotnet new classlib -n D4BuildForge.Engine -o src/D4BuildForge.Engine -f net10.0
dotnet new xunit  -n D4BuildForge.Engine.Tests -o tests/D4BuildForge.Engine.Tests -f net10.0
rm src/D4BuildForge.Engine/Class1.cs tests/D4BuildForge.Engine.Tests/UnitTest1.cs
dotnet sln add src/D4BuildForge.Engine/D4BuildForge.Engine.csproj
dotnet sln add tests/D4BuildForge.Engine.Tests/D4BuildForge.Engine.Tests.csproj
dotnet add tests/D4BuildForge.Engine.Tests/D4BuildForge.Engine.Tests.csproj reference src/D4BuildForge.Engine/D4BuildForge.Engine.csproj
```

- [ ] **Step 2: Enable nullable + implicit usings in the Engine csproj**

Ensure `src/D4BuildForge.Engine/D4BuildForge.Engine.csproj` `<PropertyGroup>` contains:

```xml
<TargetFramework>net10.0</TargetFramework>
<ImplicitUsings>enable</ImplicitUsings>
<Nullable>enable</Nullable>
```

- [ ] **Step 3: Write the `Approx` test helper**

`tests/D4BuildForge.Engine.Tests/Approx.cs`:

```csharp
using Xunit;

namespace D4BuildForge.Engine.Tests;

internal static class Approx
{
    public static void Equal(double expected, double actual, double relTol = 1e-6)
    {
        var diff = System.Math.Abs(expected - actual);
        var tol = System.Math.Max(relTol * System.Math.Abs(expected), 1e-9);
        Assert.True(diff <= tol, $"Expected {expected}, got {actual} (diff {diff}, tol {tol})");
    }
}
```

- [ ] **Step 4: Write a smoke test**

`tests/D4BuildForge.Engine.Tests/SmokeTest.cs`:

```csharp
using Xunit;

namespace D4BuildForge.Engine.Tests;

public class SmokeTest
{
    [Fact]
    public void Approx_helper_passes_for_equal_values() => Approx.Equal(2543615.648, 2543615.648);
}
```

- [ ] **Step 5: Build and run**

Run: `dotnet test`
Expected: build succeeds, 1 test passes.

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "feat(engine): scaffold solution, Engine + Engine.Tests (net10.0, xUnit) + Approx helper"
```

---

### Task 2: Core vocabulary types

**Files:**
- Create: `src/D4BuildForge.Engine/Core/StatChannel.cs`
- Create: `src/D4BuildForge.Engine/Core/ModOp.cs`
- Create: `src/D4BuildForge.Engine/Core/Tag.cs`
- Create: `src/D4BuildForge.Engine/Core/BucketKey.cs`
- Create: `src/D4BuildForge.Engine/Core/SourceRef.cs`
- Test: `tests/D4BuildForge.Engine.Tests/Core/VocabularyTests.cs`

**Interfaces:**
- Produces:
  - `enum StatChannel { WeaponDamage, MainStat, CritChance, CritDamage, AttackSpeed, SkillRank, Damage }`
  - `enum ModOp { Flat, AdditivePercent, Multiplicative }`
  - `readonly record struct Tag(string Name)` with `Tag.None`
  - `readonly record struct BucketKey(string Name)` with `BucketKey.None` and constants `Additive`, `Vulnerable`, `CritDamage`, `AllDamage`
  - `readonly record struct SourceRef(string Kind, string Name)`

- [ ] **Step 1: Write the failing test**

`tests/D4BuildForge.Engine.Tests/Core/VocabularyTests.cs`:

```csharp
using D4BuildForge.Engine.Core;
using Xunit;

namespace D4BuildForge.Engine.Tests.Core;

public class VocabularyTests
{
    [Fact]
    public void Tags_with_same_name_are_equal()
        => Assert.Equal(new Tag("Werebear"), new Tag("Werebear"));

    [Fact]
    public void BucketKey_constants_have_expected_names()
    {
        Assert.Equal("Additive", BucketKey.Additive.Name);
        Assert.Equal("Vulnerable", BucketKey.Vulnerable.Name);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test --filter VocabularyTests`
Expected: FAIL — type `Tag`/`BucketKey` not found (compile error).

- [ ] **Step 3: Write the types**

`Core/StatChannel.cs`:
```csharp
namespace D4BuildForge.Engine.Core;
public enum StatChannel { WeaponDamage, MainStat, CritChance, CritDamage, AttackSpeed, SkillRank, Damage }
```

`Core/ModOp.cs`:
```csharp
namespace D4BuildForge.Engine.Core;
public enum ModOp { Flat, AdditivePercent, Multiplicative }
```

`Core/Tag.cs`:
```csharp
namespace D4BuildForge.Engine.Core;
public readonly record struct Tag(string Name)
{
    public static readonly Tag None = new("");
    public override string ToString() => Name;
}
```

`Core/BucketKey.cs`:
```csharp
namespace D4BuildForge.Engine.Core;
public readonly record struct BucketKey(string Name)
{
    public static readonly BucketKey None = new("");
    public static readonly BucketKey Additive = new("Additive");
    public static readonly BucketKey Vulnerable = new("Vulnerable");
    public static readonly BucketKey CritDamage = new("CritDamage");
    public static readonly BucketKey AllDamage = new("AllDamage");
    public override string ToString() => Name;
}
```

`Core/SourceRef.cs`:
```csharp
namespace D4BuildForge.Engine.Core;
public readonly record struct SourceRef(string Kind, string Name)
{
    public override string ToString() => $"{Kind}:{Name}";
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test --filter VocabularyTests`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "feat(engine): core vocabulary — StatChannel, ModOp, Tag, BucketKey, SourceRef"
```

---

### Task 3: Modifier + IModifierSource

**Files:**
- Create: `src/D4BuildForge.Engine/Core/Modifier.cs`
- Create: `src/D4BuildForge.Engine/Core/IModifierSource.cs`
- Test: `tests/D4BuildForge.Engine.Tests/Core/ModifierSourceTests.cs`

**Interfaces:**
- Consumes: Task 2 types.
- Produces:
  - `record Modifier(StatChannel Channel, ModOp Op, double Value, BucketKey Bucket, IReadOnlySet<Tag> Conditions, SourceRef Source)` with static helpers `Modifier.Damage(BucketKey, double, SourceRef, params Tag[])` and `Modifier.Flat(StatChannel, double, SourceRef, params Tag[])`.
  - `interface IModifierSource { IEnumerable<Modifier> GetModifiers(); }`

- [ ] **Step 1: Write the failing test**

`tests/D4BuildForge.Engine.Tests/Core/ModifierSourceTests.cs`:

```csharp
using System.Linq;
using D4BuildForge.Engine.Core;
using Xunit;

namespace D4BuildForge.Engine.Tests.Core;

public class ModifierSourceTests
{
    private sealed class FixedSource(params Modifier[] mods) : IModifierSource
    {
        public System.Collections.Generic.IEnumerable<Modifier> GetModifiers() => mods;
    }

    [Fact]
    public void Damage_helper_sets_bucket_and_conditions()
    {
        var src = new SourceRef("Item", "Ring");
        var m = Modifier.Damage(BucketKey.Additive, 0.18, src, new Tag("Close"));
        Assert.Equal(BucketKey.Additive, m.Bucket);
        Assert.Equal(ModOp.AdditivePercent, m.Op);
        Assert.Contains(new Tag("Close"), m.Conditions);
    }

    [Fact]
    public void Source_emits_its_modifiers()
    {
        var src = new SourceRef("Item", "Amulet");
        var source = new FixedSource(Modifier.Flat(StatChannel.CritChance, 0.05, src));
        Assert.Single(source.GetModifiers());
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test --filter ModifierSourceTests`
Expected: FAIL — `Modifier`/`IModifierSource` not found.

- [ ] **Step 3: Write the types**

`Core/Modifier.cs`:
```csharp
using System.Collections.Generic;

namespace D4BuildForge.Engine.Core;

public record Modifier(
    StatChannel Channel,
    ModOp Op,
    double Value,
    BucketKey Bucket,
    IReadOnlySet<Tag> Conditions,
    SourceRef Source)
{
    public static Modifier Damage(BucketKey bucket, double value, SourceRef source, params Tag[] conditions)
        => new(StatChannel.Damage, ModOp.AdditivePercent, value, bucket, new HashSet<Tag>(conditions), source);

    public static Modifier Flat(StatChannel channel, double value, SourceRef source, params Tag[] conditions)
        => new(channel, ModOp.Flat, value, BucketKey.None, new HashSet<Tag>(conditions), source);
}
```

`Core/IModifierSource.cs`:
```csharp
using System.Collections.Generic;

namespace D4BuildForge.Engine.Core;

public interface IModifierSource
{
    IEnumerable<Modifier> GetModifiers();
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test --filter ModifierSourceTests`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "feat(engine): Modifier record + IModifierSource"
```

---

### Task 4: FormulaConfig (tuning data)

**Files:**
- Create: `src/D4BuildForge.Engine/Core/FormulaConfig.cs`
- Test: `tests/D4BuildForge.Engine.Tests/Core/FormulaConfigTests.cs`

**Interfaces:**
- Produces:
  - `record FormulaConfig(double MainStatDivisor, double GlobalSkillScalar, double BaseCritMultiplier, IReadOnlyDictionary<string, double> BucketBases)` with:
    - `static FormulaConfig Druid` — `MainStatDivisor = 800, GlobalSkillScalar = 0.2, BaseCritMultiplier = 1.5`, empty bucket-base overrides.
    - `double BucketBase(BucketKey key)` → returns override if present else `1.0`.

- [ ] **Step 1: Write the failing test**

`tests/D4BuildForge.Engine.Tests/Core/FormulaConfigTests.cs`:

```csharp
using D4BuildForge.Engine.Core;
using Xunit;

namespace D4BuildForge.Engine.Tests.Core;

public class FormulaConfigTests
{
    [Fact]
    public void Druid_defaults_match_ava_model()
    {
        var c = FormulaConfig.Druid;
        Assert.Equal(800, c.MainStatDivisor);
        Assert.Equal(0.2, c.GlobalSkillScalar);
        Assert.Equal(1.5, c.BaseCritMultiplier);
    }

    [Fact]
    public void Unknown_bucket_base_defaults_to_one()
        => Assert.Equal(1.0, FormulaConfig.Druid.BucketBase(BucketKey.Additive));
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test --filter FormulaConfigTests`
Expected: FAIL — `FormulaConfig` not found.

- [ ] **Step 3: Write the type**

`Core/FormulaConfig.cs`:
```csharp
using System.Collections.Generic;

namespace D4BuildForge.Engine.Core;

public record FormulaConfig(
    double MainStatDivisor,
    double GlobalSkillScalar,
    double BaseCritMultiplier,
    IReadOnlyDictionary<string, double> BucketBases)
{
    public static FormulaConfig Druid { get; } = new(
        MainStatDivisor: 800,
        GlobalSkillScalar: 0.2,
        BaseCritMultiplier: 1.5,
        BucketBases: new Dictionary<string, double>());

    public double BucketBase(BucketKey key)
        => BucketBases.TryGetValue(key.Name, out var b) ? b : 1.0;
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test --filter FormulaConfigTests`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "feat(engine): FormulaConfig tuning data (Druid defaults: divisor 800, scalar 0.2, crit 1.5)"
```

---

### Task 5: SkillCoefficient (rank scaling)

**Files:**
- Create: `src/D4BuildForge.Engine/Calc/SkillCoefficient.cs`
- Test: `tests/D4BuildForge.Engine.Tests/Calc/SkillCoefficientTests.cs`

**Interfaces:**
- Produces: `static double SkillCoefficient.Compute(double baseCoeff, int totalRanks)` implementing
  `baseCoeff · (1 + 0.10·(R − ⌊R/5⌋ − 1) + 0.15·⌊R/5⌋)`.

- [ ] **Step 1: Write the failing test**

`tests/D4BuildForge.Engine.Tests/Calc/SkillCoefficientTests.cs`:

```csharp
using D4BuildForge.Engine.Calc;
using Xunit;

namespace D4BuildForge.Engine.Tests.Calc;

public class SkillCoefficientTests
{
    [Fact] // Ava worked example: baseCoeff 0.45, 29 ranks -> 1.8225
    public void Ava_example_29_ranks() => Approx.Equal(1.8225, SkillCoefficient.Compute(0.45, 29));

    [Fact] // 1 rank -> base coefficient unchanged
    public void One_rank_is_base() => Approx.Equal(0.45, SkillCoefficient.Compute(0.45, 1));

    [Fact] // 5 ranks: 0.45*(1 + 0.1*3 + 0.15*1) = 0.6525
    public void Five_ranks() => Approx.Equal(0.6525, SkillCoefficient.Compute(0.45, 5));
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test --filter SkillCoefficientTests`
Expected: FAIL — `SkillCoefficient` not found.

- [ ] **Step 3: Write the implementation**

`Calc/SkillCoefficient.cs`:
```csharp
using System;

namespace D4BuildForge.Engine.Calc;

public static class SkillCoefficient
{
    public static double Compute(double baseCoeff, int totalRanks)
    {
        int fifths = totalRanks / 5;
        return baseCoeff * (1 + 0.10 * (totalRanks - fifths - 1) + 0.15 * fifths);
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test --filter SkillCoefficientTests`
Expected: PASS (all 3).

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "feat(engine): SkillCoefficient rank scaling (Ava 0.45/29 -> 1.8225)"
```

---

### Task 6: MainStatMultiplier

**Files:**
- Create: `src/D4BuildForge.Engine/Calc/MainStatMultiplier.cs`
- Test: `tests/D4BuildForge.Engine.Tests/Calc/MainStatMultiplierTests.cs`

**Interfaces:**
- Produces: `static double MainStatMultiplier.Compute(double mainStatSum, double divisor, double pooledMultPct)`
  implementing `1 + mainStatSum · (1 + pooledMultPct) / divisor`. `pooledMultPct` = sum of `[x]AllStat%` + `[x]MainStat%`.

- [ ] **Step 1: Write the failing test**

`tests/D4BuildForge.Engine.Tests/Calc/MainStatMultiplierTests.cs`:

```csharp
using D4BuildForge.Engine.Calc;
using Xunit;

namespace D4BuildForge.Engine.Tests.Calc;

public class MainStatMultiplierTests
{
    [Fact] // Ava: 2428 main stat, divisor 800, no pooled mult -> 4.035
    public void Ava_example() => Approx.Equal(4.035, MainStatMultiplier.Compute(2428, 800, 0));

    [Fact] // 800/800 with +10% pooled -> 1 + 800*1.1/800 = 2.1
    public void With_pooled_multiplier() => Approx.Equal(2.1, MainStatMultiplier.Compute(800, 800, 0.10));
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test --filter MainStatMultiplierTests`
Expected: FAIL — `MainStatMultiplier` not found.

- [ ] **Step 3: Write the implementation**

`Calc/MainStatMultiplier.cs`:
```csharp
namespace D4BuildForge.Engine.Calc;

public static class MainStatMultiplier
{
    public static double Compute(double mainStatSum, double divisor, double pooledMultPct)
        => 1 + mainStatSum * (1 + pooledMultPct) / divisor;
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test --filter MainStatMultiplierTests`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "feat(engine): MainStatMultiplier (Ava 2428/800 -> 4.035)"
```

---

### Task 7: OffenseCalculator (the Ava golden fixture)

**Files:**
- Create: `src/D4BuildForge.Engine/Calc/OffenseInputs.cs`
- Create: `src/D4BuildForge.Engine/Calc/OffenseResult.cs`
- Create: `src/D4BuildForge.Engine/Calc/OffenseCalculator.cs`
- Test: `tests/D4BuildForge.Engine.Tests/Calc/OffenseCalculatorTests.cs`

**Interfaces:**
- Consumes: nothing (pure arithmetic on resolved bucket values).
- Produces:
  - `record OffenseInputs(double WeaponDamage, double MainStatMult, double AdditiveNonCrit, double AdditiveCrit, double Vdm, double Csdm, double Admg, double SkillCoeff, double GlobalScalar, double BaseCritMult, double CritChance, double SealDmg, double SealCritDmg)`
  - `record OffenseResult(double NonCrit, double Crit, double Average)`
  - `static OffenseResult OffenseCalculator.Compute(OffenseInputs i)` implementing the spec §6 products.

- [ ] **Step 1: Write the failing test**

`tests/D4BuildForge.Engine.Tests/Calc/OffenseCalculatorTests.cs`:

```csharp
using D4BuildForge.Engine.Calc;
using Xunit;

namespace D4BuildForge.Engine.Tests.Calc;

public class OffenseCalculatorTests
{
    // Ava ALL CLASSES worked example -> NonCrit 512128.786, Crit 3264821.011, Avg 2543615.648
    private static OffenseInputs AvaExample() => new(
        WeaponDamage: 4607, MainStatMult: 4.035,
        AdditiveNonCrit: 16.602, AdditiveCrit: 16.602,
        Vdm: 2.06, Csdm: 4.25, Admg: 2.21,
        SkillCoeff: 1.8225, GlobalScalar: 0.2, BaseCritMult: 1.5,
        CritChance: 0.738, SealDmg: 0, SealCritDmg: 0);

    [Fact]
    public void Reproduces_ava_noncrit() => Approx.Equal(512128.786, OffenseCalculator.Compute(AvaExample()).NonCrit, 1e-4);

    [Fact]
    public void Reproduces_ava_crit() => Approx.Equal(3264821.011, OffenseCalculator.Compute(AvaExample()).Crit, 1e-4);

    [Fact]
    public void Reproduces_ava_average() => Approx.Equal(2543615.648, OffenseCalculator.Compute(AvaExample()).Average, 1e-4);
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test --filter OffenseCalculatorTests`
Expected: FAIL — `OffenseCalculator` not found.

- [ ] **Step 3: Write the types + calculator**

`Calc/OffenseInputs.cs`:
```csharp
namespace D4BuildForge.Engine.Calc;

public record OffenseInputs(
    double WeaponDamage,
    double MainStatMult,
    double AdditiveNonCrit,
    double AdditiveCrit,
    double Vdm,
    double Csdm,
    double Admg,
    double SkillCoeff,
    double GlobalScalar,
    double BaseCritMult,
    double CritChance,
    double SealDmg,
    double SealCritDmg);
```

`Calc/OffenseResult.cs`:
```csharp
namespace D4BuildForge.Engine.Calc;

public record OffenseResult(double NonCrit, double Crit, double Average);
```

`Calc/OffenseCalculator.cs`:
```csharp
namespace D4BuildForge.Engine.Calc;

public static class OffenseCalculator
{
    public static OffenseResult Compute(OffenseInputs i)
    {
        double common = i.WeaponDamage * i.MainStatMult * i.Vdm * i.Admg * i.SkillCoeff * i.GlobalScalar;
        double nonCrit = common * i.AdditiveNonCrit * (1 + i.SealDmg);
        double crit = common * i.AdditiveCrit * i.Csdm * i.BaseCritMult * (1 + i.SealCritDmg + i.SealDmg);
        double avg = crit * i.CritChance + nonCrit * (1 - i.CritChance);
        return new OffenseResult(nonCrit, crit, avg);
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test --filter OffenseCalculatorTests`
Expected: PASS (all 3 — the engine now reproduces Ava's model).

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "feat(engine): OffenseCalculator reproduces Ava model exactly (golden fixture #1)"
```

---

### Task 8: ModifierPool + BucketResolver (the bucket primitive)

**Files:**
- Create: `src/D4BuildForge.Engine/Calc/ModifierPool.cs`
- Create: `src/D4BuildForge.Engine/Calc/BucketResolver.cs`
- Test: `tests/D4BuildForge.Engine.Tests/Calc/BucketResolverTests.cs`

**Interfaces:**
- Consumes: `Modifier`, `IModifierSource`, `Tag`, `BucketKey`, `StatChannel`, `FormulaConfig`.
- Produces:
  - `class ModifierPool` built via `ModifierPool.From(IEnumerable<IModifierSource> sources, IReadOnlySet<Tag> activeState)`. A modifier is **active** when every tag in its `Conditions` is in `activeState` (empty conditions ⇒ always active). Methods:
    - `double BucketSum(BucketKey key)` — Σ `Value` of active `AdditivePercent` mods with that bucket.
    - `double FlatSum(StatChannel channel)` — Σ `Value` of active `Flat` mods of that channel.
    - `IReadOnlyList<Modifier> ActiveInBucket(BucketKey key)` — for breakdowns.
  - `static double BucketResolver.BucketValue(ModifierPool pool, FormulaConfig cfg, BucketKey key)` → `cfg.BucketBase(key) + pool.BucketSum(key)`.
  - `static double BucketResolver.HitMultiplier(ModifierPool pool, FormulaConfig cfg, IEnumerable<BucketKey> buckets)` → product of `BucketValue`.

- [ ] **Step 1: Write the failing test**

`tests/D4BuildForge.Engine.Tests/Calc/BucketResolverTests.cs`:

```csharp
using System.Collections.Generic;
using D4BuildForge.Engine.Calc;
using D4BuildForge.Engine.Core;
using Xunit;

namespace D4BuildForge.Engine.Tests.Calc;

public class BucketResolverTests
{
    private sealed class FixedSource(params Modifier[] mods) : IModifierSource
    {
        public IEnumerable<Modifier> GetModifiers() => mods;
    }

    private static readonly SourceRef Src = new("Test", "x");

    [Fact]
    public void Additive_sources_in_a_bucket_sum()
    {
        var pool = ModifierPool.From(new[] { new FixedSource(
            Modifier.Damage(BucketKey.Additive, 0.30, Src),
            Modifier.Damage(BucketKey.Additive, 0.18, Src)) }, new HashSet<Tag>());
        // base 1.0 + 0.30 + 0.18 = 1.48
        Approx.Equal(1.48, BucketResolver.BucketValue(pool, FormulaConfig.Druid, BucketKey.Additive));
    }

    [Fact]
    public void Conditional_modifier_only_counts_when_tag_active()
    {
        var werebear = new Tag("Werebear");
        var src = new FixedSource(Modifier.Damage(BucketKey.Additive, 0.50, Src, werebear));

        var inactive = ModifierPool.From(new[] { src }, new HashSet<Tag>());
        Approx.Equal(1.0, BucketResolver.BucketValue(inactive, FormulaConfig.Druid, BucketKey.Additive));

        var active = ModifierPool.From(new[] { src }, new HashSet<Tag> { werebear });
        Approx.Equal(1.5, BucketResolver.BucketValue(active, FormulaConfig.Druid, BucketKey.Additive));
    }

    [Fact]
    public void HitMultiplier_multiplies_buckets()
    {
        var pool = ModifierPool.From(new[] { new FixedSource(
            Modifier.Damage(BucketKey.Additive, 0.50, Src),       // -> 1.5
            Modifier.Damage(BucketKey.Vulnerable, 1.0, Src)) },   // -> 2.0
            new HashSet<Tag>());
        Approx.Equal(3.0, BucketResolver.HitMultiplier(pool, FormulaConfig.Druid,
            new[] { BucketKey.Additive, BucketKey.Vulnerable }));
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test --filter BucketResolverTests`
Expected: FAIL — `ModifierPool`/`BucketResolver` not found.

- [ ] **Step 3: Write the implementation**

`Calc/ModifierPool.cs`:
```csharp
using System.Collections.Generic;
using System.Linq;
using D4BuildForge.Engine.Core;

namespace D4BuildForge.Engine.Calc;

public sealed class ModifierPool
{
    private readonly IReadOnlyList<Modifier> _active;

    private ModifierPool(IReadOnlyList<Modifier> active) => _active = active;

    public static ModifierPool From(IEnumerable<IModifierSource> sources, IReadOnlySet<Tag> activeState)
    {
        var active = sources
            .SelectMany(s => s.GetModifiers())
            .Where(m => m.Conditions.All(activeState.Contains))
            .ToList();
        return new ModifierPool(active);
    }

    public double BucketSum(BucketKey key)
        => _active.Where(m => m.Op == ModOp.AdditivePercent && m.Bucket == key).Sum(m => m.Value);

    public double FlatSum(StatChannel channel)
        => _active.Where(m => m.Op == ModOp.Flat && m.Channel == channel).Sum(m => m.Value);

    public IReadOnlyList<Modifier> ActiveInBucket(BucketKey key)
        => _active.Where(m => m.Op == ModOp.AdditivePercent && m.Bucket == key).ToList();
}
```

`Calc/BucketResolver.cs`:
```csharp
using System.Collections.Generic;
using System.Linq;
using D4BuildForge.Engine.Core;

namespace D4BuildForge.Engine.Calc;

public static class BucketResolver
{
    public static double BucketValue(ModifierPool pool, FormulaConfig cfg, BucketKey key)
        => cfg.BucketBase(key) + pool.BucketSum(key);

    public static double HitMultiplier(ModifierPool pool, FormulaConfig cfg, IEnumerable<BucketKey> buckets)
        => buckets.Aggregate(1.0, (acc, b) => acc * BucketValue(pool, cfg, b));
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test --filter BucketResolverTests`
Expected: PASS (all 3).

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "feat(engine): ModifierPool + BucketResolver (bucket primitive: base+Σ within, multiply across)"
```

---

### Task 9: Build model + CalcResult + Breakdown

**Files:**
- Create: `src/D4BuildForge.Engine/Model/SkillSelection.cs`
- Create: `src/D4BuildForge.Engine/Model/Target.cs`
- Create: `src/D4BuildForge.Engine/Model/Build.cs`
- Create: `src/D4BuildForge.Engine/Model/Breakdown.cs`
- Create: `src/D4BuildForge.Engine/Model/CalcResult.cs`
- Test: `tests/D4BuildForge.Engine.Tests/Model/BuildModelTests.cs`

**Interfaces:**
- Produces:
  - `record SkillSelection(string SkillId, double BaseCoeff, int Ranks)`
  - `record Target(int Level, double Armor)`
  - `record Build(int Level, IReadOnlyList<IModifierSource> Sources, SkillSelection Skill, IReadOnlySet<Tag> ActiveState, Target Target)`
  - `record BreakdownLine(string Label, double Value, string Detail)`
  - `class Breakdown { void Add(string label, double value, string detail = ""); IReadOnlyList<BreakdownLine> Lines { get; } }`
  - `record CalcResult(double NonCrit, double Crit, double Average, double Dps, Breakdown Breakdown)`

- [ ] **Step 1: Write the failing test**

`tests/D4BuildForge.Engine.Tests/Model/BuildModelTests.cs`:

```csharp
using System.Collections.Generic;
using D4BuildForge.Engine.Core;
using D4BuildForge.Engine.Model;
using Xunit;

namespace D4BuildForge.Engine.Tests.Model;

public class BuildModelTests
{
    [Fact]
    public void Breakdown_accumulates_lines_in_order()
    {
        var b = new Breakdown();
        b.Add("WeaponDamage", 4607);
        b.Add("Additive", 16.602, "1 + Σ");
        Assert.Equal(2, b.Lines.Count);
        Assert.Equal("WeaponDamage", b.Lines[0].Label);
        Assert.Equal(16.602, b.Lines[1].Value);
    }

    [Fact]
    public void Build_holds_its_selection()
    {
        var build = new Build(80, new List<IModifierSource>(),
            new SkillSelection("Maul", 0.45, 29), new HashSet<Tag>(), new Target(81, 0));
        Assert.Equal("Maul", build.Skill.SkillId);
        Assert.Equal(29, build.Skill.Ranks);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test --filter BuildModelTests`
Expected: FAIL — model types not found.

- [ ] **Step 3: Write the model types**

`Model/SkillSelection.cs`:
```csharp
namespace D4BuildForge.Engine.Model;
public record SkillSelection(string SkillId, double BaseCoeff, int Ranks);
```

`Model/Target.cs`:
```csharp
namespace D4BuildForge.Engine.Model;
public record Target(int Level, double Armor);
```

`Model/Build.cs`:
```csharp
using System.Collections.Generic;
using D4BuildForge.Engine.Core;

namespace D4BuildForge.Engine.Model;

public record Build(
    int Level,
    IReadOnlyList<IModifierSource> Sources,
    SkillSelection Skill,
    IReadOnlySet<Tag> ActiveState,
    Target Target);
```

`Model/Breakdown.cs`:
```csharp
using System.Collections.Generic;

namespace D4BuildForge.Engine.Model;

public record BreakdownLine(string Label, double Value, string Detail);

public sealed class Breakdown
{
    private readonly List<BreakdownLine> _lines = new();
    public void Add(string label, double value, string detail = "") => _lines.Add(new BreakdownLine(label, value, detail));
    public IReadOnlyList<BreakdownLine> Lines => _lines;
}
```

`Model/CalcResult.cs`:
```csharp
namespace D4BuildForge.Engine.Model;
public record CalcResult(double NonCrit, double Crit, double Average, double Dps, Breakdown Breakdown);
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test --filter BuildModelTests`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "feat(engine): Build model + CalcResult + Breakdown"
```

---

### Task 10: Pipeline (insertable stages with ordering validation)

**Files:**
- Create: `src/D4BuildForge.Engine/Pipeline/CalcContext.cs`
- Create: `src/D4BuildForge.Engine/Pipeline/IStage.cs`
- Create: `src/D4BuildForge.Engine/Pipeline/Pipeline.cs`
- Test: `tests/D4BuildForge.Engine.Tests/Pipeline/PipelineTests.cs`

**Interfaces:**
- Consumes: `ModifierPool`, `FormulaConfig`, `Build`, `Breakdown`.
- Produces:
  - `class CalcContext` holding `Build Build`, `FormulaConfig Config`, `ModifierPool Pool`, `Breakdown Breakdown`, and a `double` value bag: `void Set(string key, double v)`, `double Get(string key)`, `bool Has(string key)`.
  - `interface IStage { IReadOnlySet<string> Reads { get; } IReadOnlySet<string> Writes { get; } void Run(CalcContext ctx); }`
  - `class Pipeline { Pipeline(params IStage[] stages); void Run(CalcContext ctx); }` — runs stages in given order; before each stage, throws `InvalidOperationException` if any of its `Reads` keys are not yet present (written by an earlier stage). This makes "insert a stage anywhere" safe.

- [ ] **Step 1: Write the failing test**

`tests/D4BuildForge.Engine.Tests/Pipeline/PipelineTests.cs`:

```csharp
using System;
using System.Collections.Generic;
using D4BuildForge.Engine.Core;
using D4BuildForge.Engine.Model;
using D4BuildForge.Engine.Pipeline;
using D4BuildForge.Engine.Calc;
using Xunit;

namespace D4BuildForge.Engine.Tests.Pipeline;

public class PipelineTests
{
    private sealed class WriteStage(string key, double val) : IStage
    {
        public IReadOnlySet<string> Reads { get; } = new HashSet<string>();
        public IReadOnlySet<string> Writes { get; } = new HashSet<string> { key };
        public void Run(CalcContext ctx) => ctx.Set(key, val);
    }

    private sealed class ReadStage(string needs, string produces) : IStage
    {
        public IReadOnlySet<string> Reads { get; } = new HashSet<string> { needs };
        public IReadOnlySet<string> Writes { get; } = new HashSet<string> { produces };
        public void Run(CalcContext ctx) => ctx.Set(produces, ctx.Get(needs) * 2);
    }

    private static CalcContext NewContext()
    {
        var build = new Build(80, new List<IModifierSource>(),
            new SkillSelection("Maul", 0.45, 29), new HashSet<Tag>(), new Target(81, 0));
        var pool = ModifierPool.From(build.Sources, build.ActiveState);
        return new CalcContext(build, FormulaConfig.Druid, pool, new Breakdown());
    }

    [Fact]
    public void Stages_run_in_order_and_share_values()
    {
        var ctx = NewContext();
        new Pipeline(new WriteStage("a", 10), new ReadStage("a", "b")).Run(ctx);
        Approx.Equal(20, ctx.Get("b"));
    }

    [Fact]
    public void Reading_an_unwritten_key_throws()
    {
        var ctx = NewContext();
        Assert.Throws<InvalidOperationException>(
            () => new Pipeline(new ReadStage("missing", "b")).Run(ctx));
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test --filter PipelineTests`
Expected: FAIL — `CalcContext`/`IStage`/`Pipeline` not found.

- [ ] **Step 3: Write the implementation**

`Pipeline/CalcContext.cs`:
```csharp
using System.Collections.Generic;
using D4BuildForge.Engine.Calc;
using D4BuildForge.Engine.Core;
using D4BuildForge.Engine.Model;

namespace D4BuildForge.Engine.Pipeline;

public sealed class CalcContext(Build build, FormulaConfig config, ModifierPool pool, Breakdown breakdown)
{
    private readonly Dictionary<string, double> _values = new();
    public Build Build { get; } = build;
    public FormulaConfig Config { get; } = config;
    public ModifierPool Pool { get; } = pool;
    public Breakdown Breakdown { get; } = breakdown;

    public void Set(string key, double v) => _values[key] = v;
    public bool Has(string key) => _values.ContainsKey(key);
    public double Get(string key) => _values[key];
}
```

`Pipeline/IStage.cs`:
```csharp
using System.Collections.Generic;

namespace D4BuildForge.Engine.Pipeline;

public interface IStage
{
    IReadOnlySet<string> Reads { get; }
    IReadOnlySet<string> Writes { get; }
    void Run(CalcContext ctx);
}
```

`Pipeline/Pipeline.cs`:
```csharp
using System;
using System.Collections.Generic;

namespace D4BuildForge.Engine.Pipeline;

public sealed class Pipeline
{
    private readonly IReadOnlyList<IStage> _stages;
    public Pipeline(params IStage[] stages) => _stages = stages;

    public void Run(CalcContext ctx)
    {
        foreach (var stage in _stages)
        {
            foreach (var need in stage.Reads)
                if (!ctx.Has(need))
                    throw new InvalidOperationException(
                        $"Stage {stage.GetType().Name} reads '{need}' before any stage wrote it.");
            stage.Run(ctx);
        }
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test --filter PipelineTests`
Expected: PASS (both).

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "feat(engine): insertable Pipeline with reads/writes ordering validation"
```

---

### Task 11: Concrete stages + BuildCalculator (end-to-end)

**Files:**
- Create: `src/D4BuildForge.Engine/Pipeline/Keys.cs`
- Create: `src/D4BuildForge.Engine/Pipeline/Stages/BaseStatsStage.cs`
- Create: `src/D4BuildForge.Engine/Pipeline/Stages/OffenseBucketsStage.cs`
- Create: `src/D4BuildForge.Engine/Pipeline/Stages/DpsStage.cs`
- Create: `src/D4BuildForge.Engine/BuildCalculator.cs`
- Test: `tests/D4BuildForge.Engine.Tests/EndToEndTests.cs`

**Interfaces:**
- Consumes: Tasks 4–10.
- Produces:
  - `static class Keys` — string constants: `MainStatSum`, `CritChance`, `AttackSpeed`, `NonCrit`, `Crit`, `Average`, `Dps`.
  - `BaseStatsStage` — Writes `{MainStatSum, CritChance, AttackSpeed}` from `pool.FlatSum(...)`; CritChance = `pool.FlatSum(CritChance)`, AttackSpeed = `1 + pool.FlatSum(AttackSpeed)`.
  - `OffenseBucketsStage` — Reads `{MainStatSum, CritChance}`; Writes `{NonCrit, Crit, Average}`; resolves bucket values, computes `MainStatMultiplier` (divisor from config, pooled% from `BucketKey("MainStatMult")`), `SkillCoefficient` (from `build.Skill` + `pool.FlatSum(SkillRank)`), calls `OffenseCalculator`, and records breakdown lines.
  - `DpsStage` — Reads `{Average, AttackSpeed}`; Writes `{Dps}`; `Dps = Average * AttackSpeed`.
  - `static CalcResult BuildCalculator.Calculate(Build build, FormulaConfig cfg)`.

Bucket keys used by the offense stage (string-based, open): `Additive`, `Vulnerable`, `CritDamage`, `AllDamage`, plus `"MainStatMult"` for pooled main-stat `[x]%`, and seals `"SealDmg"` / `"SealCritDmg"` (each a single-member bucket summed with base 0 — see Step 3 note).

- [ ] **Step 1: Write the failing end-to-end test**

`tests/D4BuildForge.Engine.Tests/EndToEndTests.cs`:

```csharp
using System.Collections.Generic;
using D4BuildForge.Engine;
using D4BuildForge.Engine.Core;
using D4BuildForge.Engine.Model;
using Xunit;

namespace D4BuildForge.Engine.Tests;

public class EndToEndTests
{
    private sealed class FixedSource(params Modifier[] mods) : IModifierSource
    {
        public IEnumerable<Modifier> GetModifiers() => mods;
    }

    private static readonly SourceRef Gear = new("Item", "TestGear");

    // A small synthetic Druid build with round numbers, computed by hand:
    //   weapon flat 100, mainStat 800 (divisor 800 -> mainStatMult 2.0),
    //   additive +50% (-> 1.5), no vuln/csdm/admg extras (-> 1.0), skill 0.45 @ 1 rank (-> 0.45),
    //   global scalar 0.2, crit chance 0.
    //   NonCrit = 100 * 2.0 * 1.5 * 1.0 * 1.0 * 0.45 * 0.2 = 27.0
    private static Build SyntheticBuild() => new(
        Level: 80,
        Sources: new List<IModifierSource>
        {
            new FixedSource(
                Modifier.Flat(StatChannel.WeaponDamage, 100, Gear),
                Modifier.Flat(StatChannel.MainStat, 800, Gear),
                Modifier.Damage(BucketKey.Additive, 0.50, Gear))
        },
        Skill: new SkillSelection("Maul", 0.45, 1),
        ActiveState: new HashSet<Tag>(),
        Target: new Target(81, 0));

    [Fact]
    public void Computes_noncrit_end_to_end()
        => Approx.Equal(27.0, BuildCalculator.Calculate(SyntheticBuild(), FormulaConfig.Druid).NonCrit);

    [Fact]
    public void Breakdown_is_populated()
        => Assert.NotEmpty(BuildCalculator.Calculate(SyntheticBuild(), FormulaConfig.Druid).Breakdown.Lines);
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test --filter EndToEndTests`
Expected: FAIL — `BuildCalculator` not found.

- [ ] **Step 3: Write keys, stages, and the facade**

`Pipeline/Keys.cs`:
```csharp
namespace D4BuildForge.Engine.Pipeline;

public static class Keys
{
    public const string MainStatSum = "MainStatSum";
    public const string CritChance = "CritChance";
    public const string AttackSpeed = "AttackSpeed";
    public const string NonCrit = "NonCrit";
    public const string Crit = "Crit";
    public const string Average = "Average";
    public const string Dps = "Dps";
}
```

`Pipeline/Stages/BaseStatsStage.cs`:
```csharp
using System.Collections.Generic;
using D4BuildForge.Engine.Core;

namespace D4BuildForge.Engine.Pipeline.Stages;

public sealed class BaseStatsStage : IStage
{
    public IReadOnlySet<string> Reads { get; } = new HashSet<string>();
    public IReadOnlySet<string> Writes { get; } = new HashSet<string> { Keys.MainStatSum, Keys.CritChance, Keys.AttackSpeed };

    public void Run(CalcContext ctx)
    {
        ctx.Set(Keys.MainStatSum, ctx.Pool.FlatSum(StatChannel.MainStat));
        ctx.Set(Keys.CritChance, ctx.Pool.FlatSum(StatChannel.CritChance));
        ctx.Set(Keys.AttackSpeed, 1 + ctx.Pool.FlatSum(StatChannel.AttackSpeed));
        ctx.Breakdown.Add(Keys.MainStatSum, ctx.Get(Keys.MainStatSum));
        ctx.Breakdown.Add(Keys.CritChance, ctx.Get(Keys.CritChance));
    }
}
```

`Pipeline/Stages/OffenseBucketsStage.cs` (note: seals modeled as buckets with **base 0** via a 0 override is
unnecessary — we read their sum directly as `pool.BucketSum`, which is 0 when absent):
```csharp
using System.Collections.Generic;
using D4BuildForge.Engine.Calc;
using D4BuildForge.Engine.Core;

namespace D4BuildForge.Engine.Pipeline.Stages;

public sealed class OffenseBucketsStage : IStage
{
    public IReadOnlySet<string> Reads { get; } = new HashSet<string> { Keys.MainStatSum, Keys.CritChance };
    public IReadOnlySet<string> Writes { get; } = new HashSet<string> { Keys.NonCrit, Keys.Crit, Keys.Average };

    public void Run(CalcContext ctx)
    {
        var pool = ctx.Pool;
        var cfg = ctx.Config;

        double weapon = pool.FlatSum(StatChannel.WeaponDamage);
        double pooledMainStat = pool.BucketSum(new BucketKey("MainStatMult"));
        double mainStatMult = MainStatMultiplier.Compute(ctx.Get(Keys.MainStatSum), cfg.MainStatDivisor, pooledMainStat);

        double additive = BucketResolver.BucketValue(pool, cfg, BucketKey.Additive);
        double vdm = BucketResolver.BucketValue(pool, cfg, BucketKey.Vulnerable);
        double csdm = BucketResolver.BucketValue(pool, cfg, BucketKey.CritDamage);
        double admg = BucketResolver.BucketValue(pool, cfg, BucketKey.AllDamage);

        int totalRanks = ctx.Build.Skill.Ranks + (int)pool.FlatSum(StatChannel.SkillRank);
        double skillCoeff = SkillCoefficient.Compute(ctx.Build.Skill.BaseCoeff, totalRanks);

        var inputs = new OffenseInputs(
            WeaponDamage: weapon, MainStatMult: mainStatMult,
            AdditiveNonCrit: additive, AdditiveCrit: additive,
            Vdm: vdm, Csdm: csdm, Admg: admg,
            SkillCoeff: skillCoeff, GlobalScalar: cfg.GlobalSkillScalar, BaseCritMult: cfg.BaseCritMultiplier,
            CritChance: ctx.Get(Keys.CritChance),
            SealDmg: pool.BucketSum(new BucketKey("SealDmg")),
            SealCritDmg: pool.BucketSum(new BucketKey("SealCritDmg")));

        var r = OffenseCalculator.Compute(inputs);
        ctx.Set(Keys.NonCrit, r.NonCrit);
        ctx.Set(Keys.Crit, r.Crit);
        ctx.Set(Keys.Average, r.Average);

        ctx.Breakdown.Add("WeaponDamage", weapon);
        ctx.Breakdown.Add("MainStatMult", mainStatMult);
        ctx.Breakdown.Add("Additive", additive, "1 + Σ additive %");
        ctx.Breakdown.Add("VDM", vdm);
        ctx.Breakdown.Add("ADMG", admg);
        ctx.Breakdown.Add("SkillCoeff", skillCoeff);
        ctx.Breakdown.Add(Keys.NonCrit, r.NonCrit);
        ctx.Breakdown.Add(Keys.Crit, r.Crit);
        ctx.Breakdown.Add(Keys.Average, r.Average);
    }
}
```

`Pipeline/Stages/DpsStage.cs`:
```csharp
using System.Collections.Generic;

namespace D4BuildForge.Engine.Pipeline.Stages;

public sealed class DpsStage : IStage
{
    public IReadOnlySet<string> Reads { get; } = new HashSet<string> { Keys.Average, Keys.AttackSpeed };
    public IReadOnlySet<string> Writes { get; } = new HashSet<string> { Keys.Dps };

    public void Run(CalcContext ctx)
    {
        double dps = ctx.Get(Keys.Average) * ctx.Get(Keys.AttackSpeed);
        ctx.Set(Keys.Dps, dps);
        ctx.Breakdown.Add(Keys.Dps, dps);
    }
}
```

`BuildCalculator.cs`:
```csharp
using D4BuildForge.Engine.Calc;
using D4BuildForge.Engine.Core;
using D4BuildForge.Engine.Model;
using D4BuildForge.Engine.Pipeline;
using D4BuildForge.Engine.Pipeline.Stages;

namespace D4BuildForge.Engine;

public static class BuildCalculator
{
    public static CalcResult Calculate(Build build, FormulaConfig cfg)
    {
        var pool = ModifierPool.From(build.Sources, build.ActiveState);
        var ctx = new CalcContext(build, cfg, pool, new Breakdown());
        new CalcPipeline(new BaseStatsStage(), new OffenseBucketsStage(), new DpsStage()).Run(ctx);
        return new CalcResult(
            NonCrit: ctx.Get(Keys.NonCrit),
            Crit: ctx.Get(Keys.Crit),
            Average: ctx.Get(Keys.Average),
            Dps: ctx.Get(Keys.Dps),
            Breakdown: ctx.Breakdown);
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test --filter EndToEndTests`
Expected: PASS (both).

- [ ] **Step 5: Run the full suite**

Run: `dotnet test`
Expected: all tests across all tasks PASS.

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "feat(engine): concrete stages (BaseStats/OffenseBuckets/Dps) + BuildCalculator end-to-end"
```

---

### Task 12: Druid Maul LEGO assembly demo (modularity proof)

**Files:**
- Create: `src/D4BuildForge.Engine/Sources/BaseStatsForLevel.cs`
- Create: `src/D4BuildForge.Engine/Sources/SimpleItem.cs`
- Create: `src/D4BuildForge.Engine/Sources/ShapeshiftForm.cs`
- Test: `tests/D4BuildForge.Engine.Tests/DruidMaulAssemblyTests.cs`

**Interfaces:**
- Consumes: `IModifierSource`, `Modifier`, `Tag`, `BucketKey`, `StatChannel`, `SourceRef`, `BuildCalculator`.
- Produces three reusable `IModifierSource` bricks (each independent and agnostic — only the test, acting as
  the orchestrator, assembles them):
  - `class BaseStatsForLevel(int level, double mainStat, double weaponDamage) : IModifierSource`
  - `class SimpleItem(string name, IReadOnlyList<Modifier> affixes) : IModifierSource`
  - `class ShapeshiftForm(Tag form, IReadOnlyList<Modifier> formBonuses) : IModifierSource` — its modifiers are
    all conditioned on `form`, so they only apply when that form tag is in the build's `ActiveState`.

- [ ] **Step 1: Write the failing test**

`tests/D4BuildForge.Engine.Tests/DruidMaulAssemblyTests.cs`:

```csharp
using System.Collections.Generic;
using D4BuildForge.Engine;
using D4BuildForge.Engine.Core;
using D4BuildForge.Engine.Model;
using D4BuildForge.Engine.Sources;
using Xunit;

namespace D4BuildForge.Engine.Tests;

public class DruidMaulAssemblyTests
{
    private static readonly Tag Werebear = new("Werebear");

    // LEGO bricks assembled by the test (the orchestrator's job): base stats + an item + Werebear form.
    private static Build AssembleMaulBuild(bool inWerebear)
    {
        var item = new SimpleItem("Two-Hander", new List<Modifier>
        {
            Modifier.Damage(BucketKey.Additive, 0.30, new SourceRef("Item", "Two-Hander")),
        });
        var form = new ShapeshiftForm(Werebear, new List<Modifier>
        {
            // Werebear-only +50% additive damage (representative number).
            Modifier.Damage(BucketKey.Additive, 0.50, new SourceRef("Form", "Werebear"), Werebear),
        });

        return new Build(
            Level: 80,
            Sources: new List<IModifierSource> { new BaseStatsForLevel(80, 800, 100), item, form },
            Skill: new SkillSelection("Maul", 0.45, 1),
            ActiveState: inWerebear ? new HashSet<Tag> { Werebear } : new HashSet<Tag>(),
            Target: new Target(81, 0));
    }

    [Fact]
    public void Werebear_form_modifier_applies_only_in_werebear()
    {
        double human = BuildCalculator.Calculate(AssembleMaulBuild(false), FormulaConfig.Druid).NonCrit;
        double bear = BuildCalculator.Calculate(AssembleMaulBuild(true), FormulaConfig.Druid).NonCrit;

        // Human: weapon 100 * mainStatMult 2.0 * additive(1+0.30=1.30) * 0.45 * 0.2 = 23.4
        Approx.Equal(23.4, human);
        // Werebear adds +0.50 to the additive bucket -> 1.80: 100*2.0*1.80*0.45*0.2 = 32.4
        Approx.Equal(32.4, bear);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test --filter DruidMaulAssemblyTests`
Expected: FAIL — source bricks not found.

- [ ] **Step 3: Write the source bricks**

`Sources/BaseStatsForLevel.cs`:
```csharp
using System.Collections.Generic;
using D4BuildForge.Engine.Core;

namespace D4BuildForge.Engine.Sources;

public sealed class BaseStatsForLevel(int level, double mainStat, double weaponDamage) : IModifierSource
{
    public IEnumerable<Modifier> GetModifiers()
    {
        var src = new SourceRef("BaseStats", $"L{level}");
        yield return Modifier.Flat(StatChannel.MainStat, mainStat, src);
        yield return Modifier.Flat(StatChannel.WeaponDamage, weaponDamage, src);
    }
}
```

`Sources/SimpleItem.cs`:
```csharp
using System.Collections.Generic;
using D4BuildForge.Engine.Core;

namespace D4BuildForge.Engine.Sources;

public sealed class SimpleItem(string name, IReadOnlyList<Modifier> affixes) : IModifierSource
{
    public IEnumerable<Modifier> GetModifiers() => affixes;
}
```

`Sources/ShapeshiftForm.cs`:
```csharp
using System.Collections.Generic;
using D4BuildForge.Engine.Core;

namespace D4BuildForge.Engine.Sources;

public sealed class ShapeshiftForm(Tag form, IReadOnlyList<Modifier> formBonuses) : IModifierSource
{
    public IEnumerable<Modifier> GetModifiers() => formBonuses;
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test --filter DruidMaulAssemblyTests`
Expected: PASS — Werebear-gated modifier applies only when the form tag is active.

- [ ] **Step 5: Run the full suite**

Run: `dotnet test`
Expected: every test PASSES.

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "feat(engine): Druid Maul LEGO assembly demo — modular sources + form-gated modifiers"
```

---

## Done criteria for this plan

- `dotnet test` is green: Ava's model reproduced exactly (Tasks 5–7), the bucket primitive + pipeline work
  (Tasks 8–11), and class-specific LEGO bricks compose with condition-gating (Task 12).
- The `Engine` has no AWS/web/storage dependency; everything runs locally.

## Next plans (not this one)
1. **Live-game Maul validation** — encode the user's real in-game Maul tooltip numbers as a golden fixture and
   reconcile (Milestone 1).
2. **Mitigation stage** — splice a `Mitigation` stage after offense to match the on-dummy hit (Milestone 2).
3. **Storage + BuildAssembler** — per-concept DynamoDB tables (Docker Local) + BW.Libs.Config vessels; the
   DB-backed orchestrator that assembles `Build`s from stored content (spec §5).
4. **Web UI** (spec §8, Phase 1).
