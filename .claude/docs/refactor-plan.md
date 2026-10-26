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
- [] Bug found during the run (not a refactor item): `Move.Update` (`Skills/.../TemplateActionChild/Move.cs:146`)
  keeps moving its caster after the caster is destroyed - a benchmark lane ending mid-leap floods the console with
  `MissingReferenceException` every frame. Fixed in code (`Move.Update` stops and destroys itself once its caster
  is gone); not yet checked live.

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

- [] New `MagicSchool.DevTools` assembly holding `Core/Benchmark/*` and `Core/StageEditor` (~1,800 lines), referencing Core.
  Excluded from release builds (`UNITY_EDITOR || DEVELOPMENT_BUILD`). Core goes from ~6,000 lines to ~4,200.
- [] Verify: both benchmarks and the Stage Editor window still run.

## Phase 5 - Remove pass-through layers (S-M) (bench)

- [] Delete `HeroSkill` (100% null-check forwarding). `Hero` holds a never-null `SkillDefinition`. A new skill hook
  goes from 4-5 files to 2.
- [] `HeroSpawner.Spawn` returns the `Hero` for callers inside Core, instead of an event + lookup by hex.

## Phase 6 - One copy of fight start and team loading (M) (bench)

- [] Extract `TeamCompLoader` and `FightRules` (begin fight, who is still standing). `CombatState`, `StageEditor` and
  `TeamCompBenchmark` all call them. Today the benchmark has its own copy that already skips a step, so the
  balance tool can measure a different game than the one played.

## Phase 7 - Interfaces (M)

Your review: `Hero` has 15 interfaces.

- [] Add one `ISkillCaster` that skills type their caster as, replacing ~25 runtime `is IAligned` / `is IHexMeasurable` checks.
- [] Audit the 15: for each, who calls it. Merge the ones with a single caller and one method (`IForger`, `IFreeCaster`,
  `IRevivable`, `IFallen`, `ISpawner` are candidates) into fewer, larger ones. The ones that exist because a module
  boundary needs them stay. Target: well under 10.

## Phase 8 - GameManager (M)

Your review: split it into several managers. The review found it is a sound composition root at 385 lines, and the
singleton is only used from where it is allowed. So the split is by job, not by more singletons:

- [] After Phase 4, list what it still does. Move each group of jobs into a plain C# class it builds and holds
  (likely: team moves / sell / wear / combine / aligning refresh; and stage progression / skip / jump).
- [] `GameManager` stays the one MonoBehaviour that wires things up at start.

## Phase 9 - Editor typing (S)

- [] Let `MagicSchool.Editor` reference Contracts / Items / Combat (editor-only, so free). Replace the 40+ field-name
  strings in the Align Editor and Stage Editor with `nameof(...)`, so a rename cannot silently break them.
- [] Optional: allow `Combat -> Items` (it is acyclic) and type `ForgeRow` items and `HeroPlacement.item` as
  `ItemDataSO` instead of `ScriptableObject`.

## Phase 10 - Skill builder primitives (M) (bench)

- [] `Scaled(×n)` to replace the `AmplifiedBy(_ => x - 1f)` trick (Shield Knight, Ettin), and a dynamic
  `Tune(castTime: () => ...)` instead of editing built parts at cast time. Check each ported skill with Hero
  Benchmark "Run Selected".
- [] `Tuned()` stops allocating a string on every read (it runs on every auto attack).

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
