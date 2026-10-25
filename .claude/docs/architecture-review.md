# Magic School 4: Architecture Review

Read-only review of `Assets/Scripts/` (314 .cs files, 24,006 lines, 15 asmdefs plus 1 test asmdef), the working tree as it is, uncommitted work included. All paths are relative to the repo root.

## Verdict

- **Structurally healthy for its size.** The asmdef graph has no cycles and the compiler enforces it. Files are small (the largest is 512 lines). There is no real god class. Skills follow one consistent DSL.
- **The biggest cost to an agent is context, not code.** `CLAUDE.md` is 62 KB (about 15k tokens) and loaded into every session. About 70% of it is a feature changelog, and it has drifted in at least 12 places.
- **The second cost is shotgun surgery.** A skill hook, a stat or an align field each needs 4-6 files kept in step. Registries are hand-maintained and fail silently.
- **Agents cannot check their own work.** There are 24 tests, all on targeting, and the fixture uses reflection on private fields. Almost every change needs Play mode to verify.
- **Overall:** an agent can work here with a medium context load. The fixes are mostly docs, tests and removing pass-through layers, not a re-architecture.

## Project tier

**Small-game, heading for long-lived.** It is a solo project with 10 stages, about 41 hero skills and 30 items, a lot of dev tooling, and AI-authored churn (238 commits; `Hero.cs` changed 45 times).

The architecture fits that tier, with one exception: the module split is slightly *over* the Unity skill's 3-7 module guideline (15 assemblies). Most of the extra assemblies are cheap leaves (Input 48 lines, VFX 46, Engine 72), so they cost little. They do cause some pain:

- Typed `ScriptableObject` fields stand in for `ItemDataSO`: `HeroDataSO.cs:20`, `HeroPlacement.item`.
- The Editor tooling looks fields up by string (see P7).

That is the one place the boundaries cost more than they save.

## Keep

1. **The compiler-enforced module graph.** It is acyclic, and Contracts and Engine are true leaves (`Contracts/MagicSchool.Contracts.asmdef` references nothing). The `GameManager.Instance` singleton is only reached from `Player/` and `Core/Benchmark` (`Player/Dragging.cs:50-395`, `Player/PlayerController.cs:51-159`). Both of those may legally see Core, so lower layers never reach up.
2. **The composition root.** `Core/GameManager.cs:246-281` builds every service in one `Awake()` and wires panels through Contracts interfaces. `GameStateMachine` owns the phases. Init order is explicit, which is exactly what `unity-architecture.md` recommends.
3. **Thin MonoBehaviour plus plain C# helpers for Hero.** `Hero` delegates to `HeroStateMachine`, `FindTarget`, `HeroItems`, `HeroSpawn`, `Stat` and `AttackCooldown`, which are all plain classes (`Combat/Heroes/Hero.cs:103-122`). The partial-file split is documented in the class header (`Hero.cs:16-23`).
4. **The skill DSL.** Every `Skills/Library/Build/*Skill.cs` has the same shape: `Tuned()` numbers, then `SkillName`/`Description`, then `BuildActiveFlow`. See `ElfSkill.cs` (48 lines). An agent can copy a sibling and get it right. `SkillLibrary` plus `SkillIdEnum` with frozen ints and "retired" comments (`SkillIdEnum.cs:6-29`) is a sound serialization discipline.
5. **CombatRecording as a plugin.** It is subscribed only in `Core/HeroSpawner.cs:83-87`, so Combat never knows about it.
6. **Data/config lifecycle mostly right.** `HeroDataSO`, `BattlePlacementSO`, `ShopOddsSO` and `BenchmarkSettingsSO` are authored-only. Runtime state lives in `Stat`, `Wallet` and `Shop`.
7. **Inline "FLAGGING" notes.** There are about 45, and they are an honest debt register (`grep -rn FLAGGING Assets/`). Keep the convention and turn it into a tracked list (step 1 of the plan).
8. **The benchmark harnesses.** `Core/Benchmark/*` gives headless-ish balance verification, which is rare and valuable for an agent.

## Problems, ranked by impact on agent efficiency

### P1. CLAUDE.md is too big, mostly changelog, and drifting (impact: every session; effort S-M)

