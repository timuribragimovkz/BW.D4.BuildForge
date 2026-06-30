# D4 Build Forge — Build Import Design (`D4BuildForge.Import`)

**Date:** 2026-06-30
**Status:** Approved (brainstorming → spec)
**Author:** Timur + Claude
**Related:** [engine design](2026-06-30-d4-build-forge-engine-design.md) · [import-source reference](../../reference/build-import-sources.md)

Import third-party D4 builds (Maxroll, Mobalytics) into one **source-agnostic internal contract** (the IR).
Mapping is **data-driven** via *Mapping Vessels* so a source's shape change is a config edit, not a redeploy.

## 1. Priority & scope (this spec)

**Priority order (explicit):**
1. **Build-proper import comes first** — faithful, *complete* capture: every variant, all mechanical data
   (gear + affixes/implicits/tempers/aspects/sockets, skill ranks, skill bar where available, full paragon
   boards/nodes/glyphs, mercenary, spirit boons). Correctness over everything.
2. **As automatic as feasible** — Maxroll is `curl`-able, so fetch-by-URL for Maxroll. Hand-loading JSON is
   an accepted input path (esp. Mobalytics, which is browser-only). "Hand load if we have to."
3. **No QOL** — no CLI polish, web UI, persistence niceties, or convenience wrappers until import-proper is solid.

**In scope:** parse raw source JSON → `ImportedBuild` IR, driven by per-source Mapping Vessels interpreted by
a `MappingEngine`; a Maxroll URL fetcher; TDD against two captured real builds as golden fixtures.

**Out of scope (later, separate specs):** the game-content catalog; resolving `ExternalRef`s (numeric
`nid`/node-idx/slugs) to our content rows; assembling an engine `Build`; computing DPS; Mobalytics
auto-fetch (browser automation); any UI/CLI/persistence.

## 2. The IR contract (source-agnostic internal build)

Pure records in `D4BuildForge.Import.Contracts` (no engine/storage/web/AWS deps; mirrors `Engine` purity).
Reference draft: `ImportedBuild.contracts.cs` (validated 1:1 against the normalized fixtures).

```
ImportedBuild { SchemaVersion, Source, SourceId, SourceUrl, Clazz, Name, CapturedAtUtc, Variants[], Warnings[] }
BuildVariant  { Name, Level?, WorldTier?, Gear[], SkillRanks[], SkillBar?, Paragon, Mercenary?, ClassExtras[] }
GearItem      { Slot, RawSlot?, Ref, Rarity?, ItemKind?, Mythic, Implicits[], Explicits[], Tempers[], Aspects[], Sockets[] }
ImportedAffix { Ref, Values:double[]?, Greater, Masterwork }   // Values null when source omits rolls
ParagonBoard  { Ref, Nodes[]{Ref,Rank}, Rotation?, Position?, Glyph?, GlyphLevel? }
ImportedMercenary { Primary?, Reinforcement?, Skill?, Opportunity?, SkillTree[]{Ref,Rank?} }
ClassExtra    { Group, Ref, Animal? }                          // generic: Druid spirit boons today
ExternalRef   { Source, Kind, Id, Name? }                      // identifiers kept VERBATIM
```

**Rules:** identifiers are preserved verbatim in `ExternalRef` (no game-knowledge resolution here);
source-missing fields are nullable and the reason is appended to `Warnings`.

## 3. Architecture

New pure project `src/D4BuildForge.Import/` (net10.0) + `tests/D4BuildForge.Import.Tests/` (xUnit).
The only IO lives behind one interface (`IBuildFetcher`); the mapping core is pure and offline-testable.

```
raw JSON ──▶ MappingEngine.Apply(vessel, raw) ──▶ ImportedBuild
                  ▲                  ▲
            Mapping Vessel     ITransform registry
          (data, per-source)   (small code bricks)
```

Components, each independent and single-purpose:
- **`Contracts/`** — the IR records above.
- **`Vessels/`** — the Mapping Vessel model + loader (`maxroll.vessel.json`, `mobalytics.vessel.json` ship
  as embedded resources for v1; later swappable from a config-vessel store).
- **`Mapping/MappingEngine`** — interprets a vessel against a parsed `JsonNode`, walking named binding blocks,
  resolving a JSONPath subset (`$`, `.key`, `[*]`, `[n]`), applying transform pipelines, producing an `ImportedBuild`.
