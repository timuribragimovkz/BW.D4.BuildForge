# D4 Build Forge — Pre-Spec TLDR (Product Ground Truth)

**Status:** Not a spec. The layer above every spec. Every feature proposal, implementation decision, and scope question gets checked against §4 (Laws) and §12 (Settled Decisions).
**For the agent:** If a task conflicts with this document, stop and flag it — don't silently comply. If an idea isn't covered here, ask the filter question in §12 before building.
**Date:** 2026-07-09. Market receipts verified via web research same day.

---

## 1. What the Forge is

- **Path of Building for Diablo 4.** A .NET engine that reproduces the game's damage math faithfully enough that a build's numbers are trusted *before* a single item is farmed.
- One-stop min-max and theorycraft: replaces Excel sheets, wiki archaeology, and 50-hour gear grinds that end in "this build was never going to work." That sentence is the pitch line.
- Core identity: **the game is the oracle.** The tool doesn't estimate — it matches, and regression fixtures keep it matched every season.
- Positioning sentence (use verbatim on the pricing page): **"The Forge sells direction, not destination."** It deletes the *wasted* game, not the game — the map is computed; the expedition, the farming, the temper gambles, and the polish stay yours. This is the pre-loaded answer to the inevitable "tools like this kill discovery" backlash.
- Audience triangle: **build authors** pay for depth, the **mass mid-core** pays for convenience, both stand on one engine. Meta-followers are served free — that's distribution, not revenue.
- Anti-piano manifesto: the Forge exists for proper, working, fun, quirky builds — not keyboard-piano one-second-window meta. Guide sites are structurally incentivized toward whatever clears Pit 150 regardless of playability; nobody's incentive is "comfortable and functional" except the tool that proves your quirky build works before you farm it. Later this becomes machine-readable (comfy-mode constraints, §6).

## 2. Current technical state (validated, not aspirational)

- **Engine:** modifier pool → bucket resolver → pipeline. Every source (affix, passive, glyph, gem, unique power) emits tagged modifiers; additive within a bucket, multiplicative across buckets; crit/vulnerable/conditions gated by toggles. No hardcoded game constants — all tuning is season-keyed data.
- **Validation receipts (these ARE the brand):**
  - S13 level-1 Maul: predicted 6.48/hit; live dummy averaged 6.5 → 0.3% off.
  - S14 level-70 Claw: predicted tooltip 2953 exactly; crit within ~1%.
  - Null case (1 item, 0 paragon, no uniques): 100% match — the control-group fixture.
- **Discoveries proven against wiki folklore:** the ×0.2 tooltip→hit factor is real; off-hand weapon damage sums into the main hand; variants replace base coefficients. Blizzard's own internal attribute name `Bucketed_Multiplicative_Damage_Type` confirms the resolver model is the game's model — independently arrived at, not curve-fitted.
- **Catalogs:** 26 Druid skills + variants; 9 paragon boards + 22 glyphs; 37 Druid uniques + 14 Iconic Mythics + the ×1.3 mythic transform; 124 all-class uniques; gems; 52 runes; 42 temper recipes; transfiguration. Destination: season-keyed vessels in DynamoDB.
- **Build importer:** any Maxroll guide URL → fully resolved build (rolled affix values, named skill variants, paragon nodes, glyph levels, war plans, mercenaries) with zero unresolved IDs. Purpose: personal time-saving + sound test inputs. NOT a scraping corpus (see §12).
- **Capture pipeline:** PC hotkey / hover-video capture → Claude vision reads real gear tooltips → DDB configs; a second recording pass lets Claude compare predicted vs live numbers to close the verification loop.

## 3. Three-layer architecture

