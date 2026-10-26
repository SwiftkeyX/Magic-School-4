# Board, movement and targeting

## Hexes and the board

- **`Combat/Placements/Hex/Hex.cs`**: one per tile. It works out its own neighbours by distance, not axial maths.
  The grid is flat-top: same-column neighbours are about 1.0 apart and diagonal ones about 1.118, so a 1.15x
  threshold on the nearest distance catches exactly the real neighbours, including at board edges with fewer than 6.
  It needs `Init(board, hexNumber)` from `BattleBoard` before use: never call `GetNeighbors()` first. A hex does
  **not** track its own occupant; the board does. `Hex.GetNeighbor(direction)` finds a neighbour by angle
  (`HexDirectionEnum`: N NE SE S SW NW; the player faces right, so SW/NW are behind).
- **`Combat/Placements/Hex/HexNumber.cs`**: `struct HexNumber { int column; int row; }`, column 0 at the far left,
  row 0 at the top of each column. How many columns there are depends on the layout. A hex's number carries no team:
  a unit's side is on its `HeroPlacement.team`, and a hex is only in the player's deploy zone or not. **It must stay a
  `struct`**: value equality is what makes the dictionary lookups work, and a `class` would silently turn them into
  reference equality.
- **`Combat/Placements/Board/BattleBoard.cs`**: finds its child `Hex`es in `Awake()` and keys them in
  `Dictionary<HexNumber, Hex>` worked out from position, so it survives layout changes. It owns hero tracking
  (`HeroesOnBoard`) and hex reservations (`_reservedBy`), and carries `IsBattleOn`, pushed down by `GameManager` so a
  hero never reaches for that singleton. `BattleBoard.Colours` (`BoardColours`) makes every "what colour is this hex"
  decision.

## Board layouts

The board owns no hexes. A layout is a prefab holding the hexes of one board shape, in `Assets/Prefabs/Board/`, built
from `Board/Hex.prefab`:

| Layout | Hexes |
|---|---|
| `Square Layout` | 8 columns of 7 |
| `Hexagon Layout` | 37, columns of 4 5 6 7 6 5 4 |
| `Hourglass Layout` | 47, columns of 7 6 5 4 3 4 5 6 7 |
| `Ring Layout` | 30, the hexagon minus its centre 7 |
| `Diamond Layout` | 47, columns of 2 3 4 5 6 7 6 5 4 3 2 |
| `Wide Square Layout` | 12 columns of 5, 6 a side; the left 6 are the player's whole deploy zone. Its first column sits at x -3.5, not centred, to clear the spare bench on the left. |

Every stage names the layout it is fought on in `BattlePlacementSO._boardLayout`; all 10 campaign stages and every team
comp use `Wide Square Layout`. `PreparationState` empties the board (enemies wiped, the player's team to the spare
bench), then `BattleBoard.SetLayout` destroys the old hexes and instantiates the stage's. This is skipped when the layout
is the same as the last stage's. A stage's hex numbers (enemy placements, deploy zone) only mean something on its own
layout. The layout instance inside `BattleBoard.prefab` is only there to be seen in the Editor.

## Deploy zone

`BattlePlacementSO._deployZone` lists the hexes the player may put a hero on; an empty list means nowhere
(`PreparationState` logs an error). `PreparationState` pushes it into `DeployZone.Set` (`BattleBoard.DeployZone`),
which sets `Hex.IsInDeployZone`. `Dragging.ValidatePlacement` lets a player hero onto a hex inside it, and an enemy
(Stage Editor) onto a hex outside it. A hex has no colour of its own: in Preparation the board paints the zone blue and
every other hex red, and otherwise the whole board neutral (`BoardColours.ShowDeployZone`). The zone limits placing,
never walking.

## Between stages

The player's team is revived and put back on the hexes it started the fight on (`PreparationState.ReturnTeam` ->
`HeroFormation.ReturnToStartingPlacement`), so a formation is kept from fight to fight. A kept hex can lie outside the
next stage's deploy zone.

Only when the next stage is fought on a different layout (its hexes are about to be destroyed) does the team go to the
spare bench instead: snapshot in `CombatState.OnEnter`, `ReturnToSpareBench` in `PreparationState.OnEnter`, and the
player places it again. Without this a hero would stay where the fight left it, dead ones included (`HeroDead` never
releases its hex), often on a hex the player could never have placed it on. The spare bench (`SpareBench` in the scene,
`GameManager._spareBench`, 12 slots left of the board) is a second `Bench` whose slots have `BenchSlot._acceptsDrop`
off: the player takes heroes off it but `Dragging` refuses a drop onto it. If it is full the hero goes to the normal
bench, then back to the hex it started on.

