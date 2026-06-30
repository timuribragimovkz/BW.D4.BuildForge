# D4BuildForge.Import Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Import third-party D4 builds (Maxroll, Mobalytics) into one source-agnostic internal contract (the IR), driven by data-only Mapping Vessels.

**Architecture:** A pure `D4BuildForge.Import` library. A `MappingEngine` interprets a per-source *Mapping Vessel* (JSON data) against a parsed source document, using a registry of small `ITransform` bricks, to produce an `ImportedBuild`. Identifiers are preserved verbatim in `ExternalRef`; no game-content resolution here. IO (Maxroll fetch) sits behind `IBuildFetcher` so the core is offline-testable.

**Tech Stack:** .NET 10 (net10.0), C#, `System.Text.Json` (`JsonNode`), xUnit.

## Global Constraints

- Target framework: `net10.0` (match `D4BuildForge.Engine`).
- `D4BuildForge.Import` is a **pure library**: no AWS/web/storage/engine references; IO only behind `IBuildFetcher`.
- JSON is camelCase (`JsonNamingPolicy.CamelCase`) to match the fixtures.
- **Faithful-or-warn:** never silently drop data. Unknown fields/slots → `ImportedBuild.Warnings`; structurally-invalid input → throw `ImportException`.
- Mapping logic lives in **vessels (data) + transform bricks**, never per-source `if/switch` in the core.
- TDD: write the failing test first, watch it fail, minimal implementation, watch it pass, commit.
- Fixtures are the two captured real builds; expected IR = the validated normalized JSON (both provided in Task 6).

---

### Task 1: Project scaffold

**Files:**
- Create: `src/D4BuildForge.Import/D4BuildForge.Import.csproj`
- Create: `tests/D4BuildForge.Import.Tests/D4BuildForge.Import.Tests.csproj`
- Create: `tests/D4BuildForge.Import.Tests/SmokeTest.cs`
- Modify: `D4BuildForge.sln` (add both projects)

**Interfaces:**
- Produces: the `D4BuildForge.Import` + `D4BuildForge.Import.Tests` projects, both building under the solution.

- [ ] **Step 1: Create the library csproj**

```xml
<!-- src/D4BuildForge.Import/D4BuildForge.Import.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>
```

- [ ] **Step 2: Create the test csproj**

```xml
<!-- tests/D4BuildForge.Import.Tests/D4BuildForge.Import.Tests.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.14.1" />
    <PackageReference Include="xunit" Version="2.9.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.1.4" />
    <PackageReference Include="coverlet.collector" Version="6.0.4" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="../../src/D4BuildForge.Import/D4BuildForge.Import.csproj" />
  </ItemGroup>
</Project>
```

- [ ] **Step 3: Write the smoke test**

```csharp
// tests/D4BuildForge.Import.Tests/SmokeTest.cs
namespace D4BuildForge.Import.Tests;
public class SmokeTest
{
    [Fact] public void Builds() => Assert.True(true);
}
```

- [ ] **Step 4: Add projects to the solution**

Run: `cd /Users/timuribragimov/gameSources/d4_build_forge && dotnet sln D4BuildForge.sln add src/D4BuildForge.Import/D4BuildForge.Import.csproj tests/D4BuildForge.Import.Tests/D4BuildForge.Import.Tests.csproj`
Expected: "Project ... added to the solution." ×2

- [ ] **Step 5: Build + test**

Run: `dotnet test tests/D4BuildForge.Import.Tests/D4BuildForge.Import.Tests.csproj`
Expected: PASS (1 test).

- [ ] **Step 6: Commit**

```bash
git add src/D4BuildForge.Import tests/D4BuildForge.Import.Tests D4BuildForge.sln
git commit -m "build(import): scaffold D4BuildForge.Import library + test project"
```

---

### Task 2: IR contracts

**Files:**
- Create: `src/D4BuildForge.Import/Contracts/ImportedBuild.cs`
- Create: `src/D4BuildForge.Import/ImportJson.cs` (shared `JsonSerializerOptions`)
- Test: `tests/D4BuildForge.Import.Tests/ContractsTests.cs`

**Interfaces:**
- Produces: records `ImportedBuild, BuildVariant, GearItem, ImportedAffix, ImportedAspect, ImportedSocket, SkillRank, ParagonLoadout, ParagonBoard, ParagonNode, GridPosition, ImportedMercenary, MercSkill, ClassExtra, ExternalRef`; enum `BuildSource { Maxroll, Mobalytics }`; `ImportJson.Options` (camelCase, enum-as-string).

- [ ] **Step 1: Write the failing test**

```csharp
// tests/D4BuildForge.Import.Tests/ContractsTests.cs
using System.Text.Json;
using D4BuildForge.Import;
using D4BuildForge.Import.Contracts;
namespace D4BuildForge.Import.Tests;
public class ContractsTests
{
    [Fact]
    public void ExternalRef_serializes_camelCase_with_string_enum()
    {
        var r = new ExternalRef(BuildSource.Maxroll, "affix", "1829560");
        var json = JsonSerializer.Serialize(r, ImportJson.Options);
        Assert.Contains("\"source\":\"maxroll\"", json);
        Assert.Contains("\"kind\":\"affix\"", json);
        Assert.Contains("\"id\":\"1829560\"", json);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/D4BuildForge.Import.Tests --filter ContractsTests`
Expected: FAIL — `ExternalRef` / `ImportJson` not defined.

- [ ] **Step 3: Add the contracts**

Copy the validated draft `scratchpad/ir/ImportedBuild.contracts.cs` verbatim into `src/D4BuildForge.Import/Contracts/ImportedBuild.cs` (namespace `D4BuildForge.Import.Contracts`). Then add the serializer options:

