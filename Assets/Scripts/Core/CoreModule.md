# MagicSchool.Core

The composition root. `GameManager` builds every service in `Awake()` and wires the panels through Contracts
interfaces; `GameStateMachine` runs Preparation -> Combat -> Result (`States/`).

References **Contracts, Engine, Skills, Combat, CombatRecording, Items, Economy**. Referenced by **Player**,
**DevTools** and **Editor**.

- `GameManager` - builds and wires everything; holds the run (stage, winner, team size), speed and pause.
- `PlayerActions` - what the player does to the team and items (move, sell, wear, combine), each followed by aligning.
- `PhaseRequests` - what the player asked to happen next (start the fight, continue, restart, skip), read by the states.
- `FightRules`, `TeamCompLoader` - fight start and end, and loading a saved team comp: shared by the game and the dev tools.
- `HeroSpawner` - the one place a hero is built; subscribes the combat recorder and the spawn/forge events.
- `HeroMover`, `HeroFormation`, `HeroSeed`, `HeroUpgrader` - moving, remembering and seeding teams, star-ups.
- `IStageEditing` - all Core and Player know of the Stage Editor, which lives in DevTools.

`GamePhaseEnum` lives here, not in Contracts: Combat only needed one bit of it, carried by `BattleBoard.IsBattleOn`.
