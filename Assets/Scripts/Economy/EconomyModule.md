# MagicSchool.Economy

Gold in and gold out: the player's wallet, the shop that spends it, and the seller that refunds it.

References **Contracts**, **Combat** and **Items**.

`Combat` and `Items` are there for one reason each: a shop slot has to hand over what was bought. A
hero goes to the `Bench` (`HeroEntry`), an item drops into the world (`ItemEntry` → `ItemDrop`), and
the roster is authored as `HeroDataSO` / `ItemDataSO`. Nothing else in the module knows either type -
`Shop` only sees `IShopEntry`, and `Seller` sells an `ISellable` and knows a hero only as an
`ICombatant`, for the extra cleanup a hero standing on a placement needs.

It is **not** referenced by `UI`. The shop panel reads the shop through `IShop` (Contracts), which
`GameManager` hands it, so the panel never sees `Shop` itself. `Core` is the only module that builds
these classes - `GameManager` owns the one `Wallet`, `Shop` and `Seller`, and `PreparationState`
restocks the shop each stage.

The folder is `Economy/` rather than `Shop/` for the reason `StatScaling/` is not `Scaling/`: a
namespace `MagicSchool.Shop` holding a class `Shop` cannot be used unqualified.

- `Wallet` - the player's gold (`IWallet`).
- `Shop` - what each slot sells, buying, refreshing, restocking and Lock (`IShop`).
- `ShopEntry` - `IShopEntry` and its two kinds, `HeroEntry` and `ItemEntry`: a name, a price, and
  "hand it over".
- `ShopOddsSO` - the roll odds: each slot's item chance, and the hero tier odds by stage.
- `Seller` - selling a hero or an item: refund its price, take it out of the game.
