# MagicSchool.Combat

Heroes and the board they fight on, in one assembly on purpose: a hero needs its hex and a hex's board tracks its
occupant. Splitting `Heroes/` and `Placements/` was tried and does not work. `Align/` (aligning) is a folder here, not
its own module, for the same reason.

References **Contracts, Engine, Skills, VFX, Modifiers, StatScaling**. Referenced by **UI, Economy, Core, Player, Editor**.

It references `Modifiers` for one thing: `Stat` holds a `ModifierResolver`. It never references `Items` or
`CombatRecording`: items are `IEquipment` / typed `ScriptableObject` here, and a hero only raises `DamageEvent` /
`HealEvent` for whoever listens.
