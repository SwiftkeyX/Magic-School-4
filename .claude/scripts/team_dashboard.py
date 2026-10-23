#!/usr/bin/env python3
"""Build the "Team Dashboard" tab of the "MagicSchool4 Benchmark" sheet - the team comp twin of the hero "Dashboard".

It holds formulas only: every number is read live from the "Team Runs" tab, so a new import shows up on its own and this
script only has to run again when the dashboard itself changes. Its dropdown lists live in a hidden "Team Lists" tab.

    python .claude/scripts/team_dashboard.py            (build it; refuses if the tab is already there)
    python .claude/scripts/team_dashboard.py --rebuild  (delete both tabs and build them again)

Layout:
    row 3       Run / Compare with / Aligning dropdowns
    row 5-6     tiles: comps, fights, best comp, most stages won, what aligning adds
    row 9-29    every comp x every stage: stages won aligned and not, the change since the compared run, and the win rate
                at each stage (aligned or not, by the dropdown) - room for 20 comps
    row 31-     one comp, picked in B32: each stage, then each hero averaged over the stages
"""
import sys
from pathlib import Path

import gspread

sys.path.insert(0, str(Path(__file__).parent))
import sheet_sync  # noqa: E402
from benchmark_import import WIN_LINE  # noqa: E402

SHEET = "magicschool4-benchmark"
TAB, LISTS = "Team Dashboard", "Team Lists"
RUNS = "'Team Runs'"

MATRIX_HEADER, MATRIX_FIRST, MATRIX_LAST = 9, 10, 29      # 20 comps
COMP_PICK = 32
STAGE_HEADER, STAGE_FIRST, STAGE_LAST = 34, 35, 44        # 10 stages
HERO_HEADER, HERO_FIRST, HERO_LAST = 46, 47, 58           # 12 heroes


def rgb(r, g, b): return {"red": r, "green": g, "blue": b}


WIN_BG, WIN_FG = rgb(0.8, 0.92, 0.8), rgb(0.0, 0.4, 0.0)
LOSS_BG, LOSS_FG = rgb(0.96, 0.8, 0.8), rgb(0.6, 0.0, 0.0)
# the same colours the hero "Dashboard" uses
UP_FG, DOWN_FG = rgb(0.098, 0.498, 0.098), rgb(0.749, 0.349, 0.0)
PICK_BG = rgb(1.0, 0.969, 0.847)               # a dropdown cell
MUTED_FG = rgb(0.4, 0.4, 0.4)                  # the help line, the tile labels
HEADER_BG = rgb(0.898, 0.898, 0.898)
GROUP_LINE = rgb(0.498, 0.498, 0.498)          # the line between a table's column groups

# ======================================== formulas ========================================
# Shared head of every LET: columns are found by their header name, so a column the report gains never shifts them.
#   sel  - the run picked in B3 ("Latest" = the newest), as "yyyy-mm-dd hh:mm"
#   cmp  - the run picked in D3, "" for None
#   al   - "on" or "off", from F3
HEAD = (f"hdr,{RUNS}!$1:$1,data,{RUNS}!$A$2:$Z,c,LAMBDA(n,INDEX(data,0,XMATCH(n,hdr))),"
        "times,IFERROR(TEXT(c(\"run_time\"),\"yyyy-mm-dd hh:mm\"),\"\"),"
        f"sel,IF($B$3=\"Latest\",'{LISTS}'!$B$2,LEFT($B$3,16)),"
        "cmp,IF($D$3=\"None\",\"\",LEFT($D$3,16)),"
        "al,IF($F$3=\"Not aligned\",\"off\",\"on\"),"
        "isteam,c(\"row\")=\"team\",")


# the body closes the LET itself, after its last value
def let(body): return f"=ARRAYFORMULA(LET({HEAD}{body})"


