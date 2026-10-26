# MagicSchool.Skills

A hero's skill: `SkillDefinition` + `SkillFlow` of `SkillPart`s playing `TemplateAction`s (projectile / AoE / move /
cast). One script per hero in `Library/Build/`, mapped by `SkillLibrary`. See `.claude/docs/features/skills.md`.

References **Contracts, Engine, StatScaling, Modifiers**. Referenced by **Combat, Core, Editor**. It never names `Hero`:
effects go through `IEffectable`, and the caster through Contracts interfaces.
