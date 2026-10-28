#!/usr/bin/env python3
"""The "MagicSchool4 Prog" sheet, built like the Benchmark sheet: every run kept, and a dashboard of live formulas.

    Runs       - every Team Comp Benchmark run, appended: per prog x team a "team" row (win rate at the milestone stage
                 and after, fight length, ...) and per hero a "hero" row
    Dashboard  - pick a run (and one to compare with): each prog's win rate per team in colour, the change since the
                 compared run, what is off, then the team and hero tables. Formulas only, read from Runs
    Flags      - hidden: each run's problems ("What is off"), one line each
    Lists      - hidden: the dropdowns' choices

benchmark_import.py calls publish() after importing a team comp report. By hand:
    python .claude/scripts/prog_sheet.py BalanceLogs/teamcomp-2026-10-27_19-21-06.csv   (add one run)
    python .claude/scripts/prog_sheet.py --rebuild                                      (rebuild Dashboard and Lists)
"""
import re
import sys
from pathlib import Path

import gspread

sys.path.insert(0, str(Path(__file__).parent))
import prog_report  # noqa: E402
import sheet_sync  # noqa: E402

SHEET = "magicschool4-prog"
RUNS, DASH, LISTS, FLAGS = "Runs", "Dashboard", "Lists", "Flags"
COLUMNS = ["run_time", "row", "prog", "team", "comp", "win", "win_marks_off", "win_next_stage", "win_stage_after",
           "fight_seconds", "heroes_left", "team_hp_left", "team_dps", "hero", "role", "dps", "damage_share",
           "damage_taken", "survives", "died_at"]
COL = {name: chr(ord("A") + i) for i, name in enumerate(COLUMNS)}       # run_time -> A ... died_at -> T
LAST = COL[COLUMNS[-1]]
FLAG_COLUMNS = ["run_time", "prog", "team", "problem"]


def rgb(r, g, b): return {"red": r, "green": g, "blue": b}


GREEN_BG, GREEN_FG = rgb(0.8, 0.92, 0.8), rgb(0.0, 0.4, 0.0)
RED_BG, RED_FG = rgb(0.96, 0.8, 0.8), rgb(0.6, 0.0, 0.0)
YELLOW_BG, YELLOW_FG = rgb(1.0, 0.93, 0.7), rgb(0.5, 0.35, 0.0)
UP_FG, DOWN_FG = rgb(0.098, 0.498, 0.098), rgb(0.749, 0.349, 0.0)
HEADER_BG, PICK_BG, MUTED_FG = rgb(0.898, 0.898, 0.898), rgb(1.0, 0.969, 0.847), rgb(0.4, 0.4, 0.4)
GROUP_LINE = rgb(0.498, 0.498, 0.498)
PROG_FORMAT = {"type": "NUMBER", "pattern": '"prog-"0'}
PCT_FORMAT = {"type": "PERCENT", "pattern": "0%"}
SIGNED_PCT = {"type": "PERCENT", "pattern": "+0%;-0%;0%"}


# ======================================== Runs ========================================
def run_time(path):
    m = re.search(r"(\d{4}-\d{2}-\d{2})_(\d{2})-(\d{2})-(\d{2})", Path(path).name)
    return f"{m.group(1)} {m.group(2)}:{m.group(3)}" if m else Path(path).name


def run_rows(path):
    t = run_time(path)
    rows = []
    blank = lambda: {c: "" for c in COLUMNS}
    for e in prog_report.analyse(path):
        p = e["prog"]
        for team, r in e["rows"].items():
            row = blank()
            row.update(run_time=t, row="team", prog=p, team=team, comp=r["comp"], win=r["win"],
                       win_marks_off="" if r["off"] is None else r["off"],
                       win_next_stage=r["next"].get(p + 1, ""), win_stage_after=r["next"].get(p + 2, ""),
                       fight_seconds=round(r["seconds"], 1), heroes_left=r["heroes_left"], team_hp_left=r["hp_left"],
                       team_dps=round(r["dps"]))
            rows.append(row)
        for h in e["heroes"]:
            row = blank()
            row.update(run_time=t, row="hero", prog=p, team=h["team"], comp=e["rows"][h["team"]]["comp"], hero=h["hero"],
                       role=h["role"], dps=round(h["dps"]), damage_share=h["share"], damage_taken=round(h["taken"]),
                       survives=h["survive"], died_at="" if h["died_at"] is None else round(h["died_at"], 1))
            rows.append(row)
    return t, [[row[c] for c in COLUMNS] for row in rows]