- **Evidence:** 547 lines, 62,647 bytes. The "What's built vs. pending" section alone is `CLAUDE.md:151-525` (375 lines). It is narrative feature history, for example the Reaper revive, Blacksmith forge table and Troll feed details, mixed with rules.
- **Drift:** at least 12 wrong claims (see the Doc drift section). Some sentences contradict each other: the Wide Square zone is "the left 6" at `:92` and "the left 8 columns" at `:221`.
- **Why it matters:** an agent pays about 15k tokens per session before reading any code, and stale facts cause wrong edits. For example, an agent will look for `HeroSpawner.SpawnUnplaced` or `ISellZone.ContainsScreenPoint`, and neither exists.
- **Fix:** cut CLAUDE.md to about 120-150 lines:
  - the module graph (correct it);
  - the non-obvious rules (`HexNumber` must stay a struct, add shared components to `BaseHero`, the `StatSlots` landmine, the never-reuse-enum-ints rule, `save_scene` path, UI Builder strips comments, no recompile in Play mode, never commit/stage);
  - a "where to look" index.

  Move each feature's mechanics into the module's `*Module.md`, or into `.claude/docs/features/<feature>.md`, which an agent reads only when touching that feature. For drift, rely on code comments, which already carry the same "e.g." detail.

### P2. One hook or stat change touches 4-6 files through pass-through layers (effort S-M)

- **Evidence, skill hooks:** the uncommitted `AutoAttackDamageMultiplier` touched 5 files for one number (`git diff --cached --stat`): `SkillDefinition.cs:132`, `HeroSkill.cs:310-311`, `Hero.Skill.cs:81`, `HeroAttack.cs:104` and `SkeletonArcherSkill.cs:93`. `HeroSkill` (`Combat/Heroes/HeroSkill.cs`, 109 lines) is 100% null-check forwarding: every method is `_skill?.X()`. `Hero.Skill.cs:70-99` then forwards again.
- **Evidence, stats:** adding a stat touches `StatEnum`, `ModifierEnum`, `Modifiers/ActiveCustomModifier.cs` (the mapping), `Modifiers/ModifierResolver.cs:19` (`StatSlots = 17`, a hand-kept constant), `Combat/Heroes/Stats/Stat.cs:39-52` and `:147-162`, and `Combat/Align/AlignText.cs`. CLAUDE.md admits that `StatSlots` was once one short and Omnivamp silently did nothing.
- **Fix:**
  - Delete `HeroSkill`. Give `Hero` a non-null `SkillDefinition` (a `NoSkill` null object) and forward straight to it. That is one layer fewer per hook.
  - Compute `StatSlots` from `Enum.GetValues(typeof(StatEnum)).Cast<int>().Max() + 1`.
  - Add a test asserting every `ModifierEnum` stat maps to a `StatEnum` (see P4).

### P3. Hand-kept registries with silent failure modes (effort S)

- **Evidence:**
  - Adding a hero needs `SkillIdEnum.cs` (an int), `SkillLibrary.cs:13-60` (a dictionary entry), the skill class, a `HeroDataSO` asset, a prefab variant, and `_shopRoster` in `Scenes/Board.unity`.
  - Items need `ItemIdEnum`, `Items/Library/ItemLibrary.cs` (a dictionary), a one-line `Library/Build/X.cs` and an `ItemDataSO`. Thirty items each have a 10-line file wrapping one `Buff(...)` call (`Items/Library/Build/IronPlate.cs`).
  - A miss only logs at runtime (`SkillLibrary.cs:68-72`).
- **Why it matters:** an agent cannot see a forgotten registration without Play mode.
- **Fix:** an EditMode `RegistryTests` class, with no scene:
  - every non-`None` `SkillIdEnum` and `ItemIdEnum` has a builder;
  - every `HeroDataSO` in `Data/Heroes/Stats/**` has a prefab that is a `BaseHero` variant and a resolvable `SkillId`;
  - every `ItemDataSO` id resolves;
  - every Campaign stage has a `BoardLayout` and a non-empty `DeployZone`;
  - `StatSlots` covers `StatEnum`.

  This is about 100 lines and gives the biggest safety gain per effort in the whole review.

### P4. Almost no tests, and the one fixture is fragile (effort M)

