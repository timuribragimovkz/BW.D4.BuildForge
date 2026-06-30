# Domain Contract (Slice 1) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Freeze the D4 item-record + build-record JSON shapes as typed `src/D4BuildForge.Domain` records and define the `IRecordRepository` contract, so the Builder (Slice 2a) and Build-stealer (Slice 2b) both build against an immutable contract with no live coupling.

**Architecture:** A new dependency-free `D4BuildForge.Domain` class library holds: (1) typed records mirroring the spec §3 JSON shapes (camelCase via System.Text.Json), (2) the `IRecordRepository` interface over raw `JsonObject` (lossless for the render engine). A test-only `InMemoryRecordRepository` proves the contract. No Storage/Api/Flutter here.

**Tech Stack:** .NET 10 (`net10.0`), C#, System.Text.Json (framework, no package refs), xUnit.

## Global Constraints

- **Target framework:** `net10.0`; `ImplicitUsings` enable; `Nullable` enable (mirror `src/D4BuildForge.Engine/D4BuildForge.Engine.csproj`).
- **`D4BuildForge.Domain` has ZERO package/project dependencies** — BCL only (`System.Text.Json`). No AWS/web/Engine refs.
- **JSON is camelCase.** All record (de)serialization goes through the shared `DomainJson.Options`. The build record's `class` field maps via `[JsonPropertyName("class")]` (C# keyword).
- **Test project:** mirror `tests/D4BuildForge.Engine.Tests/D4BuildForge.Engine.Tests.csproj` — xUnit 2.9.3, `Microsoft.NET.Test.Sdk` 17.14.1, `<Using Include="Xunit" />` (so test files do NOT write `using Xunit;`).
- **Repo:** `~/gameSources/d4_build_forge`. Spec: `docs/superpowers/specs/2026-06-30-d4-forge-admin-vessels-design.md` §3. Work in a worktree `feat/domain-contract` off `master`.
- **Frozen shapes are canonical.** The records + their round-trip tests ARE the contract; the importer (Slice 2b) emits JSON that deserializes cleanly into them.

**Out of scope (Slice 2a/2b):** `DynamoRecordRepository` (real AWS DDB), `src/Assembly`, `src/Api`, Flutter, importers.

**Refinement vs spec:** spec §3.3 sketched `IRecordRepository` "in `src/Storage`". It moves to `src/Domain` (it is the shared *contract*; the impl `DynamoRecordRepository` stays in `src/Storage`, Slice 2a). This keeps Domain the single contract package.

---

### Task 1: Scaffold Domain + Domain.Tests projects + JSON options

**Files:**
- Create: `src/D4BuildForge.Domain/D4BuildForge.Domain.csproj`
- Create: `src/D4BuildForge.Domain/DomainJson.cs`
- Create: `tests/D4BuildForge.Domain.Tests/D4BuildForge.Domain.Tests.csproj`
- Create: `tests/D4BuildForge.Domain.Tests/DomainJsonTests.cs`
- Modify: `D4BuildForge.sln` (add both projects)

**Interfaces:**
- Produces: `D4BuildForge.Domain.DomainJson.Options : JsonSerializerOptions` (camelCase, ignore-null-on-write, case-insensitive read).

- [ ] **Step 1: Create the projects and wire the solution**

```bash
cd ~/gameSources/d4_build_forge
dotnet new classlib -n D4BuildForge.Domain -o src/D4BuildForge.Domain -f net10.0
rm src/D4BuildForge.Domain/Class1.cs
dotnet new xunit -n D4BuildForge.Domain.Tests -o tests/D4BuildForge.Domain.Tests -f net10.0
rm tests/D4BuildForge.Domain.Tests/UnitTest1.cs
dotnet sln D4BuildForge.sln add src/D4BuildForge.Domain/D4BuildForge.Domain.csproj
dotnet sln D4BuildForge.sln add tests/D4BuildForge.Domain.Tests/D4BuildForge.Domain.Tests.csproj
dotnet add tests/D4BuildForge.Domain.Tests/D4BuildForge.Domain.Tests.csproj reference src/D4BuildForge.Domain/D4BuildForge.Domain.csproj
```

