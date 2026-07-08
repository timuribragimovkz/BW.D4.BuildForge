#!/usr/bin/env python3
"""Resolve a Maxroll D4 planner build (opaque IDs) into a human-readable build sheet.

Usage:
    python3 resolve_maxroll_build.py <planner.json> <d4data.json> <profile-name> [out.md]

Inputs:
  planner.json  — the `plannerProfile.data` object extracted from a Maxroll build-guide page
                  (curl page → find `"plannerProfile":` → brace-match JSON; see
                  docs/reference/build-import-sources.md).
  d4data.json   — Maxroll's game-data dictionary:
                  https://assets-ng.maxroll.gg/d4-tools/game/data.min.json?<ver>
                  (ver hash lives in the planner bundle's asset map; plain path 404s).

Semantics (audited adversarially, 2026-07-09):
  * Paragon/skill-tree steps are FULL snapshots; the canonical one is steps[<collection>.position]
    (the planner's own profileSelectParagon), NOT steps[-1].
  * Paragon stored node indices are in the UNROTATED base grid (21x21); `rotation` is a
    render-only transform. Do NOT rotate indices when resolving node identity.
  * Tree-node rank 0 = the unselected alternative of a choice pair — omit from output.
  * item.upgrade = the item's upgrade/quality tier (bundle tracks `masterwork` separately).
  * Aspects resolve to affix keys (e.g. legendary_druid_028_x2); no display-name table exists
    in data.min.json (desc text only).
"""
import json
import sys


def load(path):
    with open(path, encoding="utf-8") as f:
        return json.load(f)


