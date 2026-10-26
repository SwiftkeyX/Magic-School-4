# Skills

## The model

A skill is a `SkillDefinition` holding one `SkillFlow` for its active: `Flow(onStart, onHit, onExpired)`. Each of the
three slots holds one `SkillPart` (one piece of the skill, which picks a `TemplateAction`: projectile / AoE / move /
cast) or several that start together, `Together(a, b)`, where the first one leads. Nothing follows `onHit` /
`onExpired`; anything further is played from a hook on `SkillDefinition`: `OnAutoAttack`, `OnCombatStart`,
`OnHeroDied` (everyone else's death), `OnDeath` (the caster's own), `OnDamageTaken` (the caster's own hits, raised from
`Hero.TakeDamage`), or a part's own `OnSkillHit`. Effects go through `IEffectable`, so nothing in `Skills` knows the
`Hero` type. `SkillLibrary.Resolve` maps a `SkillIdEnum` to its builder; `RegistryTests` checks every id builds.

Every skill script in `Skills/Library/Build/` (41 of them) has the same shape: `Tuned()` numbers, then `SkillName` /
`Description`, then `BuildActiveFlow`. Copy a sibling. `SkillIdEnum` ints are frozen: never reuse a retired one.

`SkillDefinition.AutoAttackDamageMultiplier(target)` (1 by default) is multiplied into AD by
`HeroAttack.PerformAutoAttack` before the crit roll (Skeleton Archer).

## Descriptions show numbers

A description writes an amount through `SkillDefinition.AD()` / `AP()` / `DF()` / `MaxHP()` (e.g. `{AP(DamageRatio)}`),
never as `{X}% AP`. The inspector shows the number for that hero's current stats ("deals 310"); holding **Alt**
(`PlayerInputSystem.FormulaViewHeld`) shows the formula ("deals [200% AP]"). It goes through
`SkillDefinition.DescribeActive/DescribePassive(stats, formula)` - `IInspectableHero.SkillDescriptionFor(formula)`; a
shop preview uses its 1-star stats, a skill with no stats shows the formula. The inspector rewrites the text every frame
(only when it changed). A percent that is a buff, not an amount (Harpy's "+X% AD every cast", attack speed buffs),
stays written as a percent.

## Live tuning (dev tool)

A skill script writes each number as `private static float DamageRatio => Tuned(200f);` (never `const`: a const cannot
change at runtime); a number worked out from others is a plain `=> A * B` property. The value in the code is the
default; `Skills/SkillTuning` holds overrides keyed `"ElfSkill.DamageRatio"` (file name + member, from caller info).
**Magic School 4 > Skill Tuner** (`Core/Editor/SkillTunerWindow`) lists every skill's numbers (by reflection over each
skill class's static float/int properties), edits them, and **Write to code** puts a value back into the script's
`Tuned(...)` (in Play mode the recompile waits until Play ends). A skill reads its numbers when built, so
`CombatState.RebuildTunedSkills` rebuilds, at every fight start, the skill of any hero built before the last change
(`Hero.IsSkillOutdated` / `RebuildSkill`). The window keeps unwritten changes across Play mode but not across closing
it. The name is `Tuned`, not `Tune`, because `SkillFactory.Tune(castTime: ...)` already exists.

## Units spawned mid-fight

A skill calls `ISpawner.SpawnUnitsNearby` (Contracts) on its caster, `Hero` picks the nearest free hexes
(`Combat/Heroes/HeroSpawn`) and raises `OnSpawnRequested`, and `HeroSpawner`, which subscribes to every hero it builds,
spawns them. The unit comes from `HeroDataSO._spawnUnit`, because `Skills` cannot name a hero.

## Skills that read special marks

A skill can name marks it reads (`SkillDefinition.SpecialMarks`), see [aligning.md](aligning.md). Per hero:

- **Rock Golem**: his wards are the allies on his `"Ward"` mark.
- **Skeleton Archer**: the `"Long Shot"` bonus and his close-shot nerf (50% under 3 hexes) apply to his auto attacks too,
  through `AutoAttackDamageMultiplier`.
- **Knight**: his landing deals 100% AD plus 50% AD per ally on his Valor mark (`AmplifiedBy`, as a share of the base).
- **Blacksmith**: forges one item every cast for the nearest ally wearing nothing (anywhere, not only on the mark) whose
  role her forge table has a row for. The table is `HeroDataSO._forgeTable`, one `ForgeRow` per role, items from tier 0
  up (typed `ScriptableObject`, as Combat cannot name `ItemDataSO`); past the end a row stays on its last item, and a
  role with no row gets nothing. The tier is allies on her Anvil mark / `MarksPerTier` (1). Default rows: Tank: Iron
  Plate +20 DF, Tempered +30, Reinforced +40, Forged +50; Mage: Archmage Tome; Fighter / Marksman / Assassin: Whetstone.
  With every ally wearing something she takes the tank row's item herself, while she wears fewer than 3. The skill calls
  `IItemWearer.ReceiveForgedItem(role, tier, forger)` (`IForger.CanForgeFor` asks the table first), `Hero` raises
  `OnForgedItemRequested`, and `HeroSpawner` spawns `HeroDataSO.ForgedItem(role, tier)` and calls `Hero.WearForged`:
  worn past `HeroItems.Capacity`, destroyed by `HeroItems.ClearForged` in `Hero.ResetForNewStage`. The forged plates
  (`ForgedIronPlate` 21, `MasterworkIronPlate` 22, `TemperedIronPlate` 29, `ReinforcedIronPlate` 30) are not in the shop.
  She also gains 2% of her max HP as HP/s, plus 0.3% per ally on her Anvil mark, every cast, stacking for 3 casts
  (`MaxRegenStacks`, reset in `OnCombatStart`).
- **Troll**: grows only from kills by the allies on his Feed mark. A dying unit records who landed the killing hit
  (`IFallen.KilledBy`), and a unit a hero spawned mid-fight is a summon (`IFallen.IsSummon`, set by `HeroSpawner`),
  whose death never feeds him.
- **Reaper**: every 3 enemies she kills herself (summons excluded), each ally on her Reap mark casts its skill once for
  free: `IFreeCaster.GrantFreeCast` queues the cast, `HeroStateMachine` plays it when the hero is not casting or
  stunned and its mana is not full, and `HeroCast` then leaves the mana alone. Grants reset at fight start. She also
  revives once a fight: her `OnDeath` calls `IRevivable.ReviveAfter(3s, 30%)`; meanwhile she is an ordinary death
  (untargetable, death hooks fire, keeps her hex), `HeroDead.OnUpdate` counts down and `Hero.Revive` stands her up.
  A hero that `IsReviving` still counts as standing in `CombatState` and `TeamCompBenchmark`.