```csharp
// src/D4BuildForge.Import/ImportJson.cs
using System.Text.Json;
using System.Text.Json.Serialization;
namespace D4BuildForge.Import;
public static class ImportJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
        WriteIndented = true,
    };
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/D4BuildForge.Import.Tests --filter ContractsTests`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/D4BuildForge.Import/Contracts src/D4BuildForge.Import/ImportJson.cs tests/D4BuildForge.Import.Tests/ContractsTests.cs
git commit -m "feat(import): IR contracts (ImportedBuild) + camelCase json options"
```

---

### Task 3: JSONPath subset evaluator

**Files:**
- Create: `src/D4BuildForge.Import/Mapping/JsonPath.cs`
- Test: `tests/D4BuildForge.Import.Tests/Mapping/JsonPathTests.cs`

**Interfaces:**
- Produces: `static class JsonPath { IReadOnlyList<JsonNode?> Select(JsonNode? current, string path); }`. Grammar: leading `$`; `.key`; `[*]` (expand array elements); `[n]` (index). `$` alone returns `[current]`. Missing keys yield nothing (empty list).

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/D4BuildForge.Import.Tests/Mapping/JsonPathTests.cs
using System.Text.Json.Nodes;
using D4BuildForge.Import.Mapping;
namespace D4BuildForge.Import.Tests.Mapping;
public class JsonPathTests
{
    static JsonNode Node(string j) => JsonNode.Parse(j)!;

    [Fact] public void Self_returns_current()
        => Assert.Single(JsonPath.Select(Node("{\"a\":1}"), "$"));

    [Fact] public void Reads_nested_key()
    {
        var r = JsonPath.Select(Node("{\"a\":{\"b\":7}}"), "$.a.b");
        Assert.Single(r); Assert.Equal(7, (int)r[0]!);
    }

    [Fact] public void Wildcard_expands_array()
    {
        var r = JsonPath.Select(Node("{\"xs\":[1,2,3]}"), "$.xs[*]");
        Assert.Equal(3, r.Count);
    }

    [Fact] public void Wildcard_then_key_flattens()
    {
        var r = JsonPath.Select(Node("{\"s\":[{\"d\":1},{\"d\":2}]}"), "$.s[*].d");
        Assert.Equal(new[]{1,2}, r.Select(n => (int)n!));
    }

    [Fact] public void Index_selects_one()
    {
        var r = JsonPath.Select(Node("{\"xs\":[10,20]}"), "$.xs[1]");
        Assert.Single(r); Assert.Equal(20, (int)r[0]!);
    }

    [Fact] public void Missing_key_yields_empty()
        => Assert.Empty(JsonPath.Select(Node("{\"a\":1}"), "$.nope"));
}
```

- [ ] **Step 2: Run to verify fail**

Run: `dotnet test tests/D4BuildForge.Import.Tests --filter JsonPathTests`
Expected: FAIL — `JsonPath` not defined.

- [ ] **Step 3: Implement**

```csharp
// src/D4BuildForge.Import/Mapping/JsonPath.cs
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
namespace D4BuildForge.Import.Mapping;

public static class JsonPath
{
    // token = key optionally followed by [ * ] or [ n ]
    static readonly Regex Seg = new(@"^(?<key>[^.\[]+)?(?<idx>\[(?:\*|\d+)\])?$", RegexOptions.Compiled);

    public static IReadOnlyList<JsonNode?> Select(JsonNode? current, string path)
    {
        if (string.IsNullOrEmpty(path) || path == "$") return new[] { current };
        if (!path.StartsWith("$")) throw new ArgumentException($"path must start with $: {path}");
        IEnumerable<JsonNode?> cursor = new[] { current };
        foreach (var raw in path.Substring(1).TrimStart('.').Split('.', StringSplitOptions.RemoveEmptyEntries))
        {
            var m = Seg.Match(raw);
            if (!m.Success) throw new ArgumentException($"bad path segment: {raw}");
            var key = m.Groups["key"].Success ? m.Groups["key"].Value : null;
            var idx = m.Groups["idx"].Success ? m.Groups["idx"].Value : null;
            var next = new List<JsonNode?>();
            foreach (var node in cursor)
            {
                var n = node;
                if (key is not null) n = (n as JsonObject)?[key];
                if (idx is null) { if (n is not null || key is not null) next.Add(n); }
                else if (n is JsonArray arr)
                {
                    if (idx == "[*]") next.AddRange(arr);
                    else { var i = int.Parse(idx.Trim('[', ']')); if (i >= 0 && i < arr.Count) next.Add(arr[i]); }
                }
            }
            cursor = next;
        }
        return cursor.ToList();
    }
}
```

- [ ] **Step 4: Run to verify pass**

Run: `dotnet test tests/D4BuildForge.Import.Tests --filter JsonPathTests`
Expected: PASS (6).

- [ ] **Step 5: Commit**

```bash
git add src/D4BuildForge.Import/Mapping/JsonPath.cs tests/D4BuildForge.Import.Tests/Mapping/JsonPathTests.cs
git commit -m "feat(import): JSONPath subset evaluator"
```

---

### Task 4: Transform registry + transform bricks

**Files:**
- Create: `src/D4BuildForge.Import/Mapping/ITransform.cs` (interface + `TransformCtx`)
- Create: `src/D4BuildForge.Import/Mapping/Transforms.cs` (all v1 transforms + registry)
- Test: `tests/D4BuildForge.Import.Tests/Mapping/TransformsTests.cs`

**Interfaces:**
- Produces: `interface ITransform { string Name; JsonNode? Apply(TransformCtx ctx, JsonNode? input, string[] args); }`; `record TransformCtx(JsonNode? Root, JsonNode Vessel)`; `static class Transforms { IReadOnlyDictionary<string, ITransform> Registry; JsonNode? Run(TransformCtx ctx, JsonNode? input, IEnumerable<string> pipeline); }`. Synthetic entry nodes carry reserved keys `@key/@value/@self/@count/@members/@animal`.
- Transform names & behavior: `lower`, `const:<v>`, `format:<tpl>`, `nullable`, `bool:<default>`, `tokenAt:<n>` (split `_`), `entries` (object→`[{@key,@value}]`), `mergeDicts` (list-of-objects→one object), `dropZero` (drop entries whose `@value`==0), `derefItemPool:<scopePath>` (entries whose `@value`=poolKey → cloned pool item + `@key`), `slotFromItemId:<tablePath>` (id prefix→table), `slotFromSlug:<tablePath>` (slug→table, fallback ChaosPerk/Seal/Charm/else slug), `countRepeats` (scalars→`[{@key,@count}]` first-seen order), `groupByRegex:<pattern>` (slugs→`[{@key,@members}]` grouped by capture group 1), `flattenBoons` (`[{type,vals[]}]`→`[{@self,@animal}]`).

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/D4BuildForge.Import.Tests/Mapping/TransformsTests.cs
using System.Text.Json.Nodes;
using D4BuildForge.Import.Mapping;
namespace D4BuildForge.Import.Tests.Mapping;
public class TransformsTests
{
    static TransformCtx Ctx(string root = "{}") => new(JsonNode.Parse(root), new JsonObject{["source"]="maxroll"});
    static JsonNode N(string j) => JsonNode.Parse(j)!;

    [Fact] public void Lower() => Assert.Equal("druid", (string)Transforms.Run(Ctx(), N("\"Druid\""), new[]{"lower"})!);

    [Fact] public void Entries_emits_key_value()
    {
        var r = Transforms.Run(Ctx(), N("{\"10\":1,\"31\":0}"), new[]{"entries"})!.AsArray();
        Assert.Equal(2, r.Count);
        Assert.Equal("10", (string)r[0]!["@key"]!); Assert.Equal(1, (int)r[0]!["@value"]!);
    }

    [Fact] public void DropZero_removes_zero_ranks()
    {
        var r = Transforms.Run(Ctx(), N("{\"10\":1,\"31\":0}"), new[]{"entries","dropZero"})!.AsArray();
        Assert.Single(r);
    }

