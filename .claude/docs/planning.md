# Planning

## Current short goal

[] Balancing heroes
Before all this, to make the game playable, we need to balance the game first.

- [] Balance the heroes using HeroBenchmark
- [] Balance the item (the item still suck, we need to add the list of item to the game first before balancing item)

[] Balance enemy

- Let assume we have 5 stage, at stage 5 we have a boss battle.
- We have no boss, so make a easy one first.
- Design each stage by hand to see what it's like. Design backward from stage 5 should be a good idea.
  - Stage 5: a enemy team should look like they come in like a army.
- Now time to balance them. Let try play to it normally first.
  - No, before that, our heroes still isn't balanced too. That's more important. Balance our roster first.

## Design Decision

[] Implement aligning to enemy

[] Make enemy team have 1 main carry

- Later in the game, dps start shifting from the whole team to 1 main carry. I don't sure if I should do the same to the carry.

[] I intented to make the enemy have the bigger team. So each enemy have to have less stat, skill damage.

[] Now, the hero stronger than the bonus: Instead of trying to focus on aligning the hex, player can just fit in more hero to win the game.

- Make bonus stronger
- Maybe making bonus stronger when you upgrade hero too.

[] The alignment really do feel like a puzzle: player don't know what is best, so player can lose really easily, but only adjusting position, player can win. I proposed player have at least 4 try per 1 stage.

[] (out of scope of demo) When hero was upgraded, change his mark layout too.

[] (out of scope of demo) When the player win, let player choose which side of the board will be extended, this should increase the number of synergy player can do?

[] (out of scope of demo, I think this one is neccessary) I want the player to have foundation cost. Which force them to stay in the his current comp or flex from his current comp.

- Make a hero be able to merge with item. Now a item is like a resource that can be use to a hero. If the resource is limited, it mean merged hero is the cost that player must continue using them because it is most efficient.

[] (deferred) I still don't sure about how should the power level look like. This would need a lot of trial & error until I understand.

[x] (proposed) (not allow) I want to implement a item that aren't 1 hex, I want them to have the real weapon shape, and place on top of several hex. Just like BB.

- But before that, let try play it for real first to see what the current version feel like.
- Okay, even before all that, let balance the enemy first. If the enemy power is balance, then we have a real easy benchmark what power level hero should hit.

## Deferred plan

[] (deferred) BIG REFACTOR TIME

- [] Hey could you also look the claude skill about clean architecture or something similar, espectially for Unity project.
- [] Spawn new instance of Opus to review the entire code base. Base on the skill we founded.
- I notice that there's a lot of changed. I have not read the code myself for quite a long time. So there must be a lot of inconsistency which is fine. As long as you can work with it efficiently. What do you think of this code based base on this requirement?

My review:

- The benchmark script is quite large now, make it its own module now.
- Also the comment is annoying, could you delete all in-line comment. The summary comment above each clas is fine, not all class need it though.

## Takeaway

- Bad balancing make thing harder to design. We may need to start balancing seriously soon.
- Make the existing heroes feel cohesive first. Adding the new heroes should come after that.
- A item now can be placed on the hex. BB path COULD be the legit choice.
- ...

## [patchnote-takeaway](design/hero-patchnote/patchnote-takeaway.md)