- **`Mapping/Transforms/`** — `ITransform` registry; v1 vocabulary (see §5).
- **`BuildImporter`** — façade: `FromJson(json)` (auto-detects source via each vessel's `match`),
  `FromMaxrollUrl(url)` (fetch → parse → map).
- **`Fetching/MaxrollFetcher : IBuildFetcher`** — `HttpClient` GET of a Maxroll guide/planner URL → extract
  embedded `plannerProfile` (brace-match) → JSON. Behind the interface so the core has no network in tests.

## 4. Source reconciliation (normalized to one shape)

| Concept | Maxroll | Mobalytics |
|---|---|---|
| Root | `$.data` (`plannerProfile.data`) | `$` (cleaned doc) |
| Variants | `profiles[]` (Starter/Midgame/Endgame, with level+worldTier) | `buildVariants.values[]` (level/wt → null) |
| Gear link | `profile.items` `{slotIdx: poolKey}` → deref `data.items[poolKey]` | inline `slots[]` (entity is the item/aspect) |
| Slot | from item-id prefix (`Helm_…`) | from `slotSlug` (`chest-armor`) |
| Affix | `{nid, values, greater, upgrade}` → Values set | flags only (`gr`/`mw`) → Values null |
| Skill ranks | `skillTree.steps[].data` `{nodeId: rank}` | repeated slug count |
| Skill bar | `skillBar[]` | not separable → null + warning |
| Paragon | boards `{id, nodes{idx:rank}, rotation, position, glyph, glyphLevel}` | grid-coded node slugs grouped by prefix; no glyph/rotation |
| Class extras | `spiritBoons[]` ids | `boons[]{type, vals[]}` (carry animal) |

## 5. Mapping Vessels & Transform Registry

A vessel is a **data document** (config vessel — runtime-swappable; same philosophy as `BW.Libs.Config`
and the engine's "no hardcoded constants"). It declares how to build the IR from a source's JSON. Reference:
`mapping-vessels.md` + the two `*.vessel.json`.

Binding DSL field values: a `"$.path"` string; or `{ "from", "via":[transforms], "each":<binding|inline> }`;
or `{ "via":["const:…"] }`; or a `"@blockName"` reference; or `{ "$ref": { kind, id, name } }`. Context
tokens in `each`: `@self`, `@key`/`@value`, `@count`, `@members`/`@animal`.

**`ITransform`** — each an independent, unit-tested brick (the engine's modular-source contract again). v1 set
(everything the two real builds required): `lower`, `const`, `format`, `nullable`, `bool`, `tokenAt`,
`entries`, `mergeDicts`, `dropZero`, `derefItemPool`, `slotFromItemId`, `slotFromSlug`, `countRepeats`,
`groupByRegex`, `flattenBoons`. New source = author a vessel + (rarely) one new transform; `MappingEngine`
and the IR never change.

## 6. Validation (TDD)

Golden fixtures = the two captured real builds, committed under `tests/.../fixtures/`:
- `maxroll_cataclysm.raw.json` (Cataclysm Druid) · `mobalytics_zaior_landslide.lean.json` (Zaior Landslide).
- Expected IR = the validated `*.normalized.json` for each.

Test bar (write failing first):
1. **Parity** — both sources produce identical IR shape (top-level, variant, gear-item keys).
2. **Maxroll fidelity** — 3 variants; levels 70; Endgame 16 gear slots & 25 paragon boards; helm = mythic
   `Helm_Unique_Druid_102`; affix `Values` present; glyph + glyphLevel per board; skill ranks from `{id:rank}`.
3. **Mobalytics fidelity** — 3 variants; level/wt null; End-Game 20 slots & 5 grouped boards; affix `Values`
   null with greater/masterwork flags; skill ranks via repeat-count; `SkillBar` null with the documented warning.
4. **Transforms** — each transform brick unit-tested in isolation.
5. **Fetcher** — `MaxrollFetcher` against a saved HTML fixture via an injected `HttpMessageHandler` (no live network).

## 7. Conventions

- net10.0, C#, xUnit; `System.Text.Json` (`JsonNode`), camelCase to match fixtures.
- `Import` is a pure library (no AWS/web/engine deps); IO only behind `IBuildFetcher`.
- Vessels are data; mapping logic is data + a registry of small transform bricks — no per-source code in the core.
- Faithful-or-warn: never silently drop data; unknown fields/slots/blocks → `Warnings`, structurally-invalid input → `ImportException`.

## 8. Risks & open questions

- **Mobalytics persistence/acquisition** — 403 to curl + browser auto-download throttling make automated pull
  hard; v1 accepts hand-supplied JSON for Mobalytics (auto-fetch deferred).
- **Mobalytics skill bar** — not separable from skill-tree assignments; recorded as a known gap (warning).
- **Mobalytics glyphs/rotation** — not present in the build payload we capture; paragon imports nodes only.
- **Slot tables / item-id prefixes** — weapon/offhand prefixes vary by class; the vessel `slotTable` is data and
  extended as new classes/builds appear (Druid covered now).
- **JSONPath subset** — keep minimal (`$ . [*] [n]`); add operators only when a real vessel needs one.
