# MagicSchool.DevTools

The developer tools that run inside the game: the Hero Benchmark and Team Comp Benchmark (`Benchmark/`) and the
Stage Editor (`StageEditor`). See `.claude/docs/features/benchmarks.md` and `dev-tools.md`.

References **Core** and everything Core references. Referenced by **Editor** only.

It is left out of release builds (`defineConstraints: UNITY_EDITOR || DEVELOPMENT_BUILD`), so nothing that ships may
reference it. Core knows the Stage Editor only as `IStageEditing` (what Player asks of it); `StageEditor.Register`
builds it at start and hands it to `GameManager.RegisterStageEditor`. DevTools reaches Core's internals through
`InternalsVisibleTo` in `Core/AssemblyInfo.cs`.
