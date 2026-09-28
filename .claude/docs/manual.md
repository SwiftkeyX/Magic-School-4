# Skill System Explain
Due to how confusing skill are, I have to make a manual to make it easier to explain:

What is this skill system for? 
To create a modular skill system that can be reuse to construct several big skill.

The entire hero's skill separate into 4 big axes:
1) Step - It is how "smaller skill" in 1 hero skill was order, DUE to hero skill being construct of several smaller skill, 
we need a way to monitor the progression of hero skill. For example:
- Step 1 = OnCast, dunk on enemy
- Step 2 = OnKilled, dunk on new enemy
Suggest reading SkillStep.cs.

2) Trigger - the condition which need to be true to active the said skill. For example:
- OnCast = when hero mana is full, he activate his skill once
- OnKilled = when this hero is the one execute another hero
Suggest reading TriggerType.cs.

3) Action - The said skill that activate. For example:
- dunk on enemy
- shoot projectile
- shoot laser

4) Effect - the effect that was apply to the recipient NOT the target.
4.1) Recipient - the hero who get affected by the skill
4.2) Target - the target hero who this skill was aiming at, BUT target is not necessarily have to be the recipient
For exmaple:
- Darius(user) dunk(action) aim on Enemy(target), BUT ally(recipient) get heal(effect).
- Yasuo(user) slash(action) aim on Enemy(target), the target(recipient) get damaged(effect), BUT yasuo(recipient) get buff(effect).
- Lyra(user) shoot wave(action) aim on Furthest enemy(target), BUT all the enemy that got hit by the wave(recipient) get damaged(effect).
Suggest reading EffectRecipientType.cs

This is the example when we combine all of the axes:
Step 1 = OnCast(trigger), Yasuo(user) slash(action) aim on Enemy(target), the same target(recipient) get damaged(effect) and shred(effect).
Step 2 = OnExpired(trigger), Yasuo(user) buff(action) aim on Yasuo(target), the same target(recipient) get buff(effect).



# Done
Lumen - throw 2 hex circle AOE on the clustered enemy
- have to quickly implement this guy to clear the untested clusterd logic from Aldric

Reyn - shoot 6 projectile at the furthest enemy. Shoot in sequence.
- implement new variable in template action, "Fire timing", "shoot in sequence"
- Fire timing - could belong to projectile, laser (BoxAOE e.g. Vharn), quickAA (direct damage e.g. verity)
=> in summary, FireTiming could belong to projectile, AOE, DirectDamage (not implement yet)

Sparks - shoot 5 homing projectile in sequince at random enemy in 2 hex of current target.
- Implement new aim target, random (2 hex of current target)

Mira - do damage by doing cone AOE at herself point to current enemy, repeat 3 time, 0.2 interval
- implement AOE with "Fire timing"



# Plan
=== Interrupt ===
I prefer the data to be chart, more than a pure number like this.
- adjust the chart position, I prefered the chart of damage dealt, damage taken to be stack on each other, not line up like now. 

when sell hero with item, that item is gone too. This is not intended, the items should stay in the scene.

=== implement new system ===
I want a UI that show how much damage each hero does, how much damage each hero receive, how much damage each hero heal/omnivamp/reduce healing/etc...
I could say I want almost everything that can show number.
- split the AD and AP properly to indicate which does ad or ap damage.
- implement omnivamp properly through _lookup table.
- implement how much shield is give, but shield is not built yet, so skip this one for now.
- heal have a little problem: overheal. => fixed
- why can't wound show a number?


=== implement new hero ===
Verity - do direct damage (like auto attack does) 4 time in sequence. choose weakest enemy in 2 hex of current target.
While using skill become untargetable.
weakest enemy = lowest effective health pool
- Implement new template action, "DirectDamage"
- Implement new status, "Untargetable"

Nasus - choose 4 nearest enemy, debuff them, and buff self for that amount, last 8 sec

Solace's 3rd cast

=== other ===
I don't sure. BUT I think the attack dash animation got override by walk animation. in Vharn scene. 

Could we also make the text in HeroPanel scale as the screen size grow.

The sprite order layer is so random, I want to have a code organize it later.
e.g. order = {hero = 0, item = 1, skill = 2, etc...}

when hero count was over the limit, the player can still start the battle which is not intented.

trip .uxml and .uss comment.

# Not implement yet
## hero
Garen - can walk while using his skill
- implement can walk while casting *for some skill

Poppy - implement "Recieving Projectile" and Shield
- receiving Projectile sound dump, let skip it for now.

Shen - shield 2 lowest ally. OnExpired, shield burst into AOE, gain second shield.
- implement aim on lowest ally.

Lumen's chakram

Ashe - shoot 8 arrow in a cone at once.
- add to "Fire timing", "shoot at once"

Kaisa - shoot 15 homing projectile in sequence. at 4 nearest enemies

## test
write a MagicSchool.Combat.Tests asmdef.
- not optional anymore if we test FindEnemy: it's internal, so nothing outside Combat can call it.

when there's 2 clustered of enemies, the cluster target is always set to the same target as previous one, never set to the second one. 
The ClusteredCircle still have problem, not sure about other clustered.

## refactor
internal for variable - a ton of work
- skip it for now

Modifer part are some of the most confusing, since the name don't explain itself, we have to look into it later.

## other
2. _lookup missing AP and Omnivamp — both read as statuses and silently do nothing.



# Note