- **Layer 1 — Per-hit oracle. Built, validated.** Deterministic, closed-form bucket resolver. Microseconds per evaluation. Foundation of the moat, but not the product ceiling.
- **Layer 2 — Time domain. Build early, lightweight first.** Cast frequency, proc income (rate × frequency × target count), cooldown timelines, resource loops, uptime weighting (boss vs trash honesty). Required for: attack-speed / CDR / resource stat weights, Cataclysm-refresh-class questions ("how long until the ultimate refreshes with the Herald ring at 1/2/N targets, and how much CDR makes it comfortable against a 16s duration"), rotation output, breakpoint solving. Start with a cast-frequency multiplier layer + expected-value proc math — stays closed-form and cheap. Full stochastic timeline sim only if EV math proves insufficient. **Pattern that recurred all session: every flagship feature quietly touches Layer 2. It is not a someday item.**
- **Layer 3 — Content translation.** Pit tier HP/timer curves are known data → sim DPS + fight model → **"this build ceilings at Pit ~108."** Converts abstract DPS into the player's language; the direct answer to "will I waste 50 hours on a build that can't do Pit 110." Also the shareable verdict object (§9).
- **Breakpoint solver** falls out of L2: inverse solve — "minimum CDR such that Cataclysm uptime ≥ 90% at 3 targets."
- **Honesty tags are architectural:** any stat or mechanic not yet modeled renders visibly as "not yet modeled" — never a silent zero. Attack speed under pure L1 math is the canonical example. Silent wrongness is how tools die in public; one confidently wrong stat weight on Reddit costs more trust than the feature earned.

## 4. Product laws (non-negotiable)

1. **Paywall computation and personalization. Never information.** Information leaks at the speed of one screenshot. A screenshot of MY stat weights is useless to YOU — personalization is the DRM.
2. **Give away the answers to questions everyone asks; charge for the answers to questions only you asked.** The global season-optimal builds get computed once and published FREE — they're the marketing cannon (every repost carries the watermark) and the warm-start cache seeding every paid personalized search. "Best build given MY stash / MY mythic / MY constraints" is the paid product.
3. **Never fake scarcity.** The engine answers in microseconds; an artificial queue is a `Task.Delay` in a trenchcoat and this audience will catch it and post it. Gate real cost (search depth, OCR quota, LLM passes) and real depth — never latency theater. Queue-skip becomes an honest lever only if L2 ever goes genuinely Monte Carlo.
4. **The engine never ships.** Server-side only, at every tier. Client code is decompilable by definition (.NET IL doubly so; obfuscation is a speed bump measured in hours). The only uncrackable DRM is code that never leaves the server. D4 is online-only, so the "offline mode" objection is void. Coefficients CAN be black-boxed through the free API — accept it: they were extracted from a public oracle and melt to zero every season anyway. **Protect the refresh loop (capture pipeline, fixture library, tester workflow, week-1 turnaround, trust) — never the snapshot.**
5. **Accuracy is the brand; trust and price are the same curve.** Every published dummy-match receipt is price support. Never blur confidence classes in the UI: engine math = exact; drop rates / community data = estimated. Label them as different species ("gain: exact · odds: estimated").
6. **The physics never learns from people.** Users cannot teach the engine correctness — letting them try corrupts the oracle. What the crowd teaches is **playability**: which optimizer outputs get adopted vs bounced, tolerated button counts, refused "optimal" builds. Crowd = preferences + fixtures. Engine = truth. Never blur.
7. **The free tier is the habit factory, not charity.** D4 has no combat log → no parse culture → no "sim it" reflex. The habit must be manufactured before monetization means anything (budget 12–18 months). Raidbots monetized a pre-existing, socially enforced habit; the Forge must create one first.
8. **Season-pass billing, not monthly.** Demand pulses with seasons; fighting the pulse loses. No cancel rituals, no dark patterns — the pass simply ends and users rebuy at next launch spike, which is exactly when the Forge is strongest (week-1 data while incumbents lag).

## 5. The monetization spine: two categories of sold CPU time

The whole business is two products. Both are personalized computation → unleakable by construction → honestly priced against real compute and real depth.

- **A. "Optimize what I have."** Search relative to the user's actual position in build space:
  - Personal optimizer over their exact rolls, stash, mythic, class, constraints.
  - Droptimizer: marginal value of every unique in the season catalog vs *their* gear.
  - Full reforge with paragon pathing.
  - Stat gradient: ∂DPS/∂stat evaluated at *their* gear vector. Key insight: **stat value is a property of saturation, not of the stat** — "is attack speed good for Bloodwave" has no true general answer, which is why the Discord answer is permanently wrong and the Forge's is exact per player.
