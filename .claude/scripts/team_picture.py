#!/usr/bin/env python3
"""Draw a team comp (a BattlePlacementSO) as a picture: the deploy zone with every hero where it stands (name, tier,
item), and beside it each hero's marks, filled where an ally stands on them. Read straight from the asset files, so it
needs no Unity. Writes <out_dir>/<comp>.svg and, with Chrome installed, <comp>.png.

    python .claude/scripts/team_picture.py <out_dir> "Assets/Data/BattleSetups/Team Comps/prog-Skeleton-5.asset" ...
"""
import math
import os
import re
import subprocess
import sys
from pathlib import Path

DATA = Path("Assets/Data")
STEP = [(0, 1), (1, 0), (1, -1), (0, -1), (-1, 0), (-1, 1)]   # HexDirectionEnum N, NE, SE, S, SW, NW as (a NE, b N)
KINDS = {1: "o", 2: "g", 3: "w"}
C = dict(bg="#ffffff", tile="#dfe4e8", edge="#cbd2d8", ink="#1b2229", muted="#5a6670", hero="#26303a", heroink="#f2f4f5",
         o="#e8862a", g="#3fa862", w="#ffffff", wedge="#8c99a4", wear="#8e5bd0", link="#3a6ea8")
FONT = 'font-family="Segoe UI, Arial, sans-serif"'
CHROME = r"C:\Program Files\Google\Chrome\Application\chrome.exe"


# ======================================== reading the assets ========================================
def guid_names():
    names = {}
    for meta in DATA.rglob("*.asset.meta"):
        m = re.search(r"guid: (\w+)", meta.read_text(encoding="utf-8", errors="ignore"))
        if m: names[m.group(1)] = meta.with_suffix("")
    return names


def hero_info(asset):
    text = asset.read_text(encoding="utf-8")
    shown = re.search(r"\n  _name: (.+)", text)
    patterns = []
    if "_alignPatterns:" in text:
        body = text.split("_alignPatterns:")[1]
        body = re.split(r"\n  _\w+:", body)[0]
        for pattern in re.split(r"\n  - marks:", body)[1:]:
            marks = []
            for i, mark in enumerate(re.split(r"\n    - kind:", pattern)[1:]):
                kind = KINDS.get(int(mark.split("\n")[0].strip()) or 0) or "ogw"[min(i, 2)]   # unset: by position
                hexes = []
                for d, s, td, ts, wear in re.findall(r"- direction: (\d+)\s+distance: (\d+)\s+thenDirection: (\d+)\s+"
                                                     r"thenDistance: (\d+)\s+isWearing: (\d+)", mark):
                    d, s, td, ts = int(d), max(1, int(s)), int(td), int(ts)
                    hexes.append((STEP[d][0] * s + STEP[td][0] * ts, STEP[d][1] * s + STEP[td][1] * ts, wear == "1"))
                marks.append((kind, hexes))
            patterns.append(marks)
    return (shown.group(1).strip() if shown else asset.stem), patterns


def read_comp(path, names):
    text = Path(path).read_text(encoding="utf-8")
    placed = text.split("_itemsOnHexes:")[0]
    heroes = []
    for block in placed.split("- dataSO:")[1:]:
        get = lambda pattern, default="0": (re.search(pattern, block) or [None, default])[1]
        if get(r"team: (\d+)") != "0": continue                       # Blue = the player's team
        item = re.search(r"item: \{fileID: \d+, guid: (\w+)", block)
        asset = names.get(get(r"guid: (\w+)"))
        if asset is None: continue
        name, patterns = hero_info(asset)
        heroes.append(dict(name=name, star=int(get(r"star: (\d+)", "1")), pattern=int(get(r"alignPattern: (\d+)")),
                           col=int(get(r"column: (-?\d+)")), row=int(get(r"row: (-?\d+)")),
                           item=names[item.group(1)].stem if item and item.group(1) in names else "",
                           patterns=patterns))
    lying = [(names[g].stem, int(c), int(r)) for g, c, r in re.findall(
        r"- item: \{fileID: \d+, guid: (\w+), type: \d+\}\s+hexPlacement:\s+column: (-?\d+)\s+row: (-?\d+)",
        text.split("_itemsOnHexes:")[1]) if g in names]
    return heroes, lying


