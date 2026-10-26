# MagicSchool.Editor

Editor-only windows: Align Editor, Stage Editor, Skill Tuner, Benchmark, and what their buttons call.

References **Core, Combat, Skills, DevTools**. Referenced by nothing. It does not reference Contracts or Items, so it reaches
fields of `HeroDataSO`, `AlignMark` and `BattlePlacementSO` through `SerializedObject` by name - a rename there breaks
it silently (refactor plan, phase 9). Every window draws through `WindowStyle`.