Overwrite `src/D4BuildForge.Domain/D4BuildForge.Domain.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

</Project>
```

Overwrite `tests/D4BuildForge.Domain.Tests/D4BuildForge.Domain.Tests.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="coverlet.collector" Version="6.0.4" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.14.1" />
    <PackageReference Include="xunit" Version="2.9.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.1.4" />
  </ItemGroup>

  <ItemGroup>
    <Using Include="Xunit" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\D4BuildForge.Domain\D4BuildForge.Domain.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Step 2: Write the failing test**

`tests/D4BuildForge.Domain.Tests/DomainJsonTests.cs`:
```csharp
using D4BuildForge.Domain;

namespace D4BuildForge.Domain.Tests;

public class DomainJsonTests
{
    [Fact]
    public void Options_are_camelCase_and_ignore_null_on_write()
    {
        Assert.Equal(System.Text.Json.JsonNamingPolicy.CamelCase, DomainJson.Options.PropertyNamingPolicy);
        Assert.True(DomainJson.Options.PropertyNameCaseInsensitive);
    }
}
```

- [ ] **Step 3: Run to verify it fails**

Run: `dotnet test tests/D4BuildForge.Domain.Tests --filter DomainJsonTests`
Expected: FAIL — `DomainJson` does not exist.

- [ ] **Step 4: Implement**

`src/D4BuildForge.Domain/DomainJson.cs`:
```csharp
using System.Text.Json;
using System.Text.Json.Serialization;

namespace D4BuildForge.Domain;

/// <summary>Canonical (de)serialization options for every Domain record. camelCase JSON.</summary>
public static class DomainJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true,
    };
}
```

- [ ] **Step 5: Run to verify it passes**

Run: `dotnet test tests/D4BuildForge.Domain.Tests --filter DomainJsonTests`
Expected: PASS (1).

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "feat(domain): scaffold D4BuildForge.Domain + Domain.Tests + DomainJson options"
```

---

### Task 2: Item record shape (`ItemRecord`, `AffixEntry`, `AspectRef`)

**Files:**
- Create: `src/D4BuildForge.Domain/AffixEntry.cs`
- Create: `src/D4BuildForge.Domain/AspectRef.cs`
- Create: `src/D4BuildForge.Domain/ItemRecord.cs`
- Test: `tests/D4BuildForge.Domain.Tests/ItemRecordTests.cs`

**Interfaces:**
- Consumes: `DomainJson.Options` (Task 1).
- Produces:
  - `record AffixEntry { string Kind; string Stat; double Value; bool IsGreater; }`
  - `record AspectRef { string Name; IReadOnlyList<AffixEntry> Modifiers; }`
  - `record ItemRecord { string Id; string? SourceId; string Name; string Slot; string? ItemType; string Rarity; int ItemPower; string? ClassRestriction; IReadOnlyList<AffixEntry> Affixes; AspectRef? Aspect; IReadOnlyList<string?> Sockets; }`

- [ ] **Step 1: Write the failing test**

