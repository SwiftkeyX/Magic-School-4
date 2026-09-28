---
name: sync-sheet
description: Read and write data to/from this project's Google Sheets (tft-skill, TFT Set 9, TFT Set 9 Analysis, TFT Set 10, TFT Set 11) using the background sheet_sync.py script.
---

# Google Sheet Sync Skill (Background API)

This skill allows the agent to read and write to the project's Google Sheets directly in the background using the [sheet_sync.py](file:///c:/Organized%20Files/Working/Unity/Unity%20Project/Magic%20School%203/.claude/scripts/sheet_sync.py) command-line utility. This avoids launching a browser, dumping a giant unstructured text blob through the Drive MCP tool, or interrupting the user.

There are **5 registered sheets** (see [`sheets_config.json`](file:///c:/Organized%20Files/Working/Unity/Unity%20Project/Magic%20School%203/.claude/scripts/sheets_config.json)): `tft-skill` (default — this project's own design source), `tft-set9`, `tft-set9-analysis`, `tft-set10`, `tft-set11` (external references). All five are marked `writable: true`. Select a non-default sheet with `--sheet <name>` before the subcommand, e.g. `python .claude/scripts/sheet_sync.py --sheet tft-set9 read "Hero set 9"`.

## Purpose of the Google Sheet (`tft-skill`, the default)

This is the design source of truth for this project's hero roster and skill data — see [[reference_tft_skill_sheet]] memory for the full picture. Its tabs (as of the last sync):

`Hero Roster`, `Archetype Review`, `Dashboard`, `Hero set 9`, `Hero set 10`, `Action Model`, `Column Explain`, `Effect Types`, `Collision Types`, `Effect Recipient Types`, `Volley Shape Types`, `Scaling Types`, `AOE shape`, `Trigger Types`, `Offset Types`, `Fire Timing Types`, `Aim Target Types`, `Action Source Types`, `Design Notes`, `Hero flat`, `Role Types`, `Damage Types`.

**`Hero set 9`** (and its Set 10 sibling) is the one that matters for authoring a hero's kit — it lays out per-hero skill data in the same shape this project's C# skill system mirrors: `Step / Skill Type / Trigger (When) / Condition (Only if) / Action Source / Legacy action / Aim Target / Offset / AOE (hex) / Skill Range / Count / Volley Shape / Fire Timing / Cast (s) / Effect Recipient / Effect Category / Effect Detail / Amount / Scaling Type / Scaling / Effect Cadence / Effect Duration (s)`. A champion's row is followed by blank-identity continuation rows for each additional step/effect — read several rows past the name match, not just the one row.

Cross-reference this tab first rather than guessing a kit from League/TFT knowledge alone. It's frequently *more precise* than what the current C# system can express yet (e.g. Teemo's row names a `Trigger (When)` of `On Projectile Hit` and an `Aim Target` of `Hex That Projectile Hit`, and a `Condition` column for star-level branching — none of which `TriggerEnum`/`AimTarget` support today, only an approximation via `OnExpired` + `AimTarget.Current`). When the sheet is more precise than the code, say so explicitly rather than silently rounding down to what's easy.

There are sibling read-only reference sheets too (`tft-set9`, `tft-set9-analysis`) holding the raw real-game ability text and stat exports these designs are adapted from — useful for filling gaps `tft-skill` doesn't spell out (e.g. its `Hero Roster`/`Hero set 9` tabs sometimes only have a summary row with no authored per-step breakdown yet).

## Credentials

The credentials for this connection are stored in the root credential file [google-service-credential.json](file:///c:/Organized%20Files/Working/Unity/Unity%20Project/Magic%20School%203/google-service-credential.json) (gitignored — never commit it).

## Steps to Read the Sheet

To fetch data from any tab in the spreadsheet, run `.claude/scripts/sheet_sync.py` from the workspace root.

* **Format as Table (Human-Readable):**
  ```bash
  python .claude/scripts/sheet_sync.py read "Hero set 9"
  ```
* **Format as JSON (For parsing):**
  ```bash
  python .claude/scripts/sheet_sync.py read "Hero set 9" --format json
  ```
* **Format as CSV:**
  ```bash
  python .claude/scripts/sheet_sync.py read "Hero set 9" --format csv
  ```

## Steps to Write to the Sheet

To update a cell in the spreadsheet, run the script's `write` command:

```bash
python .claude/scripts/sheet_sync.py write "Hero set 9" B2 "Teemo"
```

## Steps to Bulk-Write Rows

To write many rows in a single API call (e.g. rebuilding a whole data block), use the `write-range` command. It takes a worksheet name, a start cell, and a `--file` pointing to a local JSON file containing a list of rows (each row itself a list of cell values — use JSON rather than CSV so fields containing commas don't need escaping):

```bash
python .claude/scripts/sheet_sync.py --sheet tft-set9 write-range "Meta Comps" A1 --file rows.json
```

If the named worksheet doesn't exist yet, it is created automatically (sized to fit the data) before writing.

## Steps to Dump All Sheet Content

To download all tabs of a sheet into a single local, self-describing JSON file (default name `<sheet>_dump_<date>.json`, embeds a `_meta` block with sheet name/key/timestamp) — put these under `.claude/reference/json/` so they're grep-able without re-fetching:

```bash
python .claude/scripts/sheet_sync.py dump -o .claude/reference/json/tft-skill_dump_$(date +%Y%m%d).json
python .claude/scripts/sheet_sync.py --sheet tft-set9 dump -o .claude/reference/json/tft-set9_dump_$(date +%Y%m%d).json
```

A dump is much easier to search (`python -c "import json; ..."` or grep the file directly) than re-reading one huge tab through the API each time, and doesn't hit the same "result exceeds max tokens" wall the Drive MCP tool does on this sheet (250k+ chars).