    [Fact] public void CountRepeats_counts_first_seen()
    {
        var r = Transforms.Run(Ctx(), N("[\"a\",\"a\",\"b\"]"), new[]{"countRepeats"})!.AsArray();
        Assert.Equal("a", (string)r[0]!["@key"]!); Assert.Equal(2, (int)r[0]!["@count"]!);
        Assert.Equal("b", (string)r[1]!["@key"]!); Assert.Equal(1, (int)r[1]!["@count"]!);
    }

    [Fact] public void GroupByRegex_groups_by_board_prefix()
    {
        var r = Transforms.Run(Ctx(), N("[\"d-a-x1-y2\",\"d-a-x3-y4\",\"d-b-x1-y1\"]"),
                               new[]{@"groupByRegex:^(.*)-x\d+-y\d+$"})!.AsArray();
        Assert.Equal(2, r.Count);
        Assert.Equal("d-a", (string)r[0]!["@key"]!);
        Assert.Equal(2, r[0]!["@members"]!.AsArray().Count);
    }

    [Fact] public void SlotFromItemId_uses_prefix_table()
    {
        var ctx = new TransformCtx(null, new JsonObject{["slotTable"]=new JsonObject{["Helm"]="Helm"}});
        var r = Transforms.Run(ctx, N("\"Helm_Unique_Druid_102\""), new[]{"slotFromItemId:$vessel.slotTable"});
        Assert.Equal("Helm", (string)r!);
    }

    [Fact] public void DerefItemPool_pulls_item_and_keeps_key()
    {
        var root = N("{\"items\":{\"3\":{\"id\":\"Pants_X\"}}}");
        var ctx = new TransformCtx(root, new JsonObject());
        var entries = Transforms.Run(ctx, N("{\"5\":3}"), new[]{"entries"}); // slot 5 -> pool key 3
        var r = Transforms.Run(ctx, entries, new[]{"derefItemPool:$root.items"})!.AsArray();
        Assert.Single(r);
        Assert.Equal("Pants_X", (string)r[0]!["id"]!);
        Assert.Equal("5", (string)r[0]!["@key"]!);
    }
}
```

- [ ] **Step 2: Run to verify fail**

Run: `dotnet test tests/D4BuildForge.Import.Tests --filter TransformsTests`
Expected: FAIL — `Transforms`/`ITransform` not defined.

- [ ] **Step 3: Implement interface + ctx**

```csharp
// src/D4BuildForge.Import/Mapping/ITransform.cs
using System.Text.Json.Nodes;
namespace D4BuildForge.Import.Mapping;
public record TransformCtx(JsonNode? Root, JsonNode Vessel);
public interface ITransform { string Name { get; } JsonNode? Apply(TransformCtx ctx, JsonNode? input, string[] args); }
```

- [ ] **Step 4: Implement transforms + registry**

```csharp
// src/D4BuildForge.Import/Mapping/Transforms.cs
using System.Globalization;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
namespace D4BuildForge.Import.Mapping;

public static class Transforms
{
    public static readonly IReadOnlyDictionary<string, Func<TransformCtx, JsonNode?, string[], JsonNode?>> Registry =
        new Dictionary<string, Func<TransformCtx, JsonNode?, string[], JsonNode?>>
    {
        ["lower"]   = (_, n, _) => (JsonNode?)(((string?)n)?.ToLowerInvariant()),
        ["nullable"]= (_, n, _) => n,
        ["const"]   = (_, _, a) => ParseConst(a),
        ["format"]  = (_, n, a) => string.Format(a[0], (object?)(string?)n),
        ["bool"]    = (_, n, a) => Truthy(n) ?? bool.Parse(a.Length>0 ? a[0] : "false"),
        ["tokenAt"] = (_, n, a) => { var p=((string?)n)?.Split('_'); var i=int.Parse(a[0]); return p!=null && i<p.Length ? p[i] : null; },
        ["entries"] = (_, n, _) => Entries(n),
        ["mergeDicts"] = (_, n, _) => MergeDicts(n),
        ["dropZero"]= (_, n, _) => Filter(n, e => (e as JsonObject)?["@value"] is JsonValue v && v.GetValueKind()==System.Text.Json.JsonValueKind.Number && (double)v! != 0 || (e as JsonObject)?["@value"]?.GetValueKind() != System.Text.Json.JsonValueKind.Number),
        ["derefItemPool"] = (c, n, a) => DerefItemPool(c, n, a[0]),
        ["slotFromItemId"]= (c, n, a) => Lookup(Table(c, a[0]), ((string?)n)?.Split('_')[0]) ?? ((string?)n)?.Split('_')[0],
        ["slotFromSlug"]  = (c, n, a) => SlotFromSlug(Table(c, a[0]), (string?)n),
        ["countRepeats"]  = (_, n, _) => CountRepeats(n),
        ["groupByRegex"]  = (_, n, a) => GroupByRegex(n, a[0]),
        ["flattenBoons"]  = (_, n, _) => FlattenBoons(n),
    };

    public static JsonNode? Run(TransformCtx ctx, JsonNode? input, IEnumerable<string> pipeline)
    {
        var node = input;
        foreach (var step in pipeline)
        {
            var (name, args) = Split(step);
            if (!Registry.TryGetValue(name, out var fn)) throw new ImportException($"unknown transform: {name}");
            node = fn(ctx, node, args);
        }
        return node;
    }