`tests/D4BuildForge.Domain.Tests/ItemRecordTests.cs`:
```csharp
using System.Text.Json;
using D4BuildForge.Domain;

namespace D4BuildForge.Domain.Tests;

public class ItemRecordTests
{
    // Canonical §3.1 example — this JSON IS the frozen item contract.
    private const string ItemJson = """
    {
      "id": "item_abc",
      "sourceId": "Pants_Legendary_Generic_053",
      "name": "Tibault's Will",
      "slot": "Pants",
      "itemType": "Pants",
      "rarity": "Unique",
      "itemPower": 800,
      "classRestriction": null,
      "affixes": [
        { "kind": "explicit", "stat": "MainStat",   "value": 120, "isGreater": false },
        { "kind": "implicit", "stat": "Vulnerable", "value": 0.15 },
        { "kind": "tempered", "stat": "MaulRanks",  "value": 2 }
      ],
      "aspect": null,
      "sockets": [ "Gem_Sapphire_04", null ]
    }
    """;

    [Fact]
    public void Item_deserializes_with_all_fields()
    {
        var item = JsonSerializer.Deserialize<ItemRecord>(ItemJson, DomainJson.Options)!;
        Assert.Equal("item_abc", item.Id);
        Assert.Equal("Pants_Legendary_Generic_053", item.SourceId);
        Assert.Equal("Tibault's Will", item.Name);
        Assert.Equal("Pants", item.Slot);
        Assert.Equal("Unique", item.Rarity);
        Assert.Equal(800, item.ItemPower);
        Assert.Null(item.ClassRestriction);
        Assert.Equal(3, item.Affixes.Count);
        Assert.Equal("explicit", item.Affixes[0].Kind);
        Assert.Equal("MainStat", item.Affixes[0].Stat);
        Assert.Equal(120, item.Affixes[0].Value);
        Assert.Equal("implicit", item.Affixes[1].Kind);
        Assert.False(item.Affixes[1].IsGreater); // absent in JSON -> default false
        Assert.Null(item.Aspect);
        Assert.Equal(2, item.Sockets.Count);
        Assert.Equal("Gem_Sapphire_04", item.Sockets[0]);
        Assert.Null(item.Sockets[1]); // empty socket
    }

    [Fact]
    public void Item_roundtrips_to_camelCase_json()
    {
        var item = JsonSerializer.Deserialize<ItemRecord>(ItemJson, DomainJson.Options)!;
        var json = JsonSerializer.Serialize(item, DomainJson.Options);
        Assert.Contains("\"itemPower\":800", json);
        Assert.Contains("\"kind\":\"explicit\"", json);
        Assert.DoesNotContain("\"ItemPower\"", json); // not PascalCase
    }
}
```

- [ ] **Step 2: Run to verify it fails**

Run: `dotnet test tests/D4BuildForge.Domain.Tests --filter ItemRecordTests`
Expected: FAIL — `ItemRecord`/`AffixEntry`/`AspectRef` not found.

- [ ] **Step 3: Implement**

`src/D4BuildForge.Domain/AffixEntry.cs`:
```csharp
namespace D4BuildForge.Domain;

/// <summary>One rolled stat on an item. Kind = explicit | implicit | tempered.</summary>
public sealed record AffixEntry
{
    public required string Kind { get; init; }
    public required string Stat { get; init; }
    public double Value { get; init; }
    public bool IsGreater { get; init; }
}
```

`src/D4BuildForge.Domain/AspectRef.cs`:
```csharp
namespace D4BuildForge.Domain;

/// <summary>A legendary aspect / unique power: a named bundle of modifiers.</summary>
public sealed record AspectRef
{
    public required string Name { get; init; }
    public IReadOnlyList<AffixEntry> Modifiers { get; init; } = [];
}
```

`src/D4BuildForge.Domain/ItemRecord.cs`:
```csharp
namespace D4BuildForge.Domain;

/// <summary>A single D4 item — general enough for every slot/rarity. Frozen contract (spec §3.1).</summary>
public sealed record ItemRecord
{
    public string Id { get; init; } = "";
    public string? SourceId { get; init; }
    public required string Name { get; init; }
    public required string Slot { get; init; }
    public string? ItemType { get; init; }
    public required string Rarity { get; init; }
    public int ItemPower { get; init; }
    public string? ClassRestriction { get; init; }
    public IReadOnlyList<AffixEntry> Affixes { get; init; } = [];
    public AspectRef? Aspect { get; init; }
    public IReadOnlyList<string?> Sockets { get; init; } = [];
}
```

- [ ] **Step 4: Run to verify it passes**

