# Magic School 4

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
MagicSchool.Economy    -> Contracts, Combat, Items          wallet, shop, seller - see below
MagicSchool.Core       -> Contracts, Engine, Skills, Combat, CombatRecording, Items, Economy
MagicSchool.Player     -> Contracts, Combat, Core, Items, Input
MagicSchool.Editor     -> Core, Combat                      (Editor-only)
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

- **`Economy` is gold in and gold out, and only `Core` references it.** `Wallet`, `Shop`,
  `ShopEntry`, `ShopOddsSO` and `Seller` live there; `GameManager` builds them. `UI` still reads the
  shop only through `IShop` (Contracts), so the panel never sees `Shop`. The folder is not `Shop/`
  for the `StatScaling` reason: a namespace `MagicSchool.Shop` holding a class `Shop` cannot be used
  unqualified.
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
  `IHeroStats`, `IModifier`, plus `TeamEnum`/`HeroStateEnum`/`ModifierEnum`.
- **`Combat/Placements/Hex/Hex.cs`** — one per tile. Computes its own neighbors by distance rather
  than axial math (flat-top grid: same-column neighbors ~1.0 apart, diagonal ~1.118; a 1.15x
  threshold on the nearest distance catches exactly the real neighbors, including at board edges
  with fewer than 6). Needs `Init(board, hexNumber)` from `BattleBoard` before use — do not call
  `GetNeighbors()` first. A hex does **not** track its own occupant; the board does.
- **`Combat/Placements/Board/BattleBoard.cs`** — discovers child `Hex`es in `Awake()` and keys them
  in `Dictionary<HexNumber, Hex>` derived from position rather than hardcoded, so it survives layout
  changes. Owns hero tracking (`HeroesOnBoard`) and hex reservations (`_reservedBy`), and carries
  `IsBattleOn`, pushed down by `GameManager` so a hero never reaches for that singleton.
