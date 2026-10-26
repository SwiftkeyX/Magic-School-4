# Campaign and enemies

## The run

10 stages: `GameManager._stages` points at `Assets/Data/BattleSetups/Campaign/Stage 01`-`10`, all on the
`Wide Square Layout` (the board every team comp is saved on). They are a first pass at raising the power level: the 5
puzzle formations played before (the deleted `Enemy Formation/` stages; their README is in git history) twice over,
stages 1-5 and again 6-10, so 5 and 10 are the Minotaur King boss, with every enemy at level 2 x the stage number (2 on
stage 1 up to 20 on stage 10). The formations are meant to be redesigned in the Stage Editor and the levels tuned with
the Team Comp Benchmark: a stage should be won aligned and lost not aligned. Heroes of one role share the same stats
whatever their tier, so difficulty comes from enemy count, skills and level ([enemy level](combat-and-stats.md#enemy-level)).

`BattleSetups/` holds only `Campaign/`, `Team Comps/` and `QuickSave.asset`. `BattlePlacementSO` has been reset to
defaults several times across past type changes: double-check its values before relying on them.

## Enemy roster

Designed in the design sheet's `Enemies` tab and built into `Assets/Data/Heroes/Stats/Enemy/` (prefabs in
`Assets/Prefabs/Hero/Enemy/`). The tanks and marksmen are built, on their shop role's stats until stats are decided:
Minotaur (single-target stun), Basilisk (+DF every cast), Big Slime (splits into 2 Small Slimes on death), Goblin
Slinger (shoots the 2 furthest enemies) and Kobold Skirmisher (3 shots at random enemies). Supports, fighters and the
assassin are only in the sheet. Hero A (`SuperTank.asset`, 2000 HP, no skill) is enemy-only.

The first boss is the Minotaur King (`Enemy/MinotaurKing`, skill 44, prefab drawn 1.5x and tinted red; his
`PolygonCollider2D` points are overridden to 2/3 so his hitbox stays a normal hero's), deliberately easy: 4000 HP,
70 AD at 0.5 AS, and Ground Slam (150% AD + 1s stun to the heroes next to him). He is in `Stage 05` and `Stage 10`.

## Hero prefabs

Every hero prefab is a variant of `Assets/Prefabs/Hero/BaseHero.prefab` (in `Roster/`, `Enemy/`, `Have Skill/`, plus
`Dummy` and `MovingDummy`) and overrides only its name, sprite colour and root position. `RegistryTests` checks it.
