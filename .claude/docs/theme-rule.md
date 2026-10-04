# Theme rules

The rules every theme must follow. Write a theme against this page before building anything.
Where it came from: the Burn paper test and the review in `themes-analysis.md`.

Resource: `theme-resource.md` - the Backpack Battles example these rules are built on. Every Backpack
Battles name on this page is explained there.

## Words


| Word      | Meaning                                                              | Backpack Battles                        | Here                  |
| --------- | -------------------------------------------------------------------- | --------------------------------------- | --------------------- |
| Theme     | a family of heroes and items                                         | Food, Pet                               | Status                |
| Chain     | one generator -> currency -> payoff line inside a theme              | Pine Protector -> Spike -> Hungry Blade | Dragon -> burn -> Imp |
| Currency  | the resource a chain makes and spends                                | Spike, Luck, Heat                       | burn, frost, shield   |
| Generator | makes the currency                                                   | Pine Protector, Blueberries             | Dragon, War Cleric    |
| Payoff    | gets stronger from the currency                                      | Hungry Blade, Rapier                    | Imp                   |
| Bridge    | a piece that serves two chains                                       | Blueberries                             | -                     |
| Keystone  | the strong piece a build is built around; lifts a whole theme or two | Big Bowl of Treats                      | -                     |

So what `themes.md` calls the Burn theme, Frost theme and Poison theme are chains, and "Status theme"
is the theme above them.

## The rules

1. **Every chain has one currency, and it can be counted.**
   "Burning enemies", "shield on the team", "crits this fight". If a player cannot see it going up,
   it is not a currency.

2. **A chain is either offensive or defensive, not both.**
   An offensive chain kills. A defensive chain keeps the team alive. A chain does not need its own
   defence - Burn has none, and that is fine.

3. **A defensive chain is built to pair with offensive ones.**
   It makes, or reacts to, a currency an offensive chain uses - the way Pine Protector heals but also
   makes the Spike a weapon wants. Example here: "an enemy who attacks a shield gets burned" pairs
   Shield with Burn.

4. **Each chain pairs well with at least two chains of the other kind.**
   If Shield only works with Burn, Shield is not its own chain, it is part of Burn.

5. **A chain splits into those who make the currency and those who use it.**
   Generators make it (Dragon, War Cleric burn). The payoff uses it (Imp gains attack speed). A chain
   where one hero does both needs nobody else.

6. **A build is pieces from several chains, not one whole theme.**
   Chains are small on purpose so a team holds two or three. That needs bridges - a hero or item that
   is useful to two chains - and it is why a hero may belong to more than one.

7. **A chain's base is 3 heroes: 1 payoff and 2 generators.**
   Small enough to find in the shop. Start every chain from this and grow it only if it needs more.

8. **Each theme has a keystone.**
   One strong hero or item that lifts every chain in the theme, and is the reason to commit to it.
   The best keystones sit in two themes, like Big Bowl of Treats. A keystone is a special piece, not
   an ordinary one - that is what lets it be this strong.

9. **Tanks belong to defensive chains.**
   This fills the "main tank = ?" in every theme. Any tank can still stand in front of any team.

10. **Items come in two kinds, and both stay.**
    A theme item makes the currency, makes it come sooner, or scales off it. A stat item adds AP, HP,
    attack speed. Which kind makes the better game is not known yet, so keep both and see.

11. **The sooner the currency flows, the better - and each chain gets it its own way.**
    A build that the currency isn't flowed well could have a problem scaling.

    A chain that only starts when skills are cast comes too late (Burn's first tick was at 8.6s).

    But the answer is not "on auto attack" every time: if every chain is fed by auto attacks they all feel the same.

    How a chain makes its currency is part of its identity - at combat start, from being hit, from casting, from a kill, from standing next to someone.

12. **The payoff must not feed itself, and it has a top.**
    If using the currency makes more of the currency, it grows without limit (Imp with an
    "autos burn" item). Either the payoff cannot generate, or the bonus is capped.

## How to write a chain

```
### <Name>                  (theme: <family>, offensive / defensive)
Currency:    what is counted
How it is made: the way that is this chain's own (rule 11)
Generators:  heroes and items that make it
Payoff:      who gets stronger from it, and how
Bridges:     pieces shared with another chain
Pairs with:  at least two chains of the other kind, and why
Gaps:        what does not exist yet (hero, item, code)
```

A chain that cannot fill "Pairs with" stands alone and needs another look.

## Target

About 10 for the first version, then iterate. With 26 non-tank heroes and 9 tanks, roughly
6 offensive and 4 defensive.

## Not decided here

- Is the target 10 chains (Burn, Frost, Shield...) or 10 themes (Status, ...)? The list in `themes.md`
  is at chain level, so this page reads it as 10 chains grouped into a few themes.
- The aligning mechanic (star shape, items on hexes). It should differ per hero, but it can wait -
  chains are designed as if an item simply belongs to a hero.
- Team size at late game against room for items on the board.
