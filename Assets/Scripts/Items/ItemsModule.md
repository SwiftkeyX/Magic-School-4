# MagicSchool.Items

An item the player can pick up, look at, and (later) give to a hero.

References **Contracts** only.

That is the whole point of the module. An item's effect is a modifier - "+20 armour" - and
`Modifiers` is deliberately separate from `Skills` so that items, traits and augments can grant
one without dragging `TemplateAction`, `SkillDefinition` and every hero builder along with them.
When items start granting their effects this module takes a reference to `Modifiers`, and nothing
else.

It does **not** reference `Combat`. An item does not know what a hero is. Equipping, when it
exists, is a thing done *to* an item by something that already knows both - the same way
`HeroMover` moves heroes without `Hero` knowing about the mover.

- `ItemDataSO` - the authored data: name, description, sprite. What `HeroDataSO` is to a hero.
- `Item` - the MonoBehaviour standing in the world. Glue, like `Hero`: it holds its data and
  answers `IInspectableItem`, and has no logic of its own.
