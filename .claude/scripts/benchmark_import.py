#!/usr/bin/env python3
"""Import one Hero Benchmark CSV (BalanceLogs/benchmark-*.csv) into the "MagicSchool4 Benchmark" sheet.

    Runs    - every run, appended below the last; each row stamped with run_time and kind (full / selected)
    Latest  - overwritten with this run alone

Run from the project root (HeroBenchmarkMenu does this when a benchmark finishes):
    python .claude/scripts/benchmark_import.py BalanceLogs/benchmark-2026-10-16_09-36-53.csv
    python .claude/scripts/benchmark_import.py --restyle      (style both tabs again, import nothing)
"""
import csv
import re
import sys
from pathlib import Path

import gspread

sys.path.insert(0, str(Path(__file__).parent))
import sheet_sync  # noqa: E402

SHEET = "magicschool4-benchmark"
RUNS = "Runs"
LATEST = "Latest"
STAMP = ["run_time", "kind"]


def read_csv(path):
    # the CSV pads its cells with spaces to line up in a text editor, and starts with a BOM
    with open(path, encoding="utf-8-sig", newline="") as f:
        rows = [[cell.strip() for cell in row] for row in csv.reader(f)]
    return rows[0], [row for row in rows[1:] if any(row)]


def stamp_of(path):
    # benchmark-2026-10-16_09-36-53.csv / benchmark-selected-2026-10-16_09-36-53.csv
    match = re.search(r"benchmark-(selected-)?(\d{4}-\d{2}-\d{2})_(\d{2})-(\d{2})-(\d{2})", Path(path).name)
    if not match:
        sys.exit(f"'{path}' is not named like a benchmark report (benchmark-YYYY-MM-DD_HH-MM-SS.csv).")
    selected, date, h, m, s = match.groups()
    return [f"{date} {h}:{m}:{s}", "selected" if selected else "full"]


def worksheet(sh, title, cols):
    try:
        return sh.worksheet(title)
    except gspread.exceptions.WorksheetNotFound:
        ws = sh.add_worksheet(title=title, rows=100, cols=cols)
        ws.freeze(rows=1)
        return ws


def append_to_runs(sh, header, rows):
    ws = worksheet(sh, RUNS, len(header))
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
    ws.append_rows(lined_up, value_input_option="USER_ENTERED")


def replace_latest(sh, header, rows):
    ws = worksheet(sh, LATEST, len(header))
    ws.clear()
    ws.update([header] + rows, range_name="A1", value_input_option="USER_ENTERED")


# ======================================== style ========================================
# Plain on purpose: a bold grey header, the run/hero columns frozen, numbers lined up, HIGH red and LOW blue,
# and in Runs every other run shaded so one run's rows read as a block. Re-applied on every import.
GREY = {"red": 0.9, "green": 0.9, "blue": 0.9}
BAND = {"red": 0.95, "green": 0.97, "blue": 1.0}
HIGH_BG, HIGH_FG = {"red": 0.96, "green": 0.8, "blue": 0.8}, {"red": 0.6, "green": 0.0, "blue": 0.0}
LOW_BG, LOW_FG = {"red": 0.8, "green": 0.87, "blue": 0.96}, {"red": 0.0, "green": 0.2, "blue": 0.6}

# each test's columns in a colour of their own: (header colour, pale body tint, columns); None = no tint
def rgb(r, g, b): return {"red": r, "green": g, "blue": b}
TESTS = [
    (GREY, None, ("run_time", "kind", "hero", "tier", "role", "price")),
    (rgb(0.98, 0.8, 0.6), rgb(1.0, 0.95, 0.9), ("dps_15s", "dps_30s", "auto_share", "healing_per_sec", "dps_vs_role", "dps_flag")),
    (rgb(0.7, 0.88, 0.7), rgb(0.93, 0.98, 0.93), ("aoe_dps_15s", "aoe_dps_30s", "aoe_gain", "aoe_vs_role", "aoe_flag")),
    (rgb(0.7, 0.8, 0.95), rgb(0.93, 0.95, 1.0), ("time_to_die", "survived", "hp_left", "damage_taken", "mitigated",
                                                 "healing_received", "ttd_vs_role", "tank_flag")),
    (GREY, None, ("dummy_revives", "error")),   # counted across all three tests
]
IDENTITY_COLUMNS = len(TESTS[0][2])

FLAG_COLUMNS = ("dps_flag", "aoe_flag", "tank_flag")
ONE_DECIMAL = ("dps_15s", "dps_30s", "healing_per_sec", "aoe_dps_15s", "aoe_dps_30s", "time_to_die")
PERCENT = ("auto_share", "dps_vs_role", "aoe_vs_role", "ttd_vs_role", "hp_left")


