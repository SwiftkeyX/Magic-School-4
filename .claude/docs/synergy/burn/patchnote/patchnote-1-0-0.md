Snapshot at (10-10-69)

# Burn chain

How the chain works. Design only - nothing about code or the rest of the game
(that is in `burn-integration.md`). Written against `synergy-rule.md`.

## The idea

Enemies collect burn stacks. The team's job is to pile them up.
Imp is the carry: he gets a little faster for every burning enemy, and when a pile gets
big enough he blows it up.

## The pieces (6)

| #   | Piece                | Job                | What it does                                             |
| --- | -------------------- | ------------------ | -------------------------------------------------------- |
| 1   | Imp                  | payoff             | see below                                                |
| 2   | Dragon               | adds a lot at once | +10 stacks to every enemy in his cone                    |
| 3   | War Cleric           | adds a lot at once | +2 stacks per second to every enemy in his zone (3s)     |
| 4   | Ember Brand (item)   | keeps it alive     | +1 stack to the target on every auto attack              |
| 5   | Manaflow Band (item) | makes it faster    | +5 mana per attack - the holder casts sooner             |
| 6   | open                 | keeps it alive     | a second steady source - a hero, or a second Ember Brand |

## Imp, the payoff

- **Small, always:** +10% attack speed for every burning enemy, up to 5 enemies (+50%).
- **Big, earned:** when an enemy reaches 30 stacks, the pile is consumed and Imp deals
  600% AD to it and 300% AD to the enemies next to it.

Imp adds no stacks himself, so he cannot feed his own payoff (rule 12).

## Review

### Imp

1. I only review the Imp's ability. This is the identity I can think of:

- The small one reward the synergy that does AoE burn and keep the burn alive, made Imp stronger pretty fast, which fit for burst build which can pump out damage really fast early.
- The big one reward the burn stacking synergy, a scaling build that need time to nuke people which is different to small one.

2. Let's try make the other 2 heroes match Imp itself:

- Small one: Dragon + Imp
  At 5 sec of the fight, Dragon's skill apply 10 burn stack to every enemies.
  Now, the Imp can have his max AS buff really early which suit for the burst build.
- Big one: War Cleric + Imp
  War Cleric skill apply burn that scale with his health. So he can stack burn fast.

3. The problem of (2):
   The (2) look fine until we're serious about the depth of the synergy.

- Dragon + Imp is too OP
  Just having Dragon alone can hit Imp's skill maximum capped at +50% AS easily which conflict with our rules. Next is just finding a way to keep the burn alive. This mean Imp's Small payoff is too easy to hit. Let nerf it:
  - Imp: +20% AS per number of enemy that have 10 burn stack.
  - Dragon: At 5 sec of the fight, Dragon's skill apply (5 + aligning [Item], capped at 10) burn stack to every enemies.
- Keep the burn alive sound easy but isn't
  If the item keep the burn alive to 1 enemy, that is okay. But the item keep the burn alive to every enemy, that isn't okay. Imagine a item that increase the duration of the burn by 50%, that sound too OP to me but I allow it but with the catch that making the item hard to very hard to find.
- **Small** build: are now are not a default build path because it was hard to find the key item. So now Imp have to play both skill at the same time and avoid playing only for **Small**.
  - New build: I want him to focus on [Burn], [AoE], and [Debuff] item because his skill reward the more enemies burn the better and also reward stacking burn too.
  - Change to **Big**: this skill should now reward stacking burn on all enemy too. When burn stack combined from all enemy, generate nuke.

4. The problem still doesn't gone. I thought if we solve keep the burn alive part, the problem will be gone but no. The problem doesn't come from the "burn is alive or not", it come from the AoE.

- AoE is a hard currency to balance: If War Cleric have small AoE and fight with 1 frontline enemy, his skill will only hit 1 enemy. Even if we try adding new item that add the size of his AoE, if it doesn't big enough, it still suck. So let not use AoE as a currency.
- War Cleric apply burn through his AoE. How do we going to balance this?
  - Mimicing **Guildrun**: This game still feel balance eventhough there's a lot of AoE inside the game. I think the main reason is the AoE isn't broken in their game. They have a lot of multiplicative scaling through item/skill/relics which is way more broken than AoE skill. So the balancing problem shift from AoE to multiplicative scaling instead.
  - Quick fix: would be stop making burn chain and move to other chain instead. Our job is to prove that this game idea work. The balancing problem can wait.
  - Burn problem: I thought it would be just not apply burn to AoE. I don't sure, we'll figure this out later.

5. AoE make a debuff currency hard to balance. Because the enemy formation alone could swing the debuff from 1 to 5.

- How **Guildrun** use debuff as a currency: It look if the target have burn/poison/frost or not. Hero's skill doesn't care how many stack the enemy have. But some relics does look at the stack though. The takeaway is just by "looking if staus exist or not" could be enough for a currency.
- FIX: Okay, how about making the War Cleric apply fix amount of burn that was divided equally between the hit enemies. If the enemies die, also trasfer the burn to the nearest enemies. So the currency doesn't go wasted.
- Decided:
  - General rule: every AoE burn applies a fixed amount, divided equally between the enemies hit. Not only War Cleric, Dragon too.
  - War Cleric: 5 stacks per second, divided between the enemies in his zone.
  - Imp's **Small**: count every burn stack on every enemy. 1 stack = +1% AS. This replaces "+10% per burning enemy" and the "+20% per enemy that have 10 burn stack" nerf in (3).
  - Transfer on death: the burn that moves resets its 5 second timer.

### Item

1. The item look off to me but it was fine for the first version.

## Takeaway

1. AoE make a debuff currency hard to balance. Because the enemy formation alone could swing the debuff from 1 to 5. Fix the problem by applying fix amount of debuff, divided equally between the enemies hit, and count the stacks instead of the enemies. 