MATRIX_HEADER_FORMULA = let(
    "stages,SORT(UNIQUE(FILTER(c(\"stage\"),times=sel,isteam))),"
    "IFERROR(HSTACK(\"Comp\",\"Stages won aligned\",\"Stages won not aligned\",\"Aligning adds\",\"Change since compared\","
    "\"Stage \"&TRANSPOSE(stages)),\"Comp\"))")

# a stage is won at WIN_LINE of its runs or more (See TeamCompBenchmark.WinLine), the same line the Team tabs colour on
MATRIX_FORMULA = let(
    "inrun,(times=sel)*isteam,"
    "comps,UNIQUE(FILTER(c(\"comp\"),inrun=1)),"
    "stages,TRANSPOSE(SORT(UNIQUE(FILTER(c(\"stage\"),inrun=1)))),"
    "won,LAMBDA(t,a,MAP(comps,LAMBDA(k,IFERROR(SUM(FILTER(--(c(\"win_rate\")>=" + str(WIN_LINE) + "),times=t,isteam,c(\"comp\")=k,c(\"aligning\")=a)),\"\")))),"
    "aligned,won(sel,\"on\"),"
    "plain,won(sel,\"off\"),"
    "was,IF(cmp=\"\",MAP(comps,LAMBDA(k,\"\")),won(cmp,\"on\")),"
    "change,MAP(aligned,was,LAMBDA(a,b,IF(b=\"\",\"\",a-b))),"
    "rates,MAKEARRAY(ROWS(comps),COLUMNS(stages),LAMBDA(i,j,IFERROR(INDEX(FILTER(c(\"win_rate\"),inrun=1,"
    "c(\"comp\")=INDEX(comps,i),c(\"stage\")=INDEX(stages,1,j),c(\"aligning\")=al),1),\"\"))),"
    "IFERROR(SORT(HSTACK(comps,aligned,plain,aligned-plain,change,rates),2,FALSE,3,FALSE),\"No team comp run yet\"))")

# a tile every other column, as on the hero Dashboard: label in row 5, value in row 6
TILES = {
    "A5": "Comps", "A6": f'=IF(A{MATRIX_FIRST}="No team comp run yet",0,COUNTA(A{MATRIX_FIRST}:A{MATRIX_LAST}))',
    "C5": "Fights", "C6": let("IFERROR(SUM(FILTER(c(\"runs\"),times=sel,isteam)),0))"),
    "E5": "Best comp", "E6": f'=IF(A6=0,"",A{MATRIX_FIRST})',
    "H5": "Its stages won (aligned)", "H6": f'=IF(A6=0,"",B{MATRIX_FIRST}&" of "&COUNTA(F{MATRIX_HEADER}:{MATRIX_HEADER}))',
    "J5": "Aligning adds (avg)", "J6": f'=IF(A6=0,"",AVERAGE(D{MATRIX_FIRST}:D{MATRIX_LAST}))',
    "L5": "Run time", "L6": let("sel)"),
}

STAGE_FORMULA = let(
    f"pick,$B${COMP_PICK},"
    "inrun,(times=sel)*isteam*(c(\"comp\")=pick),"
    "stages,SORT(UNIQUE(FILTER(c(\"stage\"),inrun=1))),"
    "get,LAMBDA(n,a,MAP(stages,LAMBDA(s,IFERROR(INDEX(FILTER(c(n),inrun=1,c(\"stage\")=s,c(\"aligning\")=a),1),\"\")))),"
    "IFERROR(HSTACK(stages,get(\"win_rate\",\"on\"),get(\"win_rate\",\"off\"),get(\"avg_seconds\",al),get(\"timeouts\",al),"
    "get(\"heroes_left\",al),get(\"team_hp_left\",al),get(\"team_dps\",al)),\"Pick a comp from this run\"))")

