# Balancing and benchmarks

Every `CombatRecord` knows `FightSeconds` / `SecondsAlive` (a `RoundClock` per round, stopped in `ResultState`; a death
is the damage event that takes HP to 0), so the `_test` CSV in `BalanceLogs/` has per-second rates.

## Settings and running

Every number the two benchmarks run with is in `Data/Benchmark/BenchmarkSettings.asset`
(`DevTools/Benchmark/BenchmarkSettingsSO`, made with the defaults if missing), edited in **Magic School 4 > Benchmark**
(`Core/Editor/BenchmarkWindow`): a tooltip each, a fight count and time estimate, and Run buttons, one tab per
benchmark. The window is the only way to run them; its buttons call `HeroBenchmarkMenu` / `TeamCompBenchmarkMenu`, which
pass the asset to `Run`; the code reads it through `BenchmarkSettingsSO.Current`. From an agent: call the internal static
`MagicSchool.Editor.HeroBenchmarkMenu.RunAll()` / `TeamCompBenchmarkMenu.RunAll()` by reflection.

## Hero Benchmark (`DevTools/Benchmark/HeroBenchmark`)

Every shop hero alone vs `Data/Benchmark/BenchDummy` (2000 HP, DF 30, topped up below half): a 30s damage test (DPS at
15s and 30s), a 30s AoE test (a tight cluster of 6 dummies around one clear nearest target; `aoe_gain` = AoE DPS /
single-target DPS) and a tank test (100 raw damage/s until death or 30s). It runs outside the game's states, 8 heroes at
once, each on its own hidden copy of the `BattleBoard` (a hero only sees its own board's heroes), in fixed 1/30s steps
(`Time.captureDeltaTime`) with vSync, the frame cap, cameras and UI off: about 50s for all. It writes
`BalanceLogs/benchmark-*.csv` with each hero vs its role's median, flagged beyond +-20%.

The tests run in up to three passes: the hero alone, then as in a smaller and a bigger team (default 3 and 5): up to 2
and 4 of its own orange mark hexes filled (`HeroBenchmark.MarkFill`, pattern 1, authored order; a stand-in ally on an
ally-paid hex, an empty deploy-zone hex for a `selfWhenEmpty` one), aligning given, then the stand-ins removed
(`BattleBoard.Remove`, `AlignBonuses.ClearSpecialMarks`) before the dummies spawn, so the self bonuses stay and nobody
else is in the fight. A pass only runs the heroes it changes. The bigger team's numbers are the main columns and drive the
comparison and flags; `*_small` and `*_alone` hold the others. Marks that need allies to act (heal links, special
marks, green) are left to the team comp benchmark. Ally-support skills read low (no allies), and heroes of a role share
stats, so the tank test only separates heroes whose skills defend them.

**Run Selected** tests only the HeroDataSOs selected in the Project window and borrows every other hero's numbers from
the newest full report; it writes `benchmark-selected-*.csv`.

## Team Comp Benchmark (`DevTools/Benchmark/TeamCompBenchmark`)

Fights every team comp in `Data/BattleSetups/Team Comps/` against every stage in `GameManager._stages`, 20 runs aligned
and 10 not (`RunsAligned` / `RunsNotAligned`); a stage counts as won at 70% (`WinLine`: 14 of 20, 7 of 10), until a side
is dead or 90s (a timeout is not a win). Real fights as `CombatState` starts them (saved hexes, patterns, worn items,
items on hexes, the stage's deploy zone), 32 at once on hidden board copies stacked 20 units apart
(`HeroBenchmark.CopyBoard`; skill hits are physics triggers with no board check, so the gap keeps lanes apart). Each lane
takes the next fight as soon as its own ends; its heroes start a round of their own in the recorder
(`CombatRecorder.BeginRoundFor`). `BenchmarkSpeed` is the fast mode both share; it also sets `Hero.IsDrawn` false, so
heroes have no world-space bars or floating text. A comp saved on another board than a stage's is reported, not fought.
"Run Selected" takes the comps selected in the Project window. A full run is about 3,900 fights and 30 minutes.

It writes `BalanceLogs/teamcomp-*.csv`: per comp x stage x aligning a `team` row (win rate, seconds, heroes left, HP
left, team DPS, `won`, `win_line`) and a `hero` row each (DPS, share of team damage, damage taken, survive rate, died at),
and logs how many stages each comp wins aligned and not.

## Google Sheet

Every report is imported into the **MagicSchool4 Benchmark** sheet (`magicschool4-benchmark` in
`.claude/scripts/sheets_config.json`): once back in Edit mode, `HeroBenchmarkMenu.ImportWhenBackInEditMode` runs
`.claude/scripts/benchmark_import.py`. Hero reports go to `Runs` (appended, stamped `run_time` and `kind`) and `Latest`
(overwritten); team reports to `Team Runs` and `Team Latest`. It needs `python` on PATH with `gspread`; a failed import
only logs an error. The `Team Dashboard` tab reads `Team Runs` with live formulas (dropdowns in the hidden `Team Lists`
tab): pick a run, a run to compare with, and aligned or not. Reports from before the `won` / `win_line` columns fall back
to the scripts' `DEFAULT_WIN_LINE`. `.claude/scripts/team_dashboard.py --rebuild` rebuilds both tabs after the dashboard
itself changes (20 comps, 10 stages and 12 heroes fit in its fixed rows).