    static (string, string[]) Split(string step)
    {
        var i = step.IndexOf(':');
        return i < 0 ? (step, Array.Empty<string>()) : (step[..i], new[] { step[(i + 1)..] });
    }
    static JsonNode? ParseConst(string[] a)
    {
        if (a.Length == 0) return null;
        return a[0] switch { "null" => null, "true" => true, "false" => false,
            var s when double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) => d, var s => s };
    }
    static bool? Truthy(JsonNode? n) => n is null ? (bool?)null :
        n is JsonValue v ? (v.TryGetValue(out bool b) ? b : v.TryGetValue(out double d) ? d != 0 : true) : true;
    static JsonArray Entries(JsonNode? n)
    {
        var a = new JsonArray();
        if (n is JsonObject o) foreach (var kv in o) a.Add(new JsonObject { ["@key"] = kv.Key, ["@value"] = kv.Value?.DeepClone() });
        return a;
    }
    static JsonObject MergeDicts(JsonNode? n)
    {
        var o = new JsonObject();
        if (n is JsonArray a) foreach (var d in a) if (d is JsonObject dd) foreach (var kv in dd) o[kv.Key] = kv.Value?.DeepClone();
        return o;
    }
    static JsonArray Filter(JsonNode? n, Func<JsonNode?, bool> keep)
    { var a = new JsonArray(); if (n is JsonArray src) foreach (var e in src) if (keep(e)) a.Add(e?.DeepClone()); return a; }
    static JsonObject? Table(TransformCtx c, string scopePath)
        => JsonPath.Select(scopePath.StartsWith("$vessel") ? c.Vessel : c.Root, scopePath.Replace("$vessel", "$").Replace("$root", "$"))
             .FirstOrDefault() as JsonObject;
    static string? Lookup(JsonObject? t, string? key) => key is not null && t?[key] is JsonNode v ? (string?)v : null;
    static JsonNode? SlotFromSlug(JsonObject? t, string? slug)
    {
        if (slug is null) return null;
        if (Lookup(t, slug) is string s) return s;
        if (slug.Contains("chaos-perk")) return "ChaosPerk";
        if (slug.EndsWith("seal") || slug.Contains("-seal")) return "Seal";
        if (slug.Contains("charm")) return "Charm";
        return slug;
    }
    static JsonNode? DerefItemPool(TransformCtx c, JsonNode? entries, string scopePath)
    {
        var pool = JsonPath.Select(c.Root, scopePath.Replace("$root", "$")).FirstOrDefault() as JsonObject;
        var a = new JsonArray();
        if (entries is JsonArray es && pool is not null)
            foreach (var e in es)
            {
                var key = (string?)(e as JsonObject)?["@key"];
                var poolKey = (e as JsonObject)?["@value"]?.ToString();
                if (poolKey is not null && pool[poolKey] is JsonObject item)
                { var clone = (JsonObject)item.DeepClone(); clone["@key"] = key; a.Add(clone); }
            }
        return a;
    }
    static JsonArray CountRepeats(JsonNode? n)
    {
        var order = new List<string>(); var counts = new Dictionary<string, int>();
        if (n is JsonArray a) foreach (var e in a) { var k = (string?)e; if (k is null) continue;
            if (!counts.ContainsKey(k)) { counts[k] = 0; order.Add(k); } counts[k]++; }
        var outA = new JsonArray();
        foreach (var k in order) outA.Add(new JsonObject { ["@key"] = k, ["@count"] = counts[k] });
        return outA;
    }
    static JsonArray GroupByRegex(JsonNode? n, string pattern)
    {
        var rx = new Regex(pattern); var order = new List<string>(); var groups = new Dictionary<string, JsonArray>();
        if (n is JsonArray a) foreach (var e in a) { var s = (string?)e; if (s is null) continue;
            var m = rx.Match(s); var g = m.Success ? m.Groups[1].Value : s;
            if (!groups.ContainsKey(g)) { groups[g] = new JsonArray(); order.Add(g); } groups[g].Add(s); }
        var outA = new JsonArray();
        foreach (var g in order) outA.Add(new JsonObject { ["@key"] = g, ["@members"] = groups[g] });
        return outA;
    }
    static JsonArray FlattenBoons(JsonNode? n)
    {
        var a = new JsonArray();
        if (n is JsonArray bs) foreach (var b in bs)
        { var animal = (string?)(b as JsonObject)?["type"];
          if ((b as JsonObject)?["vals"] is JsonArray vals) foreach (var v in vals)
            a.Add(new JsonObject { ["@self"] = (string?)v, ["@animal"] = animal }); }
        return a;
    }
}
```

- [ ] **Step 5: Run to verify pass**

Run: `dotnet test tests/D4BuildForge.Import.Tests --filter TransformsTests`
Expected: PASS (7).

- [ ] **Step 6: Commit**

```bash
git add src/D4BuildForge.Import/Mapping/ITransform.cs src/D4BuildForge.Import/Mapping/Transforms.cs tests/D4BuildForge.Import.Tests/Mapping/TransformsTests.cs
git commit -m "feat(import): transform registry + bricks (entries/deref/countRepeats/groupByRegex/...)"
```

> Note: also add `ImportException`. Create `src/D4BuildForge.Import/ImportException.cs`:
> ```csharp
> namespace D4BuildForge.Import;
> public sealed class ImportException : Exception { public ImportException(string m) : base(m) {} }
> ```
> Fold this file into Task 4's commit.

---

### Task 5: MappingEngine (vessel interpreter)

**Files:**
- Create: `src/D4BuildForge.Import/Mapping/MappingEngine.cs`
- Test: `tests/D4BuildForge.Import.Tests/Mapping/MappingEngineTests.cs`

**Interfaces:**
- Consumes: `JsonPath`, `Transforms`, `TransformCtx`, the IR contracts, `ImportJson.Options`.
- Produces: `class MappingEngine { ImportedBuild Apply(JsonNode vessel, JsonNode raw); }`. Walks `vessel["build"]`; resolves `vessel["root"]` path against `raw` for the build-root scope; evaluates bindings:
  - string `"$..."` → first `JsonPath.Select` match against the scope (`$root.`→build root, `$vessel.`→vessel, else current).
  - `{from, via?, each?}` → `Select(from)`; if `via`, pipe (single node) or for `each` pipe then map each element through the sub-binding.
  - `{via:["const",...]}` → literal.
  - `"@name"` → evaluate `vessel[name]` against current.
  - `{$ref:{kind,id,name?}}` → `ExternalRef(vessel.source, kind, id, name)`.
  - inline object → object with each field evaluated.
  - tokens `@key/@value/@self/@count/@members/@animal` read directly off the current node.
  The result `JsonObject` is deserialized into `ImportedBuild` via `ImportJson.Options`.

- [ ] **Step 1: Write the failing test (tiny inline vessel)**

```csharp
// tests/D4BuildForge.Import.Tests/Mapping/MappingEngineTests.cs
using System.Text.Json.Nodes;
using D4BuildForge.Import.Mapping;
namespace D4BuildForge.Import.Tests.Mapping;
public class MappingEngineTests
{
    static JsonNode N(string j) => JsonNode.Parse(j)!;

    [Fact]
    public void Maps_name_class_and_each_variant_with_ref_and_skillranks()
    {
        var vessel = N(@"{
          ""source"":""maxroll"", ""root"":""$.data"",
          ""build"":{ ""clazz"":{""from"":""$.class"",""via"":[""lower""]}, ""name"":""$.name"",
                      ""variants"":{""from"":""$.profiles"",""each"":""@variant""} },
          ""variant"":{ ""name"":""$.name"",
                        ""skillRanks"":{""from"":""$.skillTree.steps[*].data"",""via"":[""mergeDicts"",""entries"",""dropZero""],
                                        ""each"":{""ref"":{""$ref"":{""kind"":""skill"",""id"":""@key""}},""rank"":""@value""}} }
        }");
        var raw = N(@"{""data"":{""class"":""Druid"",""name"":""B"",
            ""profiles"":[{""name"":""End"",""skillTree"":{""steps"":[{""data"":{""416"":15,""413"":0}}]}}]}}");
        var build = new MappingEngine().Apply(vessel, raw);
        Assert.Equal("druid", build.Clazz);
        Assert.Equal("B", build.Name);
        var v = Assert.Single(build.Variants);
        Assert.Equal("End", v.Name);
        var sr = Assert.Single(v.SkillRanks);
        Assert.Equal("416", sr.Ref.Id);
        Assert.Equal(15, sr.Rank);
        Assert.Equal(Contracts.BuildSource.Maxroll, sr.Ref.Source);
    }
}
```

- [ ] **Step 2: Run to verify fail**

Run: `dotnet test tests/D4BuildForge.Import.Tests --filter MappingEngineTests`
Expected: FAIL — `MappingEngine` not defined.

- [ ] **Step 3: Implement**

```csharp
// src/D4BuildForge.Import/Mapping/MappingEngine.cs
using System.Text.Json;
using System.Text.Json.Nodes;
using D4BuildForge.Import.Contracts;
namespace D4BuildForge.Import.Mapping;