Run: `dotnet test tests/D4BuildForge.Domain.Tests --filter ItemRecordTests`
Expected: PASS (2).

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "feat(domain): ItemRecord + AffixEntry + AspectRef (frozen item contract, spec 3.1)"
```

---

### Task 3: Build record shape (`BuildRecord`, `SkillRef`, `TargetRef`)

**Files:**
- Create: `src/D4BuildForge.Domain/SkillRef.cs`
- Create: `src/D4BuildForge.Domain/TargetRef.cs`
- Create: `src/D4BuildForge.Domain/BuildRecord.cs`
- Test: `tests/D4BuildForge.Domain.Tests/BuildRecordTests.cs`

**Interfaces:**
- Consumes: `DomainJson.Options` (Task 1).
- Produces:
  - `record SkillRef { string Name; double BaseCoeff; int Ranks; }`
  - `record TargetRef { int Level; double Armor; }`
  - `record BuildRecord { string Id; string Name; string Season; string Class; int Level; SkillRef Skill; IReadOnlyList<string> ItemIds; IReadOnlyList<string> ActiveState; TargetRef Target; double? ExpectedNonCrit; }` — `Class` maps to JSON `class`.

- [ ] **Step 1: Write the failing test**

`tests/D4BuildForge.Domain.Tests/BuildRecordTests.cs`:
```csharp
using System.Text.Json;
using D4BuildForge.Domain;

namespace D4BuildForge.Domain.Tests;

public class BuildRecordTests
{
    // Canonical §3.2 example — frozen build contract.
    private const string BuildJson = """
    {
      "id": "build_xyz",
      "name": "Druid Maul (live test #1)",
      "season": "s13",
      "class": "Druid",
      "level": 80,
      "skill": { "name": "Maul", "baseCoeff": 0.45, "ranks": 1 },
      "itemIds": [ "item_a", "item_b" ],
      "activeState": [ "Werebear" ],
      "target": { "level": 81, "armor": 0 },
      "expectedNonCrit": 510000
    }
    """;

    [Fact]
    public void Build_deserializes_including_class_keyword_and_nullable_expected()
    {
        var b = JsonSerializer.Deserialize<BuildRecord>(BuildJson, DomainJson.Options)!;
        Assert.Equal("build_xyz", b.Id);
        Assert.Equal("s13", b.Season);
        Assert.Equal("Druid", b.Class);           // from "class" key
        Assert.Equal(80, b.Level);
        Assert.Equal("Maul", b.Skill.Name);
        Assert.Equal(0.45, b.Skill.BaseCoeff);
        Assert.Equal(1, b.Skill.Ranks);
        Assert.Equal(2, b.ItemIds.Count);
        Assert.Single(b.ActiveState);
        Assert.Equal("Werebear", b.ActiveState[0]);
        Assert.Equal(81, b.Target.Level);
        Assert.Equal(510000, b.ExpectedNonCrit);
    }

    [Fact]
    public void Build_without_expected_leaves_it_null()
    {
        var json = BuildJson.Replace("\"expectedNonCrit\": 510000", "\"expectedNonCrit\": null");
        var b = JsonSerializer.Deserialize<BuildRecord>(json, DomainJson.Options)!;
        Assert.Null(b.ExpectedNonCrit);
    }

    [Fact]
    public void Build_roundtrips_with_lowercase_class_key()
    {
        var b = JsonSerializer.Deserialize<BuildRecord>(BuildJson, DomainJson.Options)!;
        var json = JsonSerializer.Serialize(b, DomainJson.Options);
        Assert.Contains("\"class\":\"Druid\"", json);
        Assert.DoesNotContain("\"Class\"", json);
    }
}
```

- [ ] **Step 2: Run to verify it fails**

Run: `dotnet test tests/D4BuildForge.Domain.Tests --filter BuildRecordTests`
Expected: FAIL — `BuildRecord`/`SkillRef`/`TargetRef` not found.

- [ ] **Step 3: Implement**

`src/D4BuildForge.Domain/SkillRef.cs`:
```csharp
namespace D4BuildForge.Domain;

/// <summary>The selected skill + its base coefficient and rank count.</summary>
public sealed record SkillRef
{
    public required string Name { get; init; }
    public double BaseCoeff { get; init; }
    public int Ranks { get; init; }
}
```

`src/D4BuildForge.Domain/TargetRef.cs`:
```csharp
namespace D4BuildForge.Domain;

/// <summary>The dummy/enemy the build is measured against.</summary>
public sealed record TargetRef
{
    public int Level { get; init; }
    public double Armor { get; init; }
}
```

`src/D4BuildForge.Domain/BuildRecord.cs`:
```csharp
using System.Text.Json.Serialization;