HERO_FORMULA = let(
    f"pick,$B${COMP_PICK},"
    "mine,(c(\"row\")=\"hero\")*(c(\"comp\")=pick)*(c(\"aligning\")=al),"
    "heroes,UNIQUE(FILTER(c(\"hero\"),mine*(times=sel)=1)),"
    "mean,LAMBDA(n,t,MAP(heroes,LAMBDA(h,IFERROR(AVERAGE(FILTER(c(n),mine=1,times=t,c(\"hero\")=h)),\"\")))),"
    "dps,mean(\"dps\",sel),"
    "was,IF(cmp=\"\",MAP(heroes,LAMBDA(h,\"\")),mean(\"dps\",cmp)),"
    "IFERROR(HSTACK(heroes,dps,mean(\"damage_share\",sel),mean(\"damage_taken\",sel),mean(\"healing\",sel),"
    "mean(\"survive_rate\",sel),mean(\"died_at\",sel),MAP(dps,was,LAMBDA(a,b,IF(OR(b=\"\",b=0),\"\",a/b-1)))),"
    "\"Pick a comp from this run\"))")

# the hidden lists the dropdowns read
COMP_COLUMN = f"INDEX({RUNS}!$A$2:$Z,0,XMATCH(\"comp\",{RUNS}!$1:$1))"
TIME_TEXT = f"TEXT({RUNS}!$A$2:$A,\"yyyy-mm-dd hh:mm\")"
LIST_FORMULAS = {
    "A1": "Run label", "B1": "Run time", "C1": "Run dropdown", "D1": "Compare dropdown", "E1": "Comps in the run", "F1": "Aligning",
    "A2": f"=ARRAYFORMULA(IFERROR(MAP(FILTER(B2:B,B2:B<>\"\"),LAMBDA(t,t&\" · \"&ROWS(UNIQUE(FILTER({COMP_COLUMN},{TIME_TEXT}=t)))&\" comps\")),\"\"))",
    "B2": f"=ARRAYFORMULA(SORT(UNIQUE(FILTER({TIME_TEXT},{RUNS}!$A$2:$A<>\"\")),1,FALSE))",
    "C2": "=VSTACK(\"Latest\",FILTER(A2:A,A2:A<>\"\"))",
    "D2": "=VSTACK(\"None\",FILTER(A2:A,A2:A<>\"\"))",
    "E2": (f"=ARRAYFORMULA(LET(sel,IF('{TAB}'!$B$3=\"Latest\",$B$2,LEFT('{TAB}'!$B$3,16)),"
           f"IFERROR(SORT(UNIQUE(FILTER({COMP_COLUMN},{TIME_TEXT}=sel,{COMP_COLUMN}<>\"\"))),\"\")))"),
    "F2": "Aligned", "F3": "Not aligned",
}


# ======================================== build ========================================
def build(sh):
    titles = [ws.title for ws in sh.worksheets()]
    at = titles.index("Team Runs") if "Team Runs" in titles else len(titles)
    lists = sh.add_worksheet(title=LISTS, rows=300, cols=6)
    ws = sh.add_worksheet(title=TAB, rows=60, cols=16, index=at)

    lists.update([[LIST_FORMULAS.get(f"{col}{row}", "") for col in "ABCDEF"] for row in (1, 2, 3)],
                 range_name="A1", value_input_option="USER_ENTERED")

    cells = {
        "A1": "Team Comp Benchmark Dashboard",
        "A2": (f"Pick a run by date (newest first). A stage is won when at least {WIN_LINE:.0%} of its runs are won. \"Compare with\" adds "
               "how many more stages each comp wins aligned than in that run. \"Aligning\" picks which fights the stage "
               "columns and the comp detail show."),
        "A3": "Run", "B3": "Latest", "C3": "Compare with", "D3": "None", "E3": "Aligning", "F3": "Aligned",
        "A8": "Every comp vs every stage",
        f"A{MATRIX_HEADER}": MATRIX_HEADER_FORMULA, f"A{MATRIX_FIRST}": MATRIX_FORMULA,
        f"A{COMP_PICK - 1}": "One comp",
        f"A{COMP_PICK}": "Comp",
        f"C{COMP_PICK}": "=\"Seconds to Team DPS, and every hero number, are \"&LOWER($F$3)&\" fights.\"",
        **{f"{col}{STAGE_HEADER}": name for col, name in zip("ABCDEFGH", (
            "Stage", "Win rate aligned", "Win rate not aligned", "Seconds", "Timeouts", "Heroes left", "Team HP left", "Team DPS"))},
        f"A{STAGE_FIRST}": STAGE_FORMULA,
        **{f"{col}{HERO_HEADER}": name for col, name in zip("ABCDEFGH", (
            "Hero (avg over stages)", "DPS", "Damage share", "Damage taken", "Healing", "Survive rate", "Died at", "DPS change"))},
        f"A{HERO_FIRST}": HERO_FORMULA,
        **TILES,
    }
    ws.batch_update([{"range": a1, "values": [[v]]} for a1, v in cells.items()], value_input_option="USER_ENTERED")
    style(sh, ws, lists)

    # start the comp detail on the best comp - read now, once the grid above has been worked out
    best = ws.acell(f"A{MATRIX_FIRST}").value or ""
    if best and not best.startswith("No team comp"):
        ws.update([[best]], range_name=f"B{COMP_PICK}")
    return ws


