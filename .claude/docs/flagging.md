# FLAGGING notes

The owner's `// FLAGGING:` comments in code, collected 2026-10-26 so they survive the comment purge (refactor plan,
phase 3). Paths are under `Assets/Scripts/`; line numbers are from that date. A note the refactor plan already handles
says which phase.

## FIXLATER notes

- **Burn damage counts as skill damage; it should have its own section in the record** (`CombatRecording/CombatRecord.cs:40`).
- **An item cannot grant a timed buff yet, e.g. "on kill, +50% damage amp for 3s"** (`Items/Library/ItemFactory.cs:11`).
- **Nothing stops `ModifierEnum.ManaBlocked` from being used as a Buff or Debuff** (`Skills/Library/Build/DireWolfSkill.cs:50`).
- **Turn every hero into a `SkillDefinition` and move `Build` inside it** (`Skills/Library/Build/WerewolfSkill.cs:25`).

## Design doubts

- **`ICombatant` is a god interface** (`Contracts/Interface/ICombatant.cs:8`). Phase 7.
- **`IHeroStats` named properties are redundant now that `GetStat()` exists** (`Contracts/Interface/IHeroStats.cs:9`). Phase 7.
- **`IPlaceable.IsInCombat` does not belong there** (`Contracts/Interface/IPlaceable.cs:13`). Phase 7.
- **`IShop.OnChanged` lumps "stock changed" and "gold changed" together** (`Contracts/Interface/IShop.cs:12`).
- **`ScoreRow.IsAlive` sits in the wrong place** (`Contracts/Struct/ScoreRow.cs:9`).
- **`AlignMark` modifiers and hexes are Inspector data; the owner would prefer code** (`Combat/Align/AlignMark.cs:11`). Kept for speed.
- **A dummy should be its own unit type, not a `Hero` with `_isDummy`** (`Combat/Heroes/Data/HeroDataSO.cs:47`; also
  `Hero.cs:44`, `States/HeroIdle.cs:18`, `MovingDummy.cs:6` "temp").
- **`PlayerController` references many modules directly instead of through Contracts**, and finds objects with
  `FindObjectsByType` (`Player/PlayerController.cs:11`, `:35`). Review P12.
- **Status and stat modifiers share one interface; a status does not need a scaling source, and a status could carry an
  amount (Wound 50%, Burn 3%) that the skill builder cannot set** (`Modifiers/StatusModifier.cs:17`, `:22`).
- **`HealSkillEffect` could not be merged into `ModifierSkillEffect`**; left as is (`Skills/SkillEffectGroup/HealSkillEffect.cs:8`).
- **`Hex`'s `IHexPlaceable` special case smells, but going further would be over-engineering** (`Combat/Placements/Hex/Hex.cs:53`).
- **Two ways to measure hex distance: `Vector3.Distance` and BFS hop count. Should be one** (`Combat/Placements/Hex/Hex.cs:131`).
- **`FindTarget.BreakTie` should be reconsidered: `FindClustered` no longer follows the same logic** (`Combat/Heroes/Data/FindTarget.cs:333`).
- **What is `_stepsIfBoardWereEmpty` for in `EnemyScan.StepsToReach`?** (`Combat/Heroes/Data/EnemyScan.cs:128`).
- **`CombatState.OnEnter`: two steps could be combined** (`Core/States/CombatState.cs:19`). Phase 6.

## Behaviour bugs or gaps

- **An attack's animation is cut by a skill cast** (`Combat/Heroes/States/HeroAttack.cs:102`).
- **A new stun should add to a running stun's duration** (`Combat/Heroes/States/HeroStateMachine.cs:98`).
- **Stunning a caster mid-cast does nothing, because the skill already fired** (`Combat/Heroes/States/HeroStateMachine.cs:110`).
- **`ZoneAOE`: `cadenceDuration` overrides a tuned `_duration`; may be a problem** (`Skills/SkillActionGroup/TemplateActionChild/ZoneAOE.cs:64`).
- **`Move` only works with `ActionSourceEnum.Self`** (`Skills/SkillActionGroup/TemplateActionChild/Move.cs:68`).
- **`Companion` is not stable yet; more companion types would help** (`Skills/SkillActionGroup/TemplateActionChild/Companion.cs:11`).
- **The poison DOT is a coroutine on the AOE; it should be its own status** (`Skills/SkillActionGroup/TemplateActionChild/ContactAOE.cs:63`).

## Temporary or prototype

- **The team used to go to the spare bench after every fight; the old call is left commented out** (`Core/States/PreparationState.cs:109`).
- **Team-size growth per stage is temporary, for the demo** (`Core/States/ResultState.cs:79`).
- **Item name label is for the prototype** (`Items/Item.cs:11`).
- **`RewardPanelController._showOnStart` is for fast debugging** (`UI/Panel/RewardPanelController.cs:72`).
- **Scoreboard chart column heads are unused but worth keeping** (`UI/Panel/ScoreboardController.cs:137`, `:143`).
- **`RandomTargetPool.Fill` is hard-coded for Imp** (`Skills/SkillActionGroup/Helper/RandomTargetPool.cs:47`);
  `FireTimingRunnerProjectileTuning` has one user, Imp (`Skills/SkillActionGroup/Tuning.cs:57`).
- **Harpy's melee stance is disabled**: it read the column counted from the front line, which the board no longer has.
  Its code and passive text were commented out in `Skills/Library/Build/HarpySkill.cs` until commit `d49a785`; the
  comment purge removed them, so get them back from git history.
- **`PiercingProjectile`: a branch that never runs; maybe remove** (`Skills/SkillActionGroup/TemplateActionChild/PiercingProjectile.cs:46`).

## Numbers the design sheet does not give

- **Imp's buff length: rest of fight, like Harpy** (`Skills/Library/Build/ImpSkill.cs:19`).
- **Templar's shield length: 4s, borrowed from Rock Golem** (`Skills/Library/Build/TemplarSkill.cs:17`).
- **Werewolf's `AttackFromAS`: the sheet says 80% of bonus AS, converted to AD** (`Skills/Library/Build/WerewolfSkill.cs:20`).

## Code tidiness

- **`SkillCondition` comments are unreadable** (`Skills/SkillConditionGroup/SkillCondition.cs:7`). Phase 3.
- **`Projectile.FaceAimTarget` duplicates the AOE's** (`Skills/SkillActionGroup/TemplateActionChild/Projectile.cs:211`).
- **`Cast.PerHeroCadenceTick` should move out; not every template action needs it** (`Skills/SkillActionGroup/TemplateActionChild/Cast.cs:141`).
- **`ZoneAOE` resolves its cadence on every play instead of once** (`Skills/SkillActionGroup/TemplateActionChild/ZoneAOE.cs:23`).
- **`HeroWalk.ResumeWalk` duplicates `OnEnter`, kept apart for future animation** (`Combat/Heroes/States/HeroWalk.cs:68`).
- **`InspectorController`'s helpers are generic enough to live elsewhere** (`UI/Panel/InspectorController.cs:185`).

## Performance (fine at this size: about 56 hexes, 20 heroes)

- `AlignBonuses.Refresh` takes back and gives every bonus each time (`Combat/Align/AlignBonuses.cs:22`).
- `BattleBoard.ReleaseReservationsOf` is O(n) (`Combat/Placements/Board/BattleBoard.cs:186`).
- `HexFinder` BFS is O(n) (`Combat/Placements/Hex/HexFinder.cs:25`).
- `ModifierResolver` loops every modifier; a dictionary would do (`Modifiers/ModifierResolver.cs:58`, `:81`).
