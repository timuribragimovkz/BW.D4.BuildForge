# D4 Forge Admin — Vessel-Driven Render Engine + Test-vs-Math Flow

> **Status:** approved design (2026-06-30). Supersedes the UI/storage portions of
> `2026-06-30-d4-build-forge-engine-design.md` (the "game content in per-concept tables /
> bespoke screens" sketch). The Engine design's calc model is unchanged.

## 1. Goal

Stand up the **foundation of the real Admin UI** for D4 Build Forge and the **build-testing-vs-our-math**
flow, on the operating principle the user set:

> **"All we do is edit Vessels and then they render in the UI."**

Concretely, this slice delivers:

1. An **item generator** capable of producing **every item in D4** — not as a bespoke form, but as the
   schemaless render engine pointed at the `items` collection.
2. A **build editor** — the same render engine pointed at a `build` record.
3. A **test-vs-math** screen — assemble a stored build, run it through the existing `Engine`, and compare the
   predicted hit + bucket breakdown against the **live game** number the user reads off a training dummy.
   Each match (≤1%) becomes a golden fixture.

It is **proper, not pretty**: robust and correct, minimal QoL. Flutter web app + .NET backend for
everything; DynamoDB for persistence (DynamoDB Local in dev, cloud later — costs accepted by the owner).

This is a **personal project**, separate from Bruceware (honors project-separation). It reuses Bruceware
*patterns and generic capabilities* (the BW.Libs.Config Vessel contract; the mr_laka render-engine pattern)
but **copies no Bruceware application code**.

## 2. The paradigm: Vessels + a schemaless render engine

### 2.1 The render engine (mr_laka pattern)

The render engine introspects a raw JSON value and renders an editable field per node — **no per-type form
code**. Reference implementation: `bruceware_grooming_admin/lib/engine/{field_kind,config_field,render_rules}.dart`
(itself the Dart port of `mr_laka/utils/configRenderEngine.ts`). The Builder **re-derives** these three small
files fresh inside the D4 Flutter app (project separation — reference template, not a copy):

- `field_kind.dart` — `FieldKind { string, number, boolean, object, array, nul }`; `fieldKind(v)`,
  `coerceScalar(kind, raw)`, `isLongString(v)` (>80 chars → multiline).
- `config_field.dart` — `ConfigField`: a recursive `StatefulWidget` that, given any JSON value, renders:
  scalar → typed `TextField`/`Switch`; object → labeled column of child `ConfigField`s; array → list of child
  `ConfigField`s with add/remove. Immutable rebuild on every edit (spread map / copy list). Honors
  `RenderRule` overrides.
- `render_rules.dart` — `RenderRule { defaultRule, enumRule(options), readonly }` + `ruleFor(key, rules)`.
  Lets a screen constrain specific keys (enum dropdown, read-only) without bespoke widgets.

A **manager screen** = list/filter on the left + `ConfigField` edit panel on the right + Save (mirrors the
grooming admin's `UserTierManagerScreen`). That is the entire UI vocabulary.

### 2.2 Two data shapes, one engine

The grooming admin already renders **both** true config Vessels **and** plain domain collections
(`user-tier-manager` is a collection of user records, not an `IVessel`) through the same `ConfigField`. D4
Forge uses the same split:

- **Game content = DynamoDB *collections* of records** — `items`, `skills`, `buffs`, `paragon`, `builds`.
  "Every item in D4" = the `items` collection, **one record per item**, listed/filtered/edited individually.
  (A single monolithic items vessel would not scale to thousands of items and is awkward to edit.)
- **Tuning = true BW.Libs.Config Vessels** — `FormulaConfigVessel`, season-keyed (Vessel PK carries
  `seasonId`), holding divisors / scalars / bucket-bases. Honors the Config-Vessels mandate. This is the
  vessel-backed implementation of the `ISeasonConfigProvider` seam already built on `feat/validation-harness`.

`RenderRule`s supply the few constraints that make the generic editor produce valid D4 data:
`slot`→enum, `rarity`→enum, `id`→readonly (server-assigned), numeric fields coerced via `fieldKind`.

## 3. Data contracts (frozen in slice 1 — the shared contract for both workers)

These JSON shapes are the only thing the Builder and the Build-stealer share. They are frozen first so the
two workers run in parallel with no live coupling. Field names are camelCase in JSON; the .NET records mirror
them.

### 3.1 Item record (`items` collection)

Derived from the richest real source shape (Maxroll planner JSON; see `docs/reference/build-import-sources.md`)
plus D4 taxonomy. General enough for every D4 item — normal/magic/rare/legendary/unique/mythic, all slots.

```jsonc
{
  "id": "item_<guid>",                 // server-assigned, readonly in UI
  "sourceId": "Pants_Legendary_Generic_053", // optional: original game/Maxroll id (provenance)
  "name": "Tibault's Will",
  "slot": "Pants",                     // enum: Helm|Chest|Gloves|Pants|Boots|Amulet|Ring|Weapon|Offhand|...
  "itemType": "Pants",                 // finer type when slot is generic (e.g. Weapon→Mace/Bow/...)
  "rarity": "Unique",                  // enum: Normal|Magic|Rare|Legendary|Unique|Mythic
  "itemPower": 800,
  "classRestriction": null,            // null = any; else "Druid" etc.
  "affixes": [                         // explicit + implicit + tempered, each tagged by kind
    { "kind": "explicit", "stat": "MainStat",   "value": 120, "isGreater": false },
    { "kind": "implicit", "stat": "Vulnerable", "value": 0.15 },
    { "kind": "tempered", "stat": "MaulRanks",  "value": 2 }
  ],
  "aspect": { "name": "...", "modifiers": [ /* same affix shape */ ] } | null, // legendary power / unique power
  "sockets": [ "Gem_Sapphire_04", null ]
}
```

`affix.stat` values draw from the engine's `StatChannel`/`BucketKey` vocabulary (e.g. `MainStat`,
`WeaponDamage`, `CritChance`, `AttackSpeed`, `Additive`, `AllDamage`, `CritDamage`, `Vulnerable`,
`<skill>Ranks`). The **affix-stat → engine-modifier mapping lives in `src/Assembly`** (§4), not in the record.

### 3.2 Build record (`builds` collection)

```jsonc
{
  "id": "build_<guid>",
  "name": "Druid Maul (live test #1)",
  "season": "s13",
  "class": "Druid",
  "level": 80,
  "skill": { "name": "Maul", "baseCoeff": 0.45, "ranks": 1 },
  "itemIds": [ "item_...", "item_..." ],   // refs into the items collection
  "activeState": [ "Werebear" ],           // condition tags (e.g. Werebear, Vulnerable)
  "target": { "level": 81, "armor": 0 },
  "expectedNonCrit": 510000                 // the live-game tooltip number, null until measured
}
```

### 3.3 Repository interfaces (`src/Storage`)

```csharp
public interface IRecordRepository {                 // collections (items, builds, skills, ...)
    Task<IReadOnlyList<JsonObject>> List(string collection);
    Task<JsonObject?> Get(string collection, string id);
    Task<JsonObject> Save(string collection, JsonObject record); // assigns id if absent
    Task Delete(string collection, string id);
}
```

Backed by **DynamoDB** (one table per collection, PK = `id`). `DynamoRecordRepository` in dev points at
DynamoDB Local (Docker); same code points at cloud later. Records are stored/served as raw JSON so the render
engine round-trips them losslessly. Tuning stays on the typed `IVessel` path (`FormulaConfigVessel`), unchanged.

## 4. `src/Assembly` — record → engine `Build`

The bridge from stored records to the proven `Engine`. Pure, unit-testable, no AWS/web deps.

`BuildAssembler.Assemble(buildRecord, items[], FormulaConfig) -> Engine.Build`:
- maps each item's `affixes`/`aspect.modifiers` to engine `Modifier`s via an **affix-stat dictionary**
  (`stat` string → `StatChannel` flat OR `BucketKey` damage mod; condition-tagged where relevant, e.g.
  `Vulnerable` carries the `Vulnerable` tag);
- adds `BaseStatsForLevel(level, ...)`, the `SkillSelection`, `activeState` tags, and `Target`;
- returns a `Build` consumed by the existing `BuildCalculator.Calculate(build, season)`.

This is the single place that knows how D4 data becomes engine input — the modularity contract from the
Engine spec (only the assembler assembles; no `switch(class)` in the engine).

## 5. `src/Api` — .NET backend

Minimal API, the only network surface. Endpoints:

- `GET  /data/{collection}` → `List` (for the manager list view)
- `GET  /data/{collection}/{id}` → `Get`
- `POST /data/{collection}` → `Save` (create/update; returns the record incl. assigned `id`)
- `DELETE /data/{collection}/{id}` → `Delete`
- `POST /calc` `{ buildId }` → load build + referenced items → `BuildAssembler` → `BuildCalculator.Calculate`
  → `{ nonCrit, crit, average, dps, breakdown: BreakdownFormatter.Format(...) }`

Reuses `BreakdownFormatter` from the validation-harness branch to render the breakdown text. No auth in this
slice (local tool); auth is a later Phase-1 concern.

## 6. Flutter app (`d4_forge_admin`)

Flutter **web**, talks to `src/Api` over HTTP. Screens (all = list + `ConfigField` + Save):

- **ItemsManager** — `items` collection. Create/edit any D4 item. `RenderRule`s: `slot`/`rarity`/`itemType`
  enums, `id` readonly. *This is the "item generator that produces every item in D4."*
- **BuildEditor** — a `build` record. Edits level/skill/itemIds/state/target/expectedNonCrit.
- **TestVsMath** — picks a build, `POST /calc`, shows predicted nonCrit/crit/avg + breakdown beside the
  build's `expectedNonCrit`; flags pass/fail at ≤1%. The breakdown is the diagnosis surface on a miss.

`ForgeApi` (Dart): `getData / getItem / save / delete / calc`. No QoL beyond what these need.

## 7. Build order & worker dispatch

| # | Slice | Owner | Depends on |
|---|---|---|---|
| 0 | Finish `feat/validation-harness` tasks 3–5 (`BreakdownFormatter`, `MaulScenario`, validation fixture), merge to `master` | quick (me / one worker) | in-flight branch |
| 1 | **Freeze contracts**: §3 item/build JSON shapes + `IRecordRepository` interface + `src/Domain` record types → land on `master` | me, from this spec | this spec |
| 2a | **Builder**: `src/Storage` (DynamoDB + Local), `src/Assembly` (`BuildAssembler` + affix dictionary), `src/Api` (CRUD + `/calc`), `d4_forge_admin` Flutter (render-engine port + 3 managers) | Worker A, own worktree off `master` | slice 1 |
| 2b | **Build-stealer**: Maxroll (curl Remix JSON) + Mobalytics (browser) importers → emit item/build records **in the §3 frozen shape** → persist via `POST /data/...` | Worker B, own worktree off `master` | slice 1 |

Freezing §3 in slice 1 is what makes 2a and 2b parallel. Each worker works in its **own git worktree** off
`master` (never branch-switch the shared checkout). Merges are owner-gated.

## 8. Testing

- **Engine**: unchanged, stays green (Ava golden fixture + validation fixtures).
- **`src/Assembly`**: xUnit — assert a hand-built item/build record assembles to the same `CalcResult` as the
  equivalent hand-fed `MaulScenario` (ties new path to the proven path).
- **`src/Api`**: integration test for each endpoint against DynamoDB Local (round-trip a record; `/calc` on a
  seeded build returns the expected breakdown).
- **`src/Storage`**: `DynamoRecordRepository` round-trip tests against DynamoDB Local.
- **Flutter**: widget tests for `ConfigField` (scalar/object/array render + edit + enum/readonly rules),
  mirroring the grooming admin's `field_kind_test`/`config_field_test`.

## 9. Out of scope (later)

**`FormulaConfigVessel` (vessel-backed tuning) is a fast-follow, NOT this slice.** `/calc` runs on the
existing `InMemorySeasonConfigProvider` (the `s13` Druid preset already on `feat/validation-harness`). §2.2
describes the eventual vessel-backed implementation of that same `ISeasonConfigProvider` seam; swapping the
provider is zero-caller-change and lands after the collections substrate is proven.

Also later: auth / multi-user; cloud deploy (stays DynamoDB Local until proven); the AoE visualizer widget; Mitigation
stage (Milestone 2, separate plan); paragon/skill-tree *editing* depth beyond the generic render (records
exist, rich editors later); bulk-import trained-model corpus. Classes beyond Druid for v1.

## 10. Open / confirmed decisions

- **Confirmed:** game content = collections, tuning = Vessels (not every item as an `IVessel` singleton).
- **Confirmed:** render engine re-derived fresh in the D4 app (no Bruceware app-code copy).
- **Confirmed:** Flutter **web**; DynamoDB Local in dev, cloud later (costs accepted).
- **To settle in the plan:** the initial affix-stat dictionary coverage (start with the stats Druid Maul needs;
  extend per-affix as item-by-item validation proceeds).
