# Analysis

## Vocab

- modifier: a stacking buff or debuff, e.g. Spike, Poison, Luck, Regen, Mana, Empower.
- generator: an item that gives a modifier. What matters is how much and how early.
- align: Backpack Battle's placement mechanic. An item placed correctly next to another gets a bonus from it.
- [tag]: an item type, e.g. [Food], [weapon], [nature]. "aligning [Food]" means a Food item that is aligned to this one.
- burst: does massive damage at the start of the fight but scales badly, so it has to finish the fight quick.
- scaling: weak early, gets stronger the longer the fight goes.
- core: the few items a build is built around. A build has an offensive core and a defensive core.
- bridge: an item that connects two things that otherwise don't work together, e.g. Gold Armor bridges Regen and [Holy] items.
- depth: Depth 1 is the items added to make the core work, Depth 2 is the items added to make Depth 1 work.
- build:
  - Burst build: A build that have a lot of early damage, focus on dealing massive damage to enemy early in the fight. If enemy doesn't die from the burst, this build suck because it have bad scaling.
  - Normal build: A build that can do damage early and have scaling. But not as good as other 2 build.
  - Scaling build: A build that have no damage early but have very good scaling.

## Items

Every item is described once, here. The builds below only name an item and say why it is there.
An item sits under the modifier it mainly gives. (Potion) items follow the stacking rule in "Potion core".
The first line of an item is what it does. "Review" under it is my opinion of it.

### Generators

Spike

- Tusk Piercer: at start, gain 4 Spike.
  - Review: immediate, so good for burst.
- Tusk Poker: on hit, gain 1 Spike.
  - Review: needs time to stack, so not for burst. But easier to find than Tusk Piercer which is a big deal.
- Strong Stone Skin Potion (Potion): on reaching 45 Shield, gain 35 shield and gain 2 Spike.
  - Review: any armor item reaches 45 at the start, so it is as immediate as Tusk Piercer. In an actual game, it can get 6 Spike at the start with the right setup.
- Pine Protector: when attacked, 30% chance to gain 1 Spike. Heals you when an aligning [Food] activates. Aligns with up to 16 [Food].
  - Review: too slow for burst, but a strong defensive item.
- Wisp: gain 1 Spike per 30 damage dealt.
  - Review: so OP in late game. It generates so much Spike that other Spike generation feels useless.
- Pineapple: [Food]. Every 3 sec, gain 1 Spike.

Poison

- Belladonna's Whisper: inflicts more poison the more damage the aligning [weapon] does. Deals +0.5 damage per Poison on the enemy.
  - Review: good when that weapon has high DPS.
- Poison Spear: weapon. On hit, inflicts a lot of poison with no condition.
  - Review: decent damage, apply a lot of poison on its own.
- Pestilence Flask (Potion): when the enemy heals, inflict 3 poison.
  - Review: with the right setup the enemy is hurt and healing early, so it activates fast. Just okay alone, it is there to stack with other Potions.
- Pandamonium: on [Food] activation, inflict 1 poison. Poison has 3% crit per aligning [Food].
- Serpent Staff: weapon. Uses mana on attack and inflicts more poison the more damage it deals. Duplicates 40% of the poison inflicted.

Luck

Luck gives a little crit chance on its own. Acorn Collar is what turns it into a lot.

- Fortuna's Grace: at start, gain 5 Luck. If the aligning [weapon] crits, itself attacks twice.
- Fortuna's Hope: on hit, gain 1 Luck. Activates fast.
  - Review: similar to Tusk Poker. Looks like a downgrade to Fortuna's Grace, but it is easier to find.
- Lucky Clover, Lucky Piggy: at start, gain 1 Luck.
  - Review: small alone, needs Mega Clover or Acorn Collar to matter.
- Acorn Collar: +5% crit per Luck.
- White Lily Collar: on item activation, 30% chance to gain 1 Luck and 1 Regen.
  - Review: not reliable, not fast enough.
- Forest Dragon: [Pet]. On hit (every 2 sec), gain 1 Luck and 1 Regen. +1 damage per Regen.
  - Review: strong at both generating and dealing damage. A Pet uses no stamina, so stacking several is okay.

