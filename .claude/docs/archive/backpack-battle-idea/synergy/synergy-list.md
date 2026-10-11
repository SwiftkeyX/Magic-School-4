# Synergy list - first version

10 synergies, each with one starting chain, written against `synergy-rule.md`.
Hero skills are the real ones in the project today. "Gap" means it does not exist yet.
The synergy names, and every choice of who goes where, are a first proposal.

## Overview

The table is in `synergy-list.csv`, one row per synergy: #, Synergy, Kind, Currency, Generators, Payoff,
State today.

7 offensive, 3 defensive. Every offensive synergy pairs with two of the three defensive ones, and every
defensive synergy pairs with four offensive ones.

## Offensive

### 1. Fire - Burn

```
Currency:       enemies that are burning
How it is made: from the ground - area skills that set a place on fire
Generators:     Dragon (cone, burns 3s), War Cleric (zone around himself, burns 3s)
Payoff:         Imp - gains attack speed for every enemy burn tick
Bridges:        War Cleric - his zone also heals allies, so he is half defensive
Pairs with:     Bulwark - an item: an enemy who attacks a shield gets burned
                Control - a stunned enemy cannot walk out of the fire
Gaps:           burn must become a real status (one per enemy, refresh) before any new source;
                a source that burns before the first cast; Imp's bonus has no top
```

### 2. Frost - Frost

```
Currency:       frost on enemies (a number per enemy)
How it is made: from many small hits - each hit adds a little frost
Generators:     gap. Closest by skill: Skeleton Archer (4 fast shots), Orc Blademaster (8 ticks around him)
Payoff:         Frost Witch - gains stats from frosted enemies
Bridges:        Frost Witch herself - her field already slows attack speed by 30%
Pairs with:     Control - enough frost freezes, which is a stun
                Bulwark - slowed enemies break shields more slowly
Gaps:           frost does not exist as a number, only Frost Witch's slow; her payoff does not exist;
                two generators. "All enemies at 50 frost" is all-or-nothing - per frosted enemy is safer
```

### 3. Venom - Poison

```
Currency:       poison stacks on an enemy
How it is made: from auto attacks spread over many enemies
Generators:     Goblin Archer (poison per arrow, stacks, random targets),
                Warlock (curse over 4s that jumps when the target dies)
Payoff:         Naga - "Split Venom" hits harder on an afflicted target
Bridges:        Troll - wounds enemies, so they cannot heal the poison off (Giant)
Pairs with:     Giant - poison needs time, and big health pools buy time
                Control - stunned enemies keep ticking and do nothing back
Gaps:           poison is a damage timer like burn, not a status that can be counted;
                Warlock's curse is not poison today; Naga's bonus reads Wound, not poison
```

### 4. Tempo - Attack speed

```
Currency:       bonus attack speed on the team
How it is made: from standing near the right ally - an aura and a wave
Generators:     Dryad (allies her wave passes are 25% faster for the rest of the fight),
                Archangel (allies within 3 hexes +20% for 4s)
Payoff:         Werewolf - transformed, turns his bonus attack speed into attack damage
Bridges:        Dire Wolf - his frenzy is attack speed, his howl is a stun that grows with attacks (Control)
                Imp - his Fire payoff is attack speed
                Harpy - gets both her bonuses above 1.5 attack speed
Pairs with:     Giant - Ettin's auto attacks deal 5% of his max health, so he wants speed
                Control - through Dire Wolf
Gaps:           none for the base chain. Bandit (steals attack speed) and Ranger (speed per enemy
                pierced) feed only themselves today
```

### 5. Precision - Crit

```
Currency:       crit chance
How it is made: not decided - nothing grants crit chance today
Generators:     gap. No hero or item gives crit chance; every hero just has 25%
Payoff:         Fencer - one more strike for every 10% crit chance
Bridges:        -
Pairs with:     Control - an idea: hits on a stunned enemy always crit
                Bulwark - Fencer is a fragile melee carry; Rock Golem's ward shield fits him
Gaps:           two generators and a crit item; only auto attacks and Fencer's strikes can crit
```

### 6. Blood - Healing from damage

```
Currency:       healing the team has done this fight
How it is made: from damage dealt - a share of it comes back as health
Generators:     Priest (a fairy on the weakest ally: it heals for the damage it deals),
                Lich (heals 40% of his beam)
Payoff:         Reaper - gap. She should hit harder the more the team has healed
Bridges:        Swordsman - his slash heals far more than he can hold; the rest could become a shield (Bulwark)
                Dire Wolf - heals on every auto attack, so speed is healing (Tempo)
Pairs with:     Giant - healing only helps while there is missing health to fill
                Bulwark - healing above full health becomes shield
Gaps:           the payoff. synergy-idea.md names Fencer, Reaper and Bandit; Reaper is taken here because
                Fencer is Precision's payoff. Naga and Troll (Wound) are the natural counter to this synergy
```

