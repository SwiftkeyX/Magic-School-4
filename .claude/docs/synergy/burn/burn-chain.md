# Burn chain

How the chain works. Design only - nothing about code or the rest of the game
(that is in `burn-integration.md`). Written against `synergy-rule.md`.
The version before this one, and the review that led here, is in `patchnote/patchnote-1-0-0.md`.

## The idea

Enemies collect burn stacks. The team's job is to pile them up.
Imp is the carry: he gets a little faster for every burn stack on the enemy team, and when one
pile gets big enough he blows it up.

## Burn stacks

- Every enemy carries a number: its burn stacks.
- Each stack deals 3 damage per second.
- The whole pile goes out 5 seconds after the last stack was added.
  Adding one stack keeps all of it alive.
- An area skill adds a fixed amount, divided equally between the enemies it hits.
  10 stacks on 1 enemy is 10 on him. 10 stacks on 5 enemies is 2 each.
- When a burning enemy dies, his pile moves to the nearest enemy and its 5 seconds start again.

A pile grows from two kinds of piece: one that adds a lot at once, and one that adds a little
but often enough that the pile never goes out.

## Skills

A skill fires on its own timer, as an item does in Backpack Battles. Nothing the hero does makes.
e.g. Every 3 sec, fire skill.
it come sooner or later - not attacking, not being hit. Mana is not used by skills any more; it
is parked, and may come back later as a modifier.

For now this is on paper, for Dragon and War Cleric only.

## The pieces (6)

| #   | Piece                | Job                | What it does                                                                     |
| --- | -------------------- | ------------------ | -------------------------------------------------------------------------------- |
| 1   | Imp                  | payoff             | see below                                                                        |
| 2   | Dragon               | adds a lot at once | every 8s: 10 stacks, divided over his cone                                       |
| 3   | War Cleric           | keeps it alive     | every 3s: 3 stacks, divided over his zone                                        |
| 4   | Ember Brand (item)   | keeps it alive     | +1 stack to the target on every auto attack                                      |
| 5   | Manaflow Band (item) | makes it faster    | the holder's skill comes 20% sooner                                              |
| 6   | Cinder Guard (item)  | keeps it alive     | when the holder is hit: 1 stack to the attacker, once every 2s for each attacker |

Cinder Guard is the bridge to [Bulwark]: it gives +1 stack for every 2 [Bulwark] pieces on your
team, up to +2. It counts itself, so it starts at 1 stack and needs one more [Bulwark] piece for 2. The name is a placeholder.

## Tags

Every hero and item carries tags, the way a Backpack Battles item is [Food] or [Holy].
A tag is what another piece can count. It is not the currency: [Fire] is the tag, burn is the
currency, so a [Fire] piece does not have to make burn.

- **Element:** the 10 synergy names in `synergy-list.csv` - [Fire], [Frost], [Venom], [Tempo],
  [Precision], [Blood], [Arcana], [Bulwark], [Giant], [Control].
- **Role:** the role a hero already has - [Tank], [Mage], [Fighter], [Marksman], [Assassin].

A hero has one role and one or two elements. An item has elements only.
Backpack Battles also sorts items by class and by wearing (weapon, armor, food, pet). Both are
skipped for now.

| Piece         | Tags               |
| ------------- | ------------------ |
| Imp           | [Fire], [Marksman] |
| Dragon        | [Fire], [Mage]     |
| War Cleric    | [Fire], [Tank]     |
| Ember Brand   | [Fire]             |
| Manaflow Band | [Arcana]           |
| Cinder Guard  | [Fire], [Bulwark]  |

**How a tag is counted:** "per [Fire] piece on your team", heroes and items together. Where a
piece stands does not matter.

This is a stand-in. The aligning mechanic will replace it later, so that only the pieces placed
next to each other count. The tags themselves stay the same when that happens.

## Imp, the payoff

- **Small, always:** +1% attack speed for every burn stack, counted over every enemy.
- **Big, earned:** when an enemy reaches 30 stacks, the pile is consumed and Imp deals
  600% AD to it and 300% AD to the enemies next to it.

Imp adds no stacks himself, so he cannot feed his own payoff (rule 12).

## What each piece adds

| Pieces                                           | Burn made by 30s | First blast, 1 enemy | First blast, 3 enemies |
| ------------------------------------------------ | ---------------- | -------------------- | ---------------------- |
| Imp + War Cleric                                 | 30               | 30s                  | 90s                    |
| + Dragon                                         | 60               | 16s                  | 42s                    |
| + Ember Brand, on a hero who stays on one target | 90               | 11s                  | 18s                    |
| + Manaflow Band, on Dragon                       | 100              | 11s                  | 18s                    |
| + Cinder Guard, on the hero the enemies attack   | 115 to 145       | 9s                   | 14s                    |

Small is +1% attack speed per stack in play, so "burn made" is also the attack speed the team is
building toward, less whatever a blast consumed.