Mana

- Blueberries: [Food], [nature]. Every 3 sec, gain 1 Mana; at 10 Mana it gains 1 Luck instead. Activates fast.
  - Review: slow as a Luck source, but fits any Food build.
- Prismatic Orb: gain 2 mana per aligning [Mana] item. Aligns with up to 8 items, for 16 mana.
  - Review: A late game item that need the existing Mana item there first to be reliable.
- Mana Orb: On aligning [item] activation, 50% to gain 1 mana.
  - Review: Need fast item activation to be reliable.
- Moon Armor: Gain 100 armor. Every 3 sec, gain 3 mana and reflect 2 debuffs.
- Moon Shield: On aligning [item] gained 12 shield, gain 1 mana.

Regen

Regen heals you every 2 sec.

- Heart Container: every 3 sec, gain 1 Regen.
- Rat Chef: Pet. Generates 1 Regen per aligning [Food].
  - Review: easily 5 at the start with a Food build.
- Also gives Regen: Gold Armor, Holy Armor, Forest Dragon, White Lily Collar.

Empower

- Heart of Darkness: consumes 7 Regen to gain 100 maximum health and 4 Empower.
- Strong Heroic Potion (Potion): on running out of stamina, restore stamina and gain 1 Empower.
  - Review: easy or hard to trigger depending on the setup. Bridge to Empower builds.
- Carrot: [Food]. Every 3 sec, cleanse 1 debuff. At 4 Luck or more it gains 1 Empower too.
- Carrot Goobert: Pet. Generates Empower.
  - Review: good Empower generation.
- Thornbloom: weapon. On attack, gain 1 Spike and 60% chance to gain 1 Empower. +1 atk per Spike. On Empower gained, gain 11 max health.
- Stone Golem: On hit, gain 1 empower.

Stamina

- Banana: [Food]. Every 5 sec, gain 1 stamina.
- Also gives stamina: Strong Heroic Potion.

Shield

- Gold Armor: at start, gain 120 Shield and 4 Regen per aligning [Holy] item. Every 3 sec, gain 30 Shield.
  - Review: bridge between Regen and [Holy] items.
- Holy Armor: gain 2 Regen per aligning [Holy] item, up to 20 Regen.
- Moon Armor: gain a lot of Shield. Every 2 sec, generate 3 Mana.
  - Review: bridge to mana builds.
- Moon Shield: blocks damage. Aligning items gain +30% more shield. Each time an item gains 12 shield, generate 1 Mana.
  - Review: bridge to mana builds.
- Other Armor items in general (Leather Armor, Holy Armor, Gold Armor): at start, gain about 100 Shield.
  - Review: They're not worth mentioning so group them here.

Vampiric

- Strong Vampiric Potion (Potion): when you and the opponent are both below 80% health, gain 3 Vampiric and deal direct damage.
  - Review: hard to trigger against a big shield. Bridge to Vampiric builds.
- Also gives Vampirism: Bloodthorne.

### Payoff

Items that spend a modifier or get stronger from it.

- Mana Orb: uses 35 mana to gain 20 random other buffs.
- Manathirst: weapon. Once you gain 30 mana, the next attack hits super hard. It does not consume the mana.
- Critwood Staff: weapon. On hit, consumes mana to make all your weapons crit for a short duration.
  - Review: crit right away, without building Luck first.
- Lightsaber: weapon. On hit, consumes 3 Regen to inflict 8 Blind for 6 sec, which makes the enemy miss 40% of the time.
- Darksaber: weapon. +0.5 damage per debuff stack on the enemy. On hit, uses 1 mana to inflict 1 Blind.
- Stone Golem: weapon. Consumes 7 Regen to massively reduce its cooldown and gain 150 Shield (once). On hit, gain 1 Empower. Gain +10 damage per Bag of Stones.
  - Review: a lot of damage early, and a defensive item too. But very conditional because Bag of Stones aren't something people buy.
- Impractically Large Sword: weapon. At 5 Empower or more, massively reduces its cooldown.
  - Review: massive damage, but only once the condition is met. Best paired with itself.
