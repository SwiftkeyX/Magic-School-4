# Magic School 4

Unity auto-chess game (Unity 6.4, 2D): heroes are bought, benched and placed on a hex board, then auto-battle until one
side is dead. Instead of traits, heroes **align**: a hero marks hexes around itself and allies standing on them get a
bonus. A run is 10 stages. UI is **UI Toolkit** (not uGUI). The current goal is balancing.

## Where to look

Read the feature doc before touching a feature; it is not loaded by default.

| Working on | Read | Code |
|---|---|---|
| Board, layouts, deploy zone, walking, pathfinding, targeting | [board-and-movement](.claude/docs/features/board-and-movement.md) | `Combat/Placements/`, `Combat/Heroes/States/`, `Combat/Heroes/Data/FindTarget` |
| Damage, mana, crit, stats, modifiers, stars, enemy level | [combat-and-stats](.claude/docs/features/combat-and-stats.md) | `Combat/Heroes/`, `Modifiers/`, `StatScaling/` |
| A hero's skill, skill hooks, descriptions, live tuning | [skills](.claude/docs/features/skills.md) | `Skills/`, `Skills/Library/Build/<Hero>Skill.cs` |
| Aligning, marks, Align Editor | [aligning](.claude/docs/features/aligning.md) | `Combat/Align/`, `Core/Editor/AlignEditorWindow*` |
| Items, wearing, items on hexes | [items](.claude/docs/features/items.md) | `Items/`, `Combat/Heroes/HeroItems` |
| Shop, gold, buy/sell drag, UI panels, bench | [shop-and-ui](.claude/docs/features/shop-and-ui.md) | `Economy/`, `UI/`, `Player/Dragging` |
| Stages, enemies, boss, hero prefabs | [campaign-and-enemies](.claude/docs/features/campaign-and-enemies.md) | `Data/BattleSetups/`, `Data/Heroes/Stats/` |
| Hero / Team Comp Benchmark, Google Sheet import | [benchmarks](.claude/docs/features/benchmarks.md) | `DevTools/Benchmark/`, `.claude/scripts/` |
| Stage Editor, skip stage, add hero, window style | [dev-tools](.claude/docs/features/dev-tools.md) | `DevTools/StageEditor`, `Core/Editor/` |