public sealed class MappingEngine
{
    public ImportedBuild Apply(JsonNode vessel, JsonNode raw)
    {
        var rootPath = (string?)vessel["root"] ?? "$";
        var root = JsonPath.Select(raw, rootPath).FirstOrDefault()
                   ?? throw new ImportException($"vessel root path matched nothing: {rootPath}");
        var ctx = new TransformCtx(root, vessel);
        var built = (JsonObject)Eval(vessel["build"]!, root, root, ctx)!;
        built["source"] ??= (string?)vessel["source"];
        var b = built.Deserialize<ImportedBuild>(ImportJson.Options)
                ?? throw new ImportException("failed to materialize ImportedBuild");
        return b;
    }

    // current = node bindings read $. against; root = build root (for $root.)
    JsonNode? Eval(JsonNode binding, JsonNode? current, JsonNode? root, TransformCtx ctx)
    {
        switch (binding)
        {
            case JsonValue v when v.TryGetValue(out string? s) && s is not null:
                return EvalString(s, current, root, ctx);
            case JsonObject o when o.ContainsKey("$ref"):
                return EvalRef((JsonObject)o["$ref"]!, current, root, ctx);
            case JsonObject o when o.ContainsKey("from") || o.ContainsKey("via"):
                return EvalFromVia(o, current, root, ctx);
            case JsonObject o:
                var res = new JsonObject();
                foreach (var kv in o) res[kv.Key] = Eval(kv.Value!, current, root, ctx);
                return res;
            default:
                return binding.DeepClone();
        }
    }

    JsonNode? EvalString(string s, JsonNode? current, JsonNode? root, TransformCtx ctx)
    {
        if (s.StartsWith("@")) return s == "@self" ? current?.DeepClone() : (current as JsonObject)?[s]?.DeepClone();
        if (!s.StartsWith("$")) return s;                                   // literal
        if (s.StartsWith("$.")) return First(JsonPath.Select(current, s));
        if (s.StartsWith("$root")) return First(JsonPath.Select(root, s.Replace("$root", "$")));
        if (s.StartsWith("$vessel")) return First(JsonPath.Select(ctx.Vessel, s.Replace("$vessel", "$")));
        return First(JsonPath.Select(current, s));
    }

    JsonNode? EvalFromVia(JsonObject o, JsonNode? current, JsonNode? root, TransformCtx ctx)
    {
        var via = (o["via"] as JsonArray)?.Select(x => (string)x!).ToList();
        // const literal form: {"via":["const", <literal>]}
        if (via is { Count: >= 1 } && via[0] == "const")
            return o["via"]!.AsArray().Count > 1 ? o["via"]![1]?.DeepClone() : null;

        JsonNode? src = o.ContainsKey("from")
            ? CollectFrom((string)o["from"]!, current, root, ctx)
            : current;
        if (via is not null) src = Transforms.Run(ctx, src, via);

        if (o["each"] is JsonNode each)
        {
            var outA = new JsonArray();
            if (src is JsonArray arr)
                foreach (var el in arr) outA.Add(Eval(each, el, root, ctx));
            return outA;
        }
        return src;
    }

    // "from" may yield multiple nodes (wildcards) -> array; else single node
    JsonNode? CollectFrom(string from, JsonNode? current, JsonNode? root, TransformCtx ctx)
    {
        var scope = from.StartsWith("$root") ? root : from.StartsWith("$vessel") ? ctx.Vessel : current;
        var path = from.Replace("$root", "$").Replace("$vessel", "$");
        var matches = JsonPath.Select(scope, path);
        if (path.Contains("[*]")) { var a = new JsonArray(); foreach (var m in matches) a.Add(m?.DeepClone()); return a; }
        return First(matches);
    }

    JsonNode EvalRef(JsonObject r, JsonNode? current, JsonNode? root, TransformCtx ctx)
    {
        var refObj = new JsonObject { ["source"] = (string?)ctx.Vessel["source"] };
        refObj["kind"] = Eval(r["kind"]!, current, root, ctx);
        refObj["id"] = Eval(r["id"]!, current, root, ctx)?.ToString();
        if (r.ContainsKey("name")) refObj["name"] = Eval(r["name"]!, current, root, ctx);
        return refObj;
    }

    static JsonNode? First(IReadOnlyList<JsonNode?> ns) => ns.Count > 0 ? ns[0]?.DeepClone() : null;
}
```

- [ ] **Step 4: Run to verify pass**

Run: `dotnet test tests/D4BuildForge.Import.Tests --filter MappingEngineTests`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/D4BuildForge.Import/Mapping/MappingEngine.cs tests/D4BuildForge.Import.Tests/Mapping/MappingEngineTests.cs
git commit -m "feat(import): MappingEngine — vessel interpreter -> ImportedBuild"
```

---

### Task 6: Vessels + fixtures as embedded resources/test data

