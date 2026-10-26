# MagicSchool.UI

UI Toolkit panels (`Panel/`: shop, inspector, hero count, phase hint, result banner, scoreboard, reward) and world-space
bars over heroes (`WorldBar/`).

References **Contracts, Combat, Items, Input**, UnityEngine.UI. Referenced by nothing. It never sees `Economy` or `Core`:
the shop panel reads the shop through `IShop`, handed to it by `GameManager` (`IShopPanel.BindShop`). Each panel finds
its slot in `MainScreen.uxml` by its own root element's name.