- **B. "What is best done next."** Directive computation over the user's state and time budget:
  - Drop verdict (equip / salvage / temper-first) at the moment of looting.
  - Temper advisor *before* the mats are gambled (loss aversion felt in in-game currency) and enchant/reroll advisor at the Occultist.
  - Patch-day auto-rescore of saved builds: "your Bloodwave lost 9%, the fix is this glyph" — computed within minutes of patch notes.
  - EV-per-hour farm routing: **P(drop) × P(usable roll) × marginal DPS (from droptimizer) ÷ minutes per run** → "you have 2 hours: 6 Corrupted Reaper runs (EV +4.1%), then temper the gloves (+2.3%)." Owns the pre-session open. Requires season loot tables (§8) and confidence labeling per Law 5.
- A answers "what's my number / what should I build." B answers "what do I do tonight." **Depth of A drives willingness to pay; frequency of B drives retention** — a synced casual touches the Forge 40 times a night; a theorycrafter opens it weekly.

## 6. Flagship: "Forge It" — the global optimizer

- **Why only this engine can do it:** Monte Carlo sims (SimC) burn seconds per configuration — which is why Raidbots' Top Gear only *compares* a handful of combos and nobody has ever done global search in WoW. Closed-form L1 evaluates in microseconds → millions of candidates per minute per core → global search is feasible for the first time in the genre. This is what deterministic bucket math was secretly for.
- **CS honesty:** the raw space exceeds 10^12; paragon pathing under a point budget is prize-collecting-Steiner-tree territory (NP-hard). Never brute force. Exploit structure:
  - Skill-lock collapses the space enormously.
  - Additive-within-bucket gives analytic upper bounds → branch-and-bound pruning with proofs.
  - Multiplicative-across-buckets gives a "balance the starving bucket" gradient to follow.
  - Caps (armor, resists, life floors) become constraints → maximize DPS subject to survivability — what pros do by feel, done exhaustively.
  - Multi-seed local search / annealing (swap → microsecond recalc), cached per-season paragon sub-solutions.
- **Claim discipline:** never "the global optimum." The shippable claim: **"better than any build a human has published — proven on the dummy."** Falsifiable, survivable, devastating.
- **Constraint vocabulary / comfy mode:** ≤N active skills, movement skill mandatory, no 1-second windows, HC survivability floors — the anti-piano manifesto as machine-readable input, fed by the playability corpus (Law 6). Constraints PRUNE the search: they make queries *cheaper*. Therefore constraints are free query modes, never tier gates.
- **LLM demoted to explainer.** Classical search + engine does the *finding*. Claude does the *explaining* ("this ring wins because your multiplicative bucket was starving") and parses natural-language constraints. The LLM is never the brain and never in the calculation path.
- **Launch weapon:** run the optimizer against the published meta (Rax ~400k subs, wudijo ~200k, cliptis ~100k). Both outcomes win: finds 3–7% left on the table → launch content with dummy footage; matches within 1% → engine validated *by agreement*, and the optimizer's uncontested territory becomes the other 21 skills — nobody has ever optimized Boulder; superhuman by default; the quirky-builds manifesto made executable.
- **Synthetic corpus:** optimizer explores, engine scores, keep the Pareto frontier → a scored-builds dataset nobody without the engine can produce. Feeds recommendations and any future AI features. Never scrape guide sites for this.

## 7. Casual wing (the convenience surface)

- **Companion client:** lightweight, capture → OCR → upload ONLY. Contains zero secrets → **open-source it and code-sign it.** Solves the "install a random .exe" trust problem and lets the community verify the claim: OBS-class screen capture only, no memory reading, no injection, ever.
- **OCR economics:** D4 tooltips are fixed-font rendered text → local OCR / template matching handles the ~90% case for free; cloud vision is the ambiguity fallback only. This collapses the product's single largest marginal cost. The internal Claude-vision pipeline is fine for HQ use, ruinous at consumer scale.
- **Console players** (a huge slice of D4) can't run a client: manual web gear entry stays first-class forever.
- **The flow:** hotkey on the gaming PC → verdict pushed to the web app on the phone / second screen. Capture where the game is; answer where the eyes are.
- **Micro-moments** (all category B): drop verdict, temper advisor, enchant advisor — every loot event is a touch.
- **Percentile:** aggregate stored gear → "your Bloodwave is 68th percentile among Forge Bloodwaves; these two swaps separate you from median." A leaderboard-of-one — no shame, pure direction. The stored-gear database is also the playability corpus: a strategic asset no competitor can have.