### 7. Arcana - Casts

```
Currency:       skills cast by the team
How it is made: from mana - start mana and mana per attack
Generators:     items: Chalice of Dawn (+30 start mana), Ley Battery, Scholar's Sash (+15 each).
                No hero gives mana to another
Payoff:         Dwarf - his blast grows 1 hex every cast, up to 3
Bridges:        Ettin and Husk - both stack every cast (Giant)
                Rock Golem and Templar - every cast is a new shield (Bulwark)
                Harpy in melee - +20% attack damage every cast
Pairs with:     Giant - Ettin and Husk grow per cast
                Bulwark - more casts means the shield is up more of the time
Gaps:           a "mana per attack" item; a hero who gives mana to allies
```

## Defensive

### 8. Bulwark - Shield

```
Currency:       shield on the team
How it is made: from tanks casting - a shield for himself and one ally
Generators:     Rock Golem (500 shield to himself and his ward), Templar (shield, bigger per enemy his swipe hits)
Payoff:         Shield Knight - gap. His slam should scale with the shield the team holds or has lost
Bridges:        see Arcana and Blood
Pairs with:     Fire (attack a shield, get burned), Arcana (more casts), Blood (overheal), Precision (ward on Fencer)
Gaps:           the payoff; the "attack a shield, get burned" item; shields last 4s and that length is a placeholder
```

### 9. Giant - Bonus health

```
Currency:       bonus health on the team
How it is made: from deaths and from allies - taken, not cast
Generators:     Troll (+300 health for every enemy that dies), Husk (takes 10% health from every ally at
                combat start, and from 4 enemies per cast)
Payoff:         Ettin - auto attacks deal 5% of his max health
Bridges:        Ettin also grows his own health every cast (Arcana); Troll wounds (Venom)
Pairs with:     Tempo, Arcana, Blood, Venom
Gaps:           none for the base chain. Ettin makes and uses the currency himself, which rule 5 and
                rule 12 warn about - he has no top
```

### 10. Control - Stun

```
Currency:       stunned enemies
How it is made: from a big skill landing on a crowd
Generators:     Knight (leaps into the crowd, 2s stun), Centaur (charges through the line, 2s stun)
Payoff:         Rogue - gap. He should deal more to stunned enemies
Bridges:        Dire Wolf (Tempo); Monk and Myconid also stun and can fill in
Pairs with:     Fire and Frost (enemies stay in the area), Precision, Tempo
Gaps:           the payoff. This is the synergy with the most generators (5 heroes stun) and none who use it
```

## Not placed yet

| Hero                             | Why                                          | Could go to                                                     |
| -------------------------------- | -------------------------------------------- | --------------------------------------------------------------- |
| Blacksmith, Elf                  | both remove armour for the rest of the fight | an 11th synergy, "Sunder" - it has two generators and no payoff |
| Bandit                           | steals attack speed for himself              | Tempo                                                           |
| Ranger                           | attack speed per enemy pierced               | Tempo                                                           |
| Harpy                            | scales per attack or per cast                | Tempo or Arcana                                                 |
| Dire Wolf                        | speed, stun and self-heal                    | bridge: Tempo / Control / Blood                                 |
| Swordsman                        | one huge slash that heals                    | Blood                                                           |
| Monk, Myconid                    | stun                                         | Control                                                         |
| Skeleton Archer, Orc Blademaster | many small hits                              | Frost                                                           |

Placed in a base chain: 24 of 35. With the table above every hero has a home.

## What this list shows

- **Three synergies work today with no new code:** Tempo, Giant, and Fire (slow).
- **Three synergies are missing only a payoff:** Blood, Bulwark and Control. One changed skill each.
- **Two synergies are missing generators:** Precision and Frost (Frost's payoff too). These cost the most.
- **Venom needs relabelling, Arcana needs heroes:** Venom's pieces exist but do not read each other yet;
  Arcana's payoff works but only items generate for it.
- **Statuses are the shared code gap.** Burn, poison and frost all need to be a status on the enemy that
  can be counted, instead of a damage timer. One piece of work opens three synergies.
- **War Cleric is a tank in an offensive chain**, which rule 9 says should not happen. He is listed as a
  bridge because half his skill heals.
- **Keystones (rule 8) are not designed.** None of the 10 has one yet.