namespace D4BuildForge.Domain;

/// <summary>A full build to compute + validate against the live game. Frozen contract (spec §3.2).</summary>
public sealed record BuildRecord
{
    public string Id { get; init; } = "";
    public required string Name { get; init; }
    public required string Season { get; init; }

    [JsonPropertyName("class")]
    public required string Class { get; init; }

    public int Level { get; init; }
    public required SkillRef Skill { get; init; }
    public IReadOnlyList<string> ItemIds { get; init; } = [];
    public IReadOnlyList<string> ActiveState { get; init; } = [];
    public required TargetRef Target { get; init; }

    /// <summary>The live-game tooltip number; null until measured.</summary>
    public double? ExpectedNonCrit { get; init; }
}
```

- [ ] **Step 4: Run to verify it passes**

Run: `dotnet test tests/D4BuildForge.Domain.Tests --filter BuildRecordTests`
Expected: PASS (3).

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "feat(domain): BuildRecord + SkillRef + TargetRef (frozen build contract, spec 3.2)"
```

---

### Task 4: `IRecordRepository` contract + in-memory fake + freeze marker

**Files:**
- Create: `src/D4BuildForge.Domain/IRecordRepository.cs`
- Create: `src/D4BuildForge.Domain/README.md`
- Create: `tests/D4BuildForge.Domain.Tests/InMemoryRecordRepository.cs`
- Test: `tests/D4BuildForge.Domain.Tests/RecordRepositoryContractTests.cs`

**Interfaces:**
- Consumes: nothing (BCL `System.Text.Json.Nodes`).
- Produces:
  - `interface IRecordRepository { Task<IReadOnlyList<JsonObject>> List(string collection); Task<JsonObject?> Get(string collection, string id); Task<JsonObject> Save(string collection, JsonObject record); Task Delete(string collection, string id); }`
  - test-only `InMemoryRecordRepository : IRecordRepository` (Save assigns a `id` when absent).

- [ ] **Step 1: Write the failing test**

`tests/D4BuildForge.Domain.Tests/RecordRepositoryContractTests.cs`:
```csharp
using System.Text.Json.Nodes;
using D4BuildForge.Domain;

namespace D4BuildForge.Domain.Tests;

public class RecordRepositoryContractTests
{
    [Fact]
    public async Task Save_assigns_id_when_absent_then_get_and_list_return_it()
    {
        IRecordRepository repo = new InMemoryRecordRepository();
        var saved = await repo.Save("items", new JsonObject { ["name"] = "Maul Helm" });

        var id = saved["id"]!.GetValue<string>();
        Assert.False(string.IsNullOrEmpty(id));

        var got = await repo.Get("items", id);
        Assert.Equal("Maul Helm", got!["name"]!.GetValue<string>());

        var all = await repo.List("items");
        Assert.Single(all);
    }

    [Fact]
    public async Task Save_keeps_existing_id_and_delete_removes()
    {
        IRecordRepository repo = new InMemoryRecordRepository();
        await repo.Save("builds", new JsonObject { ["id"] = "build_1", ["name"] = "A" });
        await repo.Delete("builds", "build_1");
        Assert.Null(await repo.Get("builds", "build_1"));
        Assert.Empty(await repo.List("builds"));
    }
}
```

- [ ] **Step 2: Run to verify it fails**

Run: `dotnet test tests/D4BuildForge.Domain.Tests --filter RecordRepositoryContractTests`
Expected: FAIL — `IRecordRepository` / `InMemoryRecordRepository` not found.

- [ ] **Step 3: Implement the interface + fake**

`src/D4BuildForge.Domain/IRecordRepository.cs`:
```csharp
using System.Text.Json.Nodes;

namespace D4BuildForge.Domain;

/// <summary>
/// Persistence contract for game-content / build *collections*. Works in raw JsonObject so the
/// schemaless render engine round-trips records losslessly. Real impl: DynamoRecordRepository
/// (src/Storage, Slice 2a) over AWS DynamoDB d4bf_* tables.
/// </summary>
public interface IRecordRepository
{
    Task<IReadOnlyList<JsonObject>> List(string collection);
    Task<JsonObject?> Get(string collection, string id);
    /// <summary>Upsert. Assigns a fresh <c>id</c> when the record has none; returns the stored record.</summary>
    Task<JsonObject> Save(string collection, JsonObject record);
    Task Delete(string collection, string id);
}
```