- Amulet of Feasting: reduces food cooldown by 40%.
  - Review: very strong, balanced by being hard to find.
- King Goobert: defensive item. On 6 item activations, heal for 35, protect 3 buffs from removal and become invulnerable for 1.5 sec (up to 3 times). Aligns with [Food].
  - Review: REALLY strong. It gives everything to make you tanky, but needs about 4 fast-activating items and a little mana generation.
- Snowmaster: Pet. Applies frost to the enemy to stall the game. Activates fast.
- Bloodthorne: weapon. Consumes 1 Regen to generate 1 Vampirism and 1 Spike. +1 damage per Vampirism and Spike.
  - Review: seems like a strong scaling weapon. Not many weapons consume 1 modifier and give 2.
- Red Orchid Collar: +4% crit chance per Vampirism.
- Glove: gives a lot of trigger speed.
  - Review: necessary for most single-weapon builds.
- Djinn Lamp: uses 7 Shield, Mana, Spike and Luck to give 27 damage to an aligning [weapon].
  - Review: gives SO MUCH DAMAGE, but usually very hard to proc. Spike and Mana are the hardest of the four to get.
- Holy Spear: weapon. On hit, destroy 10 shield per aligning [Holy]. Uses 10 Mana to become invulnerable for 3 sec (once).

Not described yet: Eggscalibur, Amulet of Light, Garlic.

## Subclass Item

Backpack battle's subclass give player a free strong special item which usually a item player try to build around.

### Yggdrasil Leaf

At start, gain up to 18 Mana and 9 Regen. The max needs [nature] alignment. Every 5 mana used, heal 15 and cleanse 2 debuffs.
That generation is enough to pay some core item's cost very quickly.
A build is built on one of the two modifiers and has the other one left over, so it also carries a weapon that spends the leftover: Lightsaber for Regen, Critwood Staff for Mana.

1. Mana Orb Build

Build: A normal build.
Goal: 35 Mana early, for Mana Orb's 20 buffs.

- Mana: the Leaf (18) + Prismatic Orb. Prismatic Orb is essential, 35 is out of reach without it.
- Payoff: Mana Orb. Manathirst (x2 is fine) do damage for free when gain 30 mana.
- Leftover Regen: Lightsaber. 9 Regen is 3 hits of Blind, which help you stall the game because enemy could miss the attack.
- Defense Core: Yggdrasil Leaf itself.
  - Why: it heals per 5 mana used, so hitting Mana Orb's condition heals you automatically.

Depth 1: Regen for Lightsaber, and [Mana] items for Prismatic Orb.

- Regen: Heart Container x2.
- [Mana] item: Blueberries, to align with Prismatic Orb.

2. Stone Golem Build

Build: A burst build.
Goal: Stone Golem online from the first second. Combine with other strong early item to burst enemy down early.

- Regen: the Leaf (9) covers Stone Golem's 7.
- Payoff: Stone Golem, plus as many other weapons as fit.
- Leftover Mana: Critwood Staff, for all weapon crit right away which do a lot of damage early.
- Defense Core: Stone Golem itself.
  - Why: hitting its condition gives 150 Shield, which is a lot for 1 item.

3. Large Greatsword Build

Build: A burst build.
Goal: 5 Empower at the start and have enough stamina.

- Regen: the Leaf (9) covers Heart of Darkness's 7.
- Empower: Heart of Darkness, for 4.
- Empower: Strong Heroic Potion, 1 per trigger. It could give 3 per potion.
- Payoff: Impractically Large Sword x2. It uses more stamina than we could ever regen, so it needs an additional source of stamina.
- Leftover Mana: Critwood Staff, same as the Stone Golem Build.
- Defense Core: none.
  - Why: this is the true one-shot build. No need for a defensive item because everything is for damage.

Depth 1: stamina for the Large Swords.

- Stamina: Strong Heroic Potion x3.
- Pestilence Flask: more potion to activate Strong Heroic Potion.

4. Eggscalibur + Amulet of Feasting Build