Also: [ARCHITECTURE.md](ARCHITECTURE.md) (modules for humans, a skill worked through), each module's `*Module.md`
next to its asmdef, `.claude/docs/planning.md` (the owner's to-do), `.claude/docs/refactor-plan.md`,
`.claude/docs/flagging.md` (the owner's design doubts), `.claude/docs/design/` (hero and team design).

## Modules (`Assets/Scripts/`)

One `.asmdef` per top-level folder, so the compiler enforces the direction. Arrows point at what a module may reference:

```
MagicSchool.Contracts       -> (nothing)                 interfaces + enums everyone shares
MagicSchool.Engine          -> (nothing)                 CurveMotion, DebugTool, SceneHelper
MagicSchool.Input           -> Unity.InputSystem         PlayerInputSystem, the only place input is read
MagicSchool.VFX             -> Unity.TextMeshPro         FloatingText
MagicSchool.StatScaling     -> Contracts                 StatRatio + Scaling (amount maths), Crit
MagicSchool.CombatRecording -> Contracts                 the damage meter's books
MagicSchool.Modifiers       -> Contracts, StatScaling
MagicSchool.Skills          -> Contracts, Engine, StatScaling, Modifiers
MagicSchool.Items           -> Contracts, Modifiers, StatScaling, Unity.TextMeshPro
MagicSchool.Combat          -> Contracts, Engine, Skills, VFX, Modifiers, StatScaling
MagicSchool.Economy         -> Contracts, Combat, Items                   wallet, shop, seller
MagicSchool.UI              -> Contracts, Combat, Items, Input, UnityEngine.UI
MagicSchool.Core            -> Contracts, Engine, Skills, Combat, CombatRecording, Items, Economy
MagicSchool.Player          -> Contracts, Combat, Core, Items, Input
MagicSchool.DevTools        -> Core and everything Core references   benchmarks, Stage Editor (not in release builds)
MagicSchool.Editor          -> Core, Combat, Skills, DevTools, Contracts, Items   (Core/Editor/, Editor-only)
```

Rules no single file shows:

- **`Contracts` references nothing, ever.** Anything it reaches for drags that module under every other. So `IPlaceable`
  holds no `Hex` (the hex-aware half is `IHexPlaceable`, in Combat), and `GamePhaseEnum` stays in Core
  (`BattleBoard.IsBattleOn` carries the one bit Combat needs).
- **`Modifiers` and `StatScaling` are apart from `Skills`** so items, aligning or traits can grant "+20 armour" without
  taking every skill builder along. `Combat` references `Modifiers` only because `Stat` holds a `ModifierResolver`.
- **`CombatRecording` is a plugin: nothing in `Combat` references it.** A hero raises `DamageEvent` / `HealEvent`;
  `HeroSpawner`, the one place a hero is built, subscribes the recorder. The event structs live in `Contracts`.
- **`Input` is its own module so a keypress is not a reason to reference `Player`.** Its namespace `MagicSchool.Input`
  shadows `UnityEngine.Input` inside `MagicSchool.*`; never write a bare `Input.` - the project is New-Input-System-only
  and `UnityEngine.Input` compiles fine, then throws at runtime.
- **`Economy` is referenced only by `Core`.** `UI` reads the shop only through `IShop`, which `GameManager` hands it.
- **`Heroes/` and `Placements/` share the `Combat` assembly on purpose** (a hero needs its hex, a board tracks its
  occupant; splitting was tried). Aligning is a folder of `Combat` for the same reason.
- **Combat cannot name `ItemDataSO`**, so item fields there are typed `ScriptableObject` (`ForgeRow`, `HeroPlacement.item`).
  The Align and Stage Editors reach fields through `SerializedObject`: public ones by `nameof`, private ones
  (`_alignPatterns`, `_heroesPlacement`, ...) by string - renaming one of those breaks the window silently.
- **Folder names match assembly names; namespaces follow the folder** (`Combat/Heroes/Hero.cs` is
  `MagicSchool.Combat.Heroes`). Grouping folders that are not namespace segments are fine (`Contracts/Interface/`).
  That is why it is `StatScaling/` and `Economy/`: a namespace `MagicSchool.Scaling` holding a class `Scaling` cannot be
  used unqualified.
- `Core/GameManager` is the composition root: it builds every service in `Awake()`. The `GameManager.Instance`
  singleton is only used from `Player` and the dev tools; lower modules never reach up to it.
- **`DevTools` is left out of release builds** (`defineConstraints: UNITY_EDITOR || DEVELOPMENT_BUILD`), so nothing
  that ships may reference it. Core sees the Stage Editor only as `IStageEditing`, which DevTools registers at start
  (`StageEditor.Register`, `GameManager.RegisterStageEditor`); DevTools reaches Core's internals through
  `InternalsVisibleTo`.

## Rules that bite

- **`HexNumber` must stay a `struct`**: board lookups rely on value equality.
- **Hero prefabs are all variants of `Assets/Prefabs/Hero/BaseHero.prefab`.** Add shared components to `BaseHero`, never
  to one hero; add a new hero only as a variant of it. A hero's `HeroDataSO` (`Data/Heroes/Stats/`) holds the prefab, not
  the other way round.
- **`==` on an interface-typed reference does not see a destroyed Unity object** (plain reference equality). Cast
  first: `x as Object == null`, or `x is Object o && o == null`.
- **Never reuse a retired `SkillIdEnum` / `ItemIdEnum` / `StatEnum` / `ModifierEnum` int**: assets store the ints.
- **Skill numbers are `Tuned(...)` properties, never `const`** (live tuning rewrites them).
- **A skill description writes amounts through `AD()` / `AP()` / `DF()` / `MaxHP()`**, never as `{X}% AP`.
- A new hero needs: `SkillIdEnum` entry, `SkillLibrary` entry, the skill class, a `HeroDataSO`, a `BaseHero` variant,
  and `GameManager._shopRoster` in `Scenes/Board.unity` if sold. A new item: `ItemIdEnum`, `ItemLibrary`, a
  `Library/Build/` file, an `ItemDataSO`. `RegistryTests` catches a missed registration.
- A mark's colour is its `AlignMark.kind`: orange (self bonus only), green (ally bonus only), special (white, pays
  nobody). The Align Editor stamps it from position - orange first, green second, special after - so keep that order
  when editing marks by hand. An unset kind reads by position.
- Combat is started by the space bar (`PlayerController.TryStartCombat`); a diagnostic calls
  `GameManager.Instance.Requests.StartCombat()` itself.

## Tests

EditMode tests in `Assets/Tests/EditMode` (`MagicSchool.Combat.Tests`, targeting) and `Assets/Tests/Core`
(`MagicSchool.Core.Tests`: registries, maths, stats). Run them after any non-trivial change:

- **Editor open:** start `.claude/scripts/wait-for-tests.ps1` in the background (PowerShell), then Coplay
  `execute_script` on `.claude/scripts/RunEditModeTests.cs`. The script refuses if a scene really differs from disk; the
  helper clicks Save on the "Scene(s) Have Been Modified" dialog the Test Runner opens anyway (it freezes the Editor),
  then prints the `TOTAL passed= failed=` line and any failure from `Temp/test-results.txt`.
- **Editor closed:** `"C:/Program Files/Unity/Hub/Editor/6000.4.9f1/Editor/Unity.exe" -batchmode -projectPath . -runTests
  -testPlatform EditMode -testResults Temp/test-results.xml -logFile -` (exit 0 = passed, 2 = a test failed).

Gameplay changes are checked with the benchmarks ([benchmarks](.claude/docs/features/benchmarks.md)): a Team Comp
Benchmark run before and after, win rates within noise.

## Working in this project

- **Coplay MCP** (`mcp__coplay-mcp__*`) drives the live Editor: `play_game` / `stop_game`, `execute_script` (arbitrary
  C#), `get_game_object_info`, `get_unity_logs`, `check_compile_errors`, prefab editing via `add_component` /
  `set_property`.
- **Verify non-trivial changes live**, not by compiling alone: enter Play mode, run a throwaway diagnostic
  `execute_script` (written to the scratchpad) that checks runtime values or reproduces the bug, then `stop_game`.
  Lifecycle bugs (e.g. `Awake()` order) do not show as compile errors.
- **Never refresh or recompile in Play mode** - it breaks `GameManager`. Check the editor state first.
- `execute_script` compiles against a limited set of assemblies. A garbled `Microsoft.CodeAnalysis` resource error IS a
  compile error in your script; reach the module's types by reflection instead.
- **`save_scene` takes the full path** (`"Scenes/Board"`). A bare `"Board"` saves a new `Assets/Board.unity` and switches
  the Editor to it.
- **A raw text edit to an open `.unity` file is ignored** (Unity reserializes from memory). Check
  `EditorSceneManager.GetActiveScene().isDirty` is false, then `open_scene` the same path to reload.
- **UI Builder deletes every comment in a `.uss` / `.uxml` it saves.** After a UI Builder session, `git diff` those
  files and restore what it removed; prefer hand-editing small changes.
- The Magic School 4 windows share one look (`Core/Editor/WindowStyle`); a new window or control draws through it.
- Moving a script is safe if its `.meta` moves with it (references are by GUID, even across a namespace change).
  Verify in Play mode anyway.
- Planning docs may hold "Don't answer" notes on steps: those are self-notes, not questions.
- VS Code debugging: the `visualstudiotoolsforunity.vstuc` extension, "Attach to Unity" in `.vscode/launch.json`.

## Git

- Remote: https://github.com/SwiftkeyX/Magic-School-4.git. `main` is stable versions only; `develop` is the default
  branch, where finished features are merged; `feature/...` branches start from `develop` and track their own copy on
  GitHub, never `develop`.
- **Never commit or push.** The user commits and pushes everything themselves - finish the work and leave it in the
  working tree for them to review. Never add a `Co-Authored-By: Claude` trailer or any other AI attribution to a commit
  message or PR description: the user does not want Claude showing up as a contributor on GitHub.
- **Don't stage either.** `git mv`/`git rm` write to the index, which makes work look half-committed; use plain
  `mv`/`rm` and leave everything in the working tree.
