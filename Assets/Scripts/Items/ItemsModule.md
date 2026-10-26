# MagicSchool.Items

An item the player can buy, wear, lay on a hex and sell.

References **Contracts, Modifiers, StatScaling**, Unity.TextMeshPro. Referenced by **UI, Economy, Core, Player**.

An item's effect is a modifier, which is why `Modifiers` is separate from `Skills`: an item grants "+20 armour" without
dragging every skill builder along. It does **not** reference `Combat`: an item does not know what a hero is. Wearing is
done *to* an item by something that knows both (`GameManager`, `HeroItems` through `IEquipment`).

- `ItemDataSO` - the authored data: id, name, description, sprite, price.
- `Item` - the MonoBehaviour in the world. Glue: holds its data, answers `IInspectableItem`.
- `ItemDrop` - where a new item lands. The shop (`Economy`) and the reward card (`UI`) both drop through it.
- `Library/` - `ItemLibrary` maps `ItemIdEnum` to a builder in `Library/Build/`, one small file per item.