Not written yet. The idea is built on Amulet of Feasting's 40% food cooldown.

### Poison Ivy

When you gain Spike, inflict 2 poison. Once the enemy reaches 18 poison, gain +25% damage.
18 poison is not hard to reach at the start, so the +25% comes very early. That makes Poison Ivy a burst build, even though it does not look like one.
Aligning Poison Ivy with a [nature] item gives +5% chance to resist a debuff. Nice to have, not important.

1. Darksaber Build

Goal: 18 poison at the start, then two weapons that deal more damage per poison stack.

- Spike: Tusk Piercer + Strong Stone Skin Potion x2. With this it should hit 18 poison from the start of the battle.
- Poison (optional): Poison Spear.
- Payoff: Darksaber and Belladonna's Whisper. This playstyle inflicts a lot of poison, so its damage per debuff stack is a lot.
- Defense Core: Strong Stone Skin Potion x3 + any armor.
  - Pestilence Flask: to activate more Strong Stone Skin Potion.
  - Holy Armor: a solid choice, and the easiest armor to find.

Depth 1: Mana for Darksaber, and crit. Darksaber wants crit but this build lacks a way to generate Luck, so buy as many Lucky Clover as you can find.

- Mana: Blueberries. It is [nature], so it aligns with Poison Ivy too.
- Crit: Acorn Collar x2, which gives 40% crit.
- Luck for Acorn Collar: Lucky Clover, at least x4.

2. Pandamonium Build

Build: A normal build.
Goal: 18 poison at the start, then keep adding poison until enemy is dead. This build relies on poison damage, the damage from weapons isn't much.

- Poison: Pandamonium, with as many aligning [Food] as possible, to make poison crit.
- Spike: Pineapple x2. It is [Food], so it aligns with Pandamonium too.
- Spike: Strong Stone Skin Potion x3.
- Payoff: Apply huge amount of poison. The poison also crit.
- Defense Core: Strong Stone Skin Potion x3 + any armor.
  - Pestilence Flask: to activate more Strong Stone Skin Potion.
  - Holy Armor: a solid choice, and the easiest armor to find.

Depth 1: more poison, and Mana for Serpent Staff.

- Poison: Serpent Staff, to apply more poison.
  - Alternative: Poison Spear.
- Mana: Blueberries. It is [Food], so it aligns with Pandamonium too.

3. Pine Protector / Pineapple Build

Build: A normal build.
Goal: Generate a lot of Spike to hit 18 poison later in the fight.
Rate: sub-optimal. Weaker than the other 2 above because it can't reach 18 poison at the start. Go this build when there is no better option.

- Spike: Pine Protector.
- Spike: Pineapple x2. It is [Food], so it pairs well with Pine Protector.
- Payoff: Darksaber and Belladonna's Whisper.
- Defense Core: Pine Protector + any armor.
  - Why: Pine Protector is a good defensive option, and combined with Pineapple it generates a lot of Spike.
  - Why it is still sub-optimal: it is a snowball from weak early damage. No 18 poison at the start means less damage early, so Belladonna's Whisper inflicts less poison, so there is less debuff and less Darksaber damage.

Depth 1: Mana for Darksaber.

- Mana: Blueberries. It is [nature], so it aligns with Poison Ivy too.

### Mega Clover

Once you reach 15 Luck, gain 22 random other buffs. Every time you enter the shop, it gives 2 Lucky Clover. with the right Luck generators, 15 Luck is reachable at the start of the fight.

1. Fortuna's Grace x 2 Build

Build: A normal build.
Goal: 15 Luck at the start. And use the 22 random buffs to proc Djinn Lamp.

- Luck: Fortuna's Grace x2, for 10 at the start.
- Luck: Lucky Clover and Lucky Piggy for the last 5. It was easy to find because Mega Clover keeps handing out Lucky Clovers.
- Payoff: Djinn Lamp, proc'd by the 22 buffs. Fortuna's Grace do massive damage.
  - Why: Djinn Lamp is usually very hard to proc. Mega Clover's 22 random buffs make it easier.
