# Defensive option

What does defensive option look like?

- Generator do something, then Tank get the payoff.
  e.g. when the generator do damage, lifesteal part of that to the payoff tank.

Each option should feel distinct which mean each option should have the generator do something different, and get different payoff.

## Payoff option

The payoff should make the tank stronger. The stronger path revolve around become tankier, more utility, and special mechanic.

1. How can tank get tankier?

- Tank gain more HP, DF
- Tank is healed
- Tank is shielded

2. How can tank provide utility?

- Status: wound, stun
- Heal/Shield other
- Redirect damage from ally to itself

3. How can tank be more special?

- implement new mechanic to a tank, so we have more trick to use e.g. rush, stall, spike, etc...
  - rush(10): before 10 sec, tank is stronger
  - stall(10): after 10 sec, tank is stronger
  - spike: enemy damage a hero with spiked also get damaged. let some tank scale off number of spike he have.

## Generator option

How can generator do to generate something?
when the generator do { attack/ was attacked/ cast/ walk/ alive/ dead/ kill/ etc... }, give payoff bonus

1. attack:

- count damage dealt
  => 10% damage dealt heal the tank
  => first 1000 damage, 100% of damage dealt heal the tank
- count number of attack
  => each attack, heal the tank
  => every 100 attack, do something crazy that make team tankier e.g. everyone become invulnerable for 5 sec.

2. was attacked:

- was damaged
  => 10% damage dealt to generator, heal the tank
  => the 30% of damage at generator redirect to tank instead. suit generator that was a melee that need tankiness.
- count number of time generator was attacked
  => ...

3. cast:

- count number of cast
  => every cast, heal tank
  => rush(10) a tank become stronger: every cast, the rush duration increase.

4. kill/dead:

- when a hero was killed by a generator
  => stall(100): on kill, stall is decreased by 40 sec.
- when a generator is dead
  => when generator is dead, trigger rush(10) a tank become stronger

5. type matched (proposed)

- when a generator was [fire] hero, tank gain more max HP.

**NOTE: the game maybe too complicated with type system, let see first if the current synergy complex enough**

## Give Identity to each defensive option

A identity make each tank feel distinct and fun.
if attack speed build pair well with every tank, then attack speed have no identity, the attack speed should only be pair with some specific tank only.

1. count damage dealt

- the more damage the generator does, the better. suit for heavy damage type of team.

2. count number of auto-attack

- the more AS the generator does, the better. suit for AS type of team.

3. 10% damage dealt to generator, heal the tank

- A team that the generator was a frontline, so the payoff is easier to get.

4. the 30% of damage at generator redirect to tank instead

- this suit the generator that want tankiness.

5. count number of cast

- the more faster the generater cast, the better. suit for spammer type of team.

6. ...
