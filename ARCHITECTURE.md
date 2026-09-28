# Architecture

Scripts live in `Assets/Scripts/`, split into assemblies so the dependency direction is
enforced by the compiler rather than by convention. **One `.asmdef` is one module**, and the
module is the unit of reuse: everything inside one is deeply coupled on purpose, so reusing
something means taking the entire module.

Namespaces subdivide further inside a module, but they are naming, not boundaries.

## Who depends on whom

Arrows point at what a module is allowed to reference.

```
MagicSchool.Contracts    -> (nothing)
MagicSchool.Engine       -> (nothing)
MagicSchool.Input        -> Unity.InputSystem
MagicSchool.VFX          -> Unity.TextMeshPro
MagicSchool.StatScaling  -> Contracts
MagicSchool.Modifiers    -> Contracts
MagicSchool.Skills       -> Contracts, Engine, StatScaling, Modifiers
MagicSchool.Combat       -> Contracts, Engine, Skills, VFX, Modifiers
MagicSchool.UI           -> Contracts, Combat, Items, Input, UnityEngine.UI
MagicSchool.Core         -> Contracts, Engine, Skills, Combat
MagicSchool.Player       -> Contracts, Combat, Core, Items, Input
MagicSchool.Editor       -> Core
MagicSchool.Combat.Tests -> Combat, Contracts, Skills
```

Each module also carries a `*Module.md` next to its `.asmdef` stating its own boundary.

Most of modules are top-level folder. But two of them do not get a top-level folder of their own: `MagicSchool.Editor` is Editor-only and
lives in `Core/Editor/`, and `MagicSchool.Combat.Tests` holds the EditMode tests and lives in
`Assets/Tests/`.

## The modules

**Contracts** — the interfaces and enums every other module talks through. The most
significant are:

- `ICombatant` — a unit that can fight on a board.
- `IEffectable` — a unit that can be affected by an attack or a skill.
- `IPlaceable` — a unit that can be placed on an `IPlacement`.

It's a leaf module, and must never reference another module, because that would easily lead to
a cyclic dependency.

**Core**

- controls the game state machine, e.g. preparation, combat, result.
- controls stage progression between them, and wires the other modules together.

**Input** — the only place a key or a mouse button is read. It is a leaf on Unity.InputSystem
and nothing of ours, because "did F1 go down" is a question that belongs to no one in
particular: the board drag asks it, the stage keys ask it, a dev tool that raises a panel asks
it. It used to live inside Player, which meant wanting a keypress cost you a reference to
Player, and so to Combat, Core and Items along with it.

**Player** — processes player input: dragging heroes, right-click to inspect, and start or restart a fight.

**Combat**

- two smaller modules that are deeply coupled, Hero and Placement, Hero need to ask Placement for other Hero whereabout,
and Placement need to know which Hero standing on it.
- controls hero behaviour through a state machine.
- constructs a graph-like hex grid that heroes fight on, using A\* to path across it.

**Skills**

- a hero's skill: small steps combined into one big skill.
- built from a list of steps played in order, each step playing a template action
  (projectile, AoE, Cast, etc.).
- apply the effect to hero; an effect could be damage, status, buff or debuff.
- see [A skill, step by step](#a-skill-step-by-step) below for a worked example.

**Modifiers**

- resolves modifiers, e.g. buffs, debuffs and statuses.
- counts down modifier timers.
- computes the final stat after combining modifiers.
- returns whether a status is active when asked.

**StatScaling** — how stat ratios and scaling are computed.

**VFX** — floating combat text.

**UI** — overlay panels (inspector, shop), and world-space UI (health bar, mana
bar, skill bar).

**Engine** — services that decouple some of the game from the Unity engine.

**Editor** — inspector tooling for Core.

**Combat.Tests** — EditMode tests for Combat, Contracts and Skills.

## A skill, step by step

Every skill decomposes into the same shape. Bulwark's *Guardian's Roar* is a
useful one to read because its two steps are triggered differently: the first by the cast, the
second by the first one **expiring**.

> Braces for 2 seconds, healing steadily and taking 25% less damage. The moment the brace ends
> he slams the ground for 120% AP to every enemy around him.

![Bulwark casting Guardian's Roar](docs/Bulwark%20Skill%20Demonstration.gif)

| Step | Trigger | Action | Aim | Recipient | Effect | Amount | Cadence | Duration |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | `OnCast` | `Cast` | `Self` | `Self` | Heal | 200% MG | every 0.5s | 2s |
| 1 | `OnCast` | `Cast` | `Self` | `Self` | Damage Reduction | 25% | once | 2s |
| 2 | `OnExpired` | `CircleAOE` | `Self` | `EnemiesInArea` | Damage | 120% MG | once | — |

Reading across a row: **when** does this fire, **what** does it spawn, **where** is it aimed,
**who** does it land on, and **what happens** to them. Reading down: a skill is just a list of
those rows.

That is the whole model that was built to make human readable and codable. A `SkillDefinition` holds `SkillStep`s;
each step holds `SkillActionGroup`s that pick a `TemplateAction` (e.g. projectile, AoE, Cast) based on `SkillCondition`;
each `TemplateAction` carries the effects it applies.

Read Bulwark's skill code at:
[`BulwarkSkill.cs`](Assets/Scripts/Skills/Library/Build/BulwarkSkill.cs): `Brace()`

### Where the schema came from

This schema is derived from TFT Set 9 & 10. Those were written out before this system existed —
**135 champions**, every cost tier, kit by kit — and that encoding was iterated until one
consistent set of columns covered all of them.

The result: 135 kits break down into **284 steps**. That is why the model looks the way it does.
`SkillDefinition` / `SkillStep` / `SkillActionGroup` / `TemplateAction` are the spreadsheet's
shape, in C#. The sheet is not documentation written afterwards; the sheet is the source the
system was derived from.

Read it here: [the design sheet](https://docs.google.com/spreadsheets/d/1PSSGZAq2gkkOxTENDWpChI_2OpXvTmQIfPxZebuuDsc/edit?usp=sharing).

Those 135 kits are a design corpus, not a feature list — encoding one in these columns is not
the same as the game running it. The skills built in C# are this project's own 17 heroes,
written in the shape the corpus produced.
