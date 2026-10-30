---
name: prog-round
description: Run one prog balancing round for Magic School 4 - benchmark the prog comps (Elf benchmark team vs other teams at each stage milestone), read the result, and log it in the Prog sheet and Notion. Use when the owner changes a prog comp, a hero, a mark or an item and asks to test it, or asks to continue the prog / power-level work.
---

# Prog round

**Goal** (Notion: Power level > Prog criteria): every team has a "prog" comp per stage milestone (1, 3, 5, 7, 9, 10)
with the Elf team's hero / star / item budget. Each should win **50-95%** at its milestone stage, marks on, and
mostly lose with marks off. Elf (`prog-Elf-N`) is the benchmark; others are `prog-<Team>-N`
(`prog-Skeleton-N`), all in `Assets/Data/BattleSetups/Team Comps/`. Stage 5 (Minotaur King boss) being harder than
stage 6 is intended. Read `.claude/docs/features/benchmarks.md` for how the benchmark works.

**Items** (the simple items, `.claude/docs/features/items.md`): follow the item steps in Prog criteria on the Notion page
(start with none, add one at a time from the lowest rarity, raise rarity once full). A test comp never has more items
than heroes - worn items plus items on purple hexes count, Blacksmith's forged items do not. The owner adds more on
purple hexes by hand when they want to; never do it in a round.

## 1. See what changed

`git status` / `git diff` on `Team Comps/`, `Data/Heroes/Stats/`, `Skills/Library/Build/`. To read a comp (heroes, tier,
item, pattern, hex, marks filled) draw it: `python .claude/scripts/team_picture.py BalanceLogs/teams "<comp>.asset"` and
look at the PNG. Tell the owner in a few lines what changed before testing.

## 2. Run the benchmark (Unity must be open, Edit mode)

1. `get_unity_editor_state`: not in Play mode, no compile errors. Never refresh/recompile in Play mode.
2. In the Benchmark window, tick **Milestone and next stage** (`milestoneOnly`) for a round: each prog comp fights stage
   N and N+1. For tuning tick **Milestone only** (`milestoneStageOnly`) instead: stage N alone, half the fights (it wins
   when both are ticked). The benchmark runs about 40 fights a minute.
3. Coplay `execute_script` on `.claude/scripts/RunProgBenchmark.cs` (every prog comp; to run a few, copy it to the
   scratchpad and fill `Only`). It enters Play mode itself.
4. Wait for the report with a background Bash loop until a new `BalanceLogs/teamcomp-*.csv` appears.
5. When Play ends, the Editor runs `.claude/scripts/benchmark_import.py <csv>` (Benchmark sheet, then the **MagicSchool4
   Prog** sheet, then the team pictures in `BalanceLogs/teams/`); it takes about 3 minutes. **It sometimes does not run.**
   Check the Prog sheet's hidden Lists tab (or the Benchmark sheet's Team Runs column A) for the run time; if it is
   missing after the import should have finished (no `benchmark_import` python process left), run the import by hand.
   Do not run it twice for one run: Team Runs would get the rows twice (the Prog sheet skips a run it already has).

## 3. Read and report

- `python .claude/scripts/prog_report.py BalanceLogs/teamcomp-<time>.csv` prints the colour grid and the flags.
- Compare with the round before: hero rows in the CSV (`grep "^hero" ... | grep ",<stage>,Stage 0<stage>,on,"`).
- Report to the owner as the grid first (prog x team, 🟢 50-95%, 🔴 below, 🟡 above), then 2-4 lines of what moved and
  why, then the next step. No big per-hero tables in chat; point to the Prog sheet:
  https://docs.google.com/spreadsheets/d/1Q3M94zgw1iJhUK5AkiXB2MjDOY6tTZXIe-VeBR7UZ_A (Dashboard: Run / Compare with /
  Prog dropdowns).

## 4. Log it

- **Notion** (the `notion-docs` skill): append `<toggle heading="h3">Round N - <what changed> (<date>)` to the end of
  the **Prog Round** page (`4078c134506e8045b8abfdfbea948460`): what changed, the grid as a table with 🟢🔴🟡, the
  verdict, the next step. When the comps changed, add "The teams" pictures as in Round 0 (upload from
  `BalanceLogs/teams/`, or take in-game ones with **Magic School 4 > Team Snapshot (selected comps)**).
- Move the matching task on the Notion task board (Doing / Done).
- Commit only when the owner asks; never add AI attribution.

## Findings so far (update as rounds go)

- Round 0 (2026-10-27): every milestone about 100% or 0%; nobody in the band. Skeleton loses the boss stages (5, 10).
- Round 1 (2026-10-28): Blacksmith forges for herself first - she lasts in fair fights, not vs the stage 5 boss.
- Owner's prog-5 change (2026-10-28 09:29): an Iron Plate on each tank took both prog-5 teams from 0% to 100% - the
  boss fight is a cliff (tank lives = win), so whole items overshoot; use smaller levers (stats, mark bonuses).
- Knight's ~200 DPS at prog-9 is an AoE problem (owner's note). Shield Knight in Skeleton-5 does nothing (2 DPS).
