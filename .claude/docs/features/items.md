# Items

`Items/` holds `ItemDataSO` (authored data, `Data/Items/`), `Item` (the world-space MonoBehaviour) and `ItemDrop`. An
item's effect is a modifier built in code: `Items/Library/Build/<Item>.cs`, mapped from `ItemIdEnum` by `ItemLibrary`;
`RegistryTests` checks every id builds. `Data/Items/default.asset` is `BaseItem.prefab`'s placeholder data, not an item.

## Wearing

- Dropping an item on a hero wears it. A hero wears one item (`HeroItems.Capacity`); it gets more by sharing: an ally
  standing on its wearing hex lends it the item that ally wears (only its own, so sharing never chains).
- Wearing and taking off go through `GameManager.WearItem` / `TakeItemOffHero`, so aligning is worked out again.
- Forged items (Blacksmith) are worn past capacity and last one fight, see [skills.md](skills.md).

## Items on hexes

Aligning's second filler, because five heroes cannot fill many marks. In Preparation an item dropped on a free
deploy-zone hex lies there (`Dragging.DropItem` -> `GameManager.PutItemOnHex` -> `BattleBoard.Items`, a `HexItems`). It
takes the hex (no hero can be placed on it) but does not block walking. A marked hex whose `AlignHex.isWearing` is ticked
is a wearing hex (per hex, not per bonus; set by right-clicking it in the Align Editor): `AlignBonuses.Refresh` gives the
mark's owner the modifier of an item lying on one, through the same give-and-take-back list as every other bonus.

An item is not an ally: it gives no ally bonus and never triggers the self bonus, which keeps hero pairs mattering. Two
heroes whose wearing hexes overlap both wear the item (left alone for now). Items keep their hexes between stages unless
the next stage's board differs; then `PreparationState.ReturnItems` moves them to the loose row. `HexItems` only knows
`IEquipment`, so `Combat` does not reference `Items`. No hero has a wearing hex yet.
