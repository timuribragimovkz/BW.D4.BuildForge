# D4BuildForge.Domain — FROZEN CONTRACT

These records + `IRecordRepository` are the shared contract for the Builder (Slice 2a) and
Build-stealer (Slice 2b). The canonical JSON shapes are in
`docs/superpowers/specs/2026-06-30-d4-forge-admin-vessels-design.md` §3 and pinned by the round-trip
tests in `tests/D4BuildForge.Domain.Tests`.

- `ItemRecord` / `AffixEntry` / `AspectRef` — every D4 item (spec §3.1).
- `BuildRecord` / `SkillRef` / `TargetRef` — a computable build (spec §3.2).
- `IRecordRepository` — JsonObject CRUD over DDB collections (impl: `src/Storage`, Slice 2a).

**Changing a shape breaks both downstream workers — coordinate on Linear D4F before editing.**
