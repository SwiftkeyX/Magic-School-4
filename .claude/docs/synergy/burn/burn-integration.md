# Burn chain - integration

Everything about putting `burn-chain.md` into the game: what is known about the game around it,
and the code that exists. Read `burn-chain.md` first.

## What the game gives the chain

| Stage | Your heroes | Enemies | Enemy health in total |
|---|---|---|---|
| 1 | 4 | 3 | 1,800 |
| 5 | 8 | 7 | 5,750 |
| 10 | 13 | 12 | 10,000 |

- A hero cannot deal more than the enemy team has. Overkill is not counted as damage.
- Target: with the chain complete, Imp deals about a quarter of the team's damage on stage 10 and about
  twice the next hero. Without it he deals about a tenth.
- Mana only comes from a hero's own attacks (10 each). Dragon's first cast is at 8.6s, War Cleric's at 10s.

## Not known yet

- **How long a late fight lasts.** The balance logs only hold stage 1 fights (about 11 seconds). The
  stack numbers depend on it: a blast every 10-15 seconds is too slow for a 15-second fight.
  To measure: tick `_test` on `GameManager`, play a stage 8-10 fight, read `BalanceLogs`.
- **Imp's share of the damage** with burn stacks. Not estimated, for the same reason.

## The code that exists

An earlier version of the chain was built and tested in Play mode on 2026-10-06, then set aside so
design could go on. It is in the git stash named
`Burn chain: status + Imp passive + Ember Brand + Manaflow Band`, made on commit 7ddddfc.

What that version does (it is NOT the design in `burn-chain.md`):

- Burn is on or off, one per enemy, 5s, damage every second.
- Imp gains +25% attack speed per burning enemy, up to 5.
- Dragon and War Cleric burn for 30% AP per second. Ember Brand burns for 10% AP per second.
- Manaflow Band gives +5 mana per attack. Both items are in the shop list.
- A burning hero is drawn orange.

It was dropped because it was too easy: War Cleric + Imp was already most of the build.

What carries over to burn stacks: `Ignite`, the tick, the hook that tells Imp, Ember Brand's stat,
Manaflow Band, the orange tint. What changes: `Burn.cs` (a count instead of on/off), the three skills'
numbers, and Imp's passive, which gains the blast.

One side effect to watch when it comes back: the stash raises the stat slot count in
`ModifierResolver`. From reading the code, the old count left the Omnivamp stat out, so Lich, Swordsman
and Werewolf probably start healing from it. Not tested.

## When the stash is restored

`CLAUDE.md` is not in the stash, because it held other uncommitted changes. Put these back by hand:

- the mana line reads `StatEnum.ManaPerAttack` instead of `HeroAttack.ManaPerAttack`
- an entry on how burn works, written for whatever version is built then

## Left out of the chain on purpose

- **War Cleric and Regen:** he consumes up to 3 Regen per cast, each adding more stacks, and still works
  with none. It waits for a healing chain to make Regen.
- **Hero upgrades** (buying the same hero again) as a gate. A system for the whole game.
- **Items on hexes.** The chain is designed as if an item belongs to a hero.