## Pathfinding and walking

- **`Combat/Placements/Hex/HexPathfinder.cs`** is **A\***, not greedy and not BFS. It returns ONE step toward a hex next
  to the target. A\* so that equally short routes tie-break toward the target instead of by discovery order. The step
  can look like backward progress when routing round a blocked hex; whether to take it or wait is the caller's call.
- **`Combat/Heroes/States/`** holds the behaviour: `HeroStateMachine` over Idle / Walk / Attack / Cast / Stunned / Dead,
  with `Transition` holding the conditions.
- Walking is `Vector3.Lerp` plus an `AnimationCurve` over `1 / moveSpeed` seconds per hex, not `CharacterController`
  (the board is 2D sprites with no colliders; that path throws `MissingComponentException`).
- A hero reserves its destination hex the moment it *commits* to a step, not on arrival. Reserving on arrival would let
  two heroes target the same free hex in one frame.
- A step is `HexStep`, shared by `HeroWalk` and `HeroCast`. A skill part tuned `walkWhileCasting: true` keeps its caster
  in Cast for the whole cast time (no attacking) but walking toward its target as Walk would, standing still once in
  range (Orc Blademaster's storm).
- A full mana bar waits for a step under way to land before casting (`HeroWalk.IsMidStep`). A stun or death halfway
  through a step puts the hero back on the hex it left and lets go of the one it was stepping onto (`HexStep.CutOff`,
  from `OnExit` of Walk and Cast). A booking left behind never gets used, and `Transition.CanWalk` makes an enemy next
  to a hex booked by its foe wait for it, which once froze two Minotaurs beside a casting Skeleton Archer for the rest
  of the fight.
- Cast also holds while a skill's `Move` (a jump or dash) is still moving its caster (`Move.IsMoving` ->
  `SkillDefinition.IsMovingCaster` -> `Hero.IsSkillMovingMe`). Knight's leap outlasts his cast time, and leaving Cast
  mid-air started a step whose motion, still running after he landed, carried him back to where he jumped from.

## Targeting

- **Nearest enemy** is counted in steps until the enemy is in range (`EnemyScan.StepsToReach`): next to it for a melee
  hero, within range for a ranged one (0 when it can already shoot). Walking still heads for a hex next to the target
  (`HexPathfinder`) and stops as soon as it is in range. An enemy mid-step is measured, and walked toward, at the hex it
  is arriving on (`ArrivingHex`, beside `CurrentHex()`): a unit only gets its new hex when the step ends, and counting
  from the hex it left (with the one it is entering reserved, so a wall) once made Reaper walk 6 hexes past a Minotaur
  stepping up next to her.
- A hero is dead the moment its HP is 0 (`Hero.IsAlive`), not from its next update. `EnemyScan`'s per-frame caches drop
  whoever died since they were built (a new list, never edited in place), and `HeroAttack.CheckSwitchState` picks a new
  target when its own just died.
- **Furthest enemy** (`FindTarget.FindFurthestEnemies`, `AimTargetEnum.Furthest`: Skeleton Archer, Myconid, Goblin
  Slinger) is counted in hexes (a BFS from the hero's hex), and enemies equally far are picked at random. In world
  distance a diagonal hex (~1.118) always beat a straight one (1.0), so the same one of two always won.
- **Chosen target**: when a player hero's nearest enemy is a tie (`FindTarget.TiedNearestEnemies`, in-range enemies
  first), the player picks. In Preparation, right-clicking a placed hero inspects it; right-clicking it again while it
  is inspected steps through the tied enemies and back to automatic (`PlayerController.TrySwitchTarget` ->
  `Hero.SwitchChosenTarget`; `IInspectorPanel.Shown` says what is inspected). At fight start a pick is kept only if it
  is still tied (`KeepChosenTargetOnlyIfTied`); in the fight it is sticky: `FindNearestEnemy` returns it while it can be
  walked to, unless another enemy is already in range. The player does not choose the path.
- **Shift view** (`PlayerInputSystem.TargetViewHeld`, `AlignHighlight.ShowTargetView`): hovering a hero hides its marks
  and fills the enemy it will go for yellow (`Hero.ExpectedTarget`: the chosen one, or the only nearest; none while a tie
  is automatic), gives tied enemies a yellow band (`BoardColours.ShowTargets`, `_targetColor`), and puts a dot on every
  hex it would walk through (`Hero.RouteToExpectedTarget`, the walk's own one-step A\* repeated; `RouteDots`). In a fight
  it is a debug view over any living hero of either team (`AlignHighlight.ShowFightTargetView`, `Hero.TargetInFight`,
  `Hero.RouteTo`), repainted live.