`tests/D4BuildForge.Domain.Tests/InMemoryRecordRepository.cs`:
```csharp
using System.Text.Json.Nodes;
using D4BuildForge.Domain;

namespace D4BuildForge.Domain.Tests;

/// <summary>Test-only IRecordRepository fake; also a reference impl for Slice 2a's Api/Assembly tests.</summary>
public sealed class InMemoryRecordRepository : IRecordRepository
{
    private readonly Dictionary<string, Dictionary<string, JsonObject>> _store = new();

    public Task<IReadOnlyList<JsonObject>> List(string collection)
        => Task.FromResult<IReadOnlyList<JsonObject>>(
            _store.TryGetValue(collection, out var m) ? m.Values.Select(Clone).ToList() : []);

    public Task<JsonObject?> Get(string collection, string id)
        => Task.FromResult(
            _store.TryGetValue(collection, out var m) && m.TryGetValue(id, out var r) ? Clone(r) : null);

    public Task<JsonObject> Save(string collection, JsonObject record)
    {
        var id = record["id"]?.GetValue<string>();
        if (string.IsNullOrEmpty(id))
        {
            id = "rec_" + Guid.NewGuid().ToString("N");
            record["id"] = id;
        }
        if (!_store.TryGetValue(collection, out var m)) { m = new(); _store[collection] = m; }
        m[id] = Clone(record);
        return Task.FromResult(Clone(record));
    }

    public Task Delete(string collection, string id)
    {
        if (_store.TryGetValue(collection, out var m)) m.Remove(id);
        return Task.CompletedTask;
    }

    private static JsonObject Clone(JsonObject o) => (JsonObject)o.DeepClone();
}
```

- [ ] **Step 4: Run to verify it passes**

Run: `dotnet test tests/D4BuildForge.Domain.Tests --filter RecordRepositoryContractTests`
Expected: PASS (2).

- [ ] **Step 5: Add the freeze marker**

`src/D4BuildForge.Domain/README.md`:
```markdown
# D4BuildForge.Domain — FROZEN CONTRACT

These records + `IRecordRepository` are the shared contract for the Builder (Slice 2a) and
Build-stealer (Slice 2b). The canonical JSON shapes are in
`docs/superpowers/specs/2026-06-30-d4-forge-admin-vessels-design.md` §3 and pinned by the round-trip
tests in `tests/D4BuildForge.Domain.Tests`.

- `ItemRecord` / `AffixEntry` / `AspectRef` — every D4 item (spec §3.1).
- `BuildRecord` / `SkillRef` / `TargetRef` — a computable build (spec §3.2).
- `IRecordRepository` — JsonObject CRUD over DDB collections (impl: `src/Storage`, Slice 2a).

**Changing a shape breaks both downstream workers — coordinate on Linear D4F before editing.**
```

- [ ] **Step 6: Full suite + commit**

Run: `dotnet test` (whole solution)
Expected: PASS — existing Engine tests (33) + new Domain tests (8) green; build warning-clean.

```bash
git add -A
git commit -m "feat(domain): IRecordRepository contract + in-memory fake + freeze marker"
```

---

## Done criteria
- `dotnet test` green across the solution (Engine 33 + Domain 8).
- `D4BuildForge.Domain` builds with zero package/project deps; item + build JSON shapes pin via round-trip tests; `IRecordRepository` defined and proven by the in-memory fake.
- Branch `feat/domain-contract` ready to merge to `master` → unblocks D4F-4 (Builder) and D4F-5 (Build-stealer).

## Next (not this plan)
Slice 2a (Builder): `src/Storage` `DynamoRecordRepository` (AWS DDB `d4bf_*`), `src/Assembly` `BuildAssembler`, `src/Api`, Flutter admin. Slice 2b (Build-stealer): Maxroll/Mobalytics importers → records in this frozen shape.
