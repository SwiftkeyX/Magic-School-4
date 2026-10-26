# Refactor plan

Built from [architecture-review.md](architecture-review.md) (Opus review against the `unity-architecture`,
`unity-scriptdesign` and `clean-architecture` skills) plus the personal review in [planning.md](planning.md).

**Goal:** an agent can make a typical change (new skill, stat, shop rule, align bonus) by loading less, touching
fewer files, and checking its own work without Play mode. It is not a re-architecture: the module graph is sound.

**Rules for every phase**

- Each phase is shippable alone: it compiles, the tests pass, and the game plays the same. You commit after each one.
- Gameplay must not change. Phases marked **(bench)** are checked by running the Team Comp Benchmark before and after;
  win rates should match within noise.
- Work on one `feature/refactor` branch from `develop`.
- Commit the current WIP (Skeleton Archer auto-attack change, staged right now) before starting.

---

## Phase 0 - Baseline (S)

- [x] Commit the WIP. Run the full Hero Benchmark and Team Comp Benchmark and keep both reports as the baseline.
  Baseline, on commit `2dba717` (2026-10-25):
  - Hero: `BalanceLogs/benchmark-2026-10-25_21-09-15.csv`
  - Team comp: `BalanceLogs/teamcomp-2026-10-25_21-41-17.csv` (3,900 fights, 32 min). Stages won aligned / not:
    Elf-Ettin-ShieldKnight-10 10/5, -9 9/4, -5 5/1; Skeleton-Knight-BlackSmith-10 10/5, -5 2/1; the other 8 comps 1/0.
- [x] Bug found during the run (not a refactor item): `Move.Update` (`Skills/.../TemplateActionChild/Move.cs:146`)
  keeps moving its caster after the caster is destroyed - a benchmark lane ending mid-leap floods the console with
  `MissingReferenceException` every frame. Fixed (`Move.Update` stops and destroys itself once its caster is gone),
  checked live in phase 4.

## Phase 1 - Safety net first (S-M)

Tests before touching code, so later phases fail loudly instead of silently.