## 8. Season data ops (the operational moat)

- **PTR = bulk capture week.** The PTR vendor hands out Mythics, boosts, ~99B gold, and all mats → one person cycles an entire class catalog through the capture pipeline in days; item acquisition cost ≈ 0. (Reference: S14 PTR ran June 2–9; season launched June 30.) On live, "equip this one new unique" means farming an RNG drop — testers become farmers and cost explodes. PTR kills that.
- **Launch week = diff pass, not rebuild.** Blizzard changes numbers between PTR and live (S14 changed affix rules, slot pools, and max-roll guarantees post-PTR). The patch notes ARE the work order. Target: **provisional data day 1, dummy-verified day 3–4.** Incumbents lag 2–3 weeks — that window is both the season traffic spike and the entire competitive identity.
- **Testers = capture-hours, not farm-hours.** 3–5 gig testers × 20–30 hrs during PTR week ≈ $1.5–3k/season, fundable from freelance income; product profitability not required. Later: crowdsource fixtures through the open client with provenance/validation gates — paid testers first for QC.
- **Fixtures as CI.** Every dummy-verified scenario becomes a locked golden fixture; seasonal correctness is a regression suite — an ops problem, not a research project.
- **Exceptions table, structured.** Every special case (double-dips, lying tooltips, rule-breaking coefficients) is an entry that cites the fixture proving it. Folklore → test-backed, auditable law.
- **Bucket topology is data, not code.** Blizzard has restructured the bucket *graph* before, not just tuning. Membership rules live in season-keyed config → a mid-season rework is a config push, not a refactor under fire.

## 9. Distribution & habit manufacturing

- **The shareable verdict is D4's missing parse.** "Forge-verified: Pit ceiling 108" as a screenshot/badge object — pasted into build arguments until "check the Forge" is the Discord reflex. WoW took ~15 years to evolve "sim it"; the Forge manufactures the equivalent.
- **Build string:** compressed base64 + short URL, **openly documented format.** The string is not the moat — the engine that *scores* strings is. Open format → creators and sites adopt it → every string in the wild is an ad with a Calculate button. PoE precedent: nothing circulates without a PoB string; that ubiquity is the target state.
- **Competitions = the habit machine disguised as marketing.** "Highest Forge-verified Pit ceiling — budget gear, no Mythics — enter by Forge string." Forces format adoption overnight, generates creator content, feeds the scored-build corpus, becomes sponsorable. Highest-leverage growth feature in the plan.
- **Creators are distribution, not customers.** The relevant pond is a few dozen channels — too small for a dashboard business (Creator Pro: parked indefinitely). Free creator surface instead: named build pages with channel links, Forge-verified numbers for thumbnails, a public directory ranked by engine-scored ceiling. Their infinite video format: "I asked the Forge for the best possible builds and tested the top 3." Patch-day auto-rescore hands them reaction-video speed their competitors don't have.
- **Launch content = the receipts.** The 0.3%-off dummy screenshots, optimizer-vs-meta results, public patch-day re-validations. PoB won on exactly this kind of receipt.

## 10. Pricing (leisure shelf, season unit)

