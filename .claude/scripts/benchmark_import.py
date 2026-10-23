#!/usr/bin/env python3
"""Import one benchmark CSV into the "MagicSchool4 Benchmark" sheet. Which report it is comes from the file name:

    Hero Benchmark       BalanceLogs/benchmark-*.csv   ->  Runs / Latest
    Team Comp Benchmark  BalanceLogs/teamcomp-*.csv    ->  Team Runs / Team Latest

    *Runs    - every run, appended below the last; each row stamped with run_time (and, for heroes, kind: full / selected)
    *Latest  - overwritten with this run alone

Run from the project root (HeroBenchmarkMenu / TeamCompBenchmarkMenu do this when a benchmark finishes):
    python .claude/scripts/benchmark_import.py BalanceLogs/benchmark-2026-10-16_09-36-53.csv
    python .claude/scripts/benchmark_import.py BalanceLogs/teamcomp-2026-10-22_19-45-09.csv
    python .claude/scripts/benchmark_import.py --restyle      (style every tab again, import nothing)
"""
import csv
import re
import sys
from pathlib import Path

import gspread

sys.path.insert(0, str(Path(__file__).parent))
import sheet_sync  # noqa: E402

SHEET = "magicschool4-benchmark"


# ======================================== colours ========================================
def rgb(r, g, b): return {"red": r, "green": g, "blue": b}


GREY = rgb(0.9, 0.9, 0.9)
BAND = rgb(0.95, 0.97, 1.0)
HIGH_BG, HIGH_FG = rgb(0.96, 0.8, 0.8), rgb(0.6, 0.0, 0.0)
LOW_BG, LOW_FG = rgb(0.8, 0.87, 0.96), rgb(0.0, 0.2, 0.6)
WIN_BG, WIN_FG = rgb(0.8, 0.92, 0.8), rgb(0.0, 0.4, 0.0)
LOSS_BG, LOSS_FG = rgb(0.96, 0.8, 0.8), rgb(0.6, 0.0, 0.0)


# ======================================== the two reports ========================================
# Everything that differs between the hero report and the team comp report. The import and the style are shared.
class Report:
    def __init__(self, name, file_pattern, runs_tab, latest_tab, stamp, blocks, frozen_columns, bold_column,
                 one_decimal=(), percent=(), signed_percent=(), flag_columns=(), win_rate_column=None, bold_rows_where=None):
        self.name = name
        self.file_pattern = file_pattern            # regex on the file name: (selected-)? then date, h, m, s
        self.runs_tab, self.latest_tab = runs_tab, latest_tab
        self.stamp = stamp                          # the columns put in front of the CSV's
        self.blocks = blocks                        # (header colour, pale body tint or None, columns) - one per test
        self.frozen_columns = frozen_columns
        self.bold_column = bold_column
        self.one_decimal = one_decimal
        self.percent = percent                      # 0%
        self.signed_percent = signed_percent        # +12% / -12%
        self.flag_columns = flag_columns            # HIGH red, LOW blue
        self.win_rate_column = win_rate_column      # at least half green, below half red
        self.bold_rows_where = bold_rows_where      # (column, value): those rows bold all the way across

    @property
    def identity_columns(self):
        return len(self.blocks[0][2])

    def stamp_of(self, path):
        match = re.search(self.file_pattern, Path(path).name)
        if not match:
            return None
        selected, date, h, m, s = match.groups()
        values = {"run_time": f"{date} {h}:{m}:{s}", "kind": "selected" if selected else "full"}
        return [values[name] for name in self.stamp]


HEROES = Report(
    name="heroes",
    file_pattern=r"^benchmark-(selected-)?(\d{4}-\d{2}-\d{2})_(\d{2})-(\d{2})-(\d{2})",
    runs_tab="Runs", latest_tab="Latest",
    stamp=["run_time", "kind"],
    blocks=[
        (GREY, None, ("run_time", "kind", "hero", "tier", "role", "price")),
        (rgb(0.98, 0.8, 0.6), rgb(1.0, 0.95, 0.9), ("dps_15s", "dps_30s", "auto_share", "healing_per_sec", "dps_vs_role", "dps_flag")),
        (rgb(0.7, 0.88, 0.7), rgb(0.93, 0.98, 0.93), ("aoe_dps_15s", "aoe_dps_30s", "aoe_gain", "aoe_vs_role", "aoe_flag")),
        (rgb(0.7, 0.8, 0.95), rgb(0.93, 0.95, 1.0), ("time_to_die", "survived", "hp_left", "damage_taken", "mitigated",
                                                     "healing_received", "ttd_vs_role", "tank_flag")),
        (GREY, None, ("dummy_revives", "error")),   # counted across all three tests
    ],
    frozen_columns=3,
    bold_column="hero",
    one_decimal=("dps_15s", "dps_30s", "healing_per_sec", "aoe_dps_15s", "aoe_dps_30s", "time_to_die"),
    percent=("auto_share", "hp_left"),
    signed_percent=("dps_vs_role", "aoe_vs_role", "ttd_vs_role"),
    flag_columns=("dps_flag", "aoe_flag", "tank_flag"),
)