def flag_rows(path):
    t = run_time(path)
    return [[t, e["prog"], f.split(":")[0], f] for e in prog_report.analyse(path) for f in e["flags"]]


def tab(sh, title, rows=200, cols=26):
    try:
        return sh.worksheet(title), False
    except gspread.exceptions.WorksheetNotFound:
        return sh.add_worksheet(title=title, rows=rows, cols=cols), True


def append_run(sh, path):
    ws, new = tab(sh, RUNS, cols=len(COLUMNS))
    if new or ws.row_values(1) != COLUMNS:
        ws.update([COLUMNS], range_name="A1")
    when, rows = run_rows(path)
    added = 0
    if when not in ws.col_values(1)[1:]:                          # a run already there is not added twice
        first = len(ws.col_values(1)) + 1
        if ws.row_count < first + len(rows): ws.add_rows(first + len(rows) - ws.row_count + 50)
        ws.update(rows, range_name=f"A{first}", value_input_option="RAW")
        added = len(rows)
    style_runs(sh, ws)

    flags, new = tab(sh, FLAGS, cols=len(FLAG_COLUMNS))
    if new:
        flags.update([FLAG_COLUMNS], range_name="A1")
        sh.batch_update({"requests": [{"updateSheetProperties": {"properties": {"sheetId": flags.id, "hidden": True},
                                                                 "fields": "hidden"}}]})
    if when not in flags.col_values(1)[1:]:
        lines = flag_rows(path)
        first = len(flags.col_values(1)) + 1
        if flags.row_count < first + len(lines): flags.add_rows(first + len(lines) - flags.row_count + 50)
        if lines: flags.update(lines, range_name=f"A{first}", value_input_option="RAW")
    return when, added


# Runs looks like the Benchmark sheet's Team Runs, styled by the same code: grey identity columns, an orange block of
# team numbers, a blue block of hero numbers, team rows bold, every other run shaded, a note on each header. On top,
# the win rate at the milestone stage is coloured by the 50-70% band.
NOTES = {
    "run_time": "When the benchmark ran. Every row of one run shares it.",
    "row": "team = one prog comp's numbers at its milestone stage; hero = one of its heroes.",
    "prog": "The milestone stage the comp is built for (prog-5 = stage 5), from the end of its name.",
    "team": "Which team: the comp's name before its milestone, e.g. Elf, Skeleton.",
    "comp": "The team comp asset (Data/BattleSetups/Team Comps).",
    "win": "Win rate at the milestone stage, marks on. The target is 50-70%.",
    "win_marks_off": "Win rate at the milestone stage with aligning off - what the team is worth without its marks.",
    "win_next_stage": "Win rate at the stage after the milestone.",
    "win_stage_after": "Win rate two stages after the milestone.",
    "fight_seconds": "Average fight length at the milestone stage, marks on.",
    "heroes_left": "Average heroes alive at the end.",
    "team_hp_left": "Average share of the team's HP left at the end.",
    "team_dps": "The team's damage per second.",
    "hero": "The hero (hero rows).",
    "role": "carry = the most damage in its team; tank = took the most damage.",
    "dps": "The hero's damage per second.",
    "damage_share": "Its share of the team's damage. Over 45% is flagged.",
    "damage_taken": "Damage it took per fight.",
    "survives": "Share of the fights it was alive at the end.",
    "died_at": "Average second it died, in the fights it died.",
}


