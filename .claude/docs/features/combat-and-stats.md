# Combat and stats

## Hero

`Combat/Heroes/Hero.cs` is the only MonoBehaviour for a hero, a partial class split by job (`Hero.Board.cs`,
`Hero.Skill.cs`, `Hero.Targeting.cs`, ...). It delegates to plain C# helpers: `HeroStateMachine`, `FindTarget`,
`HeroItems`, `HeroSpawn`, `Stat`, `AttackCooldown`. It implements 7 interfaces; everything a skill may ask of it is `ISkillCaster` and is built
by `HeroSpawner` through `Hero.Init(data, board, team, skill)`. `Hero._SOData` is set at runtime by that call.

## Fighting

- Auto attack on an attack-speed cooldown.
- Mana comes mainly over time, 5 a second (`StatEnum.ManaPerSecond`), ticked by `Combat/Heroes/States/ManaRegen` from
  `HeroStateMachine.Tick`, stopped while stunned, casting, dead or mana-blocked. None per auto attack by default
  (`StatEnum.ManaPerAttack`, 0, kept as a stat for items, marks and skills to raise). Both bases are constants in `Stat`.
- HP over time is `StatEnum.HPPerSecond`, 0 by default, healed once a second by `States/HPRegen` through `Heal`, so a
  wound cuts it; it keeps going while stunned or casting.
- A skill casts when mana caps; the mana is spent in `HeroCast.OnEnter`.
- Damage and healing go through `CombatMath`; modifiers and statuses through `Stat` / `ModifierResolver`.
- There is no magic resist: DF reduces every hit, auto attack or skill: damage / (1 + DF/100). MR was removed;
  `StatEnum` 4 and `ModifierEnum` 16 are left empty so stored ints keep their meaning.
- Armour penetration is LoL's: the attacker's `StatEnum.ArmorPen` (% of the target's DF ignored, sources add, capped at
  100), then `Lethality` (flat), worked out in `Hero.TakeDamage` from `source` (`CombatMath.DFAfterPenetration`). It
  never takes DF below 0 and leaves DF already below 0 (shredded) alone. Items: Serrated Fang (+15 lethality),
  Piercing Bolt (25%), Giant's Bane (40%).
- Crit is pseudo-random (Dota/Warcraft 3 PRD), not a coin flip: `StatScaling/Crit` counts each attacker's hits since its
  last crit, auto attacks and crit-able skills together, and the n-th crits at `C * n`, with `C` solved from the chance
  so the long-run rate is exactly the chance (25% -> C 8.47%, never more than ~11 hits without one). The count is keyed
  by the attacker (a weak table) and reset in `Hero.TriggerOnCombatStart`.

## Stats and modifiers

- `ModifierResolver.StatSlots` is worked out from `StatEnum`, so every stat can be modified. It used to be hand-kept and
  was once one short, so Omnivamp modifiers silently did nothing. `RegistryTests.EveryStatCanBeModified` guards it.
- Current HP follows max HP both ways (`Stat.FollowMaxHP`): losing max HP takes as much current HP, never below 1, and
  what the floor saved is owed back first when the max HP returns. So a timed loss (Husk's steal) costs a hurt hero HP
  and nets nothing when it ends. Current mana follows `StartMana` the same way (`Stat.FollowStartMana`).
- `CustomModifier(..., keepsHealth: true)` is the exception when it expires: max HP goes back but current HP stays
  (capped at the new max). `StatSteal` builds both halves that way, so the HP Husk steals moves for good.

## Stars (upgrading)

TFT's star-up (`Combat/Heroes/HeroStar`, `Core/HeroUpgrader`): two copies of a hero at the same star combine into one a
star higher, up to 3 stars (4 copies). Each star multiplies base HP by 1.8 and base AD and AP by 1.5; DF, AS, mana and
range stay. `Stat` is built from `HeroDataSO` + star, so `Hero.Upgrade` rebuilds it, re-grants items and draws the hero
15% bigger per star; aligning is refreshed after.

A combine starts when a player hero lands on the bench (`HeroSpawner.OnSpawned` -> `PlayerActions.CombineCopies`) and
again at `PreparationState.OnEnter`; mid-fight only the benches combine. The copy on the board stays; the other's item
moves over or drops loose. A hero sells for every copy in it (`Hero.Price`). Heroes spawned on hexes (seeds, team comps)
never start a combine.

## Enemy level

`Combat/Heroes/HeroLevel`: 1-30, linear. Every level past 1 adds 10% of base HP, AD, AP and DF (level 11 = 2x, 30 = 3.9x);
attack speed, mana and range stay. It is per enemy in a stage, `HeroPlacement.level` (0, an entry saved before levels,
reads as 1), given by `HeroSeed` as it seeds the stage (`Hero.SetLevel` rebuilds the stats), so the game and the Team
Comp Benchmark both fight it. A summon takes its summoner's level (`HeroSpawner.SpawnSummon`). A hero above level 1 is
named "(Lv n)". It multiplies with stars, though stage enemies are 1 star.