- Defense Core: Any armor + Moon Shield.
  - Why: Moon Shield paired with armor gives 10 mana on average, which is enough to proc Djinn Lamp once. The mana problem is solved, next is the Spike problem.
  - Moon Armor (optional): the best armor here, because it gives a lot of mana, which makes it possible to proc Djinn Lamp 1 more time.

Depth 1: Spike for Djinn Lamp.

- Spike: Tusk Poker, because it's the easiest to find.
  - Alternative: Strong Stone Skin Potion, Tusk Piercer.

2. Large Greatsword Build

Build: A burst build.
Goal: 15 Luck at the start. 5 Empower at the start and enough stamina.

- Empower: Strong Heroic Potion, 1 per trigger. It could give 3 per potion. Stack at least 2.
- Payoff: Impractically Large Sword x2. It uses more stamina than we could ever regen, so it needs an additional source of stamina.
- Defense Core: none.
  - Why: this is the true one-shot build. No need for a defensive item because everything is for damage.

Depth 1: stamina for the Large Swords, and more Empower.

- Stamina: Strong Heroic Potion x3.
- Pestilence Flask: more potion to activate Strong Heroic Potion.
- Empower (optional): Heart of Darkness.

Depth 2: Regen for Heart of Darkness.

- Regen: White Lily Collar.

3. Forest Dragon Build

Build: A normal build. But it have good scaling though.
Goal: Forest Dragon seem strong because it scales off Regen and generates a lot of Luck. Stack at least 2 Forest Dragon and generate a lot of Regen.

- Payoff: Forest Dragon, at least x2. It is a [Pet], so the extra ones cost no stamina.
- Regen: Forest Dragon itself.
- Defense Core: Holy Armor + [Holy] items.
  - Why: more Regen, 2 per aligning [Holy] item. The maximum is 20, which is massive.

Depth 1:

- Extra DPS and survival (optional): Holy Spear. It destroys shield per aligning [Holy], and gives one invulnerability.

### Big Bowl Of Treats

Every 4 sec, gain 2 random buffs and make aligning [Food] activate 25% faster. All Pets have a 20% chance to activate twice. "Friend of the forest" are now offered in the shop.
The 2 random buffs are okay; making Food activate faster is the strong part. This is a true scaling build, weak early and strong late in the fight, and it wants Food and Pets.

1. Scaling Thornbloom Build

Build: a scaling build.
Goal: Thornbloom gains 11 max health per Empower, so generate Empower as fast as possible. It looks strong at 1.5 Empower/sec or more.

- Payoff: Thornbloom.
- Empower: Stone Golem. Needs 7 Regen to come online.
- Empower: Carrot Goobert. A Pet, so it also pair well with Big Bowl of Treats.
- Defensive Core: Stone Golem give massive shield on online.

Depth 1: Regen for Stone Golem, and more Empower.

- Regen: Rat Chef. This is a [Food] setup so it could easily give 5 Regen at start. Another 2 Regen could be anything since it is not hard to find.
- Empower: Carrot. A Food, so Big Bowl of Treats speeds it up. Needs at least 4 Luck to be useful.

Depth 2: Luck for Carrot.

- Luck: Fortuna's Hope, for early Luck.

2. Scaling Thornbloom Build v2

Build: a scaling build.
Goal: same as v1, but Carrot generates the Empower instead of Stone Golem and Carrot Goobert.

- Payoff: Thornbloom.
- Empower: Carrot. Needs 4 Luck.
- Defense core: Big Bowl of Treats + Pine Protector. It generate Spike and raises Thornbloom's damage.
  - King Goobert (Optional). Needs about 4 fast-activating items and a little Mana.

Depth 1: Luck for Carrot, and fast activations and Mana for King Goobert.

- Blueberries x2: Mana and Luck, and two fast activations. A staple in Big Bowl of Treats builds.
- Fortuna's Hope: early Luck for Carrot, and a fast activation.
- Snowmaster: stalls the game, and a fast activation. It's a Pet so pair well with Big Bowl of Treats.
- Any Pet: if there is a slot left because Pet pair well with Big Bowl of Treats.

3. Bloodthorne Build

