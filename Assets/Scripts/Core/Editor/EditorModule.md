# MagicSchool.Editor

Editor-only windows: Align Editor, Stage Editor, Skill Tuner, Benchmark, and what their buttons call.

References **Core, Combat, Skills, DevTools, Contracts, Items**. Referenced by nothing. It reaches fields of
`HeroDataSO`, `AlignMark` and `BattlePlacementSO` through `SerializedObject`: public ones by `nameof`, private ones by
string - renaming a private one breaks the window silently. Every window draws through `WindowStyle`.
