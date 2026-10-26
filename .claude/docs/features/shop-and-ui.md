# Shop, gold and UI

## UI Toolkit panels

UI is **UI Toolkit**, not uGUI (`Assets/UI Toolkit/`). One shared `UIDocument` on `MainScreen.uxml`, whose named
containers are slots. Each panel is its own `.uxml` plus a small controller in `UI/Panel/` (see
`ShopPanelController.cs`) that clones its UXML into the slot of the same name at runtime: the controller finds the slot
by reading its own root element's name, so no string field has to be kept in sync. Built: Shop, Hero (the inspector,
`InspectorController`), Hero Count, Phase Hint, Result Banner, Scoreboard, Reward. `#TraitPanel` is still empty.
World-space bars (health, mana, skill) are in `UI/WorldBar/`.

The reward card (`RewardPanelController`, an item pick after each win) is **unwired, not deleted**:
`GameManager._useRewardPanel` is off, so `GameManager.Reward` is null and every use is null-safe. The panel's own
`_showOnStart` is off too. Tick both to bring it back.

## Bench and dragging

The bench is deliberately **not** a UI Toolkit panel: it needs real `Hero` GameObjects (animated sprites), which a
`VisualElement` cannot host, so it is world-space (`Combat/Placements/Bench/`, `Assets/Prefabs/Bench/`) and the
`#BenchPanel` slot stays empty. `Player/Dragging` drags a hero or an item with Physics2D, or a click (press and release
within 10 px) keeps it in hand until the next press puts it down; Esc puts it back where it came from (an item to the
hero that wore it, the hex it lay on, or its loose spot).

## Gold and the shop (`Economy`)

`Wallet` is the player's gold, `Shop` what each slot sells, buying and refreshing. `ShopPanelController` only draws and
turns a drag into "buy slot i": it reads everything through `IShop` (Contracts), handed over via `IShopPanel.BindShop`,
because `UI` may not reference `Economy` or `Core`. A hero's price comes from its tier (`HeroPrice.Of`); `Shop.TryBuy`
spends it, `Seller.Sell` refunds it, Refresh costs `_refreshCost`, and `ResultState` pays `_stageIncome` after every
fight. The roster (`_shopRoster`, `_shopItems`) and numbers are on `GameManager` in the Inspector.

- A slot holds an `IShopEntry` (`Economy/ShopEntry.cs`): a `HeroEntry` delivers to the bench (and fails when it is full,
  so nothing is charged); an `ItemEntry` drops a world-space `Item` through `ItemDrop`, the first free place along the
  loose row. An item's price is `ItemDataSO._price`.
- Refresh rolls each slot by `ShopOddsSO` (`Assets/Data/Shop/ShopOdds.asset`, `GameManager._shopOdds`): item-or-hero by
  that slot's `_itemChanceBySlot` (`{0,0,0,100,100}`: slots 1-3 heroes, 4-5 items), then a hero tier by the stage's row
  of `_tierOdds` (only tiers that have heroes count), then any hero of that tier. Repeats are allowed; there is no
  shared pool. `PreparationState.OnEnter` calls `Shop.Restock()` (free) every stage, unless the player clicked Lock
  (`IShop.ToggleLock`). Refresh still works while locked.

## Buying and selling by drag

- **Hero slot**: pressing it hands the player the hero itself, unbought (`IShop.TryPickUp` -> `Shop.OnHeroPickedUp` ->
  `GameManager.OnShopHeroPickedUp`, built by `HeroSpawner.SpawnUnboughtHero`: on no placement, so untracked, no aligning,
  no combine). `Dragging` holds it like a hero off the board, so `AlignHighlight` shows its marks. Putting it down buys
  it (`GameManager.BuyShopHero` -> `Shop.TryBuyPickedUp`): on the free placement under the pointer if it may stand
  there (mid-fight a bench slot only), else the first free bench slot; then copies combine. Dropping it back on the
  shop, Esc, or a failed buy (re-rolled, no gold, bench full) destroys it (`ReturnShopHero`) and nothing is paid.
- **Item slot**: still a UI ghost. Drag it outside the shop panel to buy, drop it back inside to cancel
  (`ShopPanelController.ResolveDrop`).
- **Selling**: drop a held world-space hero onto the shop (`Dragging.DropHero` -> `GameManager.Sell` -> `Seller`), in
  Preparation only. A loose item sells for its full price, in any phase. Because the two drags live in different
  coordinate spaces and `Player` may not reference `UI`, the shop answers `ISellZone.IsInSellBoundary(screenPosition)`
  rather than handing out its rect; it owns the screen-to-panel conversion, y-flip included. A drop only sells when no
  `IPlacement` is under the pointer, since the bench sits directly above the shop. Heroes and items both sell through
  `ISellable`; `Seller` only knows `ICombatant` for a hero's extra cleanup.
- **Inspecting a slot**: right-click (`ShopPanelController.MakeSlotsInspectable` -> `IShop.InspectableAt`). It shows a
  stand-in, `ItemPreview` (Economy) or `HeroPreview` (Combat; it builds the skill through `SkillLibrary` with no caster,
  which only builds the text). `Economy` cannot reference `Skills`, so `GameManager` hands the shop the `HeroPreview`
  factory. The preview's `IsAlive` is "the slot still sells this", so buying or re-rolling closes the inspector.
  `UIDrag` only drags on the left button.

## Controls not built yet

There is no "Start Battle" button: the space bar starts combat (`PlayerController.TryStartCombat`, temporary). In a
fight the same key pauses (`GameManager.TogglePause` -> `GameSpeed.SetPaused`, `Time.timeScale` 0; a speed picked while
paused is kept); `CombatState.OnExit` always unpauses.
