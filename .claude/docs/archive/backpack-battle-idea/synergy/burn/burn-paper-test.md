# Paper test: the Burn build

One build written out completely, to see if items-on-hexes works before designing the other synergies.
Hero and item numbers are the real ones from the project. Everything marked (assumed) is a rule I had to
invent to finish the page - those are the decisions to make.

## Rules used (assumed)

1. Alignment is read once, at combat start, from the Preparation layout. Heroes then walk away and keep the bonus.
2. An item takes a hex in Preparation (no hero can stand there) but nothing in combat sees it - it does not block walking.
3. A hero's star = all 6 hexes around him. Every item in his star gives him its stats.
4. One item can feed every hero that touches it.

## The team (5 heroes)

| Hero       | Role     | Job in the build | What he does today                                                              |
| ---------- | -------- | ---------------- | ------------------------------------------------------------------------------- |
| Imp        | Marksman | payoff / carry   | +2% base AS for the rest of the fight every time an enemy takes a burn tick     |
| Dragon     | Mage     | burn 1           | cone: 300% AP, then burn 150% AP over 3s (3 ticks per enemy)                    |
| War Cleric | Tank     | burn 2 + heal    | 3s zone, 2 hexes: enemies burn 60% AP per second, allies heal 50% AP per second |
| Archangel  | Mage     | support          | allies within 3 hexes: -30% damage taken, +20% AS, 4s                           |
| Knight     | Tank     | second tank      | not part of the synergy - any tank works here                                   |

## The layout (10 hexes: 5 heroes, 5 items)

[ ] = hero, ( ) = item. Columns C and A sit half a row lower, the way hex columns do.

```
        back                              front -> enemy
        D           C           B           A
 0                           (Crest)
                 (Tome)                  [Knight]
 1    [Imp]                  [Dragon]
                 (Ember)                 [War Cleric]
 2    (Cord)                 (Chalice)
                 [Archangel]
```

| Item                              | Gives                                   | Feeds                         |
| --------------------------------- | --------------------------------------- | ----------------------------- |
| Chalice of Dawn                   | +30 start mana                          | Dragon, War Cleric, Archangel |
| Archmage Tome                     | +55 AP                                  | Imp, Dragon                   |
| Hunter's Cord                     | +0.3 AS                                 | Imp, Archangel                |
| Bulwark Crest                     | +15 DF, +150 HP                         | Knight, Dragon                |
| Ember Brand (NEW, does not exist) | the hero's auto attacks burn the target | Imp, Dragon, Archangel        |

## The numbers (30s fight, each area skill catches 3 enemies - assumed)

Mana only comes from a hero's own attacks (10 per attack), so the first cast is slow.

|              | Dragon casts at    | War Cleric casts at | Burn ticks | Imp AS at the end | Imp AS on average |
| ------------ | ------------------ | ------------------- | ---------- | ----------------- | ----------------- |
| No items     | 8.6s, 20s          | 10s, 25s            | 36         | +72%              | about +30%        |
| With Chalice | 4.3s, 15.7s, 27.1s | 5s, 20s             | 45         | +90%              | about +42%        |

Ember Brand is left out of the table on purpose - see finding 3.

## What the page shows

1. **The mechanic works, and its best use is not stats.** The Chalice is the best item here: one hex, three
   heroes, and it moves the first burn from 8.6s to 4.3s. An item that speeds up the synergy is more interesting
   than an item that adds AP.
2. **Rule 3 is too generous.** 5 items give 12 bonuses (Dragon alone gets 4; a worn hero holds 3 today).
   With all 6 neighbours counting, the answer is always the same: items in the middle, heroes in a ring.
   There is no puzzle. The star needs a shape - e.g. each hero has 2-3 star hexes in fixed directions,
   different per role - so two heroes cannot always share.
3. **Burn is not a status in the code, it is a damage timer - and that breaks an "autos burn" item.**
   Every burn runs on its own and they all tick together. Imp with Ember Brand starts a new 3-tick burn on
   every attack: 2.25 ticks per second at the start, each tick makes him attack faster, which makes more
   burns. It is a loop with no top. The synergy needs burn as a real status first: one burn per enemy,
   re-applying refreshes it. Then the payoff reads cleanly: "+X% AS per second for every burning enemy".
4. **Without that item, Burn is too slow.** Only two heroes burn, both only through their skill. Nothing
   burns for the first 4-9 seconds, and Imp's bonus is mostly there after the fight is decided. The synergy
   needs a steady source: the item above (after finding 3), or a third hero who burns on auto attack.
5. **Imp's stack has no cap** and lasts the whole fight. Fine at 36 ticks, not fine at 100.
6. **Burn has no defence of its own.** War Cleric heals 150% AP per cast, only near himself. Idea: a burning
   enemy deals less damage - then "who is burning" is the offence and the defence at once, like Omnivamp.
7. **The board is the real limit.** A side has about 28 hexes (4 columns x 7 rows). The hero limit is 4 on
   stage 1 and +1 per win, so 13 on stage 10. 13 heroes leave 15 hexes for items - about one item per hero,
   and the back columns fill up first. This build is only 5 heroes; a real stage-10 team is two cores plus
   flex. So either the late-game limit comes down (8-9?), or items do not take hero hexes.

## Decisions before the next synergy

- Star shape: all 6 neighbours, or a fixed pattern per hero / per role? (finding 2)
- Does burn become a status - one per enemy, refresh? (finding 3)
- Late-game team size vs room for items. (finding 7)
- Is a synergy's core 3 heroes (payoff + 2 enablers) with the tank shared? This build says yes.

## My review

1. A solid rule I agree with:

- The item should give more than just stat. Best item should play to its synergy.

2. Aliging mechanic is a puzzle but it can wait:

- To make it interesting, we need to make it different per hero. But this mechanic isn't the most important, it can wait.

3. The burn mechanic have a problem that the burn generation isn't enough.

- Let add a item that give more mana to the hero (e.g. TFT's shojin, bluebuff)
- Let add a item that also apply burn to the enemy too

4. Burn have no defensive option which is fine.

A lot of this synergy doesn't have a real defensive tool. Let’s look at how other game work so we have a better idea. BackpackBattle usually have their build center around their offensive core or defensive core. For exmaple,

    Pine Protector = a good defensive item that scale itself with the food type of build. Pine Protector can generate a lot of Spike modifier which means the offensive option we choose to pair with this item should also scale off the Spike. Best candidates are Thronewhip, Hungry blade, and its combination.

If we look at this pine protector example, the defensive option are usually separated from the offensive option BUT defensive options have a characteristic that makes it pair better with some offensive option.

With that analysis, we may pair Burn synergy with other synergy like Shield synergy. We may have the item that said the enemy who attack shield get burned. Then, we put a shield hero into the team.

5. The most important here is to create a interesting synergy that make the game fun. My criteria would be to make like 10 synergy for the first version, and iterate from that point. 