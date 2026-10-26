# MagicSchool.Core

The composition root. `GameManager` builds every service in `Awake()` and wires the panels through Contracts
interfaces; `GameStateMachine` runs Preparation -> Combat -> Result (`States/`).

References **Contracts, Engine, Skills, Combat, CombatRecording, Items, Economy**. Referenced by **Player** and
**Editor**.

- `GameManager` - phase, winner, stage progression, and the player's actions (move, sell, wear, combine, aligning refresh).
- `HeroSpawner` - the one place a hero is built; subscribes the combat recorder and the spawn/forge events.
- `HeroMover`, `HeroFormation`, `HeroSeed`, `HeroUpgrader` - moving, remembering and seeding teams, star-ups.
- `Benchmark/`, `StageEditor` - dev tools (see `.claude/docs/features/benchmarks.md`, `dev-tools.md`).

`GamePhaseEnum` lives here, not in Contracts: Combat only needed one bit of it, carried by `BattleBoard.IsBattleOn`.