def style_runs(sh, ws):
    import benchmark_import as bi
    bi.NOTES.update(NOTES)
    report = bi.Report(
        name="prog", file_pattern="", runs_tab=RUNS, latest_tab="", stamp=["run_time"],
        blocks=[
            (bi.GREY, None, ("run_time", "row", "prog", "team", "comp")),
            (bi.rgb(0.98, 0.8, 0.6), bi.rgb(1.0, 0.95, 0.9), ("win", "win_marks_off", "win_next_stage", "win_stage_after",
                                                              "fight_seconds", "heroes_left", "team_hp_left", "team_dps")),
            (bi.rgb(0.7, 0.8, 0.95), bi.rgb(0.93, 0.95, 1.0), ("hero", "role", "dps", "damage_share", "damage_taken",
                                                               "survives", "died_at")),
        ],
        frozen_columns=4, bold_column="team",
        one_decimal=("fight_seconds", "heroes_left", "died_at"),
        percent=("win", "win_marks_off", "win_next_stage", "win_stage_after", "team_hp_left", "damage_share", "survives"),
        bold_rows_where=("row", "team"),
    )
    bi.style(sh, report, ws, band_runs=True)

    sid, win = ws.id, COLUMNS.index("win")
    column = lambda i: {"sheetId": sid, "startRowIndex": 1, "startColumnIndex": i, "endColumnIndex": i + 1}
    lo, hi = prog_report.BAND
    rules = []
    for kind, vals, bg, fg in (("NUMBER_BETWEEN", [lo, hi], GREEN_BG, GREEN_FG), ("NUMBER_LESS", [lo], RED_BG, RED_FG),
                               ("NUMBER_GREATER", [hi], YELLOW_BG, YELLOW_FG)):
        rules.append({"addConditionalFormatRule": {"index": 0, "rule": {"ranges": [column(win)], "booleanRule": {
            "condition": {"type": kind, "values": [{"userEnteredValue": str(v)} for v in vals]},
            "format": {"backgroundColor": bg, "textFormat": {"foregroundColor": fg, "bold": True}}}}}})
    rules.append({"repeatCell": {"range": column(COLUMNS.index("prog")), "cell": {"userEnteredFormat": {"numberFormat": PROG_FORMAT}},
                                 "fields": "userEnteredFormat.numberFormat"}})
    sh.batch_update({"requests": rules})


# ======================================== Dashboard ========================================
GRID_TITLE, GRID_HEAD, GRID_FIRST, GRID_LAST = 8, 9, 10, 15      # 6 progs
CHANGE_COL = "I"                                                  # the change grid starts here
FLAG_TITLE, FLAG_HEAD, FLAG_FIRST, FLAG_LAST = 18, 19, 20, 49     # 30 lines
TEAM_TITLE, TEAM_HEAD, TEAM_FIRST, TEAM_LAST = 52, 53, 54, 83     # 30 rows
HERO_TITLE, HERO_HEAD, HERO_FIRST, HERO_LAST = 86, 87, 88, 207    # 120 rows

R = f"'{RUNS}'!"
SEL = f"IF($B$3=\"Latest\",'{LISTS}'!$B$2,$B$3)"
CMP = "IF($D$3=\"None\",\"\",$D$3)"


def col(name): return f"{R}${COL[name]}$2:${COL[name]}"


def team_rows(when):
    """LET pieces: m = the team rows of run `when`; progs down, teams across."""
    return (f"m,FILTER({R}$A$2:${LAST},{col('run_time')}={when},{col('row')}=\"team\"),"
            "progs,SORT(UNIQUE(INDEX(m,0,3))),teams,TRANSPOSE(SORT(UNIQUE(INDEX(m,0,4)))),")


def cell_of(m):
    return (f"LAMBDA(p,t,IFERROR(INDEX(FILTER(INDEX({m},0,6),INDEX({m},0,3)=p,INDEX({m},0,4)=t),1),\"\"))")


