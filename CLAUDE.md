# Magic School 3

Unity auto-chess game: heroes are bought, benched, and placed on a hex board, then auto-battle. Heroes on opposing teams move toward each other and fight; the side with all heroes dead loses. UI is built with **UI Toolkit** (not uGUI).

## Modules (`Assets/Scripts/`)

The scripts are split into assemblies (`.asmdef`), one per top-level folder, so the dependency
direction is enforced by the compiler rather than by convention. Arrows point at what a module is
allowed to reference:

```
MagicSchool.Contracts  -> (nothing)          leaf: interfaces + enums everyone shares
MagicSchool.Engine     -> (nothing)          leaf: DebugTool, SceneHelper, CurveMotion
MagicSchool.Input      -> Unity.InputSystem  leaf: PlayerInputSystem, the only place input is read
MagicSchool.VFX        -> (nothing)          leaf: FloatingText
MagicSchool.StatScaling-> Contracts          leaf-ish: StatRatio + Scaling, the amount math
MagicSchool.CombatRecording -> Contracts     leaf: the damage meter's books, see below
MagicSchool.Modifiers  -> Contracts, StatScaling
MagicSchool.Skills     -> Contracts, Engine, StatScaling, Modifiers
MagicSchool.Combat     -> Contracts, Engine, Skills, VFX, Modifiers
MagicSchool.UI         -> Contracts, Combat, Items, Input
MagicSchool.Core       -> Contracts, Engine, Skills, Combat, CombatRecording
MagicSchool.Player     -> Contracts, Combat, Core, Items, Input
MagicSchool.Editor     -> Core                              (Editor-only)
```

Rules that aren't visible from any single file:

- **`Contracts` references nothing and must stay that way.** It is the bottom of the graph; anything
  it reaches for drags that module underneath every other. This is why `IPlaceable` holds no `Hex`
  (the hex-aware half is `IHexPlaceable`, over in Combat next to `Hex` itself), and why
  `GamePhaseEnum` stayed in Core — Combat only needed one bit of it, so `BattleBoard.IsBattleOn`
  carries it instead.
- **`Modifiers` and `StatScaling` are separate from `Skills` on purpose.** A skill is only what
  *grants* a modifier today; traits, items or augments would grant the same ones, and none of them
  should have to reference `Skills` - and so `TemplateAction`, `SkillDefinition` and every hero
  builder - just to say "+20 armour". `Combat` references `Modifiers` for one thing only:
  `Stat` holds a `ModifierResolver`. The resolver lives in `Modifiers` rather than next to `Stat`
  because a module that holds modifiers but cannot resolve them is not reusable - a trait or item
  system would have had to take `Combat` along just to make "+20 armour" mean anything. `Combat`
  still writes nothing but `ICustomModifier`/`IModifier` in its own code. The folder is
  `StatScaling/` rather than `Scaling/` because namespaces follow the folder, and a namespace
  `MagicSchool.Scaling` holding a class `Scaling` cannot be used unqualified.
- **`CombatRecording` is a plugin, and nothing in `Combat` references it.** A hero raises
  `DamageEvent`/`HealEvent` and carries on; `HeroSpawner` - the one place a hero is ever built -
  subscribes the recorder to those events. So combat behaves identically whether or not anyone is
  counting, which is what stops a reporting feature turning into a combat feature. The four event
  and outcome structs live in `Contracts` for the same reason `IEffectable` does: they are what
  both sides must agree on, so neither side has to reference the other.
- **`Input` is its own module so a keypress is not a reason to reference `Player`.** Reading a
  key belongs to no one in particular - the board drag asks, the stage keys ask, a dev tool that
  raises a panel asks - and before the split, asking cost you `Player`, and so `Combat`, `Core`
  and `Items` behind it. `Player` no longer references `Unity.InputSystem` at all; only `Input`
  does. The namespace is `MagicSchool.Input` while the class is `PlayerInputSystem`, so the two
  never collide the way `MagicSchool.Scaling` holding a `Scaling` would - but be aware the
  namespace shadows `UnityEngine.Input` for a bare `Input.` written inside `MagicSchool.*`.
  Nothing should write one: this project is New-Input-System-only and `UnityEngine.Input`
  compiles fine then throws at runtime.