def axial(col, row):
    """Board (column, row) -> axial (a NE, b N). Odd columns sit half a hex lower."""
    z = row - (col - (col & 1)) // 2
    return col, -z - col


# ======================================== drawing ========================================
def hexpath(cx, cy, s):
    return "M" + "L".join(f"{cx + s * math.cos(math.pi / 3 * i):.1f} {cy + s * math.sin(math.pi / 3 * i):.1f}"
                          for i in range(6)) + "Z"


def text(x, y, s, size=12, fill=None, anchor="start", weight="normal"):
    s = s.replace("&", "&amp;").replace("<", "&lt;")
    return (f'<text x="{x:.1f}" y="{y:.1f}" font-size="{size}" fill="{fill or C["ink"]}" text-anchor="{anchor}" '
            f'font-weight="{weight}" {FONT}>{s}</text>')


def stars(n): return "★" * n


def draw(path, out_dir):
    names = guid_names()
    heroes, lying = read_comp(path, names)
    comp = Path(path).stem
    at = {axial(h["col"], h["row"]): h for h in heroes}
    items_at = {axial(c, r): n for n, c, r in lying}

    # each hero's marks from its hex; which ally stands on each
    for h in heroes:
        marks = h["patterns"][h["pattern"] % len(h["patterns"])] if h["patterns"] else []
        a0, b0 = axial(h["col"], h["row"])
        cells = {}
        for kind, hexes in marks:
            for a, b, wear in hexes:
                cell = cells.setdefault((a, b), {"kinds": "", "wear": False})
                if kind not in cell["kinds"]: cell["kinds"] += kind
                cell["wear"] |= wear
        h["cells"] = cells
        h["filled"] = {k for k in cells if (a0 + k[0], b0 + k[1]) in at}
        h["on_item"] = {k for k in cells if cells[k]["wear"] and (a0 + k[0], b0 + k[1]) in items_at}

    # --- left: the deploy zone, 6 columns x 5 rows
    s = 30
    pos = lambda c, r: (1.5 * s * c + s * 1.1, math.sqrt(3) * s * (r + 0.5 * (c & 1)) + s * 1.1 + 34)
    cols = max([5] + [h["col"] for h in heroes]) + 1
    rows = max([4] + [h["row"] for h in heroes]) + 1
    g = text(0, 20, comp, 18, weight="bold")
    for c in range(cols):
        for r in range(rows):
            x, y = pos(c, r)
            g += f'<path d="{hexpath(x, y, s * .95)}" fill="{C["tile"]}" stroke="{C["edge"]}" stroke-width="1"/>'
    for n, c, r in lying:
        x, y = pos(c, r)
        g += f'<path d="{hexpath(x, y, s * .6)}" fill="none" stroke="{C["wear"]}" stroke-width="2.5"/>'
        g += text(x, y + 4, re.findall(r"[A-Z][a-z]*", n)[-1][:8], 8, C["wear"], "middle", "bold")
    # a line from each hero to every ally standing on one of its marks
    for h in heroes:
        a0, b0 = axial(h["col"], h["row"])
        for k in h["filled"]:
            other = at[(a0 + k[0], b0 + k[1])]
            (x1, y1), (x2, y2) = pos(h["col"], h["row"]), pos(other["col"], other["row"])
            g += f'<line x1="{x1:.1f}" y1="{y1:.1f}" x2="{x2:.1f}" y2="{y2:.1f}" stroke="{C["link"]}" stroke-width="2" opacity=".45"/>'
    for i, h in enumerate(heroes):
        x, y = pos(h["col"], h["row"])
        g += f'<circle cx="{x:.1f}" cy="{y:.1f}" r="{s * .62:.1f}" fill="{C["hero"]}"/>'
        g += text(x, y + 1, str(i + 1), 15, C["heroink"], "middle", "bold")
        g += text(x, y + 14, stars(h["star"]), 9, "#f4c542", "middle")
    bw, bh = 1.5 * s * (cols - 1) + s * 2.2, math.sqrt(3) * s * (rows + .5) + s * .4 + 34
    g += text(bw - 4, bh + 14, "enemy →", 11, C["muted"], "end")

    # --- right: one row per hero, its marks drawn around it
    ms, x0, y = 9, bw + 30, 34
    for i, h in enumerate(heroes):
        cells = h["cells"]
        r = max([2] + [(abs(a) + abs(b) + abs(a + b)) // 2 for a, b in cells]) + 1 if cells else 2
        mpos = lambda a, b: (1.5 * ms * a, -math.sqrt(3) * ms * (b + a / 2))
        span = math.sqrt(3) * ms * (r + .5)
        cx, cy = x0 + 1.5 * ms * r + ms * 1.2, y + span
        for a in range(-r, r + 1):
            for b in range(-r, r + 1):
                if (abs(a) + abs(b) + abs(a + b)) // 2 > r: continue
                px, py = mpos(a, b); px += cx; py += cy
                me = (a, b) == (0, 0)
                g += f'<path d="{hexpath(px, py, ms * .94)}" fill="{C["hero"] if me else C["tile"]}" stroke="{C["edge"]}" stroke-width=".6"/>'
                cell = cells.get((a, b))
                if not cell or me: continue
                filled = (a, b) in h["filled"] or (a, b) in h["on_item"]
                kinds = cell["kinds"]
                if filled:
                    # solid: the first colour fills the hex, later colours as smaller cores, a dark rim on the outside
                    for j, kind in enumerate(kinds):
                        size = ms * [.9, .5, .28][min(j, 2)]
                        rim = f' stroke="{C["ink"]}" stroke-width="1.3"' if j == 0 else (
                              f' stroke="{C["wedge"]}" stroke-width=".8"' if kind == "w" else "")
                        g += f'<path d="{hexpath(px, py, size)}" fill="{C[kind]}"{rim}/>'
                else:
                    first = kinds[0]
                    g += (f'<path d="{hexpath(px, py, ms * .78)}" fill="none" stroke="{C[first] if first != "w" else C["wedge"]}" '
                          f'stroke-width="1.5" opacity=".75"/>')
        tx = cx + 1.5 * ms * r + ms * 1.6
        g += text(tx, y + span - 8, f"{i + 1}. {h['name']}  {stars(h['star'])}", 14, weight="bold")
        g += text(tx, y + span + 10, f"Item: {h['item'] or '—'}", 12, C["muted"])
        g += text(tx, y + span + 26, f"Marks filled: {len(h['filled'])} of {len(cells)}", 12, C["muted"])
        a0, b0 = axial(h["col"], h["row"])
        worn = sorted({items_at[(a0 + k[0], b0 + k[1])] for k in h["on_item"]})
        if worn: g += text(tx, y + span + 42, "Wears from a hex: " + ", ".join(worn), 12, C["wear"])
        y += 2 * span + 14

    height = max(bh + 24, y) + 44
    g += text(0, height - 24, "Marks: solid with a dark rim = an ally stands on it, faint outline = empty. "
              "Orange = bonus to itself, green = to the ally, white = special.", 11, C["muted"])
    g += text(0, height - 8, "Board: lines link a hero to the allies on its marks. Purple hex = an item lying on the board.",
              11, C["muted"])
    width = max(x0 + 420, 800)
    svg = (f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="-12 -12 {width + 24:.0f} {height + 24:.0f}" '
           f'width="{width + 24:.0f}" height="{height + 24:.0f}"><rect x="-12" y="-12" width="{width + 24:.0f}" '
           f'height="{height + 24:.0f}" fill="{C["bg"]}"/>{g}</svg>')

    os.makedirs(out_dir, exist_ok=True)
    svg_path = Path(out_dir) / f"{comp}.svg"
    svg_path.write_text(svg, encoding="utf-8")
    png_path = svg_path.with_suffix(".png")
    if os.path.exists(CHROME):
        subprocess.run([CHROME, "--headless", "--disable-gpu", "--hide-scrollbars", "--force-device-scale-factor=2",
                        f"--screenshot={png_path.resolve()}", f"--window-size={width + 24:.0f},{height + 24:.0f}",
                        svg_path.resolve().as_uri()], capture_output=True)
    return png_path if png_path.exists() else svg_path


if __name__ == "__main__":
    if len(sys.argv) < 3:
        sys.exit("usage: team_picture.py <out_dir> <comp.asset> [more...]")
    for comp in sys.argv[2:]:
        print(draw(comp, sys.argv[1]))