FORMULAS = {
    "A1": "Prog Dashboard",
    "A2": "Each prog's win rate at its milestone stage, marks on - the target is 50-70% (green); red is below, yellow "
          "above. Pick a run by date (newest first); 'Compare with' adds how much each cell moved since that run.",
    "A3": "Run", "C3": "Compare with",
    # tiles: a grey label over a big value
    "A5": "In the band (50-70%)", "C5": "Below 50%", "E5": "Above 70%", "G5": "Run", "J5": "Compared with",
    f"A{GRID_TITLE}": "Win rate at the milestone stage",
    f"{CHANGE_COL}{GRID_TITLE}": "Change since the compared run",
    f"A{GRID_HEAD}": f"=IFERROR(LET(sel,{SEL},{team_rows('sel')}HSTACK(\"Prog\",teams)),\"No run yet\")",
    f"A{GRID_FIRST}": (f"=IFERROR(LET(sel,{SEL},{team_rows('sel')}get,{cell_of('m')},"
                       "HSTACK(progs,MAKEARRAY(ROWS(progs),COLUMNS(teams),LAMBDA(i,j,get(INDEX(progs,i),INDEX(teams,1,j)))))),\"\")"),
    f"{CHANGE_COL}{GRID_HEAD}": f"=IFERROR(LET(sel,{SEL},{team_rows('sel')}HSTACK(\"Prog\",teams)),\"\")",
    f"{CHANGE_COL}{GRID_FIRST}": (
        f"=IFERROR(LET(sel,{SEL},cmp,{CMP},{team_rows('sel')}"
        f"w,IF(cmp=\"\",m,FILTER({R}$A$2:${LAST},{col('run_time')}=cmp,{col('row')}=\"team\")),"
        f"now,{cell_of('m')},was,{cell_of('w')},"
        "HSTACK(progs,MAKEARRAY(ROWS(progs),COLUMNS(teams),LAMBDA(i,j,LET(a,now(INDEX(progs,i),INDEX(teams,1,j)),"
        "b,was(INDEX(progs,i),INDEX(teams,1,j)),IF(OR(cmp=\"\",a=\"\",b=\"\"),\"\",a-b)))))),\"\")"),
    "A6": f"=IF(COUNT(B{GRID_FIRST}:G{GRID_LAST})=0,\"\",COUNTIFS(B{GRID_FIRST}:G{GRID_LAST},\">=0.5\",B{GRID_FIRST}:G{GRID_LAST},\"<=0.7\")&\" of \"&COUNT(B{GRID_FIRST}:G{GRID_LAST}))",
    "C6": f"=IF(COUNT(B{GRID_FIRST}:G{GRID_LAST})=0,\"\",COUNTIF(B{GRID_FIRST}:G{GRID_LAST},\"<0.5\"))",
    "E6": f"=IF(COUNT(B{GRID_FIRST}:G{GRID_LAST})=0,\"\",COUNTIF(B{GRID_FIRST}:G{GRID_LAST},\">0.7\"))",
    "G6": f"={SEL}",
    "J6": f"=IF({CMP}=\"\",\"-\",{CMP})",
    f"A{FLAG_TITLE}": "What is off",
    f"A{FLAG_HEAD}": "Prog", f"B{FLAG_HEAD}": "Problem",
    f"A{FLAG_FIRST}": (f"=IFERROR(LET(sel,{SEL},SORT(FILTER(HSTACK('{FLAGS}'!$B$2:$B,'{FLAGS}'!$D$2:$D),'{FLAGS}'!$A$2:$A=sel),1,TRUE)),\"Nothing off\")"),
    f"A{TEAM_TITLE}": "Teams",
    f"A{TEAM_FIRST}": (f"=IFERROR(LET(sel,{SEL},SORT(FILTER(HSTACK({col('prog')},{col('team')},{col('win')},"
                       f"{col('win_marks_off')},{col('win_next_stage')},{col('win_stage_after')},{col('fight_seconds')},"
                       f"{col('heroes_left')},{col('team_hp_left')},{col('team_dps')}),{col('run_time')}=sel,"
                       f"{col('row')}=\"team\"),1,TRUE,2,TRUE)),\"\")"),
    f"A{HERO_TITLE}": "Heroes",
    f"A{HERO_FIRST}": (f"=IFERROR(LET(sel,{SEL},SORT(FILTER(HSTACK({col('prog')},{col('team')},{col('hero')},{col('role')},"
                       f"{col('dps')},{col('damage_share')},{col('damage_taken')},{col('survives')},{col('died_at')}),"
                       f"{col('run_time')}=sel,{col('row')}=\"hero\"),1,TRUE,2,TRUE,6,FALSE)),\"\")"),
}
TEAM_HEADER = ["Prog", "Team", "Win at milestone", "Win, marks off", "Win, next stage", "Win, stage after",
               "Fight length (s)", "Heroes left", "Team HP left", "Team DPS"]