# The team comp CSV mixes two kinds of row: a "team" row per comp x stage x aligning, then a "hero" row for each of its heroes
TEAMS = Report(
    name="team comps",
    file_pattern=r"^teamcomp-(selected-)?(\d{4}-\d{2}-\d{2})_(\d{2})-(\d{2})-(\d{2})",
    runs_tab="Team Runs", latest_tab="Team Latest",
    stamp=["run_time"],
    blocks=[
        (GREY, None, ("run_time", "row", "comp", "stage", "stage_setup", "aligning", "runs")),
        (rgb(0.98, 0.8, 0.6), rgb(1.0, 0.95, 0.9), ("win_rate", "wins", "timeouts", "avg_seconds", "heroes_left",
                                                     "team_hp_left", "team_dps")),
        (rgb(0.7, 0.8, 0.95), rgb(0.93, 0.95, 1.0), ("hero", "dps", "damage_share", "damage_taken", "healing",
                                                     "survive_rate", "died_at")),
        (GREY, None, ("error",)),
    ],
    frozen_columns=6,
    bold_column="comp",
    one_decimal=("avg_seconds", "heroes_left", "team_dps", "dps", "damage_taken", "healing", "died_at"),
    percent=("win_rate", "team_hp_left", "damage_share", "survive_rate"),
    win_rate_column="win_rate",
    bold_rows_where=("row", "team"),
)

REPORTS = (HEROES, TEAMS)


# ======================================== import ========================================
def read_csv(path):
    # the CSV pads its cells with spaces to line up in a text editor, and starts with a BOM
    with open(path, encoding="utf-8-sig", newline="") as f:
        rows = [[cell.strip() for cell in row] for row in csv.reader(f)]
    return rows[0], [row for row in rows[1:] if any(row)]


def worksheet(sh, title, cols):
    try:
        return sh.worksheet(title)
    except gspread.exceptions.WorksheetNotFound:
        ws = sh.add_worksheet(title=title, rows=100, cols=cols)
        ws.freeze(rows=1)
        return ws


def append_to_runs(sh, report, header, rows):
    ws = worksheet(sh, report.runs_tab, len(header))
    existing = ws.row_values(1)

    # a column the report gained since the tab was made goes on the end; rows are matched by name
    columns = existing or header
    columns = columns + [name for name in header if name not in columns]
    if columns != existing:
        if ws.col_count < len(columns):
            ws.add_cols(len(columns) - ws.col_count)
        ws.update([columns], range_name="A1")

    index = {name: i for i, name in enumerate(header)}
    lined_up = [[row[index[name]] if name in index and index[name] < len(row) else "" for name in columns] for row in rows]

    # written at an exact row under the last filled run_time - not append_rows, which lets Google guess where the
    # table ends; once the tab was formatted it guessed inside it and every run overwrote the rows before
    first_free = len(ws.col_values(1)) + 1
    last = first_free + len(lined_up) - 1
    if ws.row_count < last:
        ws.add_rows(last - ws.row_count)
    ws.update(lined_up, range_name=f"A{first_free}", value_input_option="USER_ENTERED")


def replace_latest(sh, report, header, rows):
    ws = worksheet(sh, report.latest_tab, len(header))
    ws.clear()
    ws.update([header] + rows, range_name="A1", value_input_option="USER_ENTERED")


