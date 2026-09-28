# Magic School 3

A Unity auto-chess game. Buy heroes, place them on a hex board, then watch them fight on
their own.

![A fight in progress](docs/battle.gif)

Built with **Unity 6000.4** (URP, 2D) and **UI Toolkit**.

> Work in progress. Combat, movement and skills run; the economy around them does not yet —
> see [What works today](#what-works-today). Heroes are placeholder shapes: the systems came
> first, the art has not.

## Installing

### Getting the project

- **Git clone:** `git clone https://github.com/SwiftkeyX/Magic-School-3.git`
- **GitHub download:** click the green `Code` button and select 'Download ZIP'

The repository uses no Git LFS, so the ZIP is complete — there are no large files to miss.

### Requirements

- **Unity 6000.4.9f1**, installed through [Unity Hub](https://unity.com/download).

### Opening the project for the first time

1. In Unity Hub, click **Add** and select the project's root folder.
2. Open it. The first import takes minutes rather than seconds, while Unity builds `Library/`
   from nothing. It is cached afterwards.
3. Open `Assets/Scenes/Board.unity`.
4. Press **Play**.

### If a package fails to resolve

One dependency does not come from the Unity registry:

```
"com.coplaydev.coplay": "https://github.com/CoplayDev/unity-plugin.git#beta"
```

It is an editor-side assistant plugin, and the game does not use it. If Package Manager cannot fetch it,
delete that line from `Packages/manifest.json` and reopen the project.

## Running it

| Input | Does |
| --- | --- |
| Left-click | Pick item/hero up |
| Right-click | Open the inspector on it |
| `Space` | Start the fight, and continue to the next stage |
| `R` | Quick restart the current stage |
| `1` `2` `3` | Run the game at x1, x2 or x3 speed |

## What works today

**Running:**

- **Movement and pathfinding.** hero move on a hex toward a nearest enemy using A\* algorithm on the hex grid, with hex reservation so two heroes
  cannot commit to the same tile in the same frame.
- **Combat.** Auto-attack on an attack-speed cooldown, passive skill when condition is true, and a skill cast when mana caps.
- **Damage, healing, modifiers and statuses** they can be apply to a hero.
- **Skills.** A skill is a list of steps, each playing a template action — projectile, AoE or
  hitbox — picked by condition. **24 heroes exist; 17 of them have skills built.**
- **Item.** item wear to a hero, grant hero modifiers.

**Not built yet:**

- **Gold and economy.** The Shop resolves buy-versus-cancel when you release a drag, but it
  cannot charge for the purchase.
- **Trait panel.** Still an empty slot in the main screen layout.

## How the code is laid out

The code is split into 12 modules along `.asmdef` boundaries, so the dependency direction is
enforced by the compiler — see **[ARCHITECTURE.md](ARCHITECTURE.md)**
for the dependency graph and what each module does.

## License

No license file yet, which means all rights are reserved by default.
