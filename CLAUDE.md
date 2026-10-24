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
MagicSchool.Editor     -> Core, Combat, Skills              (Editor-only)
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
  `Wide Square Layout`, 12 columns of 5 - 6 columns a side, the left 6 being the player's whole deploy zone
  (it was 16 of 7; each side lost its 2 left columns and the board its 2 top rows, and the enemy half slid
  left to close the gap); its first column sits at x -3.5, not centred, to clear the spare bench on the left;
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
  two heroes target the same free hex in one frame. A step is `HexStep`, shared by `HeroWalk` and `HeroCast`:
  a skill part tuned `walkWhileCasting: true` keeps its caster in Cast for the whole cast time (no attacking)
  but walking toward its target as Walk would, standing still once in range (Orc Blademaster's storm).
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
  `OnHeroDied`, `OnDamageTaken` - the caster's own hits, raised from `Hero.TakeDamage` - or a part's own `OnSkillHit`). Effects are applied through `IEffectable`, so
  nothing here knows the `Hero` type. `SkillLibrary.Resolve` maps a `SkillIdEnum` to its builder.
- **`Core/`** — the composition root: `GameManager` (phase, winner, wiring), `HeroMover`,
  `HeroFormation`, `HeroSpawner`.

**UI Toolkit panel pattern** (`Assets/UI Toolkit/`): one shared `UIDocument` on `MainScreen.uxml`, which has empty named containers as slots (`#ShopPanel` is wired up; `#BenchPanel`/`#TraitPanel`/`#HeroPanel` still empty). Each actual panel is its own `.uxml` file plus a small controller script (see `ShopPanelController.cs`) that clones its UXML into the matching-named slot at runtime. The convention: name a panel's root element in its own `.uxml` the same as the slot container waiting for it in `MainScreen.uxml` — the controller looks up the slot by reading its own root element's name, so no manual string field has to be kept in sync.
- The Bench is deliberately **not** a UI Toolkit panel: it needs real `Hero` GameObjects standing on it (animated sprites, not flat icons), which `VisualElement` can't host. It's world-space instead, using the same `Hero`/Physics2D-drag approach as board placement (`Player/Dragging`: a hero or an item is dragged, or clicked once - press and
release within 10 px - to stay in hand until the next press puts it down; Esc puts it back where it came from - an item
to the hero that wore it, the hex it lay on, or its loose spot) (see `Assets/Scripts/Combat/Placements/Bench/`, `Assets/Prefabs/Bench/`). The `#BenchPanel` slot in `MainScreen.uxml` stays empty/unused for this reason. The Shop panel (buy heroes) is the UI Toolkit one. A hero slot is not dragged as a UI ghost: pressing it hands the player the hero itself, unbought (`IShop.TryPickUp` -> `Shop.OnHeroPickedUp` -> `GameManager.OnShopHeroPickedUp`, built by `HeroSpawner.SpawnUnplaced` - on no placement, so untracked, no aligning, no combine), and `Dragging` holds it like a hero off the board, so `AlignHighlight` shows its marks while it is placed. Putting it down buys it (`GameManager.BuyShopHero` -> `Shop.TryBuyPickedUp`): on the free placement under the pointer if it may stand there (mid-fight a bench slot only), else the first free bench slot; then copies combine. Dropping it back on the shop, or Esc, destroys it (`ReturnShopHero`) and nothing is paid; so does a buy that fails (re-rolled, no gold, bench full). An item slot is still a ghost: drag it outside the shop panel's bounds to buy, drop it back inside to cancel — see `ShopPanelController.ResolveDrop`. Selling is the same bounds read from the other direction: drop a held world-space `Hero` onto the shop and it's sold (`Dragging.DropHero` → `GameManager.Sell` → `Seller`). Because the two drags live in different coordinate spaces, and `Player` may not reference `UI`, the shop answers `ISellZone.ContainsScreenPoint` rather than handing out its rect — it owns the screen→panel conversion, y-flip included. A drop only sells when no `IPlacement` is under the pointer, since the bench band sits directly above the shop.

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
  modifiers/statuses through `Stat`/`ModifierResolver`. There is no magic resist: DF reduces every hit, auto attack or skill
  (`CombatMath`, damage / (1 + DF/100)). MR was removed; `StatEnum` 4 and `ModifierEnum` 16 are left empty so stored ints keep their meaning.