Goal: Bloodthorne seem strong. So generate enough Regen for it, and be tanky enough to scale safely.

- Payoff: Bloodthorne.
- Regen: Amulet of Light, Heart Container, Forest Dragon, Rat Chef. Anything you can find.
- Spike (optional): Wisp.
- Defense Core: Big Bowl of Treats + Pine Protector.
  - Why: Strong defensive option that made you very tanky, and It generate Spike which raise Bloodthorne damage.

Depth 1: trigger speed, Food, and a crit source.

- Trigger speed: Glove, at least x2. Good for a single-weapon build.
- Food: Blueberries and Carrot fit best. They give more Empower and Luck.
- Crit: Red Orchid Collar, from the Vampirism.

Depth 2: more crit, from the Luck.

- Crit (optional): Acorn Collar, on the Luck that Blueberries gives.

## Defensive Item

A defensive core is always paired with an offensive core. The Subclass Item section covers the offensive cores; this section covers the defensive ones.

1. Armor + Shield core

- Armor: Gold Armor if the build uses Regen or [Holy] items, Moon Armor if it uses Mana.
- Shield: Moon Shield. It pairs with any armor because of the +30% shield, and it also feeds a Mana build.

(I still don't understand this enough, I will leave it alone for now)

2. Potion core

When Potions are stacked on top of each other and the one below activates, the one above is triggered once for free.
Some Potions are hard to activate, which makes them inconsistent, so a stack pairs a hard one with an easy one. It takes at least 3 Potions stacked together for the synergy to work.

How hard each one is to activate:

- Easy: Strong Stone Skin Potion (one armor item is enough), Pestilence Flask.
- Depends on the setup: Strong Heroic Potion. Easy when several weapons drain stamina fast.
- Hard: Strong Vampiric Potion. Needs a build that applies poison or deals direct damage, to get past the shield.

Stacks:

- Strong Stone Skin Potion x2 + Pestilence Flask x2, for a Poison Ivy build.
  With armor it gives 12 Spike very early. Good synergy.
- Strong Heroic Potion x3, for a burst build.
  With 3-5 weapons draining stamina it gives a lot of stamina and 5 Empower early. Good synergy, most often with Impractically Large Sword.
- Strong Vampiric Potion x2 + Pestilence Flask x2, for a Vampiric build.
  If you can deal direct damage past the shield it gives 12-18 Vampiric early. Not worth it, there is a better way to get Vampiric.

3. Pine Protector core

Pairs really well with Big Bowl of Treats, because both align with [Food].
On avg, about 10 aligning [Food] is enough to make you tanky. The cap is 16.

4. King Goobert

Needs fast item activation to work.
It is often paired with Food: Food is the easiest way to get item activations, and it aligns with King Goobert too.

## Other Item

### Food

The common Food are Banana (stamina), Blueberries (Mana, then Luck), Carrot (Empower) and Pineapple (Spike). There are other Food too, e.g. Garlic.
A Food build pairs well with a build that needs a lot of Luck, Empower or Spike. Not a Mana build though, because Blueberries caps the Mana at 10.

- Luck: Forest Dragon build, Fancy Fencing build.
  - NOT Mega Clover build: Blueberries needs time before it starts generating Luck, but Mega Clover needs 15 Luck at the start of the combat.
- Empower: Thornbloom build.
- Spike: Thornbloom build, Bloodthorne build, Poison Ivy build.

## Weird Item

1. Djinn Lamp

- pair well with Gold Armor because ...

## When a modifier is generated

"When" a modifier arrives changes how an item plays, and the item list shows it:

- At start (Tusk Piercer, Fortuna's Grace, armor): immediate, so it suits burst.
- On hit or over time (Tusk Poker, Fortuna's Hope, Heart Container): has to stack, so it suits scaling.
- On a condition (the Potions, Stone Golem, Heart of Darkness): generate as early as "at start" if the condition is easy to meet, useless if it is not.

NOTE: Regen is good with [Holy] items only because Holy Armor bridges them. Forest Dragon is the Pet that scales with Regen, so the Regen core is Holy Armor + Forest Dragon.