- **`Heroes/` and `Placements/` share one assembly on purpose.** A hero needs its hex and a hex
  tracks its occupant; that coupling is real, so `Combat` admits it rather than pretending otherwise.
  Splitting them was tried and doesn't work.
- **Folder names match assembly names; namespaces follow the folder**, e.g.
  `Combat/Heroes/Hero.cs` is `MagicSchool.Combat.Heroes`. Grouping subfolders that aren't namespace
  segments are fine (`Contracts/Enum/`, `Contracts/Interface/` are both `MagicSchool.Contracts`).
- Each module carries a `*Module.md` next to its asmdef stating its boundary.

## Architecture

- **`Contracts/`** — `ICombatant` (a unit fighting on the board), `IEffectable` (damage/heal/modifier
  — what a `SkillEffect` needs), `ITargeter`, `IPlaceable`, `IPlacement` (hex or bench slot),
  `IHeroStats`, `IModifier`, plus `TeamEnum`/`HeroStateEnum`/`TriggerEnum`/`ModifierEnum`.
- **`Combat/Placements/Hex/Hex.cs`** — one per tile. Computes its own neighbors by distance rather
  than axial math (flat-top grid: same-column neighbors ~1.0 apart, diagonal ~1.118; a 1.15x
  threshold on the nearest distance catches exactly the real neighbors, including at board edges
  with fewer than 6). Needs `Init(board, hexNumber)` from `BattleBoard` before use — do not call
  `GetNeighbors()` first. A hex does **not** track its own occupant; the board does.
- **`Combat/Placements/Board/BattleBoard.cs`** — discovers child `Hex`es in `Awake()` and keys them
  in `Dictionary<HexNumber, Hex>` derived from position rather than hardcoded, so it survives layout
  changes. Owns hero tracking (`HeroesOnBoard`) and hex reservations (`_reservedBy`), and carries
  `IsBattleOn`, pushed down by `GameManager` so a hero never reaches for that singleton.
- **`Combat/Placements/Hex/HexPathfinder.cs`** — **A\***, not greedy and not BFS. Returns ONE step
  toward a hex adjacent to the target; A* so equally-short routes tie-break toward the target
  instead of by discovery order. The step returned can look like backward progress when routing
  around a blocked hex — whether to take it or wait is the caller's call.
- **`Combat/Heroes/Hero.cs`** — the only MonoBehaviour for a hero, deliberately glue with no logic of
  its own: `ICombatant, IHexPlaceable, IHeroStats`. Built by `HeroSpawner` via `Hero.Init(data,
  board, team, templateActions)`.
- **`Combat/Heroes/States/`** — the actual behaviour: `HeroStateMachine` over Idle / Walk / Attack /
  Cast / Stunned / Dead, with `Transition` holding the conditions. Walking is `Vector3.Lerp` + an
  `AnimationCurve` over `1 / moveSpeed` seconds per hex — not `CharacterController` (the board is 2D
  sprites with no colliders; that path throws `MissingComponentException`). A hero reserves its
  destination hex the moment it *commits* to a step, not on arrival — reserving on arrival would let
  two heroes target the same free hex in one frame.
- **`Combat/Placements/Hex/HexNumber.cs`** — `struct HexNumber { TeamEnum team; int column; int row; }`.
  Must stay a `struct`: value equality is what makes the dictionary lookups work: a `class` here
  would silently break them into reference equality.
- **`Skills/`** — a skill is a `SkillDefinition` of `SkillStep`s, each holding `SkillActionGroup`s
  that pick a `TemplateAction` (projectile / AoE / hitbox variants) by `SkillCondition`. Effects are
  applied through `IEffectable`, so nothing here knows the `Hero` type. `SkillLibrary.Resolve` maps
  a `SkillIdEnum` to its builder.
