# PC Setup — the whole thing (Windows gaming rig)

One manual, top to bottom. After this, every new Claude Code session on the PC boots as a Commander
(via this repo's `CLAUDE.md` — no hooks needed) with full project context (`CLAUDE.md` + `docs/HANDOFF.md`).

## 1. Prereqs (admin PowerShell)
```powershell
winget install Git.Git Microsoft.DotNet.SDK.10 GitHub.cli
# Claude Code: install per https://claude.com/claude-code (native installer or npm)
```

## 2. Auth + clone (all three repos, side by side)
```powershell
gh auth login                          # browser flow, account timuribragimovkz
mkdir C:\sources ; cd C:\sources
gh repo clone timuribragimovkz/BW.D4.BuildForge
gh repo clone timuribragimovkz/bw-automation-grooming-commander
gh repo clone timuribragimovkz/bw-automation-grooming-spyglass
```
The commander/spyglass clones sit as **siblings** of this repo — `CLAUDE.md`'s Commander section references
them at `../bw-automation-grooming-commander/…`. SpyGlass is grooming-specific (reference only here).

## 3. Secrets (manual, NEVER committed)
- **Linear API key**: put the `lin_api_…` value into the PC's `~/.claude/settings.json` (same place as on
  the Mac). Needed for THE LAW (posting work to Linear team D4F). Without it, Claude will ask.
- AWS creds (`Bruceware_Admin` SSO profile) — only needed when the storage plan starts; skip for now.

## 4. Prove the port
```powershell
cd C:\sources\BW.D4.BuildForge
dotnet test        # expect ALL green (engine + validation fixtures). Fresh machine = no NU1900 noise.
```

## 5. Screenshot workflow (the point of moving here)
- Set the game/screenshot hotkey to save into `captures\inbox\` (gear/stat panels) and `captures\hits\`
  (dummy damage moments). Claude reads the images directly — no OCR needed for tooltips.
- Processed screenshots get moved to `captures\processed\` by Claude.

## 6. First session
```powershell
cd C:\sources\BW.D4.BuildForge
claude
```
First message:
> Read CLAUDE.md and docs/HANDOFF.md. We're doing item-by-item build recreation — I'll screenshot my
> equipped gear into captures/inbox; process them, recreate the build, and predict my Claw tooltip + hit.

## 7. What auto-arms and what doesn't
- **Commander mode**: armed by `CLAUDE.md` §Commander Mode in this repo — works on any machine, no config.
- The Mac's hook mechanism (`.claude/settings.local.json` SessionStart → `commander-boot.md`) is
  grooming-workspace-specific and does NOT travel via git (`settings.local.json` is ignored). If you later
  want grooming Commander sessions on the PC, replicate that hook there — but for D4, the CLAUDE.md section
  is the arming mechanism and needs nothing.
- Claude Code **sessions do not sync between machines** — durable context lives in this repo
  (`CLAUDE.md`, `docs/HANDOFF.md`, specs/plans). Keep it that way: anything the next session must know
  goes in a committed file, not just chat.