- **Board layouts** - the board owns no hexes. A layout is a prefab holding the hexes of one board shape
  (in `Assets/Prefabs/Board/`, built from `Board/Hex.prefab`: `Square Layout`, 8 columns of 7, and
  `Hexagon Layout`, 37 hexes in columns of 4 5 6 7 6 5 4; `Hourglass Layout`, 47 in 7 6 5 4 3 4 5 6 7;
  `Ring Layout`, the hexagon minus its centre 7, 30 hexes; `Diamond Layout`, 47 in 2 3 4 5 6 7 6 5 4 3 2;
  `Wide Square Layout`, 16 columns of 7 - the square at twice the width, the left 8 columns being the player's
  whole deploy zone; its first column sits at x -5.5, not centred, to clear the spare bench on the left;
  each has a `<Shape>-test` stage in `BattleSetups/Enemy Formation/`), and every stage names
  the one it is fought on in `BattlePlacementSO._boardLayout`. `PreparationState` empties the board
  (enemies wiped, the player's team to the spare bench), then `BattleBoard.SetLayout` destroys the old
  hexes and instantiates the stage's - skipped when the layout is the same as the last stage's. A
  stage's hex numbers (enemy placements, deploy zone) only mean something on its own layout. The
  layout instance inside `BattleBoard.prefab` is only there to be seen in the Editor.
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
- **`Combat/Placements/Hex/HexNumber.cs`** — `struct HexNumber { int column; int row; }`. The board
  is one grid of 8 columns (0 = far left) with row 0 at the top of each column; a hex's number carries
  no team. A unit's side is on its `HeroPlacement.team`, not on the hex it stands on. Must stay a
  `struct`: value equality is what makes the dictionary lookups work: a `class` here would silently
  break them into reference equality. A hex belongs to no team at all: it is in the
  player's deploy zone or it is not.
- **`Skills/`** — a skill is a `SkillDefinition` holding one `SkillFlow` for its active:
  `Flow(onStart, onHit, onExpired)`. Each of the three slots holds one `SkillPart` - one piece of the
  skill, which picks a `TemplateAction` (projectile / AoE / move / cast) - or several that start
  together, `Together(a, b)`, where the first one leads. Nothing follows `onHit` / `onExpired`;
  anything further is played from a hook on `SkillDefinition` (`OnAutoAttack`, `OnCombatStart`,
  `OnHeroDied`, or a part's own `OnSkillHit`). Effects are applied through `IEffectable`, so
  nothing here knows the `Hero` type. `SkillLibrary.Resolve` maps a `SkillIdEnum` to its builder.
- **`Core/`** — the composition root: `GameManager` (phase, winner, wiring), `HeroMover`,
  `HeroFormation`, `HeroSpawner`.

**UI Toolkit panel pattern** (`Assets/UI Toolkit/`): one shared `UIDocument` on `MainScreen.uxml`, which has empty named containers as slots (`#ShopPanel` is wired up; `#BenchPanel`/`#TraitPanel`/`#HeroPanel` still empty). Each actual panel is its own `.uxml` file plus a small controller script (see `ShopPanelController.cs`) that clones its UXML into the matching-named slot at runtime. The convention: name a panel's root element in its own `.uxml` the same as the slot container waiting for it in `MainScreen.uxml` — the controller looks up the slot by reading its own root element's name, so no manual string field has to be kept in sync.
- The Bench is deliberately **not** a UI Toolkit panel: it needs real `Hero` GameObjects standing on it (animated sprites, not flat icons), which `VisualElement` can't host. It's world-space instead, using the same `Hero`/Physics2D-drag approach as board placement (see `Assets/Scripts/Combat/Placements/Bench/`, `Assets/Prefabs/Bench/`). The `#BenchPanel` slot in `MainScreen.uxml` stays empty/unused for this reason. The Shop panel (buy heroes) is the UI Toolkit one: drag a shop slot's ghost outside the shop panel's bounds to buy, drop it back inside to cancel — see `ShopPanelController.ResolveDrop`. Selling is the same bounds read from the other direction: drop a held world-space `Hero` onto the shop and it's sold (`Dragging.DropHero` → `GameManager.Sell` → `Seller`). Because the two drags live in different coordinate spaces, and `Player` may not reference `UI`, the shop answers `ISellZone.ContainsScreenPoint` rather than handing out its rect — it owns the screen→panel conversion, y-flip included. A drop only sells when no `IPlacement` is under the pointer, since the bench band sits directly above the shop.

**Hero prefabs are all variants of `BaseHero`** (`Assets/Prefabs/Hero/BaseHero.prefab`). All 24 —
including `Have Skill/` and `Dummy` — override only their name, sprite colour and root position;
everything else is inherited. **Add shared components to `BaseHero`, never to an individual hero**,
and only ever add a new hero as a variant of it (right-click `BaseHero` → Create → Prefab Variant).
A hero's `HeroDataSO` (in `Assets/Data/Heroes/Stats/`) holds the `_prefab` reference, not the other
way round — `Hero._SOData` is assigned at runtime by `HeroSpawner` via `Hero.Init()`.

## What's built vs. pending

- Movement, A* pathfinding and hex reservation work.
- Combat works: auto-attack on an attack-speed cooldown, mana (10 per attack, the base of `StatEnum.ManaPerAttack`, set in `Stat`),
  skill cast when mana caps and spent in `HeroCast.OnEnter`, damage/heal via `CombatMath`, and
  modifiers/statuses through `Stat`/`ModifierResolver`.
- Every hero in `Data/Heroes/Stats/Player/` has a skill script in `Skills/Library/Build/`.
- Aligning (the game's synergy, in place of traits): a hero marks some of the 6 hexes around itself and an
  ally standing on a marked hex gets a bonus. The code is in `Combat/Align/` (`MagicSchool.Combat.Align`) - a
  folder of `Combat`, not its own assembly: `HeroDataSO` holds the marks while `Aligning` needs `Hero`, `Hex` and
  `BattleBoard`, so a separate module would have to sit both under and over `Combat`. Marks are data, `HeroDataSO._alignMarks`: one `AlignMark`
  is one bonus and every hex that gives it - `hexes` (a list of `AlignHex`: direction, and `distance` - how
  many hexes away in a straight line, 1 = adjacent, heroes in between do not block it and a missing hex on
  the way ends it; a hex off the straight lines adds a turn, `thenDirection` / `thenDistance`, 0 = none, and
  the two legs are tried in either order so a board edge does not lose it), then modifier, amount, and `percentOf` - None for a flat amount, else a percent of the
  receiver's own base stat. A hero with two different bonuses has two marks. A mark can also pay its owner:
  `selfModifier` / `selfAmount` / `selfPercentOf` are given to the owner once for every one of the mark's
  hexes an ally stands on; `selfAmount` 0 means none, and an ally `amount` of 0 makes it a self-only mark.
  Elf is the one hero with a self bonus so far (`ModifierEnum.MaxMana` -10 per hex: an amount can be
  negative); no hero uses distance 2+ or a second mark yet. `AlignBonuses.Refresh` (owned by `GameManager`, `RefreshAligning`) takes back every bonus it gave and gives them again
  for where everyone stands now: after every `GameManager.MoveHero` and `Sell`, when a stage opens, and once more in
  `CombatState.OnEnter` - so the inspector's numbers follow the placing. The bonuses are ordinary rest-of-fight
  modifiers; `Aligning` only answers which hexes are marked. The inspector rewrites its stat numbers every frame. Player
  team only: an enemy's marks do nothing. Directions are `HexDirectionEnum` (N NE SE S SW NW, the player
  faces right, so SW/NW are behind) and `Hex.GetNeighbor(direction)` finds the hex by angle. While the
  player holds one of his heroes in Preparation, `Player/AlignHighlight` (ticked after `Dragging`) has the
  board tint the zone hexes the held hero would mark from the hex under the pointer, one colour for each
  of its marks: orange for the first mark's hexes, green for the second's, and a third is painted like
  the first (`AligningHex.MarkedHexesByBonus`, `BoardColours.ShowAlignMarks`, colours in the Inspector).
  Placed heroes' marks are not shown. **Magic School 4 > Align Editor** (`Core/Editor/AlignEditorWindow`) edits a
  hero's marks by clicking hexes around it instead of typing directions in the Inspector: heroes on the left,
  the hexes within 3 in the middle (every one can be marked - the window works out the turn), one box per `AlignMark` on the
  right. It works through `SerializedObject` on the `HeroDataSO`, outside Play mode, because `Editor` does not
  reference `Contracts` and so cannot name the modifier and stat enums. The inspector panel lists a hero's marks under ALIGNING (`IInspectableHero.AlignDescription`, written by `AlignText`, hidden when empty). The nine
  shop heroes all have marks, in three teams of three designed in the sheet's `Aligning` tab (Ettin / Dryad /
  Elf, Orc Blademaster / Werewolf / Shield Knight, Skeleton Archer / Reaper / Centaur); `_shopRoster` holds
  only those nine. The teams are being redesigned as three teams of five that share heroes (each carry -
  Elf, Werewolf, Skeleton Archer - in one team, every other hero in two). Only the Elf team is done, and
  the assets are ahead of the sheet: a spread fan where every mark is 2 hexes away - Dryad 2 N of Elf,
  Reaper 2 S, Ettin 2 NE, Orc 2 SE (Elf N/S, Ettin SW/NW, Orc NW/SW, Dryad SE/S, Reaper N/NE). It needs 5
  rows by 3 columns, which none of the Stage 01 deploy zones has (8-13 hexes on the Hexagon board), so it
  is played on `Enemy Formation/Wide Square-test` (the `Wide Square Layout`, zone = the left 8 columns). The other two teams no longer line up until their turn.
- Items on hexes (aligning's second filler, because five heroes cannot fill many marks): in Preparation
  an item dropped on a free deploy-zone hex lies there (`Dragging.DropItem` -> `GameManager.PutItemOnHex`
  -> `BattleBoard.Items`, a `HexItems`), and it takes the hex - no hero can be placed on it; it does not
  block walking. A single marked hex is a wearing hex when its `AlignHex.isWearing` is ticked - the option is per hex,
  not per bonus, set by right-clicking the hex in the Align Editor: `AlignBonuses.Refresh` gives the mark's
  owner the modifier of an item lying on one, through the same give-and-take-back list as
  every other bonus. An item is not an ally: it gives no ally bonus and never triggers the self bonus,
  which is what keeps hero pairs mattering. Two heroes whose wearing hexes overlap both wear the item
  (left alone for now). Wearing by dropping an item on a hero still works beside it. While an item is held
  the board lights up the team's wearing hexes. Items keep their hexes between stages unless the next
  stage's board differs, then `PreparationState.ReturnItems` moves them to the loose row. `HexItems` only
  knows `IEquipment`, so `Combat` still does not reference `Items`. No hero has a wearing hex yet.
- A stat needs a slot in `ModifierResolver.StatSlots` or no modifier can ever change it - the count was one
  short once and Omnivamp modifiers silently did nothing. Current mana follows `StartMana` when a modifier
  changes it after the stats are built (`Stat.FollowStartMana`), the way current HP follows max HP.
- Between stages the player's team is revived and put back on the hexes it started the fight on
  (`PreparationState.ReturnTeam` -> `HeroFormation.ReturnToStartingPlacement`), so a formation is kept from
  fight to fight. Only when the next stage is fought on a different board layout - its hexes are about to be
  destroyed - does the team go to the spare bench instead, which is how it used to be after every fight; a
  kept hex can lie outside the next stage's deploy zone. What follows describes that spare-bench path.
- The spare-bench path: the team is revived *and* sent to the spare bench (`HeroFormation`,
  snapshot in `CombatState.OnEnter`, `ReturnToSpareBench` in `PreparationState.OnEnter`), so the
  player places it again inside the next stage's deploy zone. Without this a hero stays where the
  fight left it — including dead ones, since `HeroDead` never releases its hex — often on an enemy
  tile the player could never have placed it on. The spare bench (`SpareBench` in the scene, on
  `GameManager._spareBench`, 12 slots left of the board) is a second `Bench` whose slots have
  `BenchSlot._acceptsDrop` off: the player takes heroes off it but `Dragging` refuses a drop onto
  it. If it is ever full the hero goes to the normal bench, then back to the hex it started on.
- Every stage has a deploy zone: `BattlePlacementSO._deployZone` lists the hexes the player may put
  a hero on, and an empty list means nowhere (`PreparationState` logs an error). `PreparationState`
  pushes it into `DeployZone.Set` (`BattleBoard.DeployZone`), which sets `Hex.IsInDeployZone`;
  `Dragging.ValidatePlacement` lets a player hero onto a hex inside it and an enemy (stage editor)
  onto a hex outside it, anywhere on the board. A hex has no colour of its own: the board paints the
  zone blue and every other hex red in Preparation, and the whole board neutral otherwise
  (`BoardColours.ShowDeployZone` - `BattleBoard.Colours` makes every "what colour is this hex" decision, `DeployZone` only knows which hexes are in it; colours on `BattleBoard` in the Inspector) - the zone limits placing,
  never walking. `BattleSetups/` holds only `Campaign/` and `Enemy Formation/`;
  the old test setups (`Archive/`, `ClusterTests/`, `Phase1Seeds/`, `Build demo/`) were deleted and
  live in git history.
- Gold and the shop's rules work, and both live in `Economy`: `Wallet` is the player's gold, `Shop` is
  what each slot sells, buying and refreshing. `ShopPanelController` only draws and turns a drag
  into "buy slot i" - it reads everything through `IShop` (Contracts), which `GameManager` hands it
  via `IShopPanel.BindShop`, because `UI` may not reference `Economy` or `Core`. A hero's price comes from its
  tier (`HeroPrice.Of`); `Shop.TryBuy` spends it, `Seller.Sell` refunds it, Refresh costs
  `_refreshCost`, and `ResultState` pays `_stageIncome` after every fight. The shop's roster and
  numbers are on `GameManager` in the Inspector.
- A shop slot sells a hero or an item. `Shop` holds `IShopEntry`s (`Economy/ShopEntry.cs`): a
  `HeroEntry` delivers to the bench (and fails when it is full, so nothing is charged), an
  `ItemEntry` drops a world-space `Item` through `ItemDrop` - the same spot the reward card uses,
  searched for the first free place along the row rather than counted. Heroes come from
  `_shopRoster`, items from `_shopItems`; an item's price is `ItemDataSO._price`. A loose item
  dropped on the shop sells for its full price (`Dragging.DropItem` → `GameManager.Sell` →
  `Seller`), in any phase - a hero only sells in Preparation. Heroes and items both sell through
  `ISellable` (Contracts); `Seller` only knows `ICombatant` for a hero's extra cleanup (its placement and
  board tracking), never `Hero` or `Item` themselves.
- Refresh rolls each slot by `ShopOddsSO` (`Assets/Data/Shop/ShopOdds.asset`, on `GameManager._shopOdds`):
  item-or-hero by that slot's entry in `_itemChanceBySlot` (`{0,0,0,100,100}`: slots 1-3 heroes,
  4-5 items), then a hero's tier by the row of `_tierOdds` for the current stage - only tiers that
  have heroes count, so a tier weighted 0 never comes up - then any hero of that tier. Repeats
  across slots are allowed - there is no shared pool. `PreparationState.OnEnter` calls
  `Shop.Restock()` - a free roll - so every stage, stage 1 included, opens on a new shop, unless the
  player has clicked Lock (`IShop.ToggleLock`), which keeps the stock until they unlock it.
  Refresh still works while locked.
