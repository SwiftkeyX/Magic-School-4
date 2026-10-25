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

- [] Commit the WIP. Run the full Hero Benchmark and Team Comp Benchmark and keep both reports as the baseline.

## Phase 1 - Safety net first (S-M)

Tests before touching code, so later phases fail loudly instead of silently.

- [] `RegistryTests` (EditMode, no scene): every `SkillIdEnum` / `ItemIdEnum` has a builder; every `HeroDataSO` has a
  `BaseHero`-variant prefab and a skill that resolves; every Campaign stage has a layout and a deploy zone.
- [] `ModifierResolver.StatSlots` computed from `StatEnum` instead of hand-kept (it already broke once).
- [] Fix the test fixture: `EnemyScan.Invalidate()` + `InternalsVisibleTo` instead of reflection (it misses `_stepMapFrame`).
- [] Tests for the pure classes: `CombatMath`, `Crit`, `Stat`/`ModifierResolver`, `HeroStar`, `HeroLevel`, `HeroPrice`, `Wallet`.
- [] Write down the command that runs the tests headless, so an agent can verify without Play mode.

## Phase 2 - Docs diet (S, no code risk)

- [] Cut CLAUDE.md from ~550 lines to ~150: module graph (corrected), the non-obvious rules, a where-to-look index,
  the test command. Feature history moves to each module's `*Module.md` (add the 6 missing ones) or
  `.claude/docs/features/`.
- [] Fix the 17 wrong claims listed in the review. Fix or delete `ARCHITECTURE.md`'s graph.
- [] Turn the ~45 `FLAGGING` notes in code into a list here.

## Phase 3 - Comment purge (M, no logic change)

Your call: delete inline comments; keep a summary above a class only where it is needed.

- [] One module per pass. Before deleting, move any comment that records **why** (a bug it prevents, e.g. "reserve on
  commit, not arrival") into the class summary or the `*Module.md` - with CLAUDE.md slimmed, that is the last place
  that knowledge lives.
- [] Keep summaries short and readable; most small classes get none.
- [] Leave `.uss`/`.uxml` comments alone (they are the warning about UI Builder).

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
