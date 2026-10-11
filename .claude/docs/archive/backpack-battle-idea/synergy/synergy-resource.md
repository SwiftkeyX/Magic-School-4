# Synergy resource: how Backpack Battles does it

The example the rules in `synergy-rule.md` are built on. Every Backpack Battles name used there is
explained here.

## The example

**Big Bowl of Treats** is a strong item people build around. It makes Food trigger 25% faster and gives
Pets a 20% chance to activate twice. So it belongs to two synergies at once: Food and Pet.
It is a special item, not an ordinary one - that is why it is allowed to be this strong.

Each synergy has sub-synergies it can go into:

- Food synergy
  - **Pine Protector** heals and generates Spike.
    - **Hungry Blade** and **Thornwhip** are weapons that scale off Spike.
  - **Blueberries** generate Luck.
    - **Rapier** is a weapon that scales off Luck.
  - **Banana** generates Heat.
- Pet synergy
  - **Goobert** counts item activations - the more, the better he is.
    - Blueberries activate fast, which Goobert likes.

A core build could be: Big Bowl of Treats + Pine Protector + Rapier + Goobert + Blueberries.

## What to take from it

- A synergy is a family (Food, Pet), not one closed group. Inside it are several small chains.
- A chain is: something generates a resource (Pine Protector -> Spike), something else scales off it
  (Hungry Blade).
- The defensive piece (Pine Protector heals) is separate from the offensive piece (the weapon), but the
  resource it makes decides which offence fits it best.
- One piece can serve two chains (Blueberries: Luck for Rapier, fast activation for Goobert).
- One strong piece sits on top and lifts a whole synergy, or two (Big Bowl of Treats). It is special,
  which is what lets it be that strong.
- A build picks pieces from several chains. It is not "all of one synergy".