- **Evidence:** `Assets/Tests/EditMode/` holds 24 tests, all `FindTarget` (`FindEnemyTests.cs`). `BoardFixture.cs:128-160` resets `EnemyScan` caches by reflecting on private field names. It resets 3 of the 4 frame caches and misses `_stepMapFrame` (`Combat/Heroes/Data/EnemyScan.cs:22`, `:148-156`). Tests that move a hero mid-test (`FindEnemyTests.cs:132`) can therefore read a stale step map. That is a latent fixture bug. The owner's own note says "I don't understand any of this" (`FindEnemyTests.cs:9`).
- **What is testable with no scene:** these pure classes have no tests today. `CombatMath` (damage, DF, penetration), `Crit` (the PRD constant), `HeroStar`, `HeroLevel`, `HeroPrice`, `ModifierResolver`/`Stat` (`FollowMaxHP`, `keepsHealth`), `Wallet`, the `ShopOddsSO` roll, `AlignShape`/`AlignHexOffset`, and `SkillTuning`.
- **Fix:**
  - Add `[assembly: InternalsVisibleTo("MagicSchool.Combat.Tests")]` and an `internal void EnemyScan.Invalidate()`. Replace the reflection with it.
  - Make the frame-cache clock injectable, or key the caches on a board "version" bumped by `BattleBoard` on every move, which also removes the hidden `Time.frameCount` dependency.
  - Add a `MagicSchool.Core.Tests` asmdef for the pure classes above.
  - Note that tests run through Unity's test runner (in the Editor or `-batchmode -runTests` with the Editor closed). Document the command in CLAUDE.md so an agent can verify without Play mode.

### P5. Team-comp loading and fight-start logic are copied 2-3 times (effort M)

- **Evidence:**
  - Loading a comp (pattern, star, worn item, items on hexes) is written three times: `Core/StageEditor.cs:183-226`, `Core/Benchmark/TeamCompBenchmark.cs:225-251`, and partly `Core/HeroSeed.cs:36-57` (level and star only, so `_isPlayerSeed` games get no patterns or items).
  - Fight start is `CombatState.cs:217-237` and its copy "as CombatState.OnEnter does it" at `TeamCompBenchmark.cs:197-211`. The copy already skips `RebuildTunedSkills`.
  - The "who is still standing" query is `CombatState.cs:275` and `TeamCompBenchmark.cs:261`.
- **Why it matters:** the benchmark is the balance oracle. When the real path changes and the copy does not, the benchmark measures a different game, silently.
- **Fix:** put one `TeamCompLoader.Load(comp, board, spawner, onItemFail)` in Core, plus `FightRules.Begin(board, aligning)` and `FightRules.StandingTeams(board)`. CombatState, StageEditor and both benchmarks call them.

### P6. Spawning inside Core goes through events, then a lookup by hex (effort S)

- **Evidence:** `HeroSeed`, `StageEditor` and `Bench` raise `OnSpawnRequested`, which `HeroSpawner` handles (`Core/HeroSpawner.cs:29-31`). The caller then fetches the new hero back with `_board.WhoReservedThisHex(hex) is Hero` (`HeroSeed.cs:53-56`, `StageEditor.cs:197-203`), with the comment "the spawn is immediate, so the hero is on the hex now".
- **Why it matters:** `HeroSeed` and `StageEditor` are in the *same* assembly as `HeroSpawner`, so the event buys no decoupling there. Their correctness also depends on the handler running synchronously.
- **Fix:** keep the event only for `Bench` (Combat cannot see Core). Give `HeroSeed` and `StageEditor` the `HeroSpawner` and have `Spawn(...)` return `Hero`.

### P7. The Editor tooling is stringly-typed because the Editor assembly cannot see Contracts or Items (effort S)

- **Evidence:** 40+ `FindProperty("...")`/`FindPropertyRelative("...")` calls in `Core/Editor/AlignEditorWindow.*.cs` and `StageEditorMenu.cs`, for example `"selfWhenEmpty"`, `"healFromAllyDamage"`, `"_alignPatterns"`. CLAUDE.md justifies it: the window works through `SerializedObject` "because `Editor` does not reference `Contracts`". Renaming a field in `AlignMark.cs` silently breaks the Align Editor.
- **Also:** `ForgeRow.itemsByTier` (`Combat/Heroes/Data/HeroDataSO.cs:20`) and `HeroPlacement.item` are typed `ScriptableObject` because Combat "cannot name ItemDataSO". Yet `Items` references only Contracts, Modifiers and StatScaling, so `Combat -> Items` would be **acyclic** and is a legal edge.
- **Fix:** let `MagicSchool.Editor` reference Contracts, Items and Combat. It is editor-only, so the boundary has no runtime value. Keep `SerializedObject` for undo, but write `nameof(AlignMark.selfWhenEmpty)`. Optionally add `Combat -> Items` and type the two fields as `ItemDataSO`.