# ======================================== style ========================================
# Plain on purpose: a bold grey header, the identity columns frozen, numbers lined up, each test's columns in a colour
# of its own, flags coloured, and in a Runs tab every other run shaded so one run's rows read as a block.
# Re-applied on every import.
def style(sh, report, ws, band_runs):
    sheet_id = ws.id
    columns = ws.row_values(1)
    width = len(columns)

    def column(name):
        i = columns.index(name)
        return {"sheetId": sheet_id, "startRowIndex": 1, "startColumnIndex": i, "endColumnIndex": i + 1}

    def letter(name):
        i, out = columns.index(name) + 1, ""
        while i:
            i, rem = divmod(i - 1, 26)
            out = chr(65 + rem) + out
        return out

    def number_format(name, pattern, kind):
        return {"repeatCell": {"range": column(name),
                               "cell": {"userEnteredFormat": {"numberFormat": {"type": kind, "pattern": pattern}}},
                               "fields": "userEnteredFormat.numberFormat"}}

    def colour_rule(ranges, condition, bg, fg=None, bold=False):
        fmt = {"backgroundColor": bg}
        if fg or bold:
            fmt["textFormat"] = {"bold": bold, **({"foregroundColor": fg} if fg else {})}
        return {"addConditionalFormatRule": {"index": 0, "rule": {"ranges": ranges,
                                                                  "booleanRule": {"condition": condition, "format": fmt}}}}

    # take off the rules the last import added, so they never pile up
    meta = sh.fetch_sheet_metadata({"fields": "sheets(properties.sheetId,conditionalFormats)"})
    old_rules = next((len(s.get("conditionalFormats", [])) for s in meta["sheets"] if s["properties"]["sheetId"] == sheet_id), 0)
    requests = [{"deleteConditionalFormatRule": {"sheetId": sheet_id, "index": 0}} for _ in range(old_rules)]

    requests += [
        {"updateSheetProperties": {"properties": {"sheetId": sheet_id, "gridProperties": {"frozenRowCount": 1,
                                                                                          "frozenColumnCount": report.frozen_columns}},
                                   "fields": "gridProperties.frozenRowCount,gridProperties.frozenColumnCount"}},
        {"repeatCell": {"range": {"sheetId": sheet_id, "startRowIndex": 0, "endRowIndex": 1, "startColumnIndex": 0, "endColumnIndex": width},
                        "cell": {"userEnteredFormat": {"backgroundColor": GREY, "textFormat": {"bold": True},
                                                       "horizontalAlignment": "CENTER", "verticalAlignment": "MIDDLE", "wrapStrategy": "WRAP"}},
                        "fields": "userEnteredFormat(backgroundColor,textFormat,horizontalAlignment,verticalAlignment,wrapStrategy)"}},
    ]
    if report.bold_column in columns:
        requests.append({"repeatCell": {"range": column(report.bold_column), "cell": {"userEnteredFormat": {"textFormat": {"bold": True}}},
                                        "fields": "userEnteredFormat.textFormat.bold"}})

    # each test's block: its header colour, a pale tint under it, and a thick line on its left edge
    rows = max(ws.row_count, 1)
    for header_colour, tint, names in report.blocks:
        present = [columns.index(name) for name in names if name in columns]
        if not present:
            continue
        first, last = min(present), max(present) + 1
        block = {"sheetId": sheet_id, "startColumnIndex": first, "endColumnIndex": last}
        requests.append({"repeatCell": {"range": {**block, "startRowIndex": 0, "endRowIndex": 1},
                                        "cell": {"userEnteredFormat": {"backgroundColor": header_colour}},
                                        "fields": "userEnteredFormat.backgroundColor"}})
        requests.append({"repeatCell": {"range": {**block, "startRowIndex": 1, "endRowIndex": rows},
                                        "cell": {"userEnteredFormat": {"backgroundColor": tint or rgb(1, 1, 1)}},
                                        "fields": "userEnteredFormat.backgroundColor"}})
        if first > 0:
            requests.append({"updateBorders": {"range": {**block, "startRowIndex": 0, "endRowIndex": rows},
                                               "left": {"style": "SOLID_THICK", "color": rgb(0.4, 0.4, 0.4)}}})

    if "run_time" in columns:
        requests.append(number_format("run_time", "yyyy-mm-dd hh:mm", "DATE_TIME"))
    requests += [number_format(name, "0.0", "NUMBER") for name in report.one_decimal if name in columns]
    requests += [number_format(name, "+0%;-0%;0%", "PERCENT") for name in report.signed_percent if name in columns]
    requests += [number_format(name, "0%", "PERCENT") for name in report.percent if name in columns]

    # Conditional rules. A cell takes only the FIRST rule that matches it, and each is added at index 0, so this list
    # runs from the weakest to the strongest: bold team rows, then the run band, then the flag and win colours.
    rules = []
    whole_width = [{"sheetId": sheet_id, "startRowIndex": 1, "startColumnIndex": 0, "endColumnIndex": width}]
    identity = [{"sheetId": sheet_id, "startRowIndex": 1, "startColumnIndex": 0, "endColumnIndex": report.identity_columns}]
    band_test = "ISEVEN(COUNTUNIQUE($A$2:$A2))"

    # team rows bold all the way across, so each comp x stage reads as a heading over its heroes
    bold_row_test = None
    if report.bold_rows_where and report.bold_rows_where[0] in columns:
        name, value = report.bold_rows_where
        bold_row_test = f"${letter(name)}2=\"{value}\""
        rules.append({"addConditionalFormatRule": {"index": 0, "rule": {"ranges": whole_width, "booleanRule": {
            "condition": {"type": "CUSTOM_FORMULA", "values": [{"userEnteredValue": f"={bold_row_test}"}]},
            "format": {"textFormat": {"bold": True}}}}}})

    if band_runs:
        # every other run_time shaded, counted from the top - only over the identity columns, so each test keeps its colour
        rules.append(colour_rule(identity, {"type": "CUSTOM_FORMULA", "values": [{"userEnteredValue":
                                 f"=AND($A2<>\"\", {band_test})"}]}, BAND))
        if bold_row_test:
            # a team row in a shaded run: both, since only one rule may win
            rules.append(colour_rule(identity, {"type": "CUSTOM_FORMULA", "values": [{"userEnteredValue":
                                     f"=AND($A2<>\"\", {band_test}, {bold_row_test})"}]}, BAND, bold=True))

    for name in report.flag_columns:
        if name not in columns:
            continue
        for word, bg, fg in (("HIGH", HIGH_BG, HIGH_FG), ("LOW", LOW_BG, LOW_FG)):
            rules.append(colour_rule([column(name)], {"type": "TEXT_EQ", "values": [{"userEnteredValue": word}]}, bg, fg, bold=True))

    # a stage counts as won at half its runs or more (See TeamCompBenchmark.Report)
    if report.win_rate_column in columns:
        cell = f"${letter(report.win_rate_column)}2"
        rules.append(colour_rule([column(report.win_rate_column)],
                                 {"type": "CUSTOM_FORMULA", "values": [{"userEnteredValue": f"=AND({cell}<>\"\", {cell}<0.5)"}]},
                                 LOSS_BG, LOSS_FG, bold=True))
        rules.append(colour_rule([column(report.win_rate_column)],
                                 {"type": "CUSTOM_FORMULA", "values": [{"userEnteredValue": f"=AND({cell}<>\"\", {cell}>=0.5)"}]},
                                 WIN_BG, WIN_FG, bold=True))
    requests += rules

    requests.append({"autoResizeDimensions": {"dimensions": {"sheetId": sheet_id, "dimension": "COLUMNS", "startIndex": 0, "endIndex": width}}})
    sh.batch_update({"requests": requests})


