# Items

`Items/` holds `ItemDataSO` (authored data, `Data/Items/`), `Item` (the world-space MonoBehaviour) and `ItemDrop`. An
item's effect is a modifier built in code: `Items/Library/Build/<Item>.cs`, mapped from `ItemIdEnum` by `ItemLibrary`;
`RegistryTests` checks every id builds. `Data/Items/default.asset` is `BaseItem.prefab`'s placeholder data, not an item.

## Simple items (what the shop sells)

While heroes and marks are being balanced, items are kept plain: each gives one stat, in three rarities, so a win-rate
change can only come from heroes or marks (Notion: Power level -> Takeaway 10). They are `StatItems`
(`Library/Build/StatItems.cs`, one table; ItemLibrary adds it), ids 31-51, assets in `Data/Items/Simple/`. Rare is about
what the old item of that stat gave; Common is 0.5x, Epic 1.75x.

| Item | Common / Rare / Epic | Price |
|---|---|---|
| Whetstone | +20 / 40 / 70% of base Atk | 2 / 3 / 5 |
| Archmage Tome | +20 / 40 / 70 AP | |
| Iron Plate | +15 / 30 / 50 DF | |
| Oaken Charm | +150 / 300 / 500 HP | |
| Hunter's Cord | +0.15 / 0.30 / 0.50 Attack Speed | |
| Starwell Orb | +1 / 2 / 3.5 Mana per second | |
| Lucky Coin | +10 / 20 / 35% Crit Chance | |

`GameManager._shopItems` holds only these 21. The older items (ids 1-30) still build and stay in their assets, but are
not sold; enemy comps and the non-prog team comps still use them. The prog comps wear the Rare versions, and Blacksmith's
forge table gives Common, Rare, Epic by her Anvil count.

## Wearing

- Dropping an item on a hero wears it. A hero wears one item (`HeroItems.Capacity`); it gets more by sharing: an ally
  standing on its wearing hex lends it the item that ally wears (only its own, so sharing never chains).
- Wearing and taking off go through `PlayerActions.WearItem` / `TakeItemOffHero`, so aligning is worked out again.
- Forged items (Blacksmith) are worn past capacity and last one fight, see [skills.md](skills.md).

## Items on hexes

Aligning's second filler, because five heroes cannot fill many marks. In Preparation an item dropped on a free
deploy-zone hex lies there (`Dragging.DropItem` -> `PlayerActions.PutItemOnHex` -> `BattleBoard.Items`, a `HexItems`). It
takes the hex (no hero can be placed on it) but does not block walking. A marked hex whose `AlignHex.isWearing` is ticked
is a wearing hex (per hex, not per bonus; set by right-clicking it in the Align Editor): `AlignBonuses.Refresh` gives the
mark's owner the modifier of an item lying on one, through the same give-and-take-back list as every other bonus.

An item is not an ally: it gives no ally bonus and never triggers the self bonus, which keeps hero pairs mattering. Two
heroes whose wearing hexes overlap both wear the item (left alone for now). Items keep their hexes between stages unless
the next stage's board differs; then `PreparationState.ReturnItems` moves them to the loose row. `HexItems` only knows
`IEquipment`, so `Combat` does not reference `Items`. No hero has a wearing hex yet.
