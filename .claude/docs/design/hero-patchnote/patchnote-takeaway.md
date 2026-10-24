# Patchnote Takeaway

## P-1-0-0

(skipped)

## P-1-0-1

### Need attention

[] (Ongoing) current benchmark test aren't accurrated because it didn't account for hero's special ability & mark.

[] A way to balance **On kill** trigger:

- Normally, other tank heal through his skill mainly so it was easy to tweak. Assume that he use 100 mana to cast once, assume again that it take 10 sec to cast, and the skill heal for 1000, that mean this hero heal 10 hp/s. From what you can see, when the value can turn into hp/s, it's easy to tweak.
- But troll is different to others tank. He healed for flat 150 when the enemy dies, we don't know when the enemy is going to die, so the value is harder to turn into hp/s which make it hard to tweak and balance. I suggest a wayout by making his skill work like a cashout, he build up his 1 time healed over time w (easily turn into hp/s), then the healed triggered when the enemy dies. Now his skill should be easier to tweak.

[] Assassin are too strong. All those heroes that can access backliner e.g. Myconid, Lich, FrostWitch

- Right now, when they cast 2 time, they kill the backliner which is a problem because it's too easy. If the 2 time cast was hard to achieved, then it wouldn't be a problem.
- It's easy to assassinate backliner because player can just add Myconid to the team with like 1 AP item and 2 active mark, he can 2 shot the enemy. The cost feel too easy to me.
- But maybe the cost wasn't easy? because Akshan in TFT can just 1 shot enemy too in the mid-game just by having him 2 start and have 5 shurima active.

[] (deferred) In patch 1-0-1, player's team need at least 2 tank to win every single stage. Maybe this is bad, because it feel redundant to have 2 tank every single time.

[] (deferred) I notice that the ally mark is boring, it only give stat. Need to spice it up.

### Knowledge

1. AoE test isn't consistent

AoE test have 4 enemy. Most AoE mage hit all of them, but in real game, those mage may doesn't hit the same amount of enemy in the test.

- Lich: His skill size is big, so the AoE test look trust worthy for him.
- Dragon: His skill size is not that big, so the AoE test may have a little higher dps than his average game.
- Dryad: His skill is harder to hit several enemy, mainly because his skill travel too slow. The test have quite higher dps than his average game.

2. Why does some hero pair well with others?

I notice that the reason some specific hero pair well with some specific hero is because: their mark kinda have the same pattern, so the team usually mean several guy that have the same pattern.

- to be more specific: the heroA might have 2 hex away from him at the right side, the heroB might also have 2 hex away from him at the left side. They both will naturally pair well together.
- var1 = number of hex away of that mark
- var2 = side of that mark

In summary, if both hero have 1. the hex away is equal, and 2. the side of the mark in the opposite direction. they will be paired well.

3. Melee Carry dies easily. And he need survival ability.

From Reaper case study, he have a highest dps at 132. But he don't get pass the MinotaurKing stage. The main reason is he dies first. When the carry dies, the team always lose.

This mean a melee carry always need survival ability to not let the team lose for free when he dies.

4. If tankiness don't reach the benchmark, team likely lose.

The Reaper-Monk-ShieldKnight don't win against MinotaurKing until I raise Monk stat a bit:
**Monk's DF from 30 => 50 and Health from 650 => 800.**
Then, they start winning even with out a signle Iron Plate.

- If tankiness of the member (espectially tank) don't reach the benchmark of that stage, the member dies = It lead to huge DPS drop.
- So if the tankiness don't reach the benchmark, the dps will drop a lot.

5. After several test, I found that DPS on papar now & the performance on the board have big different.

- The DPS on paper don't account for special ability of the heroes.
  e.g. Skeleton (43 dps) can shoot backliner, so it fine if he doesn't do as much damage as Naga (71 dps).

- The DPS on paper don't account for mark yet.
  e.g. Skeleton do a 75% more damage when his mark active which is massive but the test don't account for it.