def style(sh, ws, lists):
    # Matches the hero "Dashboard": Arial 10 on white, a 16pt title over a grey italic help line, bold right-aligned
    # labels beside pale yellow dropdowns, tiles of a grey label over a big bold value, grey table headers, a line
    # between a table's column groups, text left and numbers right.
    sid = ws.id

    def area(r0, r1, c0, c1):  # 1-based, inclusive rows; 0-based, exclusive columns
        return {"sheetId": sid, "startRowIndex": r0 - 1, "endRowIndex": r1, "startColumnIndex": c0, "endColumnIndex": c1}

    def fmt(rng, f, fields):
        return {"repeatCell": {"range": rng, "cell": {"userEnteredFormat": f}, "fields": f"userEnteredFormat({fields})"}}

    def number(rng, pattern, kind="NUMBER"):
        return fmt(rng, {"numberFormat": {"type": kind, "pattern": pattern}}, "numberFormat")

    def width(c0, c1, px):
        return {"updateDimensionProperties": {"range": {"sheetId": sid, "dimension": "COLUMNS", "startIndex": c0, "endIndex": c1},
                                              "properties": {"pixelSize": px}, "fields": "pixelSize"}}

    def dropdown(cell, source):
        return {"setDataValidation": {"range": cell, "rule": {"condition": {"type": "ONE_OF_RANGE", "values": [
            {"userEnteredValue": f"='{LISTS}'!{source}"}]}, "showCustomUi": True, "strict": True}}}

    def rule(rng, formula, bg=None, fg=None):
        f = {"textFormat": {"bold": True, **({"foregroundColor": fg} if fg else {})}}
        if bg:
            f["backgroundColor"] = bg
        return {"addConditionalFormatRule": {"index": 0, "rule": {"ranges": [rng], "booleanRule": {
            "condition": {"type": "CUSTOM_FORMULA", "values": [{"userEnteredValue": formula}]}, "format": f}}}}

    def table(header_row, first, last, groups, width_to):
        # a grey header, a bold first column, and a medium line on the left of every group after the first,
        # from the header to the table's last row
        out = [fmt(area(header_row, header_row, 0, width_to),
                   {"backgroundColor": HEADER_BG, "textFormat": {"bold": True}, "horizontalAlignment": "CENTER",
                    "verticalAlignment": "BOTTOM", "wrapStrategy": "WRAP"},
                   "backgroundColor,textFormat,horizontalAlignment,verticalAlignment,wrapStrategy"),
               fmt(area(first, last, 0, 1), {"textFormat": {"bold": True}, "horizontalAlignment": "LEFT"},
                   "textFormat,horizontalAlignment")]
        line = {"style": "SOLID_MEDIUM", "color": GROUP_LINE}
        out += [{"updateBorders": {"range": area(header_row, last, c, c + 1), "left": line}} for c in groups]
        return out

    pick = {"backgroundColor": PICK_BG, "horizontalAlignment": "LEFT"}
    label = {"textFormat": {"bold": True}, "horizontalAlignment": "RIGHT"}
    muted = {"textFormat": {"italic": True, "foregroundColor": MUTED_FG}}
    rates = area(MATRIX_FIRST, MATRIX_LAST, 5, 16)
    stage_rates = area(STAGE_FIRST, STAGE_LAST, 1, 3)

    requests = [
        {"updateSheetProperties": {"properties": {"sheetId": sid, "gridProperties": {"frozenRowCount": 3, "frozenColumnCount": 1}},
                                   "fields": "gridProperties(frozenRowCount,frozenColumnCount)"}},
        fmt(area(1, 60, 0, 16), {"textFormat": {"fontFamily": "Arial", "fontSize": 10}}, "textFormat"),
        fmt(area(1, 1, 0, 1), {"textFormat": {"bold": True, "fontSize": 16}}, "textFormat"),
        fmt(area(2, 2, 0, 1), muted, "textFormat"),

        # the dropdowns: Run, Compare with, Aligning - and the comp detail's Comp
        *[fmt(area(3, 3, c, c + 1), label, "textFormat,horizontalAlignment") for c in (0, 2, 4)],
        *[fmt(area(3, 3, c, c + 1), pick, "backgroundColor,horizontalAlignment") for c in (1, 3, 5)],
        fmt(area(COMP_PICK, COMP_PICK, 0, 1), label, "textFormat,horizontalAlignment"),
        fmt(area(COMP_PICK, COMP_PICK, 1, 2), pick, "backgroundColor,horizontalAlignment"),
        fmt(area(COMP_PICK, COMP_PICK, 2, 3), muted, "textFormat"),
        dropdown(area(3, 3, 1, 2), "$C$2:$C"),
        dropdown(area(3, 3, 3, 4), "$D$2:$D"),
        dropdown(area(3, 3, 5, 6), "$F$2:$F$3"),
        dropdown(area(COMP_PICK, COMP_PICK, 1, 2), "$E$2:$E"),

        # tiles
        fmt(area(5, 5, 0, 16), {"textFormat": {"foregroundColor": MUTED_FG}, "horizontalAlignment": "LEFT"},
            "textFormat,horizontalAlignment"),
        fmt(area(6, 6, 0, 16), {"textFormat": {"bold": True, "fontSize": 14}, "horizontalAlignment": "LEFT"},
            "textFormat,horizontalAlignment"),
        number(area(6, 6, 9, 10), "+0.0 \"stages\";-0.0 \"stages\";0.0 \"stages\""),

        # section titles
        *[fmt(area(r, r, 0, 1), {"textFormat": {"bold": True, "fontSize": 12}}, "textFormat") for r in (8, COMP_PICK - 1)],

        # every comp x every stage: [comp] [stages won, aligning adds] [change] [each stage]
        *table(MATRIX_HEADER, MATRIX_FIRST, MATRIX_LAST, (1, 4, 5), 16),
        number(area(MATRIX_FIRST, MATRIX_LAST, 1, 3), "0"),
        number(area(MATRIX_FIRST, MATRIX_LAST, 3, 5), "+0;-0;0"),
        number(rates, "0%", "PERCENT"),

        # one comp by stage: [stage] [win rates] [the fight]
        *table(STAGE_HEADER, STAGE_FIRST, STAGE_LAST, (1, 3), 8),
        number(area(STAGE_FIRST, STAGE_LAST, 0, 1), "\"Stage \"0"),
        number(stage_rates, "0%", "PERCENT"),
        *[number(area(STAGE_FIRST, STAGE_LAST, c, c + 1), p, k) for c, p, k in (
            (3, "0.0\"s\"", "NUMBER"), (4, "0", "NUMBER"), (5, "0.0", "NUMBER"), (6, "0%", "PERCENT"), (7, "0.0", "NUMBER"))],

        # one comp by hero: [hero] [damage dealt] [staying alive] [change]
        *table(HERO_HEADER, HERO_FIRST, HERO_LAST, (1, 3, 7), 8),
        *[number(area(HERO_FIRST, HERO_LAST, c, c + 1), p, k) for c, p, k in (
            (1, "0.0", "NUMBER"), (2, "0%", "PERCENT"), (3, "0.0", "NUMBER"), (4, "0.0", "NUMBER"),
            (5, "0%", "PERCENT"), (6, "0.0\"s\"", "NUMBER"), (7, "+0%;-0%;0%", "PERCENT"))],

        width(0, 1, 200), width(1, 16, 80),
        {"updateSheetProperties": {"properties": {"sheetId": lists.id, "hidden": True}, "fields": "hidden"}},
    ]

    # a cell takes only the first rule that matches; each goes in at index 0, so the last added wins.
    # Win rates keep the Team tabs' green / red (a stage is won at WIN_LINE of its runs); changes are coloured text, as on
    # the hero Dashboard - a stage count at any change, a DPS change from 5% (the hero Dashboard's line)
    first = MATRIX_FIRST
    requests += [
        rule(rates, f"=AND(ISNUMBER(F{first}),F{first}<{WIN_LINE})", LOSS_BG, LOSS_FG),
        rule(rates, f"=AND(ISNUMBER(F{first}),F{first}>={WIN_LINE})", WIN_BG, WIN_FG),
        rule(stage_rates, f"=AND(ISNUMBER(B{STAGE_FIRST}),B{STAGE_FIRST}<{WIN_LINE})", LOSS_BG, LOSS_FG),
        rule(stage_rates, f"=AND(ISNUMBER(B{STAGE_FIRST}),B{STAGE_FIRST}>={WIN_LINE})", WIN_BG, WIN_FG),
    ]
    for rng, cell, line in ((area(first, MATRIX_LAST, 3, 5), f"D{first}", 0), (area(6, 6, 9, 10), "J6", 0),
                            (area(HERO_FIRST, HERO_LAST, 7, 8), f"H{HERO_FIRST}", 0.05)):
        down, up = (f"{cell}<0", f"{cell}>0") if line == 0 else (f"{cell}<=-{line}", f"{cell}>={line}")
        requests += [rule(rng, f"=AND(ISNUMBER({cell}),{down})", fg=DOWN_FG),
                     rule(rng, f"=AND(ISNUMBER({cell}),{up})", fg=UP_FG)]
    # a comp that wins every stage aligned gets its name in green
    requests.append(rule(area(first, MATRIX_LAST, 0, 1),
                         f"=AND(ISNUMBER($B{first}),$B{first}=COUNTA($F${MATRIX_HEADER}:${MATRIX_HEADER}))", WIN_BG, WIN_FG))
    sh.batch_update({"requests": requests})


def main():
    rebuild = "--rebuild" in sys.argv[1:]
    sh = sheet_sync.get_spreadsheet(sheet_sync.get_client(), SHEET)
    titles = {ws.title for ws in sh.worksheets()}
    if TAB in titles or LISTS in titles:
        if not rebuild:
            sys.exit(f"'{TAB}' is already there - pass --rebuild to delete it and build it again.")
        for title in (TAB, LISTS):
            if title in titles:
                sh.del_worksheet(sh.worksheet(title))
    try:
        build(sh)
    except gspread.exceptions.APIError as e:
        sys.exit(f"could not build the dashboard: {e}")
    print(f"Built '{TAB}' in '{sh.title}'.")


if __name__ == "__main__":
    main()
