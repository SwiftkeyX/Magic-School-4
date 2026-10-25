# Power level

## Problem

Power level is low to the point that adding 1 _random hero_ to the team could make the player win a lot, which mean the synergy is not matter when player can win by even not looking at which hero they'll add to the team.

### Raise enemy power level

[x] Let enemy have a level, let him have like lvl1-30, 1lvl = +10% to all stat. This is to make enemy tweakable and raise the power level.

[x] Keep the current 5 stage inside a archive folder.

- there's so many stage. Is all this use? I think I only does use the stage 1-10 that I use earlier. Else I don't use anymore.

[x] Make new 20 stage. The power should raise every stage.

### Match player team to enemy power level

[] (Ongoing) Now each team have to test the align vs no-align to each stage. The align should win 70% of the time. While the no-align should mostly lose.

June the number from both the player side or enemy side depend on the case:

1. [x] Let begin with Naga + ShieldKnight + Ettin team. Make this team comp pass all 10 stage.

   [x] Change Naga out, to Elf instead.
   - The first team should be a great benchmark hero. Naga don't fit that because he have opportunity cost. Elf is AD which have less opportunity cost.
   - no longer have armour shred.
   - Her skill change to: Mana is at 100. Buff for 6 sec, gain 30% atk, gain 50% AS.
   - [x] Elf didn't use any AP even though she have a lot of it: 30% atk => Increase 15% AP to ATK, 30% AP to AS

   [x] Make ShieldKnight scale with AP more.
   - His skill should heal 200% AP.
   - then, The AoE should either deal more damage or do something else.
     - His AoE now deal damage equal to [30% AP] % of the damage done to him during his casting.
   - Change his white mark ability: He brace 1 more sec and heal 50%AP more total per white mark.
   - I think he need that orange mark to increase his DF.

   [x] Ettin still felt out of place
   - Do we make him scale with AP more? Yes.
   - The downside I notice: There's kinda no variety? to the game, because all I need to make this game work is just adding more number to them. But that was fine, the variety can come later.
   - His skill scale more: 100 + 200% AP.
   - Auto attack deal [5% AP] % of his HP.
   - His green mark also give AS that scale on his AP: give [25% AP] % AS.

   [x] Find new hero for the team
   - [x] Myconid
     - Try adding Myconid and it feel too strong. It's easy to assassinate backliner because player can just add Myconid to the team with like 1 AP item and 2 active mark, then, he can 2 shot the enemy.
     - The cost feel too easy to me because 2 shot enemy is huge. Compared to Akshan from TFT, he need 5 shurima active, Akshan need a lot of item, he need to be tier-2 or tier-3. When compare to that Akshan is a big payoff, shurima 5 is not easy to hit.
     - Should Myconid 2 shot enemy? I still don't know actually because 2 active mark and 1 AP item right now see like a minor setup, the payoff 2-shot 1 enemy maybe isn't that big?
     - Justified for now.
   - [x] Dryad
     - His AS buff should do more? No, he's just stub hero in this comp. Not important to buff now.

   [x] Power level progression for each milestone.

   In each milestone stage (1, 3, 5, 7, 9), we'll have 1 variation more of the team comp.
   - stage 1: {Elf, ShieldKnight, Ettin}
   - stage 3: {Elf, ShieldKnight tier-2, Ettin, Myconid}, {Archmage Tome}
   - stage 5: {Elf tier-2, ShieldKnight tier-2, Ettin, Myconid}, {Archmage Tome, Iron Plate}
   - stage 7:
   - stage 9: {Elf tier-2, ShieldKnight tier-3, Ettin tier-2, Husk tier-2, Myconid}, {Archmage Tome, Iron Plate}
   - stage 10: {Elf tier-2, ShieldKnight tier-3, Ettin tier-2, Husk tier-2, Myconid, Dryad}, {Archmage Tome, Iron Plate}

