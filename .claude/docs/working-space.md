# 23/10/69

## Interrupt

[x] Hero always focus enemy above

1. The hero always focus enemy above. If there's enemy above and below, a hero will always focus the above one.
2. The hero always walk upward. The route to the enemy, the first step will be walk upward.

Those behaviour make the result less random which could mean "If this hero focus below enemy, this team would won" There's this type of case everywhere in the game. In the case you told me, a lot of team lose on stage 4 or 5, it could have won if the hero focus the below enemy, I know because I play it so many time.

[x] Re-factor the doc

- planning.md seem out of place.
- other doc does too.

[x] Improve benchmark test => [benchmark](design/benchmark.md)

[] Get rid of the magic resist stat. Now every hero use DF to resist magic.

## Benchmark todo

[x] team comp benchmark: I still don't know what it does. Let see. Okay, it just put each team-comp against the current 5 stage.

- [x] test each team comp against each other. => why it won't work? the troll team depend on a lot of enemy dead, so he don't have a same power when test a tanky team. There's a lot of nuiance like this that won't make this easy to test.
- [x] test each team comp against the test enemy.

The best we could do is to just test all current team to tested enemy team.

[x] The current choice are:

- test each team comp against each other.
  - why it won't work? the troll team depend on a lot of enemy dead, so he don't have a same power when test a tanky team. There's a lot of nuisance like this that won't make this easy to test.
- test each team comp against the test enemy.
  - I thought this is the most consistent way **right now**. Once we get more result, we could change the test after that.

[x] Current problem to team comp benchmark test

- Each stage was tested 3 time for each team. If the team won at least 2 out of 3, it count as "won".
  - [x] to few data. Maybe mark active test for 20 match each stage, and mark in-active 10 match per stage.
  - [x] The test is also not fast enough. How could we speed it up?
  - [x] If win at least 14 match, count as won.
- It also test the in-active aligned bonus. It actually quite useful, it gaugue how strong each team was with out the mark, which mean it was a pure each hero strenght.

[x] Add tooltip to google sheet for more understanding.

[x] Make benchmark tweakable

- [x] Isn't there like a caveat to adjusting speed value? Make it tweakable without any caveat mention sound scary to me.
- [x] Some option are more toward the advance option, user don't need to know it to use it. the -test and -speed should stay advance option. Make advance option a expandable.
- [x] Let AI name the commit.

[x] normal benchmark:

- [x] mark: make it active. because hero only work when their mark active. This way the DPS on paper is more real. Maybe make mark a optional mode?
  - [x] how many mark should we make it active?
  - Answer: nobody know, we would only know when we saw the result and try to iterate from that.
- [x] hero's nuisance: Some hero's number on paper look weak than the others. I'll specify why some hero don't look balance but actually isn't. And let you paste it on the google sheet later.
  - [x] try test the new hero benchmark. and me manually put the hero's nuisance in.
  - [x] match google sheet to new benchmark.

[x] Make window for editing enemy team too.

- add selected enemy.
- save enemy team.
- bool option for changing deploy zone.
- [x] Enemy should show in the editor too for convenient.
- [x] should show each team comp too. For convenient.
- [x] Get rid of unuse menu item
- [x] There's still Balance menu, get rid of it. Keep the benchmark editor menu though.
- [x] Add selected hero to bench should be moved to Stage editor. then delete the menu.

[] Adjusting power level:

Power level is low to the point that adding 1 _random hero_ to the team could make the player win a lot, which mean the synergy is not matter when player can win by looking at which hero they add.

1.

- Keep the current 5 stage inside a archive folder
- Let enemy have a level, let him have like lvl1-30, 1lvl = +10% to all stat. This is to make enemy tweakable and raise the power level.
- Make new 20 stage. The power should raise every stage.

2.

- Now each team have to test the align vs no-align to each stage. The align should win 70% of the time. While the no-align should mostly lose.
- June the number from both the player side or enemy side depend on the case.

[] (IMPORTANT) We don't have enough enemy team to test.

- Current we have 5 enemy team stage. We should make more than that. Next is 10 stage.
- Add new variation of the minotaur. A minotaur that

## Minor

[x] The style in each custom window isn't consistent.

- Could you make all of them somehow similar to each other. I don't know if this make sense though.

[] Change the word _align_ to _mark_. I recently feel like mark is the word more fit to the mechanic.

[] Attach fight time in the scoreboard.

[] Tidy up CLAUDE.md

[] The google sheet is hard to read. Make it more readable.

My review:

- ...

## today

[] (Ongoing) Balance hero.

Balance 2 things: first the hero's skill, second the bonus from the mark.

- Hero skill: each hero should land in +- 20% of the role's median
- Mark: ...

## what's next? (rough planning)

[] Balance enemy

- Let assume we have 5 stage, at stage 5 we have a boss battle.
- We have no boss, so make a easy one first.
- Design each stage by hand to see what it's like. Design backward from stage 5 should be a good idea.
  - Stage 5: a enemy team should look like they come in like a army.
- Now time to balance them. Let try play to it normally first.
  - No, before that, our heroes still isn't balanced too. That's more important. Balance our roster first.
