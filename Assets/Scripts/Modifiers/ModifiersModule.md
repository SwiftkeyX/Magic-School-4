# MagicSchool.Modifiers

Buffs, debuffs and statuses: `ModifierResolver` counts them down and works out a stat's final value.

References **Contracts, StatScaling** (a `StatModifier` holds its own `StatRatio` list). Referenced by **Skills, Combat,
Items**. The resolver lives here, not next to `Stat` in Combat, so a trait or item system can resolve "+20 armour"
without taking Combat along.