def style(sh, ws, band_runs):
    sheet_id = ws.id
    columns = ws.row_values(1)
    width = len(columns)

    def column(name):
        i = columns.index(name)
        return {"sheetId": sheet_id, "startRowIndex": 1, "startColumnIndex": i, "endColumnIndex": i + 1}

    def number_format(name, pattern, kind):
        return {"repeatCell": {"range": column(name),
                               "cell": {"userEnteredFormat": {"numberFormat": {"type": kind, "pattern": pattern}}},
                               "fields": "userEnteredFormat.numberFormat"}}

    # take off the rules the last import added, so they never pile up
    meta = sh.fetch_sheet_metadata({"fields": "sheets(properties.sheetId,conditionalFormats)"})
    old_rules = next((len(s.get("conditionalFormats", [])) for s in meta["sheets"] if s["properties"]["sheetId"] == sheet_id), 0)
    requests = [{"deleteConditionalFormatRule": {"sheetId": sheet_id, "index": 0}} for _ in range(old_rules)]

    requests += [
        {"updateSheetProperties": {"properties": {"sheetId": sheet_id, "gridProperties": {"frozenRowCount": 1, "frozenColumnCount": 3}},
                                   "fields": "gridProperties.frozenRowCount,gridProperties.frozenColumnCount"}},
        {"repeatCell": {"range": {"sheetId": sheet_id, "startRowIndex": 0, "endRowIndex": 1, "startColumnIndex": 0, "endColumnIndex": width},
                        "cell": {"userEnteredFormat": {"backgroundColor": GREY, "textFormat": {"bold": True},
                                                       "horizontalAlignment": "CENTER", "verticalAlignment": "MIDDLE", "wrapStrategy": "WRAP"}},
                        "fields": "userEnteredFormat(backgroundColor,textFormat,horizontalAlignment,verticalAlignment,wrapStrategy)"}},
        {"repeatCell": {"range": column("hero"), "cell": {"userEnteredFormat": {"textFormat": {"bold": True}}},
                        "fields": "userEnteredFormat.textFormat.bold"}},
    ]
    # each test's block: its header colour, a pale tint under it, and a thick line on its left edge
    rows = max(ws.row_count, 1)
    for header_colour, tint, names in TESTS:
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
    requests += [number_format(name, "0.0", "NUMBER") for name in ONE_DECIMAL if name in columns]
    requests += [number_format(name, "+0%;-0%;0%", "PERCENT") for name in PERCENT if name.endswith("vs_role") and name in columns]
    requests += [number_format(name, "0%", "PERCENT") for name in PERCENT if not name.endswith("vs_role") and name in columns]

    for name in FLAG_COLUMNS:
        if name not in columns:
            continue
        for word, bg, fg in (("HIGH", HIGH_BG, HIGH_FG), ("LOW", LOW_BG, LOW_FG)):
            requests.append({"addConditionalFormatRule": {"index": 0, "rule": {
                "ranges": [column(name)],
                "booleanRule": {"condition": {"type": "TEXT_EQ", "values": [{"userEnteredValue": word}]},
                                "format": {"backgroundColor": bg, "textFormat": {"bold": True, "foregroundColor": fg}}}}}})

    if band_runs:
        # every other run_time shaded, counted from the top - only over the hero columns, so each test keeps its colour;
        # the band rule goes last so a flag colour wins over it
        requests.append({"addConditionalFormatRule": {"index": len([r for r in requests if "addConditionalFormatRule" in r]), "rule": {
            "ranges": [{"sheetId": sheet_id, "startRowIndex": 1, "startColumnIndex": 0, "endColumnIndex": IDENTITY_COLUMNS}],
            "booleanRule": {"condition": {"type": "CUSTOM_FORMULA", "values": [{"userEnteredValue":
                                "=AND($A2<>\"\", ISEVEN(COUNTUNIQUE($A$2:$A2)))"}]},
                            "format": {"backgroundColor": BAND}}}}})

    requests.append({"autoResizeDimensions": {"dimensions": {"sheetId": sheet_id, "dimension": "COLUMNS", "startIndex": 0, "endIndex": width}}})
    sh.batch_update({"requests": requests})


def drop_empty_default_tab(sh):
    # a new spreadsheet comes with one empty tab ("Sheet1", in whatever language) - remove it once ours exist
    for ws in sh.worksheets():
        if ws.title not in (RUNS, LATEST) and len(sh.worksheets()) > 2 and not any(ws.get_all_values()):
            sh.del_worksheet(ws)


def main():
    if len(sys.argv) != 2:
        sys.exit("usage: benchmark_import.py <path to benchmark csv>  |  benchmark_import.py --restyle")
    path = sys.argv[1]

    # style what is already there, importing nothing (e.g. after the style above changes)
    if path == "--restyle":
        sh = sheet_sync.get_spreadsheet(sheet_sync.get_client(), SHEET)
        style(sh, sh.worksheet(RUNS), band_runs=True)
        style(sh, sh.worksheet(LATEST), band_runs=False)
        print(f"Restyled '{sh.title}'.")
        return

    csv_header, csv_rows = read_csv(path)
    stamp = stamp_of(path)
    header = STAMP + csv_header
    rows = [stamp + row for row in csv_rows]

    sh = sheet_sync.get_spreadsheet(sheet_sync.get_client(), SHEET)
    append_to_runs(sh, header, rows)
    replace_latest(sh, header, rows)
    style(sh, sh.worksheet(RUNS), band_runs=True)
    style(sh, sh.worksheet(LATEST), band_runs=False)
    drop_empty_default_tab(sh)
    print(f"Imported {len(rows)} heroes from {Path(path).name} ({stamp[1]}, {stamp[0]}) into '{sh.title}'.")


if __name__ == "__main__":
    main()
