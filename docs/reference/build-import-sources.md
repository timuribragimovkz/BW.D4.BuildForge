# Build Import Sources — Mobalytics & Maxroll

Reference for the future **Import/Ingestion** seam: where third-party D4 build sites keep their
structured build data, how to get it, and how their shapes map onto our `Domain` POCOs
(`Item`, `Affix`, `Skill`, `PassiveNode`, `ParagonNode`, `Glyph`, …).

Verified **2026-06-30** against a live Stormclaw Druid (Mobalytics) and a Lightning Storm Druid (Maxroll).
Captured sample payloads are not committed (large); regenerate with the recipes below.

> **Key insight:** the rendered *page text* is misleading — skill trees and paragon boards render as
> visual widgets/canvas, so a text scrape loses them. But both sites ship the **complete build as
> structured JSON** in the page. Target the JSON, never the DOM text.

---

## TL;DR — which source to build against

| | **Maxroll** | **Mobalytics** |
|---|---|---|
| Headless access | ✅ plain `curl` → **HTTP 200** | ❌ **403** (Cloudflare + SPA) — needs a real browser |
| Itemization detail | 🥇 **explicit affix lines** (`{nid, values}`) | flags only (`isGreater`/`isMasterwork`) — no rolled values |
| Identifiers | numeric game IDs → needs a resolver dictionary | human-readable slugs (self-describing) |
| Variants per build | `profiles[]` (Starter/Midgame/Endgame) | `buildVariants.values[]` (End-Game/Mid-Game) |

**Recommendation:** Maxroll is the **primary** import source — curl-able, and it carries the actual rolled
affix magnitudes the engine needs to validate against Ava's model. Cost: a one-time numeric-ID → name
dictionary (harvested from Maxroll's planner JS / game-data bundle). Mobalytics is **secondary** — nicer
human-readable slugs, but no rolled affix values and ingestion requires a headless browser.

---

## Maxroll (primary)

Build-guide pages are **Remix SSR** — the entire planner build is embedded in the HTML, no API call needed.

**Extraction recipe (headless, no browser):**
1. `curl -A "<a normal desktop UA>" https://maxroll.gg/d4/build-guides/<slug>` → HTTP 200.
2. Find `"plannerProfile":` in the HTML, brace-match the JSON object (respect string escapes), `json.loads`.
   - It lives at `window.__remixContext.state.loaderData["branch-posts"].post.gutenbergBlock[0].plannerProfile` (~207 KB).
   - A standalone planner also exists at `https://maxroll.gg/d4/planner/<plannerId>` (same schema).

**Shape (`plannerProfile.data`):**
- `profiles[]` — one per progression stage, each `{ name, level, worldTier, items, skillTree, skillBar, paragon }`.
- `items{}` — shared pool keyed by index. Each item → our `Item` + `Affix[]`:
  - `id` (e.g. `"Pants_Legendary_Generic_053"`), `power`, `name`
  - `explicits: [{ nid, values:[…] }]` — **affix ID + rolled magnitude** → `Affix`
  - `implicits`, `tempered`, `aspects`, `sockets: ["Gem_Sapphire_04", …]`
- `skillTree.steps[].data` — `{ nodeId: rank }` map → `PassiveNode` ranks (e.g. `416: 15`).
- `skillBar` — `["Druid_LightningStorm", …]` (the action bar) → `Skill[]`.
- `paragon.steps[].data[]` — per board: `{ id:"Paragon_Druid_00", nodes:{idx:1}, rotation, position, glyph:"Rare_040_Willpower_Main", glyphLevel }` → `ParagonNode` + `Glyph`.
- Plus `options` (build toggles), `globalNotes` (changelog/faq/intro/summary as rich text), `buildRating`, `strAndWeak`.

**Resolver requirement:** Maxroll uses numeric/string game IDs (`nid`, paragon node indices, `Item_*` ids,
`Gem_*`, glyph ids). The Import layer needs a game-data dictionary to map these → human names + our content
rows. Maxroll's planner ships this dictionary in its JS bundle; harvest it once and treat it as reference data.

---

## Mobalytics (secondary)

SPA — **headless `curl`/WebFetch returns 403** (Cloudflare bot protection). Must run in a real browser
context (a logged-out browser session is enough; we used Claude-in-Chrome).

**Where the data is:**
- Hydration cache: `window.__PRELOADED_STATE__.diablo4State.apollo.graphqlV2.queries[1].state.data[0].game.documents.userGeneratedDocumentBySlug` (TanStack Query dehydrated cache, ~329 KB).
- Or the GraphQL API it came from: `POST https://mobalytics.gg/api/diablo4/v4/graphql/query`,
  query key `["ngf-ug-featured-document-page", "builds", "<slug>", []]`.

**Shape:** a typed-block document (`doc.data.content`, ~51 blocks — `Diablo4DocumentUgWidgetSkillTreeV1`,
`…ParagonV1`, `…MercenaryV1`, `…AssignedSkillsV1`, `NgfDocumentUgWidgetBuilderV1` = gear). The real build
is in `doc.data.data.buildVariants.values[]`, one entry per variant:
- `genericBuilder.slots[]` — gear/aspects/uniques/charms/seals/chaosPerks/elixirs. Each slot's
  `gameEntity.modifiers` carries **flags only**: `gearStats:[{isGreater, isMasterwork}]`, `temperingStats`,
  `socketStats` (runes), `transfiguredStats`, `isMythic` — **no rolled affix values**.
- `skillTree.skills[]` — `{ slug, actionType }` (a slug repeated N times = N ranks; includes runes like `druid-rune-53`).
- `assignedSkills` — spirit boons (with full descriptions), summons/expertise/specialization/spirit guardians.
- `paragon.nodes[]` — grid-coded slugs (`druid-starting-board-x11-y14`).
- `mercenary`, `equipmentPriorityList`, `talismansPriorityList`.

**Upside:** slugs are human-readable (`harlequin-crest`, `gift-of-the-stag`), so less ID-resolution work.
**Downside:** gear lacks rolled affix magnitudes — only *which* affixes are greater/masterwork.

---

## Gotchas

- **Repeat browser downloads are silently blocked.** Chrome blocks a *second* automatic download from the
  same origin with no page-console error (an omnibox prompt). For bulk pulls use `curl` (Maxroll) or chunk
  the payload out of the page — don't rely on serial blob downloads.
- **Skill ranks are encoded two different ways** — Maxroll = `{nodeId: rank}` map; Mobalytics = repeated slug
  count. The Import layer normalizes both to `PassiveNode.Rank`.
- Both encode multiple variants per build; pick a canonical mapping (e.g. import each profile/variant as a
  separate `Build`).