def main(planner_path, data_path, profile_name, out_path=None):
    data = load(planner_path)
    d = load(data_path)

    aff_by_id = {v["id"]: k for k, v in d["affixes"].items() if isinstance(v, dict) and "id" in v}
    old_by_id = {v["id"]: k for k, v in d.get("oldAffixes", {}).items() if isinstance(v, dict) and "id" in v}
    items, skills, mercs = d["items"], d["skills"], d["mercenaries"]
    boards, pnodes, glyphs = d["paragonBoards"], d["paragonNodes"], d["paragonGlyphs"]

    prof = next(p for p in data["profiles"] if p.get("name") == profile_name)
    cls_names = list(d["skillTrees"].keys())
    cls_tree_key = cls_names[prof.get("class", 0)] if isinstance(prof.get("class"), int) else "Druid"
    # class index in Maxroll order maps via d4data.classes; Druid builds carry class:1
    tree_key = {0: "Sorcerer", 1: "Druid", 2: "Barbarian", 3: "Rogue", 4: "Necromancer",
                5: "Spiritborn", 6: "Paladin_NEW", 7: "Warlock"}.get(prof.get("class"), cls_tree_key)
    tree = {n["id"]: n for n in d["skillTrees"][tree_key]["nodes"]}

    unresolved = []

    def aff(nid, ctx):
        k = aff_by_id.get(nid) or old_by_id.get(nid)
        if not k:
            unresolved.append((ctx, "affix", nid))
            return f"?nid:{nid}?"
        return k

    out = [f"# {profile_name} ({tree_key}) — S{prof.get('season')}, lvl {prof.get('level')}, WT {prof.get('worldTier')}",
           f"renown bonus points: skills +{prof.get('world', {}).get('renownSkills')}, "
           f"paragon +{prof.get('world', {}).get('renownParagon')}", "", "## Gear"]

    pool = data["items"]
    for slot in sorted(prof["items"], key=int):
        it = pool[str(prof["items"][slot])]
        meta = items.get(it.get("id"), {})
        if not meta:
            unresolved.append(("gear", "item", it.get("id")))
        name = meta.get("name") or it.get("id")
        custom = f" “{it['name']}”" if it.get("name") else ""
        myth = " [MYTHIC]" if it.get("mythic") else ""
        out.append(f"\n### {meta.get('type', '?')}: {name}{custom}{myth} "
                   f"(IP {it.get('power')}, upgrade {it.get('upgrade')})")
        for e in it.get("explicits", []):
            out.append(f"- affix: {aff(e['nid'], 'explicit')} = {e.get('values')}"
                       f"{' ✦greater' if e.get('greater') else ''}")
        for t in it.get("tempered", []) or []:
            out.append(f"- temper: {aff(t['nid'], 'temper')} = {t.get('values')}"
                       f"{' ✦greater' if t.get('greater') else ''}")
        for a in it.get("aspects", []) or []:
            out.append(f"- aspect: {aff(a['nid'], 'aspect')} = {a.get('values')}")
        for s in it.get("sockets", []) or []:
            sm = items.get(s, {})
            if not sm:
                unresolved.append(("gear", "socket", s))
            out.append(f"- socket: {sm.get('name', s)}")

    out.append("\n## Skill Bar")
    for s in prof.get("skillBar", []):
        sm = skills.get(s, {})
        if not sm:
            unresolved.append(("bar", "skill", s))
        out.append(f"- {sm.get('name', s)}")

    out.append("\n## Skill Tree (selected; canonical step)")
    st = prof["skillTree"]
    st_step = st["steps"][st.get("position", len(st["steps"]) - 1)]
    for nid_s, rank in sorted(st_step.get("data", {}).items(), key=lambda x: int(x[0])):
        if not rank:
            continue  # rank 0 = unselected choice alternative
        tn = tree.get(int(nid_s))
        if not tn:
            unresolved.append(("tree", "node", nid_s))
            continue
        rw = tn.get("reward", {})
        power = rw.get("power")
        label = skills.get(power, {}).get("name", power)
        mod_id = rw.get("mod")
        if mod_id is not None:
            mods = skills.get(power, {}).get("mods", [])
            hit = next((m for m in mods if m.get("id") == mod_id), None)
            if hit is None:
                unresolved.append(("tree", "mod", mod_id))
            label = f"{label} — {hit.get('name') if hit else f'mod {mod_id}'}"
        out.append(f"- {label}: {rank}")

    out.append("\n## Paragon (canonical step)")
    par = prof["paragon"]
    p_step = par["steps"][par.get("position", len(par["steps"]) - 1)]
    for b in p_step.get("data", []):
        board = boards.get(b["id"])
        if board is None:
            unresolved.append(("paragon", "board", b["id"]))
            continue
        gl = b.get("glyph")
        glm = glyphs.get(gl, {}) if gl else {}
        if gl and not glm:
            unresolved.append(("paragon", "glyph", gl))
        bname = b["id"]
        for nk in board["nodes"]:
            if nk and "Legendary" in nk:
                bname = pnodes.get(nk, {}).get("name", b["id"])
                break
        out.append(f"\n### Board: {bname} ({b['id']}) rot={b.get('rotation')} | "
                   f"glyph: {glm.get('name', gl)} lvl {b.get('glyphLevel')} | "
                   f"nodes {len(b.get('nodes', {}))}")
        counts = {}
        for idx in b.get("nodes", {}):
            i = int(idx)
            nk = board["nodes"][i] if i < len(board["nodes"]) else None
            if nk is None:
                unresolved.append(("paragon", "node-idx", f"{b['id']}[{idx}]"))
                continue
            pm = pnodes.get(nk)
            if pm is None:
                unresolved.append(("paragon", "node", nk))
                continue
            lbl = pm.get("name") or nk
            counts[lbl] = counts.get(lbl, 0) + 1
        for lbl, c in sorted(counts.items(), key=lambda x: -x[1]):
            out.append(f"- {lbl} ×{c}")

    wp = prof.get("warPlans") or []
    wp_meta = d.get("warPlans", [])
    if any(wp):
        out.append("\n## War Plans")
        for i, alloc in enumerate(wp):
            if not alloc:
                continue
            meta = wp_meta[i] if i < len(wp_meta) else {}
            wp_tree_key = meta.get("tree") if isinstance(meta, dict) else None
            wp_name = (meta.get("name") if isinstance(meta, dict) else None) or wp_tree_key or f"plan {i}"
            wp_nodes = {}
            if wp_tree_key and wp_tree_key in d["skillTrees"]:
                wp_nodes = {n["id"]: n for n in d["skillTrees"][wp_tree_key]["nodes"]}
            out.append(f"\n### {wp_name}")
            for nid_s, rank in sorted(alloc.items(), key=lambda x: int(x[0])):
                if not rank:
                    continue
                tn = wp_nodes.get(int(nid_s))
                power = tn.get("reward", {}).get("power") if tn else None
                label = skills.get(power, {}).get("name", power) if power else f"node {nid_s}"
                if tn is None:
                    unresolved.append(("warplan", f"{wp_name}", nid_s))
                out.append(f"- {label}: {rank}")

    m = prof.get("mercenary") or {}
    if m:
        mm = mercs.get(m.get("id"), {})
        sup = mercs.get(m.get("support"), {})
        out.append(f"\n## Mercenary: {mm.get('name', m.get('id'))} | support: {sup.get('name', m.get('support'))}")
        merc_tree_key = m.get("id", "").replace("MercenaryClass_", "Mercenary_")
        merc_nodes = {n["id"]: n for n in d["skillTrees"].get(merc_tree_key, {}).get("nodes", [])}
        for nid_s, rank in sorted((m.get("tree") or {}).items(), key=lambda x: int(x[0])):
            if not rank:
                continue
            tn = merc_nodes.get(int(nid_s))
            power = tn.get("reward", {}).get("power") if tn else None
            label = skills.get(power, {}).get("name", power) if power else f"node {nid_s}"
            if tn is None:
                unresolved.append(("merc", "tree-node", nid_s))
            out.append(f"- {label}: {rank}")
        sup_sk = m.get("supportSkills") or []
        if sup_sk:
            names = [skills.get(s, {}).get("name", s) for s in sup_sk]
            out.append(f"- support skills: {', '.join(names)}")

    out.append("\n## Spirit Boons")
    for bo in prof.get("spiritBoons", []):
        sm = skills.get(bo, {})
        if not sm:
            unresolved.append(("boons", "boon", bo))
        out.append(f"- {sm.get('name', bo)}")

    text = "\n".join(out) + "\n"
    if out_path:
        with open(out_path, "w", encoding="utf-8") as f:
            f.write(text)
    else:
        print(text)
    print(f"[resolver] profile={profile_name} unresolved={len(unresolved)}", file=sys.stderr)
    for u in unresolved[:20]:
        print(f"[resolver]   {u}", file=sys.stderr)
    return 1 if unresolved else 0


if __name__ == "__main__":
    if len(sys.argv) < 4:
        print(__doc__)
        sys.exit(2)
    sys.exit(main(*sys.argv[1:5]))
