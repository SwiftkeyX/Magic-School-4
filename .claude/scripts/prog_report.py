# Per-prog summary of a Team Comp Benchmark CSV, every team side by side, judged against the prog criteria
# (Notion: Power level). A prog comp is one whose name ends in "-N" (N = its milestone stage): prog-Skeleton-5,
# prog-Elf-5. analyse() is shared with prog_sheet.py, which writes the same numbers to the Sheet.
# Usage: python .claude/scripts/prog_report.py BalanceLogs/teamcomp-<time>.csv
import csv
import re
import sys
from collections import defaultdict

BAND = (0.50, 0.95)    # win rate at the milestone stage
MAX_SHARE = 0.45       # no hero above this share of team damage
CARRY_SPREAD = 0.20    # a carry's share within +-20% of the other teams' carries at the same prog


def _pct(s): return float(s.strip().rstrip("%")) / 100 if s and s.strip() else 0.0
def _num(s): return float(s) if s and s.strip() else 0.0


def team_and_prog(comp):
    m = re.match(r"prog-(\w+)-(\d+)$", comp) or re.match(r"(\w+)-.*-(\d+)$", comp)
    return (m.group(1), int(m.group(2))) if m else (None, None)


def verdict(win):
    """'in' inside the band, 'low' below, 'high' above."""
    return "low" if win < BAND[0] else "high" if win > BAND[1] else "in"


def analyse(path):
    """One dict per prog: its teams, a team row each, every hero, and the flags."""
    rows = [{k: (v or "").strip() for k, v in r.items()} for r in csv.DictReader(open(path, encoding="utf-8-sig"))]
    team, heroes = {}, defaultdict(list)
    for r in rows:
        key = (r["comp"], int(r["stage"]), r["aligning"])
        if r["row"] == "team": team[key] = r
        elif r["row"] == "hero": heroes[key].append(r)

    by_prog = defaultdict(dict)
    for comp in {r["comp"] for r in rows}:
        t, p = team_and_prog(comp)
        if t: by_prog[p][t] = comp

    progs = []
    for p in sorted(by_prog):
        teams = sorted(by_prog[p])
        entry = {"prog": p, "teams": teams, "rows": {}, "heroes": [], "flags": []}
        carries = {}
        for t in teams:
            c = by_prog[p][t]
            on, off = team.get((c, p, "on")), team.get((c, p, "off"))
            if not on: continue
            hs = heroes.get((c, p, "on"), [])
            entry["rows"][t] = {
                "comp": c,
                "win": _pct(on["win_rate"]),
                "off": _pct(off["win_rate"]) if off else None,
                "next": {s: _pct(team[(c, s, "on")]["win_rate"]) for s in range(p + 1, min(p + 3, 11)) if (c, s, "on") in team},
                "seconds": _num(on["avg_seconds"]),
                "heroes_left": _num(on["heroes_left"]),
                "hp_left": _pct(on["team_hp_left"]),
                "dps": _num(on["team_dps"]),
            }
            if not hs: continue
            carry = max(hs, key=lambda h: _pct(h["damage_share"]))
            tank = max(hs, key=lambda h: _num(h["damage_taken"]))
            carries[t] = carry
            for h in sorted(hs, key=lambda h: -_pct(h["damage_share"])):
                entry["heroes"].append({
                    "team": t, "hero": h["hero"],
                    "role": "carry" if h is carry else "tank" if h is tank else "",
                    "dps": _num(h["dps"]), "share": _pct(h["damage_share"]), "taken": _num(h["damage_taken"]),
                    "survive": _pct(h["survive_rate"]), "died_at": _num(h["died_at"]) if h["died_at"] else None,
                })

        flags = entry["flags"]
        for t, row in entry["rows"].items():
            v = verdict(row["win"])
            if v != "in":
                flags.append(f"{t}: wins {row['win']:.0%} at stage {p}, {'below' if v == 'low' else 'above'} "
                             f"{BAND[0]:.0%}-{BAND[1]:.0%}")
        for t in teams:
            mine = [h for h in entry["heroes"] if h["team"] == t]
            if not mine: continue
            carry = next(h for h in mine if h["role"] == "carry")
            tank = next((h for h in mine if h["role"] == "tank"), carry)
            for h in mine:
                if h["share"] > MAX_SHARE: flags.append(f"{t}: {h['hero']} deals {h['share']:.0%} of team damage")
            others = [_pct(v["damage_share"]) for k, v in carries.items() if k != t]
            if others:
                avg = sum(others) / len(others)
                if abs(carry["share"] - avg) > avg * CARRY_SPREAD:
                    flags.append(f"{t}: carry {carry['hero']} deals {carry['share']:.0%}, other teams' carries {avg:.0%}")
            if tank is not carry and tank["survive"] < carry["survive"]:
                flags.append(f"{t}: tank {tank['hero']} survives less ({tank['survive']:.0%}) than carry "
                             f"{carry['hero']} ({carry['survive']:.0%})")
        progs.append(entry)
    return progs


def grid_text(progs):
    """The one-glance grid: milestone win rate per prog x team, with a colour mark."""
    mark = {"in": "🟢", "low": "🔴", "high": "🟡"}
    teams = sorted({t for e in progs for t in e["teams"]})
    lines = ["prog      " + "".join(f"{t:>12}" for t in teams)]
    for e in progs:
        cells = []
        for t in teams:
            row = e["rows"].get(t)
            cells.append(f"{row['win']:>8.0%} {mark[verdict(row['win'])]}" if row else f"{'-':>10}  ")
        lines.append(f"prog-{e['prog']:<5}" + "".join(cells))
    return "\n".join(lines)


if __name__ == "__main__":
    data = analyse(sys.argv[1])
    print(grid_text(data))
    print(f"\n🟢 {BAND[0]:.0%}-{BAND[1]:.0%} at the milestone stage   🔴 below   🟡 above\n")
    for e in data:
        if e["flags"]:
            print(f"prog-{e['prog']}:")
            for f in e["flags"]: print(f"  - {f}")
