# Dev tools

All the Magic School 4 windows share one look (`Core/Editor/WindowStyle`): a grey intro line, tabs, a toolbar search
field, a fixed side-list width, tall main buttons (`Action`) and compact row buttons (`RowButton`), asset lists drawn row
by row through `ListRow`, hints and status boxes. A new window or control draws through it.

Other windows: Align Editor ([aligning.md](aligning.md)), Skill Tuner ([skills.md](skills.md)), Benchmark
([benchmarks.md](benchmarks.md)).

## Skipping a stage

**N**, in Preparation or mid-fight, wins the stage on the spot (`GameManager.SkipStage`). The enemies are taken off the
board (`ClearTeam`, so no death skill fires) and Blue wins, so the normal result screen, income, team-size growth and
next stage follow. The team stays where it stood in Preparation (`PreparationState.SkipStage` remembers the formation as
`CombatState.OnEnter` would); mid-fight it goes back to where the fight started. A skipped round still goes into the
balance log.

## Stage Editor

**Magic School 4 > Stage Editor** (`Core/Editor/StageEditorWindow`). `DevTools/StageEditor` is the runtime half
(`GameManager.StageEditor`, an `IStageEditing`, is null while the mode is off, which is what `Dragging` checks;
`StageEditor.Current` is the concrete one the window uses); `Core/Editor/StageEditorMenu`
writes the assets.

- **Stage Edit Mode** toggle (`GameManager._stageEditMode`: in Play mode set live for that session; outside it, the
  scene's value), a status line saying why a button is greyed out, a search box.
- **Quick Save / Quick Load**: one slot, to pick up after restarting Play. Both teams on the board (the player's as a
  team comp saves them, the enemies with levels) and the deploy zone go to `Data/BattleSetups/QuickSave.asset` (never
  listed or benchmarked); the stage being played to EditorPrefs. Load goes to that stage (`JumpToStage`), then puts both
  teams back (`StageEditor.LoadQuickSave`). Neither asks first. The bench is not saved.
- With the mode on, in Preparation, enemies drag between the hexes outside the deploy zone, and dropping one on the shop
  removes it (no gold).

**Enemy tab** (sub-tabs Stage / Add / Levels, with Edit deploy zone and the two saves pinned under all of them):

- **Add**: every hero in `Data/Heroes/Stats/Enemy/` (and, folded, `Stats/Player/`) with an Add button that puts it on the
  first free enemy hex at the "Add at level" slider's level (`StageEditor.NewEnemyLevel`).
- **Levels**: every enemy on the board with a level slider and a "Set all", live (`StageEditor.SetLevel`), saved with the
  stage.
- **Stage**: every stage in `Campaign/` with a Load button (`StageEditor.LoadStage` -> `GameManager.JumpToStage`): the
  run's stage number is set to it and Preparation opens again (`GameStateMachine.Reenter`). The team is remembered where
  it stands first (`Formation.Remember`), the shop is kept (`ConsumeKeepShop`), the team-size limit does not change. A
  stage not in `_stages` only has its enemies swapped in (`StageEditor.LoadEnemyTeam`). Loading saves nothing.
- **Save To Current Stage / Save As New Stage...** write the enemy team into the stage's `BattlePlacementSO` (red entries
  only, blue ones kept) with the deploy zone. Clicking an empty hex toggles it in the zone (`Player/ZonePainting`, ticked
  after `Dragging`). **Edit deploy zone** (`StageEditor.EditsDeployZone`) off: a click paints nothing and Save To Current
  Stage keeps the stage's zone; Save As New Stage always writes the board's zone. A save with an empty zone is refused.

**Player tab** (sub-tabs Team Comp / Bench / Item):

- **Team Comp**: every comp in `Data/BattleSetups/Team Comps/` with a Load button, which swaps the blue heroes on the
  board for the comp's, items included (greyed out on a comp saved on another layout). **Save Player Team As...** writes
  the blue heroes on the board into a team comp with the layout, each hero's worn item (`HeroPlacement.item`), star
  (`HeroPlacement.star`, 0 reads as 1, given back by `Hero.UpgradeTo`) and pattern, and the items on hexes
  (`_itemsOnHexes`). Items are typed `ScriptableObject` because `Combat` cannot name `ItemDataSO`.
- **Bench**: every hero with a **Bench** button that puts it on the bench for free (Play mode;
  `Core/Editor/AddHeroMenu.Add` -> `GameManager.AddHeroToBench`).
- **Item**: every item in `Data/Items/` with a **Drop** button that puts it on the loose row for free
  (`AddHeroMenu.AddItem` -> `GameManager.AddItemToLooseRow` -> `ItemDrop.Spawn`).