What the table assumes: every enemy stays inside War Cleric's zone, so no pile goes out; Ember
Brand's holder attacks once per second; "3 enemies" means every skill hits all three; every
enemy attacks Cinder Guard's holder, and it has no other [Bulwark] piece with it (1 stack).

- 2 pieces work, slowly. War Cleric's 3s is shorter than the 5s a pile lasts, so he keeps burn
  alive by himself.
- The 3rd piece, Dragon, doubles the burn.
- The 4th, Ember Brand, matters most when the burn is spread out: 42s down to 18s.
- The 5th, Manaflow Band, makes 10 more burn but does not move the first blast at all.
- The 6th, Cinder Guard, is worth more the more enemies attack its holder: 15 burn from one
  attacker, 45 from three.

## A choice the player makes

Ember Brand depends on who holds it.

- On a hero who stays on one target: one deep pile. Blasts.
- On Goblin Archer, who hits a new random enemy every time: many piles kept alive. More stacks
  in play, so more attack speed, but no pile gets deep enough to blast.

## War Cleric, the defensive core

War Cleric is the tank, so he is also what keeps the build alive, the way a Backpack Battles
build has a defensive core next to its offensive one.

- **Regen** is a second currency. It sits on a hero: each stack heals that hero 4 health every
  2 seconds, and it does not go out.
- **War Cleric:** at the start of the fight, gain 2 Regen for every [Fire] piece on your team.
  He counts himself. Only he holds this Regen, not the team.
- He keeps his burn zone, so he is in two chains at once: he makes burn for Imp and Regen for
  himself (rule 6).

| [Fire] pieces on the team                           | Regen | Health per second |
| --------------------------------------------------- | ----- | ----------------- |
| 1 - War Cleric alone                                | 2     | 4                 |
| 3 - Imp, Dragon, War Cleric                         | 6     | 12                |
| 5 - the full chain (Manaflow Band is not [Fire])    | 10    | 20                |

He has 800 health, so the full chain heals him 2.5% of it every second. The 4 health per stack
is a first guess.

Like the tags, "on your team" is a stand-in: with aligning it becomes the [Fire] pieces placed
next to him.

## Against the rules

| Rule                                     | How                                                       |
| ---------------------------------------- | --------------------------------------------------------- |
| 1 - one countable currency               | burn stacks, shown on the enemy                           |
| 5 - makers and user are different heroes | Dragon and War Cleric make, Imp uses. Regen has no user   |
| 7 - base of 3 heroes                     | Imp, Dragon, War Cleric                                   |
| 11 - made its own way                    | from the ground on a timer, and from being hit (piece 6)  |
| 12 - payoff does not feed itself         | Imp adds no stacks; Big stops at 30. Small has no top yet |
| 13 - a gate adds, it does not lock       | every piece does something alone                          |
| 3, 4 - pairs with defensive chains       | Bulwark (Cinder Guard) and Regen (War Cleric)             |
| 8 - a keystone                           | not done yet                                              |

## Open

1. Does the pile go out all at once after 5s, or do stacks fall off one by one?
2. 30 stacks and 600% / 300% AD for the blast - a first guess.
3. Burn damage flat (3 per stack), or a share of the adder's AP?
4. Cinder Guard's numbers are a first guess. At the top it is 3 stacks every 2s for each
   attacker - too much when many enemies attack one tank?
5. Small has no top. At +1% per stack it keeps growing while the piles do. Rule 12 asks for one.
6. Dragon's 10 is the old "10 to every enemy" number, now divided. Is 10 every 8s enough next
   to War Cleric's 3 every 3s?
7. A split that does not divide evenly (5 stacks on 3 enemies): round down, or carry the rest?
8. Big: still 30 on one enemy, or the burn of all enemies combined (patchnote, review 3)?
   With the split, combined would fire after a set number of casts whatever the player does.
9. Piles move on death, so late in a fight they join up on the last enemies. Is a blast then
   too easy?
10. Regen has a maker and no user. It only heals. Rule 5 asks for a payoff: a hero or item that
    gets stronger from Regen, as Forest Dragon and Bloodthorne do in Backpack Battles.
11. Regen's size: 4 health per stack every 2 seconds, and 2 Regen per [Fire] piece - a first
    guess. Is 20 health per second on the tank too much, or too little to notice?

## Review

1. Okay new goal is to copy backpack battle combat formula:

- The hero's skill shouldn't rely on mana that was spawn on hit. The skill should be a cooldown base instead e.g. War Cleric's skill apply 3 burn every 3 sec.

NOTE: Next in line should be to make burn generator item, payoff burn generator, more mechanic that we could build upon on.

2. Make more mechanic that we could build upon on:
   It's hard to build something when there's not enough building block. The

- Let assign pseudo-trait to each hero, item. In Backpack battle, Read more at @bb-item-category.
- Then, we create a bridge from [Burn] to other trait.

3. War Cleric is a main tank and should be treated like a defensive core the same to @bb-analysis, let build synergy chain around him:

- Aligning mechanic: gain 2 Regen per number of aligning to [Burn] item.
  (Let go back to (2) first)