HERO_HEADER = ["Prog", "Team", "Hero", "Role", "DPS", "Damage share", "Damage taken", "Survives", "Died at (s)"]


def build_dashboard(sh):
    for title in (DASH, LISTS):
        try: sh.del_worksheet(sh.worksheet(title))
        except gspread.exceptions.WorksheetNotFound: pass

    lists = sh.add_worksheet(title=LISTS, rows=60, cols=3)
    runs = f"SORT(UNIQUE(FILTER({col('run_time')},{col('run_time')}<>\"\")),1,FALSE)"
    lists.update([["Run", "Newest", "Compare with"],
                  [f"=VSTACK(\"Latest\",{runs})", f"=IFERROR(INDEX({runs},1),\"\")", f"=VSTACK(\"None\",{runs})"]],
                 range_name="A1", value_input_option="USER_ENTERED")

    ws = sh.add_worksheet(title=DASH, rows=HERO_LAST + 5, cols=16, index=0)
    cells = [{"range": a1, "values": [[v]]} for a1, v in FORMULAS.items()]
    cells += [{"range": "B3", "values": [["Latest"]]}, {"range": "D3", "values": [["None"]]},
              {"range": f"A{TEAM_HEAD}", "values": [TEAM_HEADER]}, {"range": f"A{HERO_HEAD}", "values": [HERO_HEADER]}]
    ws.batch_update(cells, value_input_option="USER_ENTERED")

    sid, lid = ws.id, lists.id

    def rng(r0, r1, c0, c1): return {"sheetId": sid, "startRowIndex": r0 - 1, "endRowIndex": r1, "startColumnIndex": c0, "endColumnIndex": c1}

    def fmt(r, f, fields): return {"repeatCell": {"range": r, "cell": {"userEnteredFormat": f}, "fields": fields}}

    def pick(a1_col, source_col):
        return {"setDataValidation": {"range": rng(3, 3, a1_col, a1_col + 1), "rule": {
            "condition": {"type": "ONE_OF_RANGE", "values": [{"userEnteredValue": f"='{LISTS}'!${source_col}$2:${source_col}$60"}]},
            "showCustomUi": True, "strict": True}}}

    def band(ranges):
        lo, hi = prog_report.BAND
        out = []
        for kind, vals, bg, fg in (("NUMBER_BETWEEN", [lo, hi], GREEN_BG, GREEN_FG), ("NUMBER_LESS", [lo], RED_BG, RED_FG),
                                   ("NUMBER_GREATER", [hi], YELLOW_BG, YELLOW_FG)):
            out.append({"addConditionalFormatRule": {"index": 0, "rule": {"ranges": ranges, "booleanRule": {
                "condition": {"type": kind, "values": [{"userEnteredValue": str(v)} for v in vals]},
                "format": {"backgroundColor": bg, "textFormat": {"foregroundColor": fg, "bold": True}}}}}})
        return out

    def updown(ranges):
        return [{"addConditionalFormatRule": {"index": 0, "rule": {"ranges": ranges, "booleanRule": {
            "condition": {"type": kind, "values": [{"userEnteredValue": "0"}]},
            "format": {"textFormat": {"foregroundColor": fg, "bold": True}}}}}}
            for kind, fg in (("NUMBER_GREATER", UP_FG), ("NUMBER_LESS", DOWN_FG))]

    # The Team Dashboard's look: Arial 10, a 16pt title over a grey italic help line, bold labels beside pale yellow
    # dropdowns, tiles of a grey label over a big bold value, grey table headers, a line between column groups.
    def table(head, first, last, groups, width_to):
        out = [fmt(rng(head, head, 0, width_to), {"backgroundColor": HEADER_BG, "textFormat": {"bold": True},
                   "horizontalAlignment": "CENTER", "verticalAlignment": "BOTTOM", "wrapStrategy": "WRAP"},
                   "userEnteredFormat(backgroundColor,textFormat,horizontalAlignment,verticalAlignment,wrapStrategy)"),
               fmt(rng(first, last, 0, 1), {"textFormat": {"bold": True}, "horizontalAlignment": "LEFT"},
                   "userEnteredFormat(textFormat,horizontalAlignment)")]
        line = {"style": "SOLID_MEDIUM", "color": GROUP_LINE}
        out += [{"updateBorders": {"range": rng(head, last, c, c + 1), "left": line}} for c in groups]
        return out

    def number(r, f): return fmt(r, {"numberFormat": f}, "userEnteredFormat.numberFormat")

    change0 = ord(CHANGE_COL) - ord("A")
    label = {"textFormat": {"bold": True}, "horizontalAlignment": "RIGHT"}
    pick_cell = {"backgroundColor": PICK_BG, "horizontalAlignment": "LEFT"}
    centred_big = {"horizontalAlignment": "CENTER", "textFormat": {"fontSize": 12}}
    one_decimal = {"type": "NUMBER", "pattern": "0.0"}
    req = [
        {"updateSheetProperties": {"properties": {"sheetId": sid, "gridProperties": {"frozenRowCount": 3, "frozenColumnCount": 1}},
                                   "fields": "gridProperties(frozenRowCount,frozenColumnCount)"}},
        fmt(rng(1, HERO_LAST, 0, 16), {"textFormat": {"fontFamily": "Arial", "fontSize": 10}}, "userEnteredFormat.textFormat"),
        fmt(rng(1, 1, 0, 1), {"textFormat": {"bold": True, "fontSize": 16}}, "userEnteredFormat.textFormat"),
        fmt(rng(2, 2, 0, 1), {"textFormat": {"italic": True, "foregroundColor": MUTED_FG}}, "userEnteredFormat.textFormat"),
        # the dropdowns
        *[fmt(rng(3, 3, c, c + 1), label, "userEnteredFormat(textFormat,horizontalAlignment)") for c in (0, 2)],
        *[fmt(rng(3, 3, c, c + 1), pick_cell, "userEnteredFormat(backgroundColor,horizontalAlignment)") for c in (1, 3)],
        pick(1, "A"), pick(3, "C"),
        # tiles
        fmt(rng(5, 5, 0, 16), {"textFormat": {"foregroundColor": MUTED_FG}, "horizontalAlignment": "LEFT"},
            "userEnteredFormat(textFormat,horizontalAlignment)"),
        fmt(rng(6, 6, 0, 16), {"textFormat": {"bold": True, "fontSize": 14}, "horizontalAlignment": "LEFT"},
            "userEnteredFormat(textFormat,horizontalAlignment)"),
        # section titles
        *[fmt(rng(r, r, c, c + 1), {"textFormat": {"bold": True, "fontSize": 12}}, "userEnteredFormat.textFormat")
          for r, c in ((GRID_TITLE, 0), (GRID_TITLE, change0), (FLAG_TITLE, 0), (TEAM_TITLE, 0), (HERO_TITLE, 0))],
        # the two grids: [prog] | [one column per team]
        *table(GRID_HEAD, GRID_FIRST, GRID_LAST, (1,), 7),
        fmt(rng(GRID_HEAD, GRID_HEAD, change0, change0 + 7), {"backgroundColor": HEADER_BG, "textFormat": {"bold": True},
            "horizontalAlignment": "CENTER"}, "userEnteredFormat(backgroundColor,textFormat,horizontalAlignment)"),
        fmt(rng(GRID_FIRST, GRID_LAST, change0, change0 + 1), {"textFormat": {"bold": True}}, "userEnteredFormat.textFormat"),
        {"updateBorders": {"range": rng(GRID_HEAD, GRID_LAST, change0 + 1, change0 + 2),
                           "left": {"style": "SOLID_MEDIUM", "color": GROUP_LINE}}},
        number(rng(GRID_FIRST, GRID_LAST, 0, 1), PROG_FORMAT),
        number(rng(GRID_FIRST, GRID_LAST, change0, change0 + 1), PROG_FORMAT),
        fmt(rng(GRID_FIRST, GRID_LAST, 1, 7), {"numberFormat": PCT_FORMAT, **centred_big},
            "userEnteredFormat(numberFormat,horizontalAlignment,textFormat)"),
        fmt(rng(GRID_FIRST, GRID_LAST, change0 + 1, change0 + 7), {"numberFormat": SIGNED_PCT, **centred_big},
            "userEnteredFormat(numberFormat,horizontalAlignment,textFormat)"),
        # what is off: [prog] | [problem]
        *table(FLAG_HEAD, FLAG_FIRST, FLAG_LAST, (1,), 2),
        number(rng(FLAG_FIRST, FLAG_LAST, 0, 1), PROG_FORMAT),
        # teams: [prog, team] | [win rates] | [the fight]
        *table(TEAM_HEAD, TEAM_FIRST, TEAM_LAST, (2, 6), 10),
        number(rng(TEAM_FIRST, TEAM_LAST, 0, 1), PROG_FORMAT),
        number(rng(TEAM_FIRST, TEAM_LAST, 2, 6), PCT_FORMAT),
        number(rng(TEAM_FIRST, TEAM_LAST, 6, 8), one_decimal),
        number(rng(TEAM_FIRST, TEAM_LAST, 8, 9), PCT_FORMAT),
        number(rng(TEAM_FIRST, TEAM_LAST, 9, 10), {"type": "NUMBER", "pattern": "0"}),
        # heroes: [prog, team, hero, role] | [damage] | [tanking]
        *table(HERO_HEAD, HERO_FIRST, HERO_LAST, (4, 6), 9),
        number(rng(HERO_FIRST, HERO_LAST, 0, 1), PROG_FORMAT),
        number(rng(HERO_FIRST, HERO_LAST, 5, 6), PCT_FORMAT),
        number(rng(HERO_FIRST, HERO_LAST, 7, 8), PCT_FORMAT),
        number(rng(HERO_FIRST, HERO_LAST, 8, 9), one_decimal),
        {"updateDimensionProperties": {"range": {"sheetId": sid, "dimension": "COLUMNS", "startIndex": 0, "endIndex": 16},
                                       "properties": {"pixelSize": 100}, "fields": "pixelSize"}},
        {"updateDimensionProperties": {"range": {"sheetId": sid, "dimension": "COLUMNS", "startIndex": 1, "endIndex": 2},
                                       "properties": {"pixelSize": 150}, "fields": "pixelSize"}},
        {"updateSheetProperties": {"properties": {"sheetId": lid, "hidden": True}, "fields": "hidden"}},
    ]
    req += band([rng(GRID_FIRST, GRID_LAST, 1, 7), rng(TEAM_FIRST, TEAM_LAST, 2, 3)])
    req += updown([rng(GRID_FIRST, GRID_LAST, change0 + 1, change0 + 7)])
    sh.batch_update({"requests": req})
    return ws


def publish(path):
    """Append a run to Runs, make the Dashboard if there is none, and drop tabs this sheet no longer uses."""
    sh = sheet_sync.get_spreadsheet(sheet_sync.get_client(), SHEET)
    when, added = append_run(sh, path)
    titles = [w.title for w in sh.worksheets()]
    dash = sh.worksheet(DASH) if DASH in titles and LISTS in titles else build_dashboard(sh)
    for other in sh.worksheets():
        if other.title not in (RUNS, DASH, LISTS, FLAGS):
            sh.del_worksheet(other)                     # the old single "Prog" tab, a new sheet's empty first tab
    return dash, when, added


def main():
    if len(sys.argv) != 2:
        sys.exit("usage: prog_sheet.py <path to teamcomp csv>  |  prog_sheet.py --rebuild")
    if sys.argv[1] == "--rebuild":
        sh = sheet_sync.get_spreadsheet(sheet_sync.get_client(), SHEET)
        ws = build_dashboard(sh)
        print(f"Rebuilt the Dashboard: {ws.url}")
        return
    ws, when, added = publish(sys.argv[1])
    print(f"Run {when}: {added} rows added" + (" (already there)" if added == 0 else "") + f". Dashboard: {ws.url}")


if __name__ == "__main__":
    main()
