# 3/10/69

[x] Next
    [x] 1. Gold. This is the real blocker. Buying, selling and refreshing are all free right now, so nothing is a choice yet. It needs a gold counter, income per stage, a price per hero (by tier is the obvious rule), a refund on sell, and a cost on Refresh.
    [x] 2. Mixed slots. The shop slots only hold a HeroDataSO today. To roll items into the same slots, a slot needs to hold "something buyable" — hero or item — and buying an item needs somewhere to put it, since the bench is for heroes.
    [x] 3. Roll rules. Refresh currently picks uniformly from the whole roster and can repeat. It needs hero-vs-item odds and tier odds, and probably an automatic roll at the start of each stage.
    [x]4. Five slots. This is just the layout in Shop.uxml.

[] Shop follow-ups
    [x] 3 hero slots + 2 item slots, from your FIXLATER in ShopOddsSO. Instead of each slot having a 20% chance to be an item, slots 0–2 always roll heroes by tier and slots 3–4 always roll items. It's small: _itemChance goes away and Roll() picks by slot index. This also makes the shop more predictable for the player.
    [x] Lock: the dead "Lock" box in Shop.uxml. It would keep the current shop through the next stage's free restock.
    [] Code cleanups: merge HeroSeller and ItemSeller behind an ISellable, and move the shop into its own asmdef. These are structure changes only; nothing in the game changes.

[] Game loop gaps (from the plan of shop → board → fight → about 10 stages)
    [] Stages: Board.unity has 5 BattlePlacementSO stages, not 10. The stage 7+ odds row never applies until there are more.
    [] Start Battle button: combat still starts on the temporary space bar in PlayerController.TryStartCombat.
    [] Restart: it gives a free shop re-roll and leaves bought items lying in the world. You set this aside earlier as its own problem.
    [] Reward panel: you said it should go away eventually, now that the shop sells items.