2. [] Skeleton + BlackSmith + Knight
   [x] Skeleton feel weak
   - buff base skill damage
   - buff white mark
   - add orange hex, buff AD
   - he do too much damage: his white mark should scale with his upgraded. He do [30 + (10 * tier) * number of mark]
   - This guy felt incredible hard to balance. maybe because enemy have too much armor?
   - he feel hard to balance, let adjust him.
   - He deal 10,000 damage while Elf did 6,000 damge in stage 10. This mean Skeleton do too much damage, let nerf him down.
   - His white mark now buff all his attack. His 3 hex-away nerf apply all his attack too.
   - Okay, now his auto deal too much damage, let nerf his AS.

   [] BlackSmith don't have sustain.
   - Every time she cast, she gain 10hp/s: change to she gain 2% hp/s
   - give her white mark. her skill repeat for [number of white mark] time.
   - Compare to shieldknight: that guy have both the damage and tankiness.
   - Blacksmith should provide both too. On cast, give her white mark ally item, AD hero get Whet stone, AP hero get Archmage tome, tank gat better Iron plate (50 DF). If all ally already have item, she put tank item on herself (she can wear several item, no limit)
   - Blacksmith no longer do AoE part.
   - Tweak her number
   - She gain too much tankiness. Now she only give 1 item. Every 2 active mark, the quality of the item she gave is increased.
     - AD/AP stay the same.
     - Tank: 0 mark = 20 DF, 2 mark = 50 DF, 4 mark = 100 DF
     - She now have item capped at 3.
   - Her regen feel too OP. give a capped number to it. Let it capped at 6% hp/s.
   - [x] Now her orange mark feel too strong. Nerf from 300 hp => 150 hp.
   - [] She feel hard to balance, could you make more version of iron plate? +20DF, +30DF, +40DF, +50DF. Could you make a field for items list that going to be give to each role, then allow me to june it in the inspector for easier tweaking?
   - her HP/s is actually strong and should grow with her white mark. skill gain 0.3 hp/s per white mark.

   [x] Knight
   - He does a lot of damage early, feel more like a fighter than tank.
   - buff his orange mark to make him more tanky.
   - His skill now scale with AD.
   - He gain 4 sec shield for 100% of his skill damage. He have 90 mana, 7 regen/s, meaning he take 13 sec to cast once.
   - change again: He get 300% AD shield per enemy hit. The shield stay 4 sec. ??? I try june his AD % first.
   - Give the knight a white mark. his skill deal [100 + (50 * active white mark)]% AD.

   [] add new hero
   - [] Myconid
   - [] Werewolf
   - [] Templar

   [] Power level progression for each milestone.

   In each milestone stage (1, 3, 5, 7, 9), we'll have 1 variation more of the team comp.
   - stage 1: {Skeleton, Knight, BlackSmith}
   - stage 3: {Skeleton, Knight, BlackSmith tier-2, ?}, {Iron Plate}
   - stage 5: {Skeleton tier-2, Knight, BlackSmith tier-2, ?}, {Whet Stone, Iron Plate}
   - stage 7: {Skeleton tier-2, Knight, BlackSmith tier-2, ?}, {Whet Stone, Iron Plate x 2}
   - stage 9: {Skeleton tier-2, Knight-2, BlackSmith tier-2, ?, ?}, {Whet Stone, Iron Plate x 2}
   - stage 9: {Skeleton tier-2, Knight-2, BlackSmith tier-3, Myconid tier-2, Troll tier-2, Priest}, {Whet Stone, Iron Plate x 2}

3. [] Dwarf
   [] Criteria for making 1 team
   - Find 5 heroes that was well synergy which mean their mark align together: 1 is always tank, another is whatever. Then make Dwarf a new hero synergize well with this team by making his mark fit their mark layout.
   - 2 core hero:
     - main tank should have 3-4 mark active
     - main carry (dwarf) should have 3-4 mark active
     - 1 of them must have 4 mark active.
     - else should have at least 2 mark active.

## Takeaway

1. What is the choice that going to make our team stronger?

- Add more item
- Make more active mark
- Upgrade hero

2. Practical example to follow (rough power level):

- First Arc (stage 1-5)
  - Stage 1-3: 2 item, 4 hero, At least 1 Tier-2
  - Stage 4-5: 5 item, 5 hero, At least 2 Tier-2
- Second Arc (stage 6-10)
  - Stage 6-7: 7 item, 5 hero, At least 1 Tier-3
  - Stage 8-9: 9 item, 5 hero, At least 2 Tier-3

3. Adjust item's power level

In (2), 7 item and 9 item are quite unachivable and doesn't look fun to me. In each hero team, I though realistic amount of item that you can put on is like 5 item maximum.

So we need a way to power up the power level from the item beside just adding more item:

- Adding item rarity
- Make item be able to merge into more powerful version.

4. Easiest way to add more powerlevel is to add more skill's number. That felt redundant, but it was fine since variety can come later.

Do I have a prove? no, but it's easier to focus on 1 thing at a time.

5. Make Elf team a power level benchmark.

At stage 10, in Elf team, Elf itself deal 6,000 damage. I feel great about this power level. I will try make oterh team match this power. Since Elf use only auto-attack, she feel easy to balance unlike other hero that aren't reliable to read their number.

Example of bad benchmark:

- Skeleton assassinate backliner, so he usually have lower DPS.
- Naga have opportunity cost when he switch target. She lose her +30% damage buff.
- FrostWitch use AoE, so each game she may doesn't deal equal amount of damage.
  These example have other factors that made it harder to balance, so they're not a good reference point like Elf does.
