# D4 Build Forge

A Path-of-Building-class theorycrafting tool for **Diablo 4** — a faithful damage/defence/life/movement
engine that reproduces in-game numbers to ~1%, plus (beyond PoB) an AoE visualizer. **Online tool:
.NET + DynamoDB (Docker-local) + web UI.** Engine first.

- Engine design spec: [docs/superpowers/specs/2026-06-30-d4-build-forge-engine-design.md](docs/superpowers/specs/2026-06-30-d4-build-forge-engine-design.md)
- Reference damage model: `docs/reference/ava-damage-optimization.xlsx`

**Stack:** pure C# `Engine` library (storage-agnostic) · `Domain` POCOs · `Storage` (one DynamoDB table per
game-content concept — Items, Buffs, ClassesAndStats, Skills, Passives, Paragon, Conditions… — plus
BW.Libs.Config vessels for CalculationSettings) · `Assembly` (the "LEGO": selection → engine `Build`) · `Api` (later).

Status: design approved; engine (pure C#) implementation next.