- **Free (the habit factory):** instant single-build calc with full per-bucket breakdown; item compare; shareable strings and build pages (no login wall on viewing — sharing is sacred); published global season-optimal builds; manual gear entry + the stat-gradient button; URL importer; AoE/skill visualizer. Speed itself is marketing — the anti-PoB-load-time.
- **Forge Pass — ~$15–20/season:** personal optimizer (your gear + constraints, bounded search, N runs/season), client auto-sync, unlimited drop/temper/enchant verdicts, farm routing.
- **Deep Forge — ~$40–50/season:** full reforge incl. paragon pathing, multi-seed overnight runs, rotation priority output, patch-day auto-rescore of saved builds, priority compute.
- **Founder's — $99/season, optional:** whale shelf; priority everything, name in the credits. Whales self-select; never plan around them.
- **Shelf anchors:** D4's battle pass ≈ $10/season is the trained mental category; WoW's $15/mo is gaming's all-time sub ceiling; Raidbots tops out ≈ $15/mo in the most sub-friendly ecosystem alive; GTO Wizard's $40–150/mo works only because poker hours convert back to dollars. Leisure tools price against the entertainment shelf, not hours saved. **$50/month is a Reddit outrage thread; $40–50/season is premium-next-to-a-battle-pass. Same number, right shelf.**
- **Expectation math:** ~1% paid conversion is calibrated, not optimistic (NeverSink: ~1M filter followers → 7,395 paid ≈ 0.7%). Revenue pulses with seasons; 4 season-passes/year out-earn a monthly sub that realistically survives 3–4 pulsed months anyway. Break-even ≈ 20–30 subscribers. Infra ≈ $50–150/mo: warm Fargate task with the season catalog in memory; DDB build storage is a rounding error (1M builds ≈ 2GB ≈ $0.50/mo storage, ~$2.50 in writes). The only real marginal costs are vision-fallback OCR and LLM passes → meter those, never the arithmetic.

## 11. Business context (receipts from the field)

- **Dammitt** (GrimTools + Last Epoch Tools; the most trusted tool author across two ARPG communities; 8 years running): 88 patrons, $249/month. → Tip-jar-as-strategy fails regardless of ubiquity and love.
- **NeverSink** (FilterBlade; ~1M filter followers; genre gold standard): 7,395 paid members; the filter is free forever; the ONE paid thing is a $3/mo auto-update *service* covering server costs. → Free artifact + paid living service.
- **Raidbots** (WoW): 4,867 paid members at Patreon peak (rank 144 of all Patreon), tiers ≈ $6/$8/$16; sells queue priority, bigger combinatorial runs, and literal CPU cores (SwiftSim: 64 vs 32). → The paywall sits on honest compute; and crucially, Raidbots monetized a *pre-existing, socially enforced* habit ("sim it" is the standard class-Discord answer).
- **Maxroll → IGN (Mar 2025); Mobalytics → ESL FACEIT (Mar 2025).** Ad-funded guide traffic consolidates into media groups; that's the endgame of the SEO-interception model. It does not transfer to a destination tool: tools generate deep engagement but few pageviews, served to the highest-uBlock demographic on the internet.
- **Revenue stack ordering:** paid computation is the business. Ads = floor/doormat. Merch = print-on-demand checkbox. Sponsorships = later, at scale (sponsors buy audience, not clicks — beats affiliate at identical traffic). B2B API / engine licensing = later, if the string format wins the ecosystem.
- **The Forge earns at $0 revenue** via the rate card: "solo-built a sim engine reproducing a AAA game's damage model, validated to 0.3% against live, with an AI capture pipeline delivering season data in week 1" is the portfolio artifact for €7.5k/mo freelance positioning from Nov 1. One landed client outearns years of the donation button.

## 12. Settled decisions — do not reopen without new data