- Right-click a shop slot to inspect it (`ShopPanelController.MakeSlotsInspectable` → `IShop.InspectableAt`).
  A slot holds data, not a live object, so it shows a stand-in: `ItemPreview` (Economy) or `HeroPreview`
  (Combat - it builds the skill through `SkillLibrary` with no caster, which only builds the text). `Economy`
  cannot reference `Skills`, so `GameManager` hands the shop the `HeroPreview` factory. The preview's
  `IsAlive` is "the slot still sells this", so buying or re-rolling closes the inspector. `UIDrag` only
  drags on the left button, so a right-click on a slot inspects instead of picking it up.
- Balancing: every `CombatRecord` knows `FightSeconds` / `SecondsAlive` (a `RoundClock` per round, stopped in
  `ResultState`; a death is the damage event that takes HP to 0), so the `_test` CSV in `BalanceLogs/` has
  per-second rates. **Magic School 4 > Balance > Run Hero Benchmark** (`Core/Benchmark/HeroBenchmark`,
  menu in `Core/Editor/HeroBenchmarkMenu`) runs every shop hero alone vs `Data/Benchmark/BenchDummy`
  (2000 HP, DF 30, topped up below half): a 30s damage test (DPS at 15s and 30s), a 30s AoE test (a tight
  cluster of 4 dummies around one clear nearest target; `aoe_gain` = AoE DPS / single-target DPS) and a tank test (100 raw damage/s until
  death or 30s). It runs outside the game's states, 8 heroes at once, each on its own hidden copy of the
  `BattleBoard` (a hero only sees its own board's heroes, so lanes cannot touch each other), in fixed 1/30s
  steps (`Time.captureDeltaTime`) with vSync, the frame cap, cameras and UI off - about 47s for all 35 -
  and writes `BalanceLogs/benchmark-*.csv` with each hero vs its role's median, flagged beyond +-20%.
  **Run Hero Benchmark (Selected)** tests only the HeroDataSOs selected in the Project window (seconds per
  hero) and borrows every other hero's numbers from the newest full report for the peer comparison; it
  writes `benchmark-selected-*.csv`. Ally-support skills still read low (no allies in any test), and heroes
  of a role share stats, so the tank test only separates heroes whose skills defend them.
- Adding a hero while testing (dev tool): **Magic School 4 > Add Selected Heroes To Bench**
  (`Core/Editor/AddHeroMenu`) puts the HeroDataSOs selected in the Project window on the player's bench for
  free, any hero, enemy-only ones included - Play mode only, through `GameManager.AddHeroToBench`. The Align
  Editor's "Add to bench" button does the same for the hero it is editing.
- Stage editing (dev tool): tick `GameManager._stageEditMode` and, in Preparation, enemies drag between
  the hexes outside the deploy zone like your own heroes do inside it, and dropping one on the shop removes it (no gold). **Magic School 4 >
  Stage Editor** adds the HeroDataSOs selected in the Project window as enemies and saves the enemy team
  into the current stage's `BattlePlacementSO` or a new one - red entries only, blue ones are kept.
  The same save writes the deploy zone: in the mode, clicking an empty hex puts it in or out of
  the zone (blue = in, red = out), and a save with an empty zone is refused. The click is read in
  `Player/ZonePainting`, ticked after `Dragging` so a click that picked something up never paints.
  `Core/StageEditor` is the runtime half (`GameManager.StageEditor` is null while the mode is off, which is
  what `Dragging` checks); `Core/Editor/StageEditorMenu` only writes the asset. The menu goes through
  `StageEditor` for anything team-specific because `Editor` does not reference `Contracts`.
- The reward card (`RewardPanelController`, an item pick after each win) is **unwired, not deleted**:
  `GameManager._useRewardPanel` is off, so `GameManager.Reward` is null and every use of it is
  null-safe - a win goes straight on. The panel's own `_showOnStart` is off too. Tick both to bring it back.
- **Not built yet:** Trait Panel and Hero Panel are still empty slots. There is no "Start Battle" button: combat is triggered by the space bar in
  `PlayerController.TryStartCombat`, which is temporary.
- The run is 10 stages: `GameManager._stages` points at `Assets/Data/BattleSetups/Campaign/Stage 01`-`10`
  (3 enemies on stage 1 up to 12 on stage 10, tier mix tracking the shop odds; stage 5 and 10 are the
  bosses). Heroes of one role share the same stats whatever their tier, so difficulty comes from
  enemy count and skills. Hero A (`SuperTank.asset`, 2000 HP, no skill) is enemy-only - it is not in
  the shop roster. The enemy roster is designed in the sheet's `Enemies` tab and built into
  `Assets/Data/Heroes/Stats/Enemy/` (prefabs in `Assets/Prefabs/Hero/Enemy/`). The tanks and marksmen
  are built, on their shop role's stats until stats are decided: Minotaur (single-target stun), Basilisk
  (+DF every cast), Big Slime (splits into 2 Small Slimes on death), Goblin Slinger (shoots the 2
  furthest enemies) and Kobold Skirmisher (3 shots at random enemies). Supports, fighters and the
  assassin are still only in the sheet. None is in a stage yet - add them with the Stage Editor.
- A hero can put units on the board mid-fight: a skill calls `ISpawner.SpawnUnitsNearby` (Contracts) on
  its caster, `Hero` picks the nearest free hexes (`Combat/Heroes/HeroSpawn`) and raises
  `OnSpawnRequested`, and `HeroSpawner` - which subscribes to every hero it builds - spawns them. The
  unit comes from `HeroDataSO._spawnUnit`, because `Skills` cannot name a hero. `SkillDefinition.OnDeath`
  is the caster's own death; `OnHeroDied` is everyone else's.
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
- GitHub remote: https://github.com/SwiftkeyX/Magic-School-4.git, branch `main`.
- **Never commit or push.** The user commits and pushes everything themselves - finish the work and leave it in the working tree for them to review. Never add a `Co-Authored-By: Claude` trailer or any other AI attribution to a commit message or PR description: the user does not want Claude showing up as a contributor on GitHub.
- **Don't stage either.** `git mv`/`git rm` write to the index, which makes work look half-committed; use plain `mv`/`rm` and leave everything in the working tree.