- **`Core/`** — the composition root: `GameManager` (phase, winner, wiring), `HeroMover`,
  `HeroSeller`, `HeroFormation`, `HeroSpawner`.

**UI Toolkit panel pattern** (`Assets/UI Toolkit/`): one shared `UIDocument` on `MainScreen.uxml`, which has empty named containers as slots (`#ShopPanel` is wired up; `#BenchPanel`/`#TraitPanel`/`#HeroPanel` still empty). Each actual panel is its own `.uxml` file plus a small controller script (see `ShopPanelController.cs`) that clones its UXML into the matching-named slot at runtime. The convention: name a panel's root element in its own `.uxml` the same as the slot container waiting for it in `MainScreen.uxml` — the controller looks up the slot by reading its own root element's name, so no manual string field has to be kept in sync.
- The Bench is deliberately **not** a UI Toolkit panel: it needs real `Hero` GameObjects standing on it (animated sprites, not flat icons), which `VisualElement` can't host. It's world-space instead, using the same `Hero`/Physics2D-drag approach as board placement (see `Assets/Scripts/Combat/Placements/Bench/`, `Assets/Prefabs/Bench/`). The `#BenchPanel` slot in `MainScreen.uxml` stays empty/unused for this reason. The Shop panel (buy heroes) is the UI Toolkit one: drag a shop slot's ghost outside the shop panel's bounds to buy, drop it back inside to cancel — see `ShopPanelController.ResolveDrop`. Selling is the same bounds read from the other direction: drop a held world-space `Hero` onto the shop and it's sold (`PlayerController.DropHero` → `GameManager.SellHero` → `HeroSeller`). Because the two drags live in different coordinate spaces, and `Player` may not reference `UI`, the shop answers `ISellZone.ContainsScreenPoint` rather than handing out its rect — it owns the screen→panel conversion, y-flip included. A drop only sells when no `IPlacement` is under the pointer, since the bench band sits directly above the shop.

**Hero prefabs are all variants of `BaseHero`** (`Assets/Prefabs/Hero/BaseHero.prefab`). All 24 —
including `Have Skill/` and `Dummy` — override only their name, sprite colour and root position;
everything else is inherited. **Add shared components to `BaseHero`, never to an individual hero**,
and only ever add a new hero as a variant of it (right-click `BaseHero` → Create → Prefab Variant).
A hero's `HeroDataSO` (in `Assets/Data/Heroes/Stats/`) holds the `_prefab` reference, not the other
way round — `Hero._SOData` is assigned at runtime by `HeroSpawner` via `Hero.Init()`.

## What's built vs. pending

- Movement, A* pathfinding and hex reservation work.
- Combat works: auto-attack on an attack-speed cooldown, mana (10 per attack, `HeroAttack.ManaPerAttack`),
  skill cast when mana caps and spent in `HeroCast.OnEnter`, damage/heal via `CombatMath`, and
  modifiers/statuses through `Stat`/`ModifierResolver`.
- Skills are built for 8 heroes (Vharn, Sithra, Bulwark, Roland, Quatre, Solace, Vesper, Pip).
- Between stages the player's team is revived *and* walked back to the hexes it started the
  fight on (`HeroFormation`, snapshot in `CombatState.OnEnter`, restore in
  `PreparationState.OnEnter`). Without the restore a hero stays where the fight left it —
  including dead ones, since `HeroDead` never releases its hex — often on an enemy tile the
  player could never have placed it on.
- **Not built yet:** the gold/economy system — the Shop resolves buy-vs-cancel on drag release but
  can't charge for it, and selling removes the hero but can't refund (see the BLOCKED notes in
  `ShopPanelController` and `HeroSeller`). Trait Panel and Hero Panel are
  still empty slots. There is no "Start Battle" button: combat is triggered by the space bar in
  `PlayerController.TryStartCombat`, which is temporary.