**Hard NOs:**
- ❌ **RMT / boost / carry money. Ever.** It's the best-paying advertiser in the niche and a poison pill: Blizzard-ecosystem standing, creator relations, and the trust brand cannot coexist with it. Policy, not case-by-case.
- ❌ Marketplace / merchant-of-record anything (we build payments for a living; we know exactly how much operational hell hides behind "just take a %"). No hardware-affiliate strategy (~$200/mo fantasy: tool traffic has no purchase intent — PCPartPicker works because the purchase IS its output; a DPS calc's output is not a purchase).
- ❌ Fake queues / artificial latency (Law 3). Paywalled data freshness (accuracy is the brand; sell the *personalized application* of freshness — patch-day rescore of YOUR builds — never the data itself). Selling "the top-tier list" (Law 2: a once-per-season artifact is information; it leaks in one screenshot and becomes free content with the name filed off).
- ❌ Client-side engine, downloadable fat app, obfuscation-as-security (Law 4). Companion client only: open-source, code-signed, out-of-process capture ONLY — memory reading / input injection is the Warden line, never crossed.
- ❌ Bulk scraping of guide sites (single user-pasted URL import is fine; the corpus comes from the engine, §6).
- ❌ Creator dashboards as a revenue line (pond of dozens, not thousands). Monthly billing. Commodity features (world-boss alerts etc. — solved elsewhere, off-mission).
- ❌ **Borderlands.** No seasons → no billing rhythm; aim-mediated damage → no deterministic oracle → the golden-fixture method has nothing to lock onto; client-side saves + a decades-old editor tradition → scarcity is voluntary; and the farm is the *terminal* content there — efficiency is anti-product. (Also: uninstalled it twice.)

**Standing YESes:**
- ✅ **Sequencing:** D4 only, until the optimizer demonstrably converts → Last Epoch (optimizer vacuum, tool-friendly dev, real seasonal cycles) → PoE last and *sideways*: consume PoB strings and sell optimization on top; never fight the incumbent artifact — ride its fifteen years of habit-building.
- ✅ **The filter for every new idea: "Is this a Forge feature, or a new treadmill?"** Features compound. Treadmills divide the one resource in the plan that doesn't scale (Tim). Each additional game = a full catalog extraction + validation pipeline + trust reputation from zero + four more seasonal sprints per year.
- ✅ **Money reality:** freelance funds life from Nov 1; the Forge gets 15–20 focused hrs/week, releases timed to season windows; 12–18 months of habit-building before any revenue verdict; downside bounded at evenings. First milestone that matters is boring: one full current-meta build, imported by URL, producing a per-bucket breakdown that survives a live dummy session end to end — vertical slice with receipts, not catalog completeness.

## 13. Stack anchors

- .NET engine, AOT-friendly house style (sealed types, minimal reflection, manual DI, Dapper-school data access).
- Server-side engine on a warm Fargate task (or Lambda) with the season catalog in memory; deterministic evals in µs; optimizer batch runs are the only meaningful CPU line item.
- DynamoDB, season-keyed vessels: catalogs, builds, fixtures, exceptions table. Build strings: 1–2KB compressed base64 + short-URL variant.
- Web-first UI at `d4.buildforge.bruceware.com`; mobile-friendly (second-screen flow is a core UX, not a port); accounts only where compute is metered — never on viewing or sharing.
- Claude/LLM roles: internal capture-pipeline vision (local-OCR-first at consumer scale), natural-language constraint parsing, answer explanation. **Never in the calculation path.**

---

**The one-sentence version:** *Give away the answers to the questions everyone asks; sell CPU time on the questions only they asked — about their gear, their constraints, and their next hour.*

---

## 14. Amendments (2026-07-09, post-review with Tim — same authority as the sections above)

1. **Build importer = INTERNAL tooling.** Kept for us: fast full-build test inputs (Maxroll URL → resolved
   build in seconds). It does NOT ship in the end product as a Maxroll/Mobalytics "stealer." Whether any
   public import surface ever exists is a separate future decision — until then, §10's free-tier "URL
   importer" line is superseded by this amendment.
2. **Auth & build storage model.** Viewing/sharing stays wall-free (Law: sharing is sacred). Users may auth
   with any provider they like. FREE users: full DPS calculator, builds stored in BROWSER CACHE only (no
   server storage). PAID users: proper auth required — server-side stored builds, recommendations/optimizer,
   and all metered compute sit behind it. Accounts exist exactly where compute or storage is real.
3. **LLM roles, complete list:** (a) internal season-drop analysis — deriving/updating the season's NEW math
   and data model when the patch lands (HQ work, Claude-driven); (b) internal capture-pipeline vision;
   (c) natural-language constraint parsing; (d) answer explanation. NEVER in the runtime calculation path —
   the engine's math is code + season config, produced with Claude's help, executed without it.
4. **OCR economics clarified:** HQ/internal capture keeps using Claude vision (cheap at our volume). The
   consumer companion client uses LOCAL OCR (fixed-font tooltips) with cloud vision only as ambiguity
   fallback. Same law, two contexts.
5. **Layer 2 sequencing:** DPS (time domain) was always the goal — but explicitly staged: FIRST perfect the
   per-hit oracle (current vertical slice), THEN layer cast-frequency/attack-speed/EV-proc math on top to
   produce true DPS. L2 remains "build early, lightweight first," entered immediately after the per-hit
   vertical slice ships its receipts.