- [x] `RegistryTests` (`Assets/Tests/Core`, EditMode, no scene): every `SkillIdEnum` / `ItemIdEnum` builds; every stat
  can be modified; every `HeroDataSO` has a `BaseHero`-variant prefab and a skill that resolves; every item asset
  resolves (except `Data/Items/default`, `BaseItem.prefab`'s placeholder); every Campaign stage and team comp has a
  layout, a deploy zone and real heroes.
- [x] `ModifierResolver.StatSlots` computed from `StatEnum` instead of hand-kept.
- [x] Test fixture: `EnemyScan.Invalidate()` (all four frame caches) + `InternalsVisibleTo` instead of reflection.
- [x] Tests for the pure classes: `CombatMath`, `Crit`, `Stat`/`ModifierResolver`, `HeroStar`, `HeroLevel`, `HeroPrice`,
  `Wallet`, in a new `MagicSchool.Core.Tests` assembly. 58 tests in all (24 old, 34 new).
- [x] How to run the tests:
  - **Editor open (Coplay):** see CLAUDE.md "Tests" (`wait-for-tests.ps1` + `RunEditModeTests.cs`). It returns at once (or times
    out - harmless) and writes `Temp/test-results.txt`: a `TOTAL passed= failed=` line, then one line per test.
    It refuses to start while a scene has unsaved changes: the Test Runner would open a modal "save?" dialog and
    freeze the Editor.
  - **Editor closed (headless):**
    `"C:/Program Files/Unity/Hub/Editor/6000.4.9f1/Editor/Unity.exe" -batchmode -projectPath . -runTests -testPlatform EditMode -testResults Temp/test-results.xml -logFile -`
    Exit code 0 = all passed, 2 = a test failed. It cannot run while the Editor has the project open.

## Phase 2 - Docs diet (S, no code risk)

- [x] Cut CLAUDE.md from 547 lines / 62.6 KB to 137 lines / 10.8 KB: module graph (from the asmdefs), the non-obvious
  rules, a where-to-look table, the test command. The feature mechanics moved to `.claude/docs/features/` (9 files);
  the 6 missing `*Module.md` were added and the wrong ones rewritten, so all 15 modules have one.
- [x] Fixed the 17 wrong claims from the review while moving the text. Fixed `ARCHITECTURE.md`'s graph and its skill
  example (Bulwark -> Shield Knight, `SkillStep` -> `SkillFlow` / `SkillPart`, 17 heroes -> 41 skills).
- [x] The 51 `FLAGGING` notes are listed, grouped, in [flagging.md](flagging.md).

## Phase 3 - Comment purge (M, no logic change)

Your call: delete inline comments; keep a summary above a class only where it is needed.

- [x] One module per pass, one commit each. The "why" comments (bugs they prevent, Unity quirks, the skill DSL's rules)
  were moved into class summaries first. About 2,950 comments removed; the comment block directly above each type is
  kept. A Roslyn tool did the removal and checked every file's code tokens and `#if`s came out identical.
- [x] Summaries: only where a class needed one.
- [x] `.uss`/`.uxml` untouched. Exception: test files keep their inline comments - they explain the board geometry
  behind each expected answer.
- [x] Owner notes (`FIXLATER`, the commented-out Harpy stance) added to [flagging.md](flagging.md).

## Phase 4 - DevTools module (M)

Your call: the benchmark gets its own module.

- [x] New `MagicSchool.DevTools` assembly (`Assets/Scripts/DevTools/`) holding the benchmarks and the Stage Editor,
  referencing Core, excluded from release builds (`UNITY_EDITOR || DEVELOPMENT_BUILD`). Core and Player know the Stage
  Editor only as `IStageEditing`, which DevTools registers at start; `HeroSpawner.SpawnHero` is public for it, and
  DevTools reads Core's internals through `InternalsVisibleTo`.
- [x] Verified: the Stage Editor registers and adds an enemy in Play mode, and is null with the mode off. Hero Benchmark
  ran twice (`benchmark-2026-10-26_08-14-27` / `08-15-45`): baseline vs new differs no more than two new runs differ
  from each other (max 12% vs 8.8%, random crits and targets). Team Comp Run Selected on Skeleton-Knight-BlackSmith-5
  (`teamcomp-2026-10-26_08-18-45`) wins the same stages as the baseline, and the console stays clean (the leap fix).

## Phase 5 - Remove pass-through layers (S-M) (bench)

- [x] Deleted `HeroSkill`. `Hero` holds a never-null `SkillDefinition` (`Skills/NoSkill` for a hero without one) and
  calls it directly. A new skill hook is `SkillDefinition` + `Hero.Skill.cs` (+ the caller).
- [x] `HeroSpawner.SpawnHero` returns the `Hero`. `HeroSeed` and the Stage Editor are handed it and use the hero it
  returns, instead of an event + a lookup by hex. `Bench` keeps its event (Combat cannot see Core).
- [x] Verified: tests; Team Comp Run Selected on Skeleton-Knight-BlackSmith-5 and Elf-Ettin-ShieldKnight-5
  (`teamcomp-2026-10-26_08-28-00`) win the same stages as the baseline; Stage Editor adds an enemy in Play mode.

## Phase 6 - One copy of fight start and team loading (M) (bench)

- [x] `Core/FightRules` (Begin: board live, aligning, Skill Tuner rebuilds, combat start; Standing: who is still up,
  reviving counts) and `Core/TeamCompLoader` (a saved comp's heroes with hex, level, star, pattern, worn item, then the
  items on hexes; the caller decides what a left-over item does). `CombatState` and `TeamCompBenchmark` share
  `FightRules`; the Stage Editor and `TeamCompBenchmark` share `TeamCompLoader`. The benchmark's copy had skipped the
  Skill Tuner rebuild; it now runs the same step.
- [x] Verified: tests; Stage Editor loads a comp in Play mode (4 heroes, stars and worn item kept); Team Comp Run
  Selected on two comps (`teamcomp-2026-10-26_08-36-51`) wins the same stages as the baseline.
- [x] Full Team Comp Benchmark after phases 5-6 (`teamcomp-2026-10-26_09-07-47`): 246 of 260 win rates identical to
  the baseline, mean change 0.67 points, no errors. Two verdicts flipped, both on the 70% line and in opposite
  directions (Elf-9 stage 4 off 60 -> 80%, Skeleton-10 stage 10 on 70 -> 50%): noise.

## Phase 7 - Interfaces (M)

Your review: `Hero` has 15 interfaces.

- [x] `Contracts/ISkillCaster : ICombatant, IHeroStats` holds everything a skill asks of a unit. `SkillDefinition.Caster`
  is typed as it, so `Caster is IAligned aligned ? ... : 0` became `Caster?.AlliesOnSpecialMark(...)`; a missing answer
  is now a compile error instead of a bonus of 0.
- [x] Audit: `ISpawner`, `IAligned`, `IItemWearer`, `IForger`, `IHexMeasurable`, `IFallen`, `IFreeCaster`, `IRevivable` were
  only used by Skills (and one `IRevivable` check in FightRules): merged into `ISkillCaster` and deleted. `Hero` now
  implements 7: `ICombatant, IHexPlaceable, IHeroStats, IInspectableHero, IDraggable, ISellable, ISkillCaster`.
- [x] Verified: tests; Hero Benchmark within noise (`benchmark-2026-10-26_09-11-10`); full Team Comp Benchmark
  (`teamcomp-2026-10-26_09-38-59`) vs the run before this phase: 0 verdicts flipped, mean change 0.69 points.

## Phase 8 - GameManager (M)

Your review: split it into several managers. The review found it is a sound composition root at 385 lines, and the
singleton is only used from where it is allowed. So the split is by job, not by more singletons:

- [x] `Core/PlayerActions` (move, sell, wear, items on hexes, turn a pattern, buy / put back a shop hero, combine, each
  followed by aligning) and `Core/PhaseRequests` (start combat, continue, restart, skip, keep the shop once), built and
  held by GameManager as `Actions` and `Requests`. Callers name the job (`GameManager.Instance.Actions.MoveHero`)
  rather than GameManager forwarding, which would only be a new pass-through layer. GameManager: 347 -> 199 lines.
- [x] `GameManager` stays the one MonoBehaviour that wires things up at start, and holds the run (stage, winner, team
  size), speed and pause.
- [x] Verified: tests; in Play mode a hero added to the bench and moved onto the board, a fight started, the stage
  skipped and the next stage reached (Preparation -> Combat -> Result -> Preparation, stage 2), console clean; Team
  Comp Run Selected on two comps (`teamcomp-2026-10-26_09-47-29`) wins the same stages as the baseline.

## Phase 9 - Editor typing (S)

- [x] `MagicSchool.Editor` references Contracts and Items too (editor-only, so free). The 30 field-name strings for
  public fields (`AlignMark`, `AlignHex`, `AlignPattern`, `HeroPlacement`) are `nameof(...)`, so a rename is a compile
  error. Seven stay strings because the fields are private and `nameof` cannot reach them from another assembly:
  `_alignPatterns`, `_heroesPlacement`, `_stageEditMode`, `_skillId`, `_price`, `_name`, `_description`.
- [ ] Optional, not done: allow `Combat -> Items` and type `ForgeRow` items and `HeroPlacement.item` as `ItemDataSO`.
  The docs record "Combat does not reference Items" as a deliberate boundary, so this is the owner's call.

## Phase 10 - Skill builder primitives (M) (bench)

- [x] `Scaled(×n)` replaces the `AmplifiedBy(_ => x - 1f)` trick (Shield Knight, Ettin). `Tune(castTime: () => ...)`
  and `HealSkillEffect.LastingFor(() => ...)` are read when played, so Shield Knight no longer edits built parts at
  cast time. Verified: tests; Hero Benchmark numbers for Shield Knight and Ettin identical to before.
  Follow-up: Dwarf still resizes its blast tuning at each cast.
- [x] `Tuned()` keys its numbers by caller (file + member), so a read no longer builds a string.

## Phase 11 - AlignMark colour as a field (M, touches assets) - last

- [] An explicit `kind` (Orange / Green / Special) instead of meaning-by-list-position, migrated in `OnValidate`.
  Diff every `HeroDataSO` afterwards.

---

## Skip

DI containers, event buses, Clean Architecture use-case/port layers, splitting `Contracts`, re-splitting
`Heroes/`+`Placements/`, replacing the `GameManager` singleton, micro-optimising loops over <60 hexes.

## Open questions for you

1. **When?** The current goal is balancing. Phases 0-4 make balancing work cheaper (tests, smaller docs, cleaner
   code, benchmark in its own module) and change no gameplay. Do 0-4 now and 5-11 after balancing?
2. **Comments:** OK to move the "why" comments into class summaries / `*Module.md` rather than delete them outright?
3. **Phase 9 optional edge `Combat -> Items`:** allow it?
