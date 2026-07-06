# Session Handoff — Mac HQ → PC (2026-07-06)

Companion to `/CLAUDE.md` (the operating context — read that FIRST). This file is the session record:
how we got here, decisions with their *why*, and everything parked/in-flight. Written by the Mac session
that built Plans 1–2 and validated the engine against the live game.

## PC SETUP — COMPLETE (2026-07-06, first PC session). Machine state, do not redo.
Everything in `docs/PC-SETUP.md` is done and verified on the gaming rig:
- **Repos**: all three cloned side-by-side in `C:\sources\` (this repo at `e2dd71a`+).
- **Auth**: SSH key registered with GitHub (account `timuribragimovkz`), `gh` CLI logged in, git protocol SSH.
- **Toolchain**: .NET SDK 10.0.301 installed (9.x also present). `dotnet test` = **74/74 green**
  (Engine 35, Domain 8, Import 26, Assembly 5) — golden fixtures port cleanly to this machine.
- **Secrets**: `LINEAR_API_KEY` (raw `lin_api_…`) lives in `~/.claude/settings.json` under `env` —
  available as an env var in every Claude Code session on this PC. AWS creds NOT set up yet (per manual).
- **Permissions**: `~/.claude/settings.json` has `permissions.defaultMode = bypassPermissions` —
  new sessions don't prompt (one-time confirm dialog on first launch).
- **Screenshot pipeline (VERIFIED end-to-end on a live game capture)**: AutoHotkey v2 runs
  `tools/capture/hotkeys.ahk` (auto-starts via Startup-folder shortcut, also running now):
  - **Right Shift + F12** → `captures\inbox\` (gear/stat panels)
  - **Right Shift + F11** → `captures\hits\` (dummy damage moments)
  - `tools/capture/shot.ps1` does the capture; it is **DPI-aware** (SetProcessDPIAware) because the
    monitor is **3840×2160 at 150% Windows scaling** — without it, captures clip to 2560×1440 (bug
    already hit and fixed; do not remove the DPI call). Exclusive fullscreen captures fine — no
    windowed mode needed. Output: timestamped PNGs, full 4K frame.
- **First input already in inbox**: `captures/inbox/shot-20260706-152457-282.png` — equipped
  **The Basilisk** (Ancestral Unique Staff, 900 IP, 25 quality): 4,428 DPS, [3,737–5,119]/hit,
  1.00 APS, +283 Weapon Damage, +5,145 Max Life, x40% Crit Damage Mult, x23% Lightning Mult,
  +12.5% Crit Chance, unique Petrify effect (86%[+] crit-chance-taken / 69%[x] damage-taken),
  2× socketed x18% All Damage Mult. **Tooltip has a Scroll-Down fold** — ask the user for a
  scrolled second shot before locking this item's model.
- **Next task (this is where you pick up)**: item-by-item build recreation per §In-flight below —
  user screenshots each remaining equipped item; extract affixes (bucket by printed [+]/[x]),
  wire scenario rows, predict full-build Claw tooltip + hit, verify at the training dummy.

## Where the project stands (one paragraph)
The pure-C# engine (`D4BuildForge.Engine`) reproduces the game's damage math and has been validated against
the live game **twice**: S13 lvl-1 Maul (predicted 6.48 vs dummy avg 6.5) and S14 lvl-70 Claw (tooltip 2953
predicted EXACTLY; crit avg within ~1%). The tooltip→hit ×0.2 factor, /800 Willpower divisor, off-hand
weapon-damage summing, and rank scaling are all game-confirmed. Current phase: **item-by-item recreation of
the user's real lvl-70 S14 Druid build (full 850 unique/aspect gear) from screenshots, on the Windows PC.**

## Decision log (chronological, with why)
1. **PoB-for-D4, engine-first** — the calc framework is the product; UI is a shell. TDD throughout.
2. **Pivoted Dart/Flutter/macOS → .NET + web** (user: "do it in .net ffs"). Flutter is DEAD — ignore any
   stale references (old Linear card D4F-4 mentioned Flutter; that scope is stale).
3. **Ava's Excel = reference model** (`docs/reference/ava-damage-optimization.xlsx`); its worked numbers are
   golden fixture #1 (OffenseCalculatorTests). Web guides (Maxroll etc.) are DIAGNOSIS-ONLY on mismatch —
   this rule already paid off: Maxroll said "no 0.2 scalar"; the live game proved the 0.2 IS real.
4. **Max-modularity contract** — engine knows nothing of classes/items; only orchestrators assemble; no
   `switch(class)` ever. Class differences = data + IModifierSources + Tags.
5. **Storage = table-per-concept** (Items, Buffs, ClassesAndStats, Skills, Passives, Paragon, Conditions…),
   NOT single-table. `Assembly` is "the LEGO" that snaps bricks into a Build.
6. **Season seam** — `Season` → `ISeasonConfigProvider` → `FormulaConfig`. In-memory now (s13 preset);
   later a DynamoDB-vessel provider with **seasonId in the PK**; new season = PTR-test then clone-and-tweak
   the prior season's vessel. s14 currently = s13 values (validated live; register the id when touched).
7. **Infra steer** — REAL AWS DynamoDB (no Docker-local), compute local, NO ALB/hosting yet. Account
   560719246675 (profile `Bruceware_Admin`), eu-central-1, tables to be `d4bf_*`-prefixed. Future domain:
   `d4.buildforge.bruceware.com`. Infra-sharing with Bruceware is deliberate; code stays separate.
8. **Dev moved to the Windows PC** — game+screenshots+dev colocated; Claude reads item screenshots natively
   (no OCR for tooltips). This Mac session = HQ/fallback for specs, storage plan, Linear.
9. **Repo**: private GitHub `timuribragimovkz/BW.D4.BuildForge`; local branch renamed master→**main**.

## Game-truth discoveries (hard-won, do not re-derive)
- **×0.2 tooltip→hit** is real. Tooltip = SkillCoeff × WeaponAvg × (1+Σadd) × (1+Will/800); hit = ×0.2.
- **Off-hand weapon damage SUMS into main hand** (S13 test: mace avg 20 + totem avg 20 = 40).
- **Maul base 0.80 / Claw base 0.60**; rank formula `base×(1+0.10(R−⌊R/5⌋−1)+0.15⌊R/5⌋)` matches wiki ranks.
- **S14:** Key Passives REMOVED; Werebear/Werewolf skills usable in either form (Spirit Boons = form system);
  Paragon = max 5 boards + Legendary Glyphs (lvl 45+); the S14 formula rewrite was Barbarian-only.
- **Overpower is a STACKING mechanic in S14 — there are NO blue overpower numbers.** On-screen hits are
  WHITE (non-crit) and YELLOW (crit) only. (User corrected this twice; do not regress.)
- **Bucket by the printed bracket:** some S14 "Damage to [state]" lines are `[x]` multiplicative, not `[+]`.

## In-flight / next steps (PC session owns these)
1. **Item-by-item build recreation** — user screenshots each equipped item → `captures/inbox/` → extract
   affixes (bucket by [+]/[x]) → extend the scenario/fixture set until the FULL build's tooltip + dummy hits
   are predicted. Then the proof: **swap one item (same powers, different rolls) → blind-predict → match.**
2. **Crit needs more samples** — base ×1.5 validated (~1%), but with CritDamage% gear it must be re-verified.
3. **Rename `MaulScenario` → `SkillScenario`** (fields are Maul-legacy; it already serves Claw) on next touch.
4. **Capture tool (optional)** — spec agreed for `tools/capture/`: Python, mss/ffmpeg + EasyOCR, HSV
   classify white/yellow, dedupe persistent floating numbers, CSV out, hand-counted reference clip as its
   regression fixture. Build only if bulk hit-sampling becomes worth it.

## Parked at Mac HQ (don't duplicate on PC)
- **Storage plan** (real-DDB vessels + BuildAssembler-from-DB) — next Mac-side spec→plan cycle when called.
- **Linear team D4F** exists (multi-Commander claim-before-touch protocol, card D4F-1; Alpha owns the domain
  contract D4F-3; D4F-4/5 scopes partly stale re Flutter). API key in `~/.claude/settings.json`, raw key in
  Authorization header (no "Bearer"). Only relevant when multiple Commanders run again.
- **Mac auto-memory** holds the same facts as this file + Bruceware context; it does not sync to the PC.

## Deferred engineering debt (from whole-branch reviews; none blocking)
- `CalcPipeline` validates Reads but not Writes — add before the Mitigation-stage plan lands.
- Consolidate duplicated `FixedSource` test helper (3 test files).
- `ShapeshiftForm.Form`/`SimpleItem.Name` unreferenced until breakdown-provenance/UI.
- `ModOp.Multiplicative` reserved/unused — model expresses [x] as single-member buckets; comment it.
- A `1.5`-ambiguity comment in `CalculateBySeasonTests` (additive 1.5 vs crit 1.5) — clarify to `(1+0.50)`.

## Roadmap (user's vision, in order)
Full-build recreation → one-item-swap blind proof → Druid item test-list → more skills → Mitigation stage
(Milestone 2) → defence/life/movement → storage (d4bf_* vessels) → web UI → **AoE visualizer widget** →
**specially-trained AI recommendations** → publish behind an **email gate**, seed to YouTube creators.