- Crit is pseudo-random (Dota/Warcraft 3's PRD), not a coin flip: `StatScaling/Crit` counts each attacker's hits since
  its last crit, auto attacks and crit-able skills together, and the n-th crits at `C * n`, with `C` solved from the crit
  chance so the long-run rate is still exactly the chance (25% -> C 8.47%, never more than ~11 hits without one). The count
  is keyed by the attacker (a weak table, so `Roll`'s signature did not change) and reset in `Hero.TriggerOnCombatStart`.
- Every hero in `Data/Heroes/Stats/Player/` has a skill script in `Skills/Library/Build/`.
- Aligning (the game's synergy, in place of traits): a hero marks some of the 6 hexes around itself and an
  ally standing on a marked hex gets a bonus. The code is in `Combat/Align/` (`MagicSchool.Combat.Align`) - a
  folder of `Combat`, not its own assembly: `HeroDataSO` holds the marks while `Aligning` needs `Hero`, `Hex` and
  `BattleBoard`, so a separate module would have to sit both under and over `Combat`. Marks are data, `HeroDataSO._alignPatterns`: a hero has one `AlignPattern` or more - each a full set of
  marks, authored by hand (a pattern need not be the one before it turned; it can mark other hexes, but its bonuses are the hero's - orange and green pay the same in every pattern, and the Align Editor copies an edit to every pattern and matches the rest to pattern 1).
  In Preparation, right-clicking while holding a player hero turns it to its next pattern, and does not inspect - only a
  held hero turns (`GameManager.RotateAlign` -> `Hero.RotateAlign`, then aligning is refreshed); a hero with one pattern cannot turn. Which
  pattern a hero is on lives on the `Hero`, so it is kept between stages, and team comps save it (`HeroPlacement.alignPattern`).
  Within a pattern, one `AlignMark`
  is one bonus and every hex that gives it - `hexes` (a list of `AlignHex`: direction, and `distance` - how
  many hexes away in a straight line, 1 = adjacent, heroes in between do not block it and a missing hex on
  the way ends it; a hex off the straight lines adds a turn, `thenDirection` / `thenDistance`, 0 = none, and
  the two legs are tried in either order so a board edge does not lose it), then modifier, amount, and `percentOf` - None for a flat amount, else a percent of the
  receiver's own base stat. `perOwnerAP` makes the ally amount per 100 of the mark owner's AP, read when aligning is worked out (Ettin's
  green: 25% of the ally's base AS per 100 AP; `AlignMark.AllyAmount`). A hero with two different bonuses has two marks. A mark can also pay its owner:
  `selfModifier` / `selfAmount` / `selfPercentOf` are given to the owner once for every one of the mark's
  hexes an ally stands on; `selfAmount` 0 means none, and an ally `amount` of 0 makes it a self-only mark.
  `selfWhenEmpty` flips the self bonus: once for every one of the mark's hexes that is in the deploy zone
  and that nobody stands on (an item lying there does not count as someone). `healFromAllyDamage` makes an
  ally on one of the mark's hexes a generator: the owner heals that percent of every damage it lands. It is
  wired in `AlignBonuses` to `Hero.OnDamageDealt` (raised on the dealer by whoever took the hit) and cut on
  the next `Refresh`, so like every bonus it is fixed by where everyone stood when the fight started.
  Shield Knight has it at 10% on his N/S mark.
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
  the first; its wearing hexes are purple, which wins over the bonus colour since a wearing hex is a bonus
  hex too (`AligningHex.MarkedHexesByBonus` / `WearingHexes`, `BoardColours.ShowAlignMarks`, colours in the Inspector).
  Holding nothing, hovering a placed player hero shows its marks the same way, from the hex it stands on (a bench hero
  or an enemy shows nothing). **Magic School 4 > Align Editor** (`Core/Editor/AlignEditorWindow`, a partial class over `.Patterns` / `.Hexes` /
  `.Marks` files, with the hex maths in `AlignHexOffset`) edits a
  hero's marks by clicking hexes around it instead of typing directions in the Inspector: heroes on the left,
  the hexes within 6 in the middle, scrolling when the window is smaller than the grid (every one can be marked - the window works out the turn), one box per `AlignMark` on the
  right, and a row of pattern tabs above the hexes (`+ Rotated copy` drafts a new pattern as the selected one
  turned 60 degrees clockwise, to edit from there; a hero with no pattern shows a Start button, so looking never writes). It works through `SerializedObject` on the `HeroDataSO`, outside Play mode, because `Editor` does not
  reference `Contracts` and so cannot name the modifier and stat enums. The inspector panel lists a hero's marks under ALIGNING (`IInspectableHero.AlignDescriptionFor(formula)`, written by `AlignText`, hidden when empty; with Alt held a bonus that
  scales with AP shows its formula, like skill text). The nine
  shop heroes all have marks, in three teams of three designed in the sheet's `Aligning` tab (Ettin / Dryad /
  Elf, Orc Blademaster / Werewolf / Shield Knight, Skeleton Archer / Reaper / Centaur); `_shopRoster` holds
  only those nine. The teams are being redesigned as three teams of five that share heroes (each carry -
  Elf, Werewolf, Skeleton Archer - in one team, every other hero in two). Only the Elf team is done, and
  the assets are ahead of the sheet: a spread fan where every mark is 2 hexes away - Dryad 2 N of Elf,
  Reaper 2 S, Ettin 2 NE, Orc 2 SE (Elf N/S, Ettin SW/NW, Orc NW/SW, Dryad SE/S, Reaper N/NE). It needs 5
  rows by 3 columns, which none of the Stage 01 deploy zones has (8-13 hexes on the Hexagon board), so it
  is played on `Enemy Formation/Wide Square-test` (the `Wide Square Layout`, zone = the left 8 columns). The other two teams no longer line up until their turn.
- **Mark colours are fixed** (this overrides the "first mark / second mark" wording above): a pattern's `marks` are
  `[0]` **orange** - pays only its self bonus (per ally on it, or per empty hex with `selfWhenEmpty`, or the
  `healFromAllyDamage` link) - `[1]` **green** - pays only its ally bonus - then the **white** special marks, which pay
  nobody (`AligningHex.ColourOf`; a third ordinary mark does nothing). One hex can be in several marks: Elf's and Shield
  Knight's hexes are orange and green both. The Align Editor shows exactly these boxes, each with only its own fields, paints
  a click into the selected box only, and draws a hex in several as rings; the board does the same in-game
  (`Hex.SetColors`: ring sprites made on demand, order 0 nudged toward the camera so they stay under heroes at order 1;
  purple outermost, then orange, green, white). A wearing hex in two marks is still worn once. The inspector writes one
  line per colour (`AlignText`: "Orange: for every ally here, this hero gets ...", "Green: an ally here gets ...").
- Special marks: a skill can name marks it reads (`SkillDefinition.SpecialMarks`, e.g. Rock Golem's `"Ward"`, Skeleton
  Archer's `"Long Shot"`). A special mark is an `AlignMark` with `special` set to that name: the Align Editor puts it in
  every pattern of the hero by itself, after orange and green (it asks `HeroDataSO.SpecialMarksOf`, which builds the skill),
  and will not remove it; its hexes are painted separately in each pattern. `AlignBonuses.Refresh` records who stands on
  each one and the skill reads them through `IAligned.AlliesOnSpecialMark(name)`. The board paints them white
  (`BoardColours._specialColor`). Rock Golem's wards are the allies on his Ward mark. Troll grows only from the kills of the
  allies on his Feed mark: a unit that dies records who landed the killing hit (`IFallen.KilledBy`, Contracts), and a
  unit a hero spawned mid-fight is a summon (`IFallen.IsSummon`, set by `HeroSpawner`), whose death never feeds him.
  Reaper's Reap mark: every 3 enemies she kills herself (summons excluded), each ally on it casts its skill once for free -
  `IFreeCaster.GrantFreeCast` (Contracts) queues the cast on the `Hero`, `HeroStateMachine` plays it when the hero is not
  casting or stunned and its mana is not full, and `HeroCast` then leaves the mana alone. Grants reset at fight start.
  Reaper also revives once a fight: her `OnDeath` calls `IRevivable.ReviveAfter(3s, 30%)` (Contracts) on herself; she is an
  ordinary death meanwhile (untargetable, death hooks fire, keeps her hex), `HeroDead.OnUpdate` counts down and
  `Hero.Revive` stands her up on her hex with that share of max HP (mana and modifiers kept). A hero that `IsReviving`
  still counts as standing in `CombatState` and `TeamCompBenchmark`, so a fight never ends while she is down.
- Items on hexes (aligning's second filler, because five heroes cannot fill many marks): in Preparation
  an item dropped on a free deploy-zone hex lies there (`Dragging.DropItem` -> `GameManager.PutItemOnHex`
  -> `BattleBoard.Items`, a `HexItems`), and it takes the hex - no hero can be placed on it; it does not
  block walking. A single marked hex is a wearing hex when its `AlignHex.isWearing` is ticked - the option is per hex,
  not per bonus, set by right-clicking the hex in the Align Editor: `AlignBonuses.Refresh` gives the mark's
  owner the modifier of an item lying on one, through the same give-and-take-back list as
  every other bonus. An item is not an ally: it gives no ally bonus and never triggers the self bonus,
  which is what keeps hero pairs mattering. Two heroes whose wearing hexes overlap both wear the item
  (left alone for now). Wearing by dropping an item on a hero still works beside it, but a hero wears only
  one item (`HeroItems.Capacity`); it gets more by sharing: an ally standing on its wearing hex lends it the
  item that ally wears (only its own, so sharing never chains). Wearing and taking off go through
  `GameManager.WearItem` / `TakeItemOffHero` so aligning is worked out again. While an item is held
  the board lights up the team's wearing hexes, in the same purple. Items keep their hexes between stages unless the next
  stage's board differs, then `PreparationState.ReturnItems` moves them to the loose row. `HexItems` only
  knows `IEquipment`, so `Combat` still does not reference `Items`. No hero has a wearing hex yet.
- Nearest enemy is counted in steps until the enemy is in range (`EnemyScan.StepsToReach`): next to it for a melee hero,
  within its range for a ranged one (0 when it already can shoot). It used to be steps to stand next to the enemy for
  everyone, which made an archer's ties rare and its picks arbitrary. Walking itself still heads for a hex next to the
  target (`HexPathfinder`) and stops as soon as it is in range. An enemy mid-step is measured, and walked toward, at the hex
  it is arriving on (`ArrivingHex`, next to `CurrentHex()`): a unit is only handed its new hex when the step ends, so counting
  from the hex it left - with the hex it is entering reserved, so a wall - once made Reaper walk 6 hexes past a Minotaur
  stepping up next to her.
  A hero is dead the moment its HP is 0 (`Hero.IsAlive`), not only from its next update, so a target killed this frame
  must not be gone for: `EnemyScan`'s per-frame caches drop whoever died since they were built (a new list, never edited in
  place), and `HeroAttack.CheckSwitchState` picks a new target when its own just died - before both, Skeleton Archer walked
  toward the slime his own arrow had killed while another stood in his range.
- Chosen target: when a player hero's nearest enemy is a tie (`FindTarget.TiedNearestEnemies` - the same tie
  `FindNearestEnemy` breaks, in-range enemies first), the player picks which one it goes for. In Preparation, right-clicking a
  placed hero inspects it; right-clicking it again while it is inspected steps through the tied enemies and back to automatic
  (`PlayerController.TrySwitchTarget` -> `Hero.SwitchChosenTarget`; `IInspectorPanel.Shown` says what is inspected). Holding
  Shift while hovering the hero (`PlayerInputSystem.TargetViewHeld`, `AlignHighlight.ShowTargetView`) hides its marks - they sit
  on the hexes the route starts on - and fills the enemy it will go for yellow (`Hero.ExpectedTarget`: the chosen one, or the only
  nearest; none while a tie is left automatic), gives tied enemies a yellow band (`BoardColours.ShowTargets`, `_targetColor`), and
  puts a dot on every hex it would walk through to get that enemy in range (`Hero.RouteToExpectedTarget`, the walk's own
  one-step A* repeated; `RouteDots`, small copies of the hex sprite) - so a detour round its allies reads as on the way. At fight start
  a pick is kept only if it is still tied (`KeepChosenTargetOnlyIfTied`); in the fight it is sticky - `FindNearestEnemy`
  returns it while it can be walked to, unless another enemy is already in range. The player does not choose the path.
  The same Shift view works in a fight as a debug tool, over any living hero of either team (`AlignHighlight.ShowFightTargetView`):
  the enemy it is going for right now filled yellow (`Hero.TargetInFight` - the one it is engaged with, else the one it last
  walked toward), its tied nearest with a yellow band, and the dots of the walk it plans (`Hero.RouteTo`), on the neutral board,
  repainted live as anyone moves.
- A stat needs a slot in `ModifierResolver.StatSlots` or no modifier can ever change it - the count was one
  short once and Omnivamp modifiers silently did nothing. Current mana follows `StartMana` when a modifier
  changes it after the stats are built (`Stat.FollowStartMana`), the way current HP follows max HP - both ways
  (`Stat.FollowMaxHP`): losing max HP takes as much current HP, never below 1, and what the floor saved is owed back
  first when the max HP returns - so a timed loss (Husk's steal) costs a hurt hero HP and nets nothing when it ends.
  A modifier built with `CustomModifier(..., keepsHealth: true)` is the exception when it expires: max HP goes back but
  current HP stays (capped at the new max). `StatSteal` builds both halves that way, so the HP Husk steals moves for good -
  he keeps it, and the enemy does not get it back when the steal ends; only their max HPs return.
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
  never walking. `BattleSetups/` holds only `Campaign/` (the stages played) and `Team Comps/`. Everything older - the 5
  `Enemy Formation/` stages played before enemy levels (and their README of each stage's puzzle), an older `Campaign/` of 10,
  the `<Shape>-test` stages and the old test setups - was deleted and lives in git history.
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
- Upgrading (TFT's star-up, `Combat/Heroes/HeroStar`, `Core/HeroUpgrader`): two copies of a hero at the same
  star combine into one a star higher, up to 3 stars (4 copies). Each star multiplies base HP by 1.8 and base AD
  and AP by 1.5 (TFT's HP/AD; AP stands in for TFT's per-star skill numbers); DF, AS, mana and range stay.
  `Stat` is built from `HeroDataSO` + star, so `Hero.Upgrade` rebuilds it, re-grants items and redraws the hero
  15% bigger per star; aligning is refreshed after. A combine starts when a player hero lands on the bench
  (`HeroSpawner.OnSpawned` -> `GameManager.CombineCopies`) and again at `PreparationState.OnEnter`; mid-fight
  only the benches combine. The copy on the board stays; the other's item moves over or drops loose. A hero
  sells for every copy in it (`Hero.Price`). Heroes spawned on hexes (seeds, team comps) never start a combine.
- Right-click a shop slot to inspect it (`ShopPanelController.MakeSlotsInspectable` → `IShop.InspectableAt`).
  A slot holds data, not a live object, so it shows a stand-in: `ItemPreview` (Economy) or `HeroPreview`
  (Combat - it builds the skill through `SkillLibrary` with no caster, which only builds the text). `Economy`
  cannot reference `Skills`, so `GameManager` hands the shop the `HeroPreview` factory. The preview's
  `IsAlive` is "the slot still sells this", so buying or re-rolling closes the inspector. `UIDrag` only
  drags on the left button, so a right-click on a slot inspects instead of picking it up.
- Balancing: every `CombatRecord` knows `FightSeconds` / `SecondsAlive` (a `RoundClock` per round, stopped in
  `ResultState`; a death is the damage event that takes HP to 0), so the `_test` CSV in `BalanceLogs/` has
  per-second rates. Every number below that the two benchmarks run with is a default: they are all in
  `Data/Benchmark/BenchmarkSettings.asset` (`Core/Benchmark/BenchmarkSettingsSO`, made with the defaults if missing), edited -
  with a tooltip each, a fight count and time estimate, and Run buttons, one tab per benchmark - in **Magic School 4 > Benchmark**
  (`Core/Editor/BenchmarkWindow`). The menus pass the asset to `Run`; the code reads it through `BenchmarkSettingsSO.Current`
  (the old constants are now properties of the same names). The window is the only way to run them - the old Balance menu
  was removed; its Run buttons call `HeroBenchmarkMenu` / `TeamCompBenchmarkMenu`. **The Hero Benchmark** (`Core/Benchmark/HeroBenchmark`) runs every shop hero alone vs `Data/Benchmark/BenchDummy`
  (2000 HP, DF 30, topped up below half): a 30s damage test (DPS at 15s and 30s), a 30s AoE test (a tight
  cluster of 6 dummies around one clear nearest target; `aoe_gain` = AoE DPS / single-target DPS) and a tank test (100 raw damage/s until
  death or 30s). It runs outside the game's states, 8 heroes at once, each on its own hidden copy of the
  `BattleBoard` (a hero only sees its own board's heroes, so lanes cannot touch each other), in fixed 1/30s
  steps (`Time.captureDeltaTime`) with vSync, the frame cap, cameras and UI off - about 50s for all 24 -
  and writes `BalanceLogs/benchmark-*.csv` with each hero vs its role's median, flagged beyond +-20%.
  The three tests run in up to three passes: the hero alone, then as in a smaller and a bigger team (default a team of 3 and of 5) - up to 2 and 4 of its own
  orange mark hexes filled (`HeroBenchmark.MarkFill`, pattern 1, in authored order; a stand-in ally on an ally-paid hex, an
  empty deploy-zone hex for a `selfWhenEmpty` one), aligning given (`AlignBonuses.Refresh`), then the stand-ins removed
  (`BattleBoard.Remove`, `AlignBonuses.ClearSpecialMarks`) before the dummies spawn, so the self bonuses stay and nobody else is
  in the fight. A pass only runs the heroes it changes. The bigger team's numbers are the main columns and drive the role comparison
  and flags; `*_small` and `*_alone` columns hold the others. Marks that need allies to act (heal links, special marks, green)
  are left to the team comp benchmark.
  Its **Run Selected** tests only the HeroDataSOs selected in the Project window (seconds per
  hero) and borrows every other hero's numbers from the newest full report for the peer comparison; it
  writes `benchmark-selected-*.csv`. Every report, full or selected, is also imported into the **MagicSchool4 Benchmark**
  Google Sheet (`magicschool4-benchmark` in `.claude/scripts/sheets_config.json`): `HeroBenchmarkMenu` runs
  `.claude/scripts/benchmark_import.py` once back in Edit mode, which appends the run to the `Runs` tab (each row stamped
  `run_time` and `kind`) and overwrites `Latest`. It needs `python` on PATH with `gspread`; a failed import only logs an error. Ally-support skills still read low (no allies in any test), and heroes
  of a role share stats, so the tank test only separates heroes whose skills defend them.
  **The Team Comp Benchmark** (`Core/Benchmark/TeamCompBenchmark`; its "Run Selected" takes
  the comps selected in the Project window) fights every team comp in `Data/BattleSetups/Team Comps/` against every stage in
  `GameManager._stages`, 20 runs with aligning and 10 without (`RunsAligned` / `RunsNotAligned`), a stage counting as won at 70%
  of them (`WinLine`, counted in wins: 14 of 20, 7 of 10; each team row carries its verdict and line, `won` / `win_line`, so the
  Sheet counts and colours by what the run decided - only reports from before those columns fall back to the scripts'
  `DEFAULT_WIN_LINE`; the Team Dashboard colours each rate from won flags in its hidden columns R:AA), until a side is dead or 90s
  (a timeout is not a win) - real fights
  as `CombatState` starts them (saved hexes, patterns, worn items, items on hexes, the stage's deploy zone), 32 at once on hidden
  board copies stacked 20 units apart (`HeroBenchmark.CopyBoard`; skill hits are physics triggers with no board check, so the gap
  is what keeps lanes apart - 0 of 14,679 hits crossed at 10 units, 32 lanes; the lane count is speed only; `BenchmarkSpeed` is the fast mode both share), each lane taking the next fight as soon
  as its own ends - its heroes start a round of their own in the recorder (`CombatRecorder.BeginRoundFor`) so the others' books
  are untouched. `BenchmarkSpeed` also sets `Hero.IsDrawn` false: heroes made then have no world-space bars and no floating
  skill names, which updated every frame unseen and were the largest cost. A comp saved on another board than a
  stage's is reported, not fought. It writes `BalanceLogs/teamcomp-*.csv`: per comp x stage x aligning a `team` row (win rate,
  seconds, heroes left, HP left, team DPS) and a `hero` row each (DPS, share of the team's damage, damage taken, survive rate,
  died at), and logs how many stages each comp wins aligned and not. It is imported into the same Sheet by the same script (it tells the reports apart by file name), into `Team Runs` (appended, stamped `run_time`) and `Team Latest` (overwritten); `TeamCompBenchmarkMenu` queues it through `HeroBenchmarkMenu.ImportWhenBackInEditMode`.
  The `Team Dashboard` tab reads `Team Runs` with live formulas (its dropdown lists in the hidden `Team Lists` tab), so an
  import needs no step of its own: pick a run, a run to compare with, and aligned or not; it shows every comp x stage win
  rate with stages won, then one picked comp by stage and by hero. `.claude/scripts/team_dashboard.py --rebuild` builds
  both tabs again after the dashboard itself changes (20 comps, 10 stages and 12 heroes fit in its fixed rows).
- Skill descriptions show numbers: a description writes an amount through `SkillDefinition.AD()` / `AP()` / `DF()` / `MaxHP()`
  (e.g. `{AP(DamageRatio)}`), never as `{X}% AP`. The inspector shows it as the number for that hero's current stats ("deals
  310"); holding **Alt** (`PlayerInputSystem.FormulaViewHeld`) shows the formula instead ("deals [200% AP]"). It goes through
  `SkillDefinition.DescribeActive/DescribePassive(stats, formula)` - `IInspectableHero.SkillDescriptionFor(formula)`; a shop
  preview uses its 1-star stats, a skill with no stats shows the formula. The inspector rewrites the text every frame (only
  when it changed), so buffs, items, aligning and enemy levels show at once. A percent that is a buff, not an amount (Harpy's
  "+X% AD every cast", attack speed buffs), stays written as a percent.
- Skill numbers are tunable live (dev tool): a skill script writes each number as
  `private static float DamageRatio => Tuned(200f);` (never `const` - a const cannot change at runtime), and a
  number worked out from others is a plain `=> A * B` property. The value in the code is the default;
  `Skills/SkillTuning` holds overrides keyed `"ElfSkill.DamageRatio"` (file name + member, from caller info).
  **Magic School 4 > Skill Tuner** (`Core/Editor/SkillTunerWindow`) lists every skill's numbers (found by reading
  each skill class's static float/int properties by reflection), edits them, and **Write to code** puts a value back
  into the script's `Tuned(...)`, replacing the default (in Play mode the recompile waits until Play ends). A skill
  reads its numbers when built, so `CombatState.RebuildTunedSkills` rebuilds, at every fight start, the skill of any
  hero on the board built before the last change (`Hero.IsSkillOutdated` / `RebuildSkill`); descriptions follow at
  once. The window keeps unwritten changes across Play mode but not across closing it. The name is `Tuned`, not
  `Tune`, because `SkillFactory.Tune(castTime: ...)` already exists.
- Skipping a stage (dev tool): **N**, in Preparation or mid-fight, wins the stage on the spot (`GameManager.SkipStage`).
  The enemies are taken off the board (`ClearTeam`, so no death skill fires - a Big Slime would split) and Blue is the
  winner, so the normal result screen, income, team-size growth and next stage follow. The team stays where it stood
  in Preparation (`PreparationState.SkipStage` remembers the formation as `CombatState.OnEnter` would); mid-fight it
  goes back to where the fight started, as after any win. A skipped round still goes into the balance log.
- Adding a hero while testing (dev tool): the Stage Editor window's **Player > Bench** tab lists every hero (enemy-only ones folded
  under it) with a **Bench** button that puts it on the player's bench for free - Play mode only, Stage Edit Mode not needed,
  through `Core/Editor/AddHeroMenu.Add` -> `GameManager.AddHeroToBench`. The Align Editor's "Add to bench" button does the
  same for the hero it is editing. (It was a menu item on the Project window's selection; that was removed.)
  **Player > Item** lists every item in `Data/Items/` (name and price) with a **Drop** button that puts it on the loose row for free,
  where a bought one lands (`AddHeroMenu.AddItem` -> `GameManager.AddItemToLooseRow` -> `ItemDrop.Spawn`; typed
  `ScriptableObject` because `Editor` does not reference `Items`). Play mode only.
- Stage editing (dev tool): **Magic School 4 > Stage Editor** (`Core/Editor/StageEditorWindow`; it is the only way in - the
  old Stage Editor submenu was removed) has a Stage Edit Mode toggle (`GameManager._stageEditMode`: in Play mode set live, for
  that session only; outside it, the scene's value), a status line saying why a button is greyed out, a search box, and two tabs.
  With the mode on, in Preparation, enemies drag between the hexes outside the deploy zone like your own heroes do inside it, and
  dropping one on the shop removes it (no gold).
  **Enemy tab** has three parts of its own (`WindowStyle.SubTabs`) - Stage (load), Add, Levels - with Edit deploy zone and the two
  saves pinned under all of them. In it, every hero in `Data/Heroes/Stats/Enemy/` (and, folded, every other one in `Stats/Player/`) with an Add button that
  puts it on the first free enemy hex, at the "Add at level" slider's level (`StageEditor.NewEnemyLevel`); under them every enemy on
  the board with a level slider each and a "Set all", which change it live (`StageEditor.SetLevel`) and are saved with the stage;
  under the save buttons, **Load a stage** lists every stage in `Campaign/` (enemy count and levels) with a Load button that goes to
  the stage (`StageEditor.LoadStage` -> `GameManager.JumpToStage`): the run's stage number is set to it and Preparation opens
  again (`GameStateMachine.Reenter`) as for a new stage - its board, enemies at their levels and deploy zone - so it is now the
  current stage and Save To Current Stage writes to it. The team is remembered where it stands first (`Formation.Remember`), so
  it stays put, or goes to the spare bench if the board changes; the shop is kept (`ConsumeKeepShop` skips the restock); the
  team-size limit does not change. The current stage is marked in the list. A stage not in `_stages` only has its enemies swapped
  in (`StageEditor.LoadEnemyTeam`, the old Load: its zone too with Edit deploy zone on, greyed out on another board), and the current
  stage stays. Loading saves nothing. **Save To Current Stage** / **Save As New Stage...** write the enemy team into the
  current stage's `BattlePlacementSO` or a new one - red entries only, blue ones are kept - with the deploy zone. Clicking an
  empty hex puts it in or out of the zone (blue = in, red = out; read in `Player/ZonePainting`, ticked after `Dragging` so a
  click that picked something up never paints), and a save with an empty zone is refused. Its **Edit deploy zone** option is
  `StageEditor.EditsDeployZone`: off, a click paints nothing (`TryToggleDeployHex` refuses) and Save To Current Stage keeps the
  stage's zone; Save As New Stage always writes the board's zone, since a stage with none leaves the player nowhere to stand.
  **Player tab** has three parts of its own, a smaller row of tabs (`WindowStyle.SubTabs`): Team Comp, Bench and Item (both
  above). **Team Comp:** every team comp in `Data/BattleSetups/Team Comps/` with a Load button, which swaps the blue heroes on the board
  for the comp's, items included (items already on hexes go to the loose row; no gold either way; greyed out on a comp saved on
  another board layout, since hex numbers only mean something on their own); **Save Player Team As...** writes the blue heroes
  on the board (not the bench) into a team comp - a `BattlePlacementSO` with blue entries only and the board layout it was placed
  on - with each hero's worn item (`HeroPlacement.item`) and the items lying on hexes (`_itemsOnHexes`); both are typed
  `ScriptableObject` because `Combat` cannot name `ItemDataSO`, and `Core` reads them back as items. Each hero's star is saved
  too (`HeroPlacement.star`, 0 reads as 1) and given back by `Hero.UpgradeTo` in `HeroSeed` and `LoadPlayerTeam`, so the game
  and the Team Comp Benchmark fight it upgraded.
  `Core/StageEditor` is the runtime half (`GameManager.StageEditor` is null while the mode is off, which is what `Dragging`
  checks); `Core/Editor/StageEditorMenu` - no menu items any more, just what the window's buttons call - writes the assets, and
  goes through `StageEditor` for anything team-specific because `Editor` does not reference `Contracts`.
- The reward card (`RewardPanelController`, an item pick after each win) is **unwired, not deleted**:
  `GameManager._useRewardPanel` is off, so `GameManager.Reward` is null and every use of it is
  null-safe - a win goes straight on. The panel's own `_showOnStart` is off too. Tick both to bring it back.
- **Not built yet:** Trait Panel and Hero Panel are still empty slots. There is no "Start Battle" button: combat is triggered by the space bar in
  `PlayerController.TryStartCombat`, which is temporary. In a fight the same key pauses and goes on (`GameManager.TogglePause`
  -> `GameSpeed.SetPaused`, `Time.timeScale` 0; a speed picked while paused is kept); `CombatState.OnExit` always unpauses.
- The run is 10 stages: `GameManager._stages` points at `Assets/Data/BattleSetups/Campaign/Stage 01`-`10`, all on the
  `Wide Square Layout` (the board every team comp is saved on). They are a first pass at raising the power level: the 5
  puzzle formations the game played before (the deleted `Enemy Formation/` stages, see their README in git history) twice over - stages 1-5 and again
  6-10, so 5 and 10 are the Minotaur King boss - with every enemy at level 2 x the stage number (2 on stage 1 up to 20 on
  stage 10). The formations are meant to be redesigned in the Stage Editor and the levels tuned with the Team Comp
  Benchmark: a stage should be won aligned and lost not aligned. Heroes of one role share the same stats whatever their
  tier, so difficulty comes from enemy count, skills and level.
- Enemy level (`Combat/Heroes/HeroLevel`): 1-30, linear - every level past 1 adds 10% of the base HP, AD, AP and DF
  (level 11 = 2x, 30 = 3.9x); attack speed, mana and range stay. It is per enemy in a stage, `HeroPlacement.level` (0, an
  entry saved before levels, reads as 1), given by `HeroSeed` as it seeds the stage (`Hero.SetLevel` rebuilds the stats),
  so the game and the Team Comp Benchmark both fight it. A summon takes its summoner's level (`HeroSpawner.SpawnSummon`).
  A hero above level 1 is named "(Lv n)". It multiplies with stars, though stage enemies are 1 star. Hero A (`SuperTank.asset`, 2000 HP, no skill) is enemy-only - it is not in
  the shop roster. The enemy roster is designed in the sheet's `Enemies` tab and built into
  `Assets/Data/Heroes/Stats/Enemy/` (prefabs in `Assets/Prefabs/Hero/Enemy/`). The tanks and marksmen
  are built, on their shop role's stats until stats are decided: Minotaur (single-target stun), Basilisk
  (+DF every cast), Big Slime (splits into 2 Small Slimes on death), Goblin Slinger (shoots the 2
  furthest enemies) and Kobold Skirmisher (3 shots at random enemies). Supports, fighters and the
  assassin are still only in the sheet. None is in a stage yet - add them with the Stage Editor window.
  The first boss is the Minotaur King (`Enemy/MinotaurKing`, skill 44, prefab drawn 1.5x and tinted red),
  deliberately easy: 4000 HP, 70 AD at 0.5 AS, and Ground Slam (150% AD + 1s stun to the heroes next to
  him). He is in `Campaign/Stage 05` and `Stage 10`.
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
- **The Magic School 4 windows share one look** (`Core/Editor/WindowStyle`): a grey intro line at the top, tabs, a toolbar
  search field, a fixed side-list width with one pickable-item style, tall main buttons (`Action`) and compact row buttons
  (`RowButton`), asset lists drawn row by row through `ListRow` (the button first, the name right beside it in a column as wide
  as the longest name, details, Select at the far end; every other row shaded and the row under the mouse lit), hints and status boxes. A new window, or a new control in an old one, draws through it instead of choosing
  its own sizes and styles.
- Moving script files between folders is safe as long as the `.meta` travels with the `.cs` — scene and prefab references are by GUID, so they survive both the move and a namespace change. Verify in Play mode anyway.
 — but respect explicit "Don't answer" notes on individual steps, those are self-notes rather than questions.
- VS Code debugging uses the `visualstudiotoolsforunity.vstuc` extension (the current official one, not the deprecated "Debugger for Unity"), with an "Attach to Unity" launch config already in `.vscode/launch.json`.
- GitHub remote: https://github.com/SwiftkeyX/Magic-School-4.git. Branches: `main` is stable versions only;
  `develop` is the default branch, where finished features are merged; `feature/...` branches start from `develop`
  and track their own copy on GitHub, never `develop`.
- **Never commit or push.** The user commits and pushes everything themselves - finish the work and leave it in the working tree for them to review. Never add a `Co-Authored-By: Claude` trailer or any other AI attribution to a commit message or PR description: the user does not want Claude showing up as a contributor on GitHub.
- **Don't stage either.** `git mv`/`git rm` write to the index, which makes work look half-committed; use plain `mv`/`rm` and leave everything in the working tree.