### P8. AlignMark is a flag-bag whose meaning depends on its list index (effort M; needs a data migration)

- **Evidence:** `Combat/Align/AlignMark.cs:52-99` has one struct with ally fields, self fields, `selfWhenEmpty`, `healFromAllyDamage` and `special`. Which fields count is decided by *position*: `AligningHex.ColourOf` (`AligningHex.cs:40-45`) says index 0 is orange (self only), index 1 is green (ally only), and a third ordinary mark "does nothing". `AlignBonuses.cs:70-72` applies this.
- **Why it matters:** an agent editing an asset or adding a mark cannot see from the type which fields are dead. CLAUDE.md needs a whole paragraph to override its own earlier wording about mark colours.
- **Fix:** an explicit `kind` enum (Orange, Green, Special). Migrate it in `OnValidate` from the index, then ignore the index. Later, split the payloads if the owner wants (the owner's own note at `AlignMark.cs:11` prefers code over Inspector data anyway).

### P9. The skill DSL is being bent for advanced skills (effort M)

- **Evidence:**
  - `ShieldKnightSkill.cs:57-60` changes already-built `Tuning.CastTime` and `Cadence.cadenceDuration` at cast time.
  - `:90` and `:133` use `.AmplifiedBy(_ => x - 1f)`, where "an amplifier of n - 1 is x n", to express "deal X% of damage taken".
  - `EttinSkill.cs:67` documents the same off-by-one convention.
- **Why it matters:** agents copy neighbouring skills, so the hack spreads, and the "-1" is an easy sign error.
- **Fix:**
  - Add `Damage(...).Scaled(Func<IEffectable,float> multiplier)` with ×n semantics, and leave `AmplifiedBy` as is or deprecate it.
  - Add `Tune(castTime: () => seconds)` as a dynamic provider, so nothing changes built parts.

### P10. Skills sniff interfaces on `Caster` (effort M)

- **Evidence:** `ICombatant` is self-flagged as a "god interface" (`Contracts/Interface/ICombatant.cs:8-12`). `Hero` implements 15 interfaces (`Hero.cs:24`). Skills downcast at runtime: 10× `is IAligned`, 4× `is IHeroStats`, 2× `is IHexMeasurable`, `is IItemWearer`, `is IFallen`, and others (`grep "is I[A-Z]" Skills/`). See `SkeletonArcherSkill.cs:95` and `:139-140`.
- **Why it matters:** a typo or a missing interface on a summon type only fails at runtime as "bonus is 0". An agent has to know which of the 15 interfaces exists.
- **Fix:** add one `ISkillCaster : ICombatant, IHeroStats, IAligned, IHexMeasurable, IItemWearer, IFreeCaster, IRevivable, ISpawner` in Contracts. Type it on `SkillDefinition.Caster` and in the effect callbacks. Do not split `ICombatant` further.

### P11. Runtime dev tools live in Core and ship in builds (effort M)

- **Evidence:** `Core/Benchmark/*` (1,531 lines) and `Core/StageEditor.cs` (303 lines) are runtime code with file IO (`HeroBenchmark.Report.cs:100-176`). No code carries `#if UNITY_EDITOR` except `Economy/ShopOddsSO.cs` and `Items/Item.cs`. They also mutate global statics: `Hero.IsDrawn` (`Hero.cs:69`) and `BenchmarkSettingsSO._current`. Core's real composition code is only about 1,500 of its 5,975 lines.
- **Why it matters:** an agent searching "Core" loads benchmark noise, and the Web build carries dev tooling.
- **Fix:** add a `MagicSchool.DevTools` runtime asmdef that references Core, with `defineConstraints: ["UNITY_EDITOR || DEVELOPMENT_BUILD"]`. Expose the few `GameManager` internals it needs through `InternalsVisibleTo`.

### P12. Smaller items (effort S each)

- **Hot-path allocation:** `SkillDefinition.Tuned()` does `Path.GetFileNameWithoutExtension` plus string concat on every read (`Skills/SkillTuning.cs:38-39`). Numbers are read per auto attack (`SkeletonArcherSkill.cs:93-96` reads 4 per attack) and per frame for inspector text. Key the dictionary on the `(filePath, name)` tuple instead.
- **Scene-wide `FindObjectsByType<MonoBehaviour>` to find interfaces** happens in 3 places: `GameManager.cs:273`, `PlayerController.cs:38` (self-flagged) and `ShopPanelController.cs:129`. Let `GameManager` hand the panels to Player too.
- **The `Engine` wrappers are barely used:** `SceneHelper.Instantiate` is called once against 6 direct `Instantiate` calls, and `DebugTool` is called 9 times against 46 direct `Debug.Log` calls. Either delete them or stop claiming they decouple anything (ARCHITECTURE.md "Engine").
- **`Dragging.cs:441`** does `(Hero)tracked == hero`, a hard cast over `ICombatant` that throws on any non-Hero combatant.
- **The working tree has staged changes** (`M ` on the 5 WIP files), which goes against CLAUDE.md's "Don't stage either" rule.

## Doc drift (all checked against the code)

| # | Claim | Reality |
|---|---|---|
| 1 | CLAUDE.md:20 `Combat -> Contracts, Engine, Skills, VFX, Modifiers` | Also references **StatScaling** (`Combat/MagicSchool.Combat.asmdef`, GUID 65a07a…) |
| 2 | CLAUDE.md:12-24 graph | **Items is missing entirely**; it actually references Contracts, Modifiers, StatScaling and TextMeshPro |
| 3 | CLAUDE.md:15 `VFX -> (nothing)` | References Unity.TextMeshPro |
| 4 | CLAUDE.md:72 "Each module carries a `*Module.md`" | Missing for Core, Player, UI, Engine, VFX and Editor (6 of 15) |
| 5 | `Items/ItemsModule.md` "References Contracts only", "(later) give to a hero" | References Modifiers and StatScaling too; items are already worn, shared and forged |
| 6 | `Combat/CombatModule.md` "referenced by UI/Gameplay" | There is no Gameplay module; it is referenced by UI, Economy, Core, Player and Editor. Modifiers and StatScaling are missing from its list |
| 7 | CLAUDE.md:106 `Hero` is glue implementing `ICombatant, IHexPlaceable, IHeroStats`, `Init(data, board, team, templateActions)` | 15 interfaces (`Hero.cs:24`); `Init(..., SkillDefinition skill)` (`Hero.cs:103`); real logic in `TakeDamage`, `Revive` and `RouteTo` |
| 8 | CLAUDE.md:142 `HeroSpawner.SpawnUnplaced`, `ISellZone.ContainsScreenPoint` | `SpawnUnboughtHero` (`HeroSpawner.cs:60`), `IsInSellBoundary` (`ISellZone.cs:10`) |
| 9 | CLAUDE.md:176 `Hero.RotateAlign` | `Hero.RotateAlignMark` (`Hero.Board.cs:25`) |
| 10 | CLAUDE.md:124 "one grid of 8 columns" | The Wide Square layout used by every stage has 12 columns |
| 11 | CLAUDE.md:95 and :221 `<Shape>-test` stages in `BattleSetups/Enemy Formation/` | That folder is gone (CLAUDE.md:334 says so itself); `BattleSetups/` holds only `Campaign/`, `Team Comps/` and `QuickSave.asset` |
| 12 | CLAUDE.md:213-216 "`_shopRoster` holds only those nine" | 24 entries in `Scenes/Board.unity` |
| 13 | CLAUDE.md:139 and :492 `#HeroPanel` still empty, Hero Panel not built | `MainScreen.uxml:3,15` instances `HeroPanel.uxml`, driven by `UI/Panel/InspectorController.cs` |
| 14 | CLAUDE.md:144 "All 24 [hero prefabs]" | 46 prefab variants (35 Roster, 8 Enemy, 3 Have Skill) plus Dummy and MovingDummy |
| 15 | CLAUDE.md:92 vs :221 | The Wide Square zone is "left 6" in one place and "left 8 columns" in the other |
| 16 | `ARCHITECTURE.md:24-26` graph | Core is missing CombatRecording, Items and Economy; Editor is missing Combat and Skills; there are no Economy, Items or CombatRecording rows |
| 17 | `ARCHITECTURE.md:98-138` | `BulwarkSkill.cs` does not exist (the skill is Shield Knight's); `SkillStep`/`SkillActionGroup` were renamed `SkillFlow`/`SkillPart` (the folder is still `SkillActionGroup/`); "17 heroes" is now 41 skills |

Spot-checked and accurate: `StatSlots`, `FollowMaxHP`/`FollowStartMana`, `KeepChosenTargetOnlyIfTied`, `BeginRoundFor`, `IsMidStep`, `HexStep.CutOff`, `ConsumeKeepShop`, `TryBuyPickedUp`, `AmplifiedBy`, `IsSkillMovingMe`, `ArrivingHex`, `StepsToReach`, and the Editor/UI/Economy/Core/Player asmdef edges.

**Is CLAUDE.md too large?** Yes. At about 15k tokens per session it costs more than reading the 5 files most changes touch, and at roughly one drifted claim per 45 lines, a fair share of what it says is wrong. It should hold rules and pointers; the code comments already hold the mechanics, accurately.

## Refactor plan (smallest risk first; each step ships on its own)

1. **Docs diet (S, zero code risk).** Cut CLAUDE.md to rules, the corrected graph, a where-to-look index and the test command. Move the feature notes into per-module `*Module.md` files (add the 6 missing ones) or `.claude/docs/features/`. Fix all 17 drift rows; regenerate or delete ARCHITECTURE.md's graph. Turn the FLAGGING grep into a list in `.claude/docs/planning.md`.
2. **Registry and invariant tests (S).** Add `RegistryTests` (P3), and compute `ModifierResolver.StatSlots` from the enum (P2). Afterwards, any agent adding a hero, item or stat gets a red test instead of a silent zero.
3. **Fix the test seam (S).** Add `InternalsVisibleTo` and `EnemyScan.Invalidate()`, and delete the reflection in `BoardFixture` (this fixes the missed `_stepMapFrame`). Add a `Core.Tests` asmdef with tests for CombatMath, Crit, Stat/ModifierResolver, HeroStar, HeroLevel, HeroPrice and Wallet (P4).
4. **Delete `HeroSkill` (S-M).** Use a null-object `SkillDefinition` and have `Hero.Skill.cs` call it directly. A new hook then touches 2 files instead of 4 (P2).
5. **Editor references Contracts/Items/Combat; `nameof` replaces string property names (S)** (P7). Optionally add `Combat -> Items` and type `ForgeRow` and `HeroPlacement.item` as `ItemDataSO`.
6. **Return the hero from `HeroSpawner` inside Core (S)** (P6). Then **extract `TeamCompLoader` and `FightRules` (M)** and point CombatState, StageEditor and TeamCompBenchmark at them (P5). Verify with one Team Comp Benchmark run before and after: the win rates should match within noise.
7. **`ISkillCaster` in Contracts, typed `SkillDefinition.Caster` (M)** (P10). This is mechanical; the compiler finds every site.
8. **Skill DSL primitives: `Scaled(×n)` and dynamic `Tune` providers (M).** Port Shield Knight, Ettin and the others off the `-1f` and mutation hacks (P9). Verify each with Hero Benchmark "Run Selected".
9. **`MagicSchool.DevTools` asmdef for Benchmark and StageEditor (M)** (P11).
10. **Explicit `AlignMark.kind` with an `OnValidate` migration (M)** (P8). Do this last: it touches serialized assets, so diff every `HeroDataSO` after the migration.

## Skip now

- **A DI container, service locator framework, event bus or Zenject-style install.** `GameManager.Awake` is a fine bootstrap at this size.
- **Clean Architecture use-case and port layers, presenters, gateways and mappers** (`usecase-input-output-ports`, `adapt-*`). There is no external system to isolate. Domain classes using `UnityEngine.Mathf`, `Vector3` and `Debug` is fine (`frame-domain-purity` and `frame-logging-abstraction` do not pay here). If anything, *remove* the Engine wrappers rather than extend them.
- **Splitting Contracts per client** (`dep-interface-ownership`). One shared leaf is simpler for an agent to search.
- **Splitting `Heroes/` and `Placements/` into separate assemblies.** It was tried; the coupling is real.
- **Merging or splitting more small assemblies.** Input, VFX and Engine cost almost nothing; churning asmdefs risks GUID and reference breakage for no gain.
- **Moving item and skill modifiers into ScriptableObject data.** The owner prefers code (`AlignMark.cs:11`), and the 10-line item files are harmless once P3's test exists.
- **Replacing the `GameManager.Instance` singleton.** It is only used upward-legal, from Player and DevTools.
- **Micro-optimising the FLAGGING'd O(n) loops** (`BattleBoard.cs:186`, `ModifierResolver.cs:56`, `HexFinder.cs:25`). n is at most 56 hexes and about 20 heroes. Only the `Tuned()` allocation in P12 is a real per-hit cost.