- `BattlePlacementSO` (the Inspector list of starting `HeroPlacement`s, in `Assets/Data/BattleSetups/`)
  has been reset to defaults multiple times across past type changes — double-check its values
  before relying on it.

## Working in this project

- **Coplay MCP** (`mcp__coplay-mcp__*` tools) is connected to this project's live Unity Editor: `play_game`/`stop_game`, `execute_script` (runs arbitrary C# in the Editor), `get_game_object_info`, `get_unity_logs`, `check_compile_errors`, prefab editing via `add_component`/`set_property`.
- **Always verify non-trivial script/prefab changes live** rather than trusting compilation alone: enter Play mode and run a throwaway diagnostic `execute_script` (write it to the scratchpad dir) that checks actual runtime values or reproduces the reported bug, then `stop_game`. Unity execution-order/lifecycle bugs (e.g. `Awake()` ordering races between objects) don't show up as compile errors.
  - Combat is space-bar gated, so a diagnostic usually needs to call `GameManager.Instance.StartCombat()` itself.
  - A script passed to `execute_script` is compiled by Coplay against a limited set of assemblies. If it fails with a garbled `Microsoft.CodeAnalysis` resource error rather than a real message, that IS a compile error in your script — reach for reflection instead of referencing the module's types directly.
- **`mcp__coplay-mcp__save_scene` gotcha:** passing a bare scene name (e.g. `"Board"`) does a "Save As" into the wrong path (`Assets/Board.unity` instead of `Assets/Scenes/Board.unity`) and silently switches the Editor's active scene to that new wrong file. Always pass the full relative path without extension, e.g. `"Scenes/Board"`.
- **Editing a `.unity` scene file directly (e.g. renaming a serialized field) has no effect while the Editor already has that scene open** — Unity reserializes from its in-memory copy, not from disk, so the on-disk edit is silently ignored (and would be clobbered by the next in-Editor save) until the scene is reloaded. After a raw text edit to a scene file's YAML, check `EditorSceneManager.GetActiveScene().isDirty` is `false` (no unsaved in-memory changes to lose) then call `open_scene` on the same path to force a reload before trusting/testing the change.
- **UI Builder deletes every comment in a `.uss`/`.uxml` it saves.** It reserializes the whole file from its in-memory model, and comments are not in that model — so opening a file to nudge one padding value silently strips the prose explaining every other value in it, with no warning and no diff until you look. It has already emptied `MatchStatus.uss` (10 blocks) and `HeroPanel.uxml` (4). Two consequences: after any UI Builder session, `git diff` the `.uss`/`.uxml` files and restore what it removed rather than assuming the deletions were deliberate; and prefer hand-editing these files in a text editor when the change is small enough to type. `HeroPanel.uxml` and `Ghost.uxml` carry a comment saying this, which the Builder will also delete.
- Moving script files between folders is safe as long as the `.meta` travels with the `.cs` — scene and prefab references are by GUID, so they survive both the move and a namespace change. Verify in Play mode anyway.
 — but respect explicit "Don't answer" notes on individual steps, those are self-notes rather than questions.
- VS Code debugging uses the `visualstudiotoolsforunity.vstuc` extension (the current official one, not the deprecated "Debugger for Unity"), with an "Attach to Unity" launch config already in `.vscode/launch.json`.
- GitHub remote: https://github.com/SwiftkeyX/Magic-School-3.git, branch `main`.
- **Never commit on your own.** Finish the work and leave it in the working tree — the user reads every diff before it becomes a commit. Only run `git commit` when they ask for it in that turn, in plain words ("commit"). Nothing else counts as permission: not a plan they approved, not an option they picked in a question, not "the change is done and tested", not a long stretch of work that feels like it needs a checkpoint. When they do ask, commit to `main` (solo project, no branches unless they ask for a PR), and split multi-part work into one change per commit. Never push unless asked.
- **Don't stage either.** `git mv`/`git rm` write to the index, which makes work look half-committed; use plain `mv`/`rm` and leave everything in the working tree.
