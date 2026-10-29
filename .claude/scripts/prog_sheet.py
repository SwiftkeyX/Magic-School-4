#!/usr/bin/env python3
"""The "MagicSchool4 Prog" sheet, built like the Benchmark sheet: every run kept, and a dashboard of live formulas.

    Runs       - every Team Comp Benchmark run, appended: per prog x team a "team" row (win rate at the milestone stage
                 and after, fight length, ...) and per hero a "hero" row
    Dashboard  - pick a run, one to compare with, and a prog (or All): each prog's win rate per team in colour, the
                 change since the compared run, then that prog's rows from Runs. Formulas only, read from Runs
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
TABLE_TITLE, TABLE_HEAD, TABLE_FIRST, TABLE_LAST = 18, 19, 20, 219 # the picked prog's Runs rows, or every prog's (200)

R = f"'{RUNS}'!"
# The dropdowns show "yyyy-mm-dd hh:mm · N progs", not a bare time: Sheets reads a bare time picked from a list as a
# date number, which then matches neither the list nor Runs. The formulas take its first 16 characters.
SEL = f"IF($B$3=\"Latest\",'{LISTS}'!$B$2,LEFT($B$3,16))"
CMP = "IF($D$3=\"None\",\"\",LEFT($D$3,16))"
# "All" in the Prog dropdown reads as prog 0: the table then keeps every prog of the run
PROG = "IF($F$3=\"All\",0,VALUE(SUBSTITUTE($F$3,\"prog-\",\"\")))"


def col(name): return f"{R}${COL[name]}$2:${COL[name]}"


def team_rows(when):
    """LET pieces: m = the team rows of run `when`; progs down, teams across."""
    return (f"m,FILTER({R}$A$2:${LAST},{col('run_time')}={when},{col('row')}=\"team\"),"
            "progs,SORT(UNIQUE(INDEX(m,0,3))),teams,TRANSPOSE(SORT(UNIQUE(INDEX(m,0,4)))),")


def cell_of(m):
    return (f"LAMBDA(p,t,IFERROR(INDEX(FILTER(INDEX({m},0,6),INDEX({m},0,3)=p,INDEX({m},0,4)=t),1),\"\"))")


GRID = f"B{GRID_FIRST}:G{GRID_LAST}"
FORMULAS = {
    "A1": "Prog Dashboard",
    "A2": "Each prog's win rate at its milestone stage, marks on - the target is 50-70% (green); red is below, yellow "
          "above. Pick a run by date (newest first), a run to compare with, and the prog whose rows you want below (All for every prog).",
    "A3": "Run", "C3": "Compare with", "E3": "Prog",
    # tiles: a grey label over a big value
    "A5": "In the band (50-70%)", "C5": "Below 50%", "E5": "Above 70%", "G5": "Run", "J5": "Compared with",
    "A6": f"=IF(COUNT({GRID})=0,\"\",COUNTIFS({GRID},\">=0.5\",{GRID},\"<=0.7\")&\" of \"&COUNT({GRID}))",
    "C6": f"=IF(COUNT({GRID})=0,\"\",COUNTIF({GRID},\"<0.5\"))",
    "E6": f"=IF(COUNT({GRID})=0,\"\",COUNTIF({GRID},\">0.7\"))",
    "G6": f"={SEL}",
    "J6": f"=IF({CMP}=\"\",\"-\",{CMP})",
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
    # the picked prog's rows of the picked run (every prog's for "All"), as in Runs: by prog, each team's row, then its
    # heroes by damage share
    f"A{TABLE_TITLE}": "=\"Runs - \"&IF($F$3=\"All\",\"all progs\",$F$3)",
    f"A{TABLE_FIRST}": (f"=IFERROR(LET(sel,{SEL},p,{PROG},SORT(FILTER({R}$A$2:${LAST},{col('run_time')}=sel,"
                        f"({col('prog')}=p)+(p=0)),3,TRUE,4,TRUE,2,FALSE,{COLUMNS.index('damage_share') + 1},FALSE)),"
                        f"IF($F$3=\"All\",\"Nothing in this run\",\"No \"&$F$3&\" in this run\"))"),
}

# Runs' column blocks, repeated over the table: (header colour, tint, first column, last column) - 0-based
TEAM_BLOCK_FG, HERO_BLOCK_FG = rgb(0.98, 0.8, 0.6), rgb(0.7, 0.8, 0.95)
BLOCKS = [(HEADER_BG, rgb(1, 1, 1), COLUMNS.index("run_time"), COLUMNS.index("comp")),
          (TEAM_BLOCK_FG, rgb(1.0, 0.95, 0.9), COLUMNS.index("win"), COLUMNS.index("team_dps")),
          (HERO_BLOCK_FG, rgb(0.93, 0.95, 1.0), COLUMNS.index("hero"), COLUMNS.index("died_at"))]


def first_prog_of_latest(sh):
    """The prog the Prog dropdown starts on: the lowest prog of the newest run."""
    rows = sh.worksheet(RUNS).get_all_values()[1:]
    if not rows: return "prog-1"
    newest = max(r[0] for r in rows)
    progs = sorted({int(re.sub(r"\D", "", r[2])) for r in rows if r[0] == newest and re.search(r"\d", r[2])})
    return f"prog-{progs[0]}" if progs else "prog-1"


def build_dashboard(sh):
    for title in (DASH, LISTS):
        try: sh.del_worksheet(sh.worksheet(title))
        except gspread.exceptions.WorksheetNotFound: pass

    lists = sh.add_worksheet(title=LISTS, rows=60, cols=4)
    runs = f"SORT(UNIQUE(FILTER({col('run_time')},{col('run_time')}<>\"\")),1,FALSE)"
    labels = (f"MAP({runs},LAMBDA(r,r&\" · \"&COUNTUNIQUE(FILTER({col('prog')},{col('run_time')}=r))"
              f"&IF(COUNTUNIQUE(FILTER({col('prog')},{col('run_time')}=r))=1,\" prog\",\" progs\")))")
    progs = f"=VSTACK(\"All\",ARRAYFORMULA(\"prog-\"&SORT(UNIQUE(FILTER({col('prog')},{col('prog')}<>\"\")))))"
    lists.update([["Run", "Newest", "Compare with", "Prog"],
                  [f"=VSTACK(\"Latest\",{labels})", f"=IFERROR(INDEX({runs},1),\"\")", f"=VSTACK(\"None\",{labels})", progs]],
                 range_name="A1", value_input_option="USER_ENTERED")

    width = len(COLUMNS)
    ws = sh.add_worksheet(title=DASH, rows=TABLE_LAST + 5, cols=width, index=0)
    cells = [{"range": a1, "values": [[v]]} for a1, v in FORMULAS.items()]
    cells += [{"range": "B3", "values": [["Latest"]]}, {"range": "D3", "values": [["None"]]},
              {"range": "F3", "values": [[first_prog_of_latest(sh)]]},
              {"range": f"A{TABLE_HEAD}", "values": [COLUMNS]}]
    ws.batch_update(cells, value_input_option="USER_ENTERED")

    sid, lid = ws.id, lists.id

    def rng(r0, r1, c0, c1): return {"sheetId": sid, "startRowIndex": r0 - 1, "endRowIndex": r1, "startColumnIndex": c0, "endColumnIndex": c1}

    def fmt(r, f, fields): return {"repeatCell": {"range": r, "cell": {"userEnteredFormat": f}, "fields": fields}}

    def number(r, f): return fmt(r, {"numberFormat": f}, "userEnteredFormat.numberFormat")

    def pick(c, source_col):
        return {"setDataValidation": {"range": rng(3, 3, c, c + 1), "rule": {
            "condition": {"type": "ONE_OF_RANGE", "values": [{"userEnteredValue": f"='{LISTS}'!${source_col}$2:${source_col}$60"}]},
            "showCustomUi": True, "strict": True}}}

    def band(ranges):
        lo, hi = prog_report.BAND
        return [{"addConditionalFormatRule": {"index": 0, "rule": {"ranges": ranges, "booleanRule": {
            "condition": {"type": kind, "values": [{"userEnteredValue": str(v)} for v in vals]},
            "format": {"backgroundColor": bg, "textFormat": {"foregroundColor": fg, "bold": True}}}}}}
            for kind, vals, bg, fg in (("NUMBER_BETWEEN", [lo, hi], GREEN_BG, GREEN_FG), ("NUMBER_LESS", [lo], RED_BG, RED_FG),
                                       ("NUMBER_GREATER", [hi], YELLOW_BG, YELLOW_FG))]

    def updown(ranges):
        return [{"addConditionalFormatRule": {"index": 0, "rule": {"ranges": ranges, "booleanRule": {
            "condition": {"type": kind, "values": [{"userEnteredValue": "0"}]},
            "format": {"textFormat": {"foregroundColor": fg, "bold": True}}}}}}
            for kind, fg in (("NUMBER_GREATER", UP_FG), ("NUMBER_LESS", DOWN_FG))]

    # The Team Dashboard's look: Arial 10, a 16pt title over a grey italic help line, bold labels beside pale yellow
    # dropdowns, tiles of a grey label over a big bold value, grey table headers, a line between column groups.
    header = {"backgroundColor": HEADER_BG, "textFormat": {"bold": True}, "horizontalAlignment": "CENTER",
              "verticalAlignment": "BOTTOM", "wrapStrategy": "WRAP"}
    header_fields = "userEnteredFormat(backgroundColor,textFormat,horizontalAlignment,verticalAlignment,wrapStrategy)"
    line = {"style": "SOLID_MEDIUM", "color": GROUP_LINE}
    change0 = ord(CHANGE_COL) - ord("A")
    label = {"textFormat": {"bold": True}, "horizontalAlignment": "RIGHT"}
    pick_cell = {"backgroundColor": PICK_BG, "horizontalAlignment": "LEFT"}
    centred_big = {"horizontalAlignment": "CENTER", "textFormat": {"fontSize": 12}}
    one_decimal = {"type": "NUMBER", "pattern": "0.0"}
    c = COLUMNS.index
    req = [
        {"updateSheetProperties": {"properties": {"sheetId": sid, "gridProperties": {"frozenRowCount": 3, "frozenColumnCount": 1}},
                                   "fields": "gridProperties(frozenRowCount,frozenColumnCount)"}},
        fmt(rng(1, TABLE_LAST, 0, width), {"textFormat": {"fontFamily": "Arial", "fontSize": 10}}, "userEnteredFormat.textFormat"),
        fmt(rng(1, 1, 0, 1), {"textFormat": {"bold": True, "fontSize": 16}}, "userEnteredFormat.textFormat"),
        fmt(rng(2, 2, 0, 1), {"textFormat": {"italic": True, "foregroundColor": MUTED_FG}}, "userEnteredFormat.textFormat"),
        # the dropdowns: Run, Compare with, Prog
        *[fmt(rng(3, 3, i, i + 1), label, "userEnteredFormat(textFormat,horizontalAlignment)") for i in (0, 2, 4)],
        *[fmt(rng(3, 3, i, i + 1), pick_cell, "userEnteredFormat(backgroundColor,horizontalAlignment)") for i in (1, 3, 5)],
        pick(1, "A"), pick(3, "C"), pick(5, "D"),
        # tiles
        fmt(rng(5, 5, 0, width), {"textFormat": {"foregroundColor": MUTED_FG}, "horizontalAlignment": "LEFT"},
            "userEnteredFormat(textFormat,horizontalAlignment)"),
        fmt(rng(6, 6, 0, width), {"textFormat": {"bold": True, "fontSize": 14}, "horizontalAlignment": "LEFT"},
            "userEnteredFormat(textFormat,horizontalAlignment)"),
        # section titles
        *[fmt(rng(r, r, i, i + 1), {"textFormat": {"bold": True, "fontSize": 12}}, "userEnteredFormat.textFormat")
          for r, i in ((GRID_TITLE, 0), (GRID_TITLE, change0), (TABLE_TITLE, 0))],
        # the two grids: [prog] | [one column per team]
        fmt(rng(GRID_HEAD, GRID_HEAD, 0, 7), header, header_fields),
        fmt(rng(GRID_HEAD, GRID_HEAD, change0, change0 + 7), header, header_fields),
        fmt(rng(GRID_FIRST, GRID_LAST, 0, 1), {"numberFormat": PROG_FORMAT, "textFormat": {"bold": True}},
            "userEnteredFormat(numberFormat,textFormat)"),
        fmt(rng(GRID_FIRST, GRID_LAST, change0, change0 + 1), {"numberFormat": PROG_FORMAT, "textFormat": {"bold": True}},
            "userEnteredFormat(numberFormat,textFormat)"),
        {"updateBorders": {"range": rng(GRID_HEAD, GRID_LAST, 1, 2), "left": line}},
        {"updateBorders": {"range": rng(GRID_HEAD, GRID_LAST, change0 + 1, change0 + 2), "left": line}},
        fmt(rng(GRID_FIRST, GRID_LAST, 1, 7), {"numberFormat": PCT_FORMAT, **centred_big},
            "userEnteredFormat(numberFormat,horizontalAlignment,textFormat)"),
        fmt(rng(GRID_FIRST, GRID_LAST, change0 + 1, change0 + 7), {"numberFormat": SIGNED_PCT, **centred_big},
            "userEnteredFormat(numberFormat,horizontalAlignment,textFormat)"),
        # the Runs table: the same header, blocks, tints and number formats as the Runs tab
        fmt(rng(TABLE_HEAD, TABLE_HEAD, 0, width), header, header_fields),
        number(rng(TABLE_FIRST, TABLE_LAST, c("prog"), c("prog") + 1), PROG_FORMAT),
        *[number(rng(TABLE_FIRST, TABLE_LAST, c(n), c(n) + 1), PCT_FORMAT)
          for n in ("win", "win_marks_off", "win_next_stage", "win_stage_after", "team_hp_left", "damage_share", "survives")],
        *[number(rng(TABLE_FIRST, TABLE_LAST, c(n), c(n) + 1), one_decimal) for n in ("fight_seconds", "heroes_left", "died_at")],
        {"updateDimensionProperties": {"range": {"sheetId": sid, "dimension": "COLUMNS", "startIndex": 0, "endIndex": width},
                                       "properties": {"pixelSize": 100}, "fields": "pixelSize"}},
        {"updateDimensionProperties": {"range": {"sheetId": sid, "dimension": "COLUMNS", "startIndex": 1, "endIndex": 2},
                                       "properties": {"pixelSize": 150}, "fields": "pixelSize"}},
        {"updateDimensionProperties": {"range": {"sheetId": sid, "dimension": "COLUMNS", "startIndex": c("comp"), "endIndex": c("comp") + 1},
                                       "properties": {"pixelSize": 170}, "fields": "pixelSize"}},
        {"updateSheetProperties": {"properties": {"sheetId": lid, "hidden": True}, "fields": "hidden"}},
    ]
    for head_bg, tint, c0, c1 in BLOCKS:
        req.append(fmt(rng(TABLE_HEAD, TABLE_HEAD, c0, c1 + 1), {"backgroundColor": head_bg}, "userEnteredFormat.backgroundColor"))
        req.append(fmt(rng(TABLE_FIRST, TABLE_LAST, c0, c1 + 1), {"backgroundColor": tint}, "userEnteredFormat.backgroundColor"))
        if c0 > 0:
            req.append({"updateBorders": {"range": rng(TABLE_HEAD, TABLE_LAST, c0, c0 + 1),
                                          "left": {"style": "SOLID_THICK", "color": rgb(0.4, 0.4, 0.4)}}})
    # team rows bold all the way across, so each team reads as a heading over its heroes
    req.append({"addConditionalFormatRule": {"index": 0, "rule": {"ranges": [rng(TABLE_FIRST, TABLE_LAST, 0, width)], "booleanRule": {
        "condition": {"type": "CUSTOM_FORMULA", "values": [{"userEnteredValue": f"=$B{TABLE_FIRST}=\"team\""}]},
        "format": {"textFormat": {"bold": True}}}}}})
    req += band([rng(GRID_FIRST, GRID_LAST, 1, 7), rng(TABLE_FIRST, TABLE_LAST, c("win"), c("win") + 1)])
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
