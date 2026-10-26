# Aligning

The game's synergy, in place of traits: a hero marks some hexes around itself, and an ally standing on a marked hex gets
a bonus. Code in `Combat/Align/` (`MagicSchool.Combat.Align`), a folder of `Combat`, not its own assembly: `HeroDataSO`
holds the marks while `Aligning` needs `Hero`, `Hex` and `BattleBoard`, so a separate module would sit both under and
over `Combat`. Player team only: an enemy's marks do nothing.

## Data

- Marks are data, `HeroDataSO._alignPatterns`: a hero has one `AlignPattern` or more, each a full set of marks authored
  by hand. A pattern need not be the one before it turned; it can mark other hexes, but its bonuses are the hero's
  (orange and green pay the same in every pattern; the Align Editor copies an edit to every pattern).
- In Preparation, right-clicking while holding a player hero turns it to its next pattern, and does not inspect
  (`GameManager.RotateAlign` -> `Hero.RotateAlignMark`, then aligning is refreshed). A hero with one pattern cannot turn.
  The pattern lives on the `Hero`, so it is kept between stages, and team comps save it (`HeroPlacement.alignPattern`).
- One `AlignMark` is one bonus and every hex that gives it: `hexes`, a list of `AlignHex` - direction, and `distance`
  (hexes away in a straight line, 1 = adjacent; heroes between do not block it; a missing hex on the way ends it). A hex
  off the straight lines adds a turn, `thenDirection` / `thenDistance` (0 = none), and the two legs are tried in either
  order so a board edge does not lose it.
- Then modifier, amount, and `percentOf`: None for a flat amount, else a percent of the receiver's own base stat.
  `perOwnerAP` makes the ally amount per 100 of the owner's AP (Ettin's green: 25% of the ally's base AS per 100 AP;
  `AlignMark.AllyAmount`). An amount can be negative.
- A mark can pay its owner: `selfModifier` / `selfAmount` / `selfPercentOf`, once for every one of its hexes an ally
  stands on. `selfAmount` 0 means none. `selfWhenEmpty` flips it: once for every one of its hexes that is in the deploy
  zone and nobody stands on (an item lying there does not count as someone).
- `healFromAllyDamage`: an ally on one of the mark's hexes heals the owner that percent of every damage it lands. Wired in
  `AlignBonuses` to `Hero.OnDamageDealt` and cut on the next `Refresh`. Shield Knight has it at 10% on his N/S mark.
- `AlignHex.isWearing` makes a hex a wearing hex (see [items.md](items.md)).

## Mark colours are fixed by position

A pattern's `marks` are:

- `[0]` **orange**: pays only its self bonus (per ally on it, per empty hex with `selfWhenEmpty`, or the
  `healFromAllyDamage` link);
- `[1]` **green**: pays only its ally bonus;
- then the **white** special marks, which pay nobody. A third ordinary mark does nothing (`AligningHex.ColourOf`).

One hex can be in several marks (Elf's and Shield Knight's are orange and green both). The board draws a hex in several
as rings (`Hex.SetColors`: ring sprites made on demand, order 0 nudged toward the camera so they stay under heroes at
order 1; purple outermost, then orange, green, white). A wearing hex in two marks is still worn once. The inspector
writes one line per colour under ALIGNING (`IInspectableHero.AlignDescriptionFor(formula)`, written by `AlignText`; with
Alt held a bonus that scales with AP shows its formula).

## Special marks

A skill can name marks it reads (`SkillDefinition.SpecialMarks`, e.g. Rock Golem's `"Ward"`, Skeleton Archer's
`"Long Shot"`). A special mark is an `AlignMark` with `special` set to that name. The Align Editor puts it in every
pattern by itself, after orange and green (it asks `HeroDataSO.SpecialMarksOf`, which builds the skill), and will not
remove it; its hexes are painted separately in each pattern. `AlignBonuses.Refresh` records who stands on each one and the
skill reads them through `ISkillCaster.AlliesOnSpecialMark(name)`. The board paints them white (`BoardColours._specialColor`).
What each skill does with its mark is in [skills.md](skills.md).

## Working it out

`AlignBonuses.Refresh` (owned by `GameManager`, `RefreshAligning`) takes back every bonus it gave and gives them again for
where everyone stands now: after every `GameManager.MoveHero` and `Sell`, when a stage opens, and once more in
`CombatState.OnEnter`, so the inspector's numbers follow the placing. The bonuses are ordinary rest-of-fight modifiers,
fixed by where everyone stood when the fight started; `Aligning` only answers which hexes are marked.

## Showing marks

While the player holds one of their heroes in Preparation, `Player/AlignHighlight` (ticked after `Dragging`) tints the
zone hexes the held hero would mark from the hex under the pointer: orange, green, white as above, and its wearing hexes
purple, which wins over the bonus colour (`AligningHex.MarkedHexesByBonus` / `WearingHexes`,
`BoardColours.ShowAlignMarks`). Holding nothing, hovering a placed player hero shows its marks from its hex (a bench hero
or an enemy shows nothing). While an item is held the board lights up the team's wearing hexes in the same purple.

## Align Editor

**Magic School 4 > Align Editor** (`Core/Editor/AlignEditorWindow`, a partial class over `.Patterns` / `.Hexes` /
`.Marks`, hex maths in `AlignHexOffset`) edits a hero's marks by clicking hexes instead of typing directions: heroes on
the left, the hexes within 6 in the middle (every one can be marked; the window works out the turn), one box per mark on
the right, and pattern tabs above (`+ Rotated copy` drafts a new pattern as the selected one turned 60 degrees
clockwise; a hero with no pattern shows a Start button, so looking never writes). It shows exactly the orange / green /
special boxes, each with only its own fields, paints a click into the selected box only, and right-clicking a hex toggles
`isWearing`. It works through `SerializedObject` on the `HeroDataSO`, outside Play mode. Its "Add to bench" button puts
the hero on the bench (Play mode).

## Team design (in progress)

Shop heroes were designed as three teams of three in the design sheet's `Aligning` tab (Ettin / Dryad / Elf, Orc
Blademaster / Werewolf / Shield Knight, Skeleton Archer / Reaper / Centaur). They are being redesigned as three teams of
five that share heroes (each carry - Elf, Werewolf, Skeleton Archer - in one team, every other hero in two). Only the
Elf team is done, and the assets are ahead of the sheet: a spread fan where every mark is 2 hexes away - Dryad 2 N of
Elf, Reaper 2 S, Ettin 2 NE, Orc 2 SE (Elf N/S, Ettin SW/NW, Orc NW/SW, Dryad SE/S, Reaper N/NE). It needs 5 rows by 3
columns, which the `Wide Square Layout`'s deploy zone has. The other two teams no longer line up until their turn.