**Files:**
- Create: `src/D4BuildForge.Import/Vessels/maxroll.vessel.json` (copy from `scratchpad/ir/maxroll.vessel.json`)
- Create: `src/D4BuildForge.Import/Vessels/mobalytics.vessel.json` (copy from `scratchpad/ir/mobalytics.vessel.json`)
- Modify: `src/D4BuildForge.Import/D4BuildForge.Import.csproj` (embed Vessels/*.json)
- Create: `tests/D4BuildForge.Import.Tests/fixtures/maxroll_cataclysm.raw.json` (copy from scratchpad)
- Create: `tests/D4BuildForge.Import.Tests/fixtures/mobalytics_zaior_landslide.lean.json` (copy from scratchpad)
- Modify: `tests/D4BuildForge.Import.Tests/D4BuildForge.Import.Tests.csproj` (copy fixtures to output)
- Create: `src/D4BuildForge.Import/Vessels/VesselStore.cs`
- Test: `tests/D4BuildForge.Import.Tests/Vessels/VesselStoreTests.cs`

**Interfaces:**
- Produces: `static class VesselStore { JsonNode Load(string source); IReadOnlyList<JsonNode> All(); }` reading embedded `Vessels/*.json`.

- [ ] **Step 1: Copy the four data files**

```bash
cp /private/tmp/claude-501/-Users-timuribragimov-Desktop-mySources/dbc22d17-cb6f-4f7c-86c1-10b713d96511/scratchpad/ir/maxroll.vessel.json src/D4BuildForge.Import/Vessels/
cp /private/tmp/claude-501/-Users-timuribragimov-Desktop-mySources/dbc22d17-cb6f-4f7c-86c1-10b713d96511/scratchpad/ir/mobalytics.vessel.json src/D4BuildForge.Import/Vessels/
mkdir -p tests/D4BuildForge.Import.Tests/fixtures
cp /private/tmp/claude-501/-Users-timuribragimov-Desktop-mySources/dbc22d17-cb6f-4f7c-86c1-10b713d96511/scratchpad/ir/maxroll_cataclysm.raw.json tests/D4BuildForge.Import.Tests/fixtures/
cp /private/tmp/claude-501/-Users-timuribragimov-Desktop-mySources/dbc22d17-cb6f-4f7c-86c1-10b713d96511/scratchpad/ir/mobalytics_zaior_landslide.lean.json tests/D4BuildForge.Import.Tests/fixtures/
```

- [ ] **Step 2: Embed vessels + copy fixtures**

In `src/D4BuildForge.Import/D4BuildForge.Import.csproj` add:
```xml
  <ItemGroup>
    <EmbeddedResource Include="Vessels/*.json" />
  </ItemGroup>
```
In `tests/D4BuildForge.Import.Tests/D4BuildForge.Import.Tests.csproj` add:
```xml
  <ItemGroup>
    <None Include="fixtures/*.json" CopyToOutputDirectory="PreserveNewest" />
  </ItemGroup>
```

- [ ] **Step 3: Write the failing test**

```csharp
// tests/D4BuildForge.Import.Tests/Vessels/VesselStoreTests.cs
using D4BuildForge.Import.Vessels;
namespace D4BuildForge.Import.Tests.Vessels;
public class VesselStoreTests
{
    [Fact] public void Loads_maxroll_vessel() => Assert.Equal("maxroll", (string)VesselStore.Load("maxroll")["source"]!);
    [Fact] public void All_returns_both() => Assert.Equal(2, VesselStore.All().Count);
}
```

- [ ] **Step 4: Run to verify fail**

Run: `dotnet test tests/D4BuildForge.Import.Tests --filter VesselStoreTests`
Expected: FAIL — `VesselStore` not defined.

- [ ] **Step 5: Implement**

```csharp
// src/D4BuildForge.Import/Vessels/VesselStore.cs
using System.Reflection;
using System.Text.Json.Nodes;
namespace D4BuildForge.Import.Vessels;
public static class VesselStore
{
    static readonly Assembly Asm = typeof(VesselStore).Assembly;
    public static IReadOnlyList<JsonNode> All() =>
        Asm.GetManifestResourceNames().Where(n => n.Contains(".Vessels.") && n.EndsWith(".json"))
           .Select(Read).ToList();
    public static JsonNode Load(string source) =>
        All().FirstOrDefault(v => (string?)v["source"] == source)
        ?? throw new ImportException($"no vessel for source {source}");
    static JsonNode Read(string res)
    { using var s = Asm.GetManifestResourceStream(res)!; using var r = new StreamReader(s);
      return JsonNode.Parse(r.ReadToEnd())!; }
}
```

- [ ] **Step 6: Run to verify pass + commit**

Run: `dotnet test tests/D4BuildForge.Import.Tests --filter VesselStoreTests` → PASS (2).
```bash
git add src/D4BuildForge.Import/Vessels tests/D4BuildForge.Import.Tests/fixtures src/D4BuildForge.Import/D4BuildForge.Import.csproj tests/D4BuildForge.Import.Tests/D4BuildForge.Import.Tests.csproj tests/D4BuildForge.Import.Tests/Vessels/VesselStoreTests.cs
git commit -m "feat(import): embed Mapping Vessels + add golden build fixtures"
```

---

### Task 7: Golden test — Maxroll Cataclysm end-to-end

**Files:**
- Create: `tests/D4BuildForge.Import.Tests/Golden/MaxrollGoldenTests.cs`
- Iterate: `src/D4BuildForge.Import/Vessels/maxroll.vessel.json` (until green)

**Interfaces:**
- Consumes: `MappingEngine`, `VesselStore`, the fixture file.
- Produces: a `LoadFixture(name)` test helper + the validated Maxroll vessel.

- [ ] **Step 1: Write the failing test (exact values from the normalized output)**

```csharp
// tests/D4BuildForge.Import.Tests/Golden/MaxrollGoldenTests.cs
using System.Text.Json.Nodes;
using D4BuildForge.Import.Mapping;
using D4BuildForge.Import.Vessels;
namespace D4BuildForge.Import.Tests.Golden;
public class MaxrollGoldenTests
{
    static JsonNode Fixture(string f) => JsonNode.Parse(File.ReadAllText(Path.Combine("fixtures", f)))!;

    [Fact]
    public void Imports_cataclysm_full_fidelity()
    {
        var build = new MappingEngine().Apply(VesselStore.Load("maxroll"), Fixture("maxroll_cataclysm.raw.json"));

        Assert.Equal(Contracts.BuildSource.Maxroll, build.Source);
        Assert.Equal("druid", build.Clazz);
        Assert.Equal(3, build.Variants.Count);

        var end = build.Variants[2];
        Assert.Equal("Endgame", end.Name);
        Assert.Equal(70, end.Level);
        Assert.Equal(14, end.WorldTier);
        Assert.Equal(16, end.Gear.Count);
        Assert.Equal(35, end.SkillRanks.Count);
        Assert.Equal(25, end.Paragon.Boards.Count);
        Assert.Equal(5, end.ClassExtras.Count);
        Assert.NotNull(end.SkillBar);

        var helm = end.Gear.Single(g => g.Slot == "Helm");
        Assert.Equal("Helm_Unique_Druid_102", helm.Ref.Id);
        Assert.True(helm.Mythic);
        Assert.NotNull(helm.Explicits[0].Values);           // Maxroll has rolled values

        var board0 = end.Paragon.Boards[0];
        Assert.NotNull(board0.Glyph);
        Assert.NotNull(board0.GlyphLevel);
    }
}
```

- [ ] **Step 2: Run to verify fail**

Run: `dotnet test tests/D4BuildForge.Import.Tests --filter MaxrollGoldenTests`
Expected: FAIL (assertion mismatch or null). Read the failure; adjust the **vessel** (paths/transforms), not the engine, until green. Common fixes: `derefItemPool` scope, `mergeDicts` ordering, glyph `nullable`.

- [ ] **Step 3: Make it pass by correcting the vessel**

Iterate `maxroll.vessel.json` only. Re-run after each edit.

- [ ] **Step 4: Run to verify pass**

Run: `dotnet test tests/D4BuildForge.Import.Tests --filter MaxrollGoldenTests`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add tests/D4BuildForge.Import.Tests/Golden/MaxrollGoldenTests.cs src/D4BuildForge.Import/Vessels/maxroll.vessel.json
git commit -m "test(import): Maxroll Cataclysm golden import (full fidelity)"
```

---

### Task 8: Golden test — Mobalytics Zaior Landslide end-to-end

**Files:**
- Create: `tests/D4BuildForge.Import.Tests/Golden/MobalyticsGoldenTests.cs`
- Iterate: `src/D4BuildForge.Import/Vessels/mobalytics.vessel.json` (until green)

**Interfaces:**
- Consumes: `MappingEngine`, `VesselStore`, the fixture file.

- [ ] **Step 1: Write the failing test (exact values from the normalized output)**

```csharp
// tests/D4BuildForge.Import.Tests/Golden/MobalyticsGoldenTests.cs
using System.Text.Json.Nodes;
using D4BuildForge.Import.Mapping;
using D4BuildForge.Import.Vessels;
namespace D4BuildForge.Import.Tests.Golden;
public class MobalyticsGoldenTests
{
    static JsonNode Fixture(string f) => JsonNode.Parse(File.ReadAllText(Path.Combine("fixtures", f)))!;

    [Fact]
    public void Imports_zaior_landslide_full_fidelity()
    {
        var build = new MappingEngine().Apply(VesselStore.Load("mobalytics"), Fixture("mobalytics_zaior_landslide.lean.json"));

        Assert.Equal(Contracts.BuildSource.Mobalytics, build.Source);
        Assert.Equal("druid", build.Clazz);
        Assert.Equal(3, build.Variants.Count);
        Assert.Equal(3, build.Warnings.Count);

        var end = build.Variants[2];
        Assert.Equal("End Game", end.Name);
        Assert.Null(end.Level);
        Assert.Null(end.WorldTier);
        Assert.Null(end.SkillBar);                          // action bar not separable
        Assert.Equal(20, end.Gear.Count);
        Assert.Equal(27, end.SkillRanks.Count);
        Assert.Equal(5, end.Paragon.Boards.Count);
        Assert.Equal(5, end.ClassExtras.Count);

        var helm = end.Gear.Single(g => g.Slot == "Helm");
        Assert.Equal("gathlens-birthright", helm.Ref.Id);
        Assert.Null(helm.Explicits[0].Values);              // Mobalytics: flags only
        Assert.Contains(helm.Explicits, a => a.Greater);    // greater flag present on End Game helm

        Assert.All(end.Paragon.Boards, b => Assert.Null(b.Glyph));
    }
}
```

- [ ] **Step 2: Run to verify fail**

Run: `dotnet test tests/D4BuildForge.Import.Tests --filter MobalyticsGoldenTests`
Expected: FAIL. Adjust `mobalytics.vessel.json` only (e.g. `groupByRegex` pattern, `countRepeats`, `flattenBoons`, the `warnings` const block) until green.

- [ ] **Step 3: Make it pass by correcting the vessel**

- [ ] **Step 4: Run to verify pass**

Run: `dotnet test tests/D4BuildForge.Import.Tests --filter MobalyticsGoldenTests`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add tests/D4BuildForge.Import.Tests/Golden/MobalyticsGoldenTests.cs src/D4BuildForge.Import/Vessels/mobalytics.vessel.json
git commit -m "test(import): Mobalytics Zaior Landslide golden import (full fidelity)"
```

---

### Task 9: BuildImporter façade + source auto-detection

**Files:**
- Create: `src/D4BuildForge.Import/BuildImporter.cs`
- Test: `tests/D4BuildForge.Import.Tests/BuildImporterTests.cs`

**Interfaces:**
- Consumes: `VesselStore`, `MappingEngine`, `JsonPath`.
- Produces: `class BuildImporter { ImportedBuild FromJson(string json); JsonNode? PickVessel(JsonNode raw); }`. `match.anyPathExists` on each vessel selects the source; no match → `ImportException`.

- [ ] **Step 1: Write the failing test**

```csharp
// tests/D4BuildForge.Import.Tests/BuildImporterTests.cs
using D4BuildForge.Import;
namespace D4BuildForge.Import.Tests;
public class BuildImporterTests
{
    static string Fixture(string f) => File.ReadAllText(Path.Combine("fixtures", f));

    [Fact] public void Detects_maxroll()
        => Assert.Equal(Contracts.BuildSource.Maxroll, new BuildImporter().FromJson(Fixture("maxroll_cataclysm.raw.json")).Source);

    [Fact] public void Detects_mobalytics()
        => Assert.Equal(Contracts.BuildSource.Mobalytics, new BuildImporter().FromJson(Fixture("mobalytics_zaior_landslide.lean.json")).Source);

    [Fact] public void Unknown_throws()
        => Assert.Throws<ImportException>(() => new BuildImporter().FromJson("{\"nothing\":true}"));
}
```

- [ ] **Step 2: Run to verify fail**

Run: `dotnet test tests/D4BuildForge.Import.Tests --filter BuildImporterTests`
Expected: FAIL — `BuildImporter` not defined.

- [ ] **Step 3: Implement**

```csharp
// src/D4BuildForge.Import/BuildImporter.cs
using System.Text.Json.Nodes;
using D4BuildForge.Import.Contracts;
using D4BuildForge.Import.Mapping;
using D4BuildForge.Import.Vessels;
namespace D4BuildForge.Import;
public sealed class BuildImporter
{
    readonly MappingEngine _engine = new();
    public ImportedBuild FromJson(string json)
    {
        var raw = JsonNode.Parse(json) ?? throw new ImportException("invalid json");
        var vessel = PickVessel(raw) ?? throw new ImportException("no matching source vessel");
        return _engine.Apply(vessel, raw);
    }
    public JsonNode? PickVessel(JsonNode raw) =>
        VesselStore.All().FirstOrDefault(v =>
            (v["match"]?["anyPathExists"] as JsonArray)?.Any(p =>
                JsonPath.Select(raw, (string)p!).Any(n => n is not null)) == true);
}
```

- [ ] **Step 4: Run to verify pass + commit**

Run: `dotnet test tests/D4BuildForge.Import.Tests --filter BuildImporterTests` → PASS (3).
```bash
git add src/D4BuildForge.Import/BuildImporter.cs tests/D4BuildForge.Import.Tests/BuildImporterTests.cs
git commit -m "feat(import): BuildImporter facade + source auto-detection"
```

---

### Task 10: MaxrollFetcher (automatic acquisition — priority 2)

**Files:**
- Create: `src/D4BuildForge.Import/Fetching/IBuildFetcher.cs`
- Create: `src/D4BuildForge.Import/Fetching/MaxrollFetcher.cs`
- Modify: `src/D4BuildForge.Import/BuildImporter.cs` (add `FromMaxrollUrlAsync`)
- Create: `tests/D4BuildForge.Import.Tests/fixtures/maxroll_cataclysm_guide.html` (re-fetch via curl)
- Test: `tests/D4BuildForge.Import.Tests/Fetching/MaxrollFetcherTests.cs`

**Interfaces:**
- Produces: `interface IBuildFetcher { Task<string> FetchJsonAsync(string url, CancellationToken ct=default); }`; `class MaxrollFetcher(HttpClient http) : IBuildFetcher` — GETs the URL, extracts the embedded `"plannerProfile"` object via brace-matching, returns it as JSON. `BuildImporter.FromMaxrollUrlAsync(url, fetcher)`.

- [ ] **Step 1: Re-capture the HTML fixture**

```bash
curl -sS -A "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 Chrome/124.0 Safari/537.36" \
  -o tests/D4BuildForge.Import.Tests/fixtures/maxroll_cataclysm_guide.html \
  https://maxroll.gg/d4/build-guides/cataclysm-druid-build-guide
```
Add to test csproj `None Include` glob already covers `fixtures/*` — extend the glob to `fixtures/*.*` if needed so the `.html` copies to output.

- [ ] **Step 2: Write the failing test (offline — no live network)**

```csharp
// tests/D4BuildForge.Import.Tests/Fetching/MaxrollFetcherTests.cs
using System.Net;
using System.Text;
using D4BuildForge.Import.Fetching;
namespace D4BuildForge.Import.Tests.Fetching;
public class MaxrollFetcherTests
{
    sealed class StubHandler(string body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken c)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){ Content = new StringContent(body, Encoding.UTF8, "text/html") });
    }

    [Fact]
    public async Task Extracts_plannerProfile_from_guide_html()
    {
        var html = await File.ReadAllTextAsync(Path.Combine("fixtures", "maxroll_cataclysm_guide.html"));
        var http = new HttpClient(new StubHandler(html));
        var json = await new MaxrollFetcher(http).FetchJsonAsync("https://maxroll.gg/d4/build-guides/cataclysm-druid-build-guide");
        var node = System.Text.Json.Nodes.JsonNode.Parse(json)!;
        Assert.NotNull(node["data"]?["profiles"]);              // it's a plannerProfile
    }
}
```

- [ ] **Step 3: Run to verify fail**

Run: `dotnet test tests/D4BuildForge.Import.Tests --filter MaxrollFetcherTests`
Expected: FAIL — `MaxrollFetcher` not defined.

- [ ] **Step 4: Implement the fetcher (brace-match extraction)**

```csharp
// src/D4BuildForge.Import/Fetching/IBuildFetcher.cs
namespace D4BuildForge.Import.Fetching;
public interface IBuildFetcher { Task<string> FetchJsonAsync(string url, CancellationToken ct = default); }
```
```csharp
// src/D4BuildForge.Import/Fetching/MaxrollFetcher.cs
namespace D4BuildForge.Import.Fetching;
public sealed class MaxrollFetcher(HttpClient http) : IBuildFetcher
{
    public async Task<string> FetchJsonAsync(string url, CancellationToken ct = default)
    {
        var html = await http.GetStringAsync(url, ct);
        return ExtractPlannerProfile(html);
    }
    public static string ExtractPlannerProfile(string html)
    {
        const string key = "\"plannerProfile\"";
        var i = html.IndexOf(key, StringComparison.Ordinal);
        if (i < 0) throw new ImportException("plannerProfile not found in page");
        var start = html.IndexOf('{', html.IndexOf(':', i));
        int depth = 0; bool instr = false, esc = false;
        for (var j = start; j < html.Length; j++)
        {
            var c = html[j];
            if (esc) { esc = false; continue; }
            if (c == '\\') { esc = true; continue; }
            if (c == '"') { instr = !instr; continue; }
            if (instr) continue;
            if (c == '{') depth++;
            else if (c == '}' && --depth == 0) return html.Substring(start, j - start + 1);
        }
        throw new ImportException("unterminated plannerProfile object");
    }
}
```
Add to `BuildImporter`:
```csharp
    public async Task<ImportedBuild> FromMaxrollUrlAsync(string url, Fetching.IBuildFetcher fetcher, CancellationToken ct = default)
        => FromJson(await fetcher.FetchJsonAsync(url, ct));
```

- [ ] **Step 5: Run to verify pass**

Run: `dotnet test tests/D4BuildForge.Import.Tests --filter MaxrollFetcherTests`
Expected: PASS.

- [ ] **Step 6: Full suite + commit**

Run: `dotnet test tests/D4BuildForge.Import.Tests`
Expected: ALL PASS.
```bash
git add src/D4BuildForge.Import/Fetching src/D4BuildForge.Import/BuildImporter.cs tests/D4BuildForge.Import.Tests/Fetching tests/D4BuildForge.Import.Tests/fixtures/maxroll_cataclysm_guide.html tests/D4BuildForge.Import.Tests/D4BuildForge.Import.Tests.csproj
git commit -m "feat(import): MaxrollFetcher (URL -> plannerProfile json) + FromMaxrollUrlAsync"
```

---

## Self-Review

**Spec coverage:** IR contract (T2) ✓ · MappingEngine + JSONPath subset (T3,T5) ✓ · Transform registry/bricks (T4) ✓ · Mapping Vessels as data (T6) ✓ · BuildImporter + auto-detect (T9) ✓ · MaxrollFetcher behind IBuildFetcher (T10) ✓ · faithful-or-warn (`Warnings` const in vessel, asserted T8; `ImportException` T4/T9) ✓ · both golden builds full-fidelity (T7,T8) ✓ · pure library / IO behind interface ✓. Out-of-scope items (catalog, ref resolution, engine Build, DPS, Mobalytics auto-fetch) intentionally absent.

**Placeholder scan:** every code/test step contains complete code; no TBD/"similar to". The two "iterate the vessel until green" steps (T7/T8) are deliberate TDD-against-real-data loops, with the exact target assertions supplied and the data files already validated by `scratchpad/ir/normalize.py`.

**Type consistency:** `ExternalRef(Source,Kind,Id,Name?)`, `SkillRank(Ref,Rank)`, `ImportedAffix(Ref,Values,Greater,Masterwork)`, `ParagonBoard(Ref,Nodes,Rotation,Position,Glyph,GlyphLevel)`, `MappingEngine.Apply(JsonNode,JsonNode)`, `Transforms.Run(TransformCtx,JsonNode?,IEnumerable<string>)`, `VesselStore.Load/All`, `BuildImporter.FromJson/FromMaxrollUrlAsync`, `IBuildFetcher.FetchJsonAsync` — consistent across tasks.