def drop_empty_default_tab(sh):
    # a new spreadsheet comes with one empty tab ("Sheet1", in whatever language) - remove it once ours exist
    ours = {tab for report in REPORTS for tab in (report.runs_tab, report.latest_tab)}
    for ws in sh.worksheets():
        if ws.title not in ours and len(sh.worksheets()) > 2 and not any(ws.get_all_values()):
            sh.del_worksheet(ws)


def restyle(sh):
    titles = {ws.title for ws in sh.worksheets()}
    for report in REPORTS:
        if report.runs_tab in titles:
            style(sh, report, sh.worksheet(report.runs_tab), band_runs=True)
        if report.latest_tab in titles:
            style(sh, report, sh.worksheet(report.latest_tab), band_runs=False)


def main():
    if len(sys.argv) != 2:
        sys.exit("usage: benchmark_import.py <path to benchmark or teamcomp csv>  |  benchmark_import.py --restyle")
    path = sys.argv[1]

    # style what is already there, importing nothing (e.g. after the style above changes)
    if path == "--restyle":
        sh = sheet_sync.get_spreadsheet(sheet_sync.get_client(), SHEET)
        restyle(sh)
        print(f"Restyled '{sh.title}'.")
        return

    report, stamp = next(((r, s) for r in REPORTS for s in [r.stamp_of(path)] if s), (None, None))
    if report is None:
        sys.exit(f"'{path}' is not named like a benchmark report (benchmark-YYYY-MM-DD_HH-MM-SS.csv or teamcomp-...).")

    csv_header, csv_rows = read_csv(path)
    header = report.stamp + csv_header
    rows = [stamp + row for row in csv_rows]

    sh = sheet_sync.get_spreadsheet(sheet_sync.get_client(), SHEET)
    append_to_runs(sh, report, header, rows)
    replace_latest(sh, report, header, rows)
    style(sh, report, sh.worksheet(report.runs_tab), band_runs=True)
    style(sh, report, sh.worksheet(report.latest_tab), band_runs=False)
    drop_empty_default_tab(sh)
    print(f"Imported {len(rows)} {report.name} rows from {Path(path).name} ({stamp[0]}) into '{sh.title}'.")


if __name__ == "__main__":
    main()
