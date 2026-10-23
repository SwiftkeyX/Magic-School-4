using System;
using System.Collections.Generic;
using MagicSchool.Contracts;
using MagicSchool.Combat.Heroes;
using MagicSchool.Combat.Placements;
using MagicSchool.StatScaling;

namespace MagicSchool.Combat.Align
{
    /// <summary>
    /// Remember the bonuses from the aligning mechanic that are applying right now.
    /// So we can control when to give the bonus or take bonus back.
    /// e.g.    the player drags an Elf behind Ettin    => Refresh: the Elf gains +30% attack speed
    ///         the player drags the Elf away again     => Refresh: the Elf loses the bonus
    /// (read AlignMark.cs and AlignHex.cs)
    /// </summary>
    public class AlignBonuses
    {
        private readonly List<(Hero receiver, ICustomModifier modifier)> _given = new List<(Hero, ICustomModifier)>();
        private readonly List<(Hero generator, Action<DamageEvent> heal)> _links = new List<(Hero, Action<DamageEvent>)>();

        // FLAGGING: This isn't optimal obviously. But let leave it alone for the speed.
        // Take back every bonus, then give every ally standing on a marked hex its bonus.
        // This method was called when:
        //      during prepartion, when the heroes's position is changed, refresh
        //      when the fight starts, refresh
        public void Refresh(BattleBoard board, TeamEnum team)
        {
            // Wipe every modifier given by the aligning mechanic
            TakeBack();

            // loop through every combatant on the board.
            // if the mark from each combatant is aligned, give bonus to self/receiver.
            foreach (ICombatant combatant in new List<ICombatant>(board.HeroesOnBoard))
            {
                if (!(combatant is Hero me) || me.Team != team || !me.IsAlive) continue;

                Hex standingOn = me.CurrentHex;
                if (standingOn == null) continue;

                // allies on each special mark's hexes, for the skill that reads them (See IAligned)
                var alliesOnSpecialMark = new Dictionary<string, List<ICombatant>>();

                // loop through every mark own by this hero
                IReadOnlyList<AlignMark> marks = me.AlignMarks;

                // a wearing hex can be in more than one mark (orange and green both): it is still worn once
                var wearingHexes = new HashSet<Hex>();
                foreach (AlignMark mark in marks) wearingHexes.UnionWith(mark.GetWearingHexes(standingOn));

                foreach (Hex wearing in wearingHexes)
                {
                    // an item lying on a wearing hex: this hero wears it.
                    // An item is not an ally, so it fills nothing else: no ally bonus, no self bonus.
                    Wear(me, board.Items.ItemOn(wearing));

                    // an ally standing on a wearing hex: this hero wears what the ally wears, too.
                    // Only what the ally wears itself - not what it is sharing from its own wearing hexes - so it never chains.
                    if (board.WhoReservedThisHex(wearing) is Hero ally && ally != me && ally.Team == team)
                    {
                        foreach (IEquipment item in ally.WornItems) Wear(me, item);
                    }
                }

                for (int i = 0; i < marks.Count; i++)
                {
                    AlignMark mark = marks[i];

                    // orange pays the hero itself, green pays the ally standing there, white pays nobody (See AligningHex)
                    int colour = AligningHex.ColourOf(marks, i);
                    bool paysSelfOnly = colour == AligningHex.Orange;
                    bool paysAllyOnly = colour == AligningHex.Green;

                    if (mark.IsSpecial && !alliesOnSpecialMark.ContainsKey(mark.special)) alliesOnSpecialMark[mark.special] = new List<ICombatant>();

                    // loop through every hex that was marked
                    foreach (Hex marked in mark.GetMarkedHexes(standingOn))
                    {
                        // get who is standing on the marked hex
                        Hero receiver = board.WhoReservedThisHex(marked) as Hero;
                        bool isAlly = receiver != null && receiver.Team == team;

                        if (isAlly && receiver != me && mark.IsSpecial)
                        {
                            alliesOnSpecialMark[mark.special].Add(receiver);
                            LinkSpecialMark(receiver, me, mark.special);
                        }

                        // this mark give bonus to the receiver
                        if (paysAllyOnly && isAlly && mark.HasAllyBonus) Give(receiver, mark.BuildAllyModifier(), me);

                        // this mark give bonus to self: for an ally standing there,
                        // or the opposite - for a hex left empty
                        bool paysSelf = mark.selfWhenEmpty ? IsEmpty(board, marked) : isAlly;
                        if (paysSelfOnly && paysSelf && mark.HasSelfBonus) Give(me, mark.BuildSelfModifier(), me);

                        // the ally is a generator: its damage heals this hero
                        if (paysSelfOnly && isAlly && mark.HasDamageHeal) LinkDamageHeal(receiver, me, mark.healFromAllyDamage);
                    }
                }

                me.SetAlliesOnSpecialMarks(alliesOnSpecialMark);
            }
        }

        // The hero counts nobody on its special marks any more - the bonuses it was given stay.
        // e.g. the hero benchmark's stand-in allies fill a hero's marks for Refresh, then leave the board before the fight
        public static void ClearSpecialMarks(Hero hero) => hero.SetAlliesOnSpecialMarks(new Dictionary<string, List<ICombatant>>());

        // Nobody stands on it, and the player could have put someone there.
        // Outside the deploy zone a hex is always empty of the player's team, so it would pay for nothing.
        // An item lying there is not someone.
        private static bool IsEmpty(BattleBoard board, Hex hex)
            => hex.IsInDeployZone && board.WhoReservedThisHex(hex) == null;

        // The hero wears an item that lies on one of its wearing hexes, or that an ally standing there wears
        private void Wear(Hero wearer, IEquipment item)
        {
            // `as Object`: a destroyed item still in a list is not == null through the interface (See HeroItems.ReGrantAll)
            if (item as UnityEngine.Object == null || item.Modifier == null) return;

            Give(wearer, item.Modifier, wearer);
        }

        // Add modifier to the specify hero
        private void Give(Hero receiver, ICustomModifier bonus, Hero giver)
        {
            receiver.AddModifier(bonus, giver);
            _given.Add((receiver, bonus));
        }

        // Every damage the generator deals heals the payoff hero by [percent] of it
        // e.g.     15% - the generator hits for 200 => Shield Knight heals 30
        private void LinkDamageHeal(Hero generator, Hero payoff, float percent)
        {
            Action<DamageEvent> heal = hit => Omnivamp.HealFromDamage(payoff, hit.Outcome.Landed, percent, source: generator);

            generator.OnDamageDealt += heal;
            _links.Add((generator, heal));
        }

        // Every damage an ally on the owner's special mark deals is told to the owner's skill
        // e.g.     Shield Knight's Guard mark - the ally hits for 200 => his skill heals him 20
        private void LinkSpecialMark(Hero ally, Hero owner, string mark)
        {
            Action<DamageEvent> tell = hit => owner.TriggerOnMarkedAllyDealtDamage(mark, ally, hit);

            ally.OnDamageDealt += tell;
            _links.Add((ally, tell));
        }

        // Wipe every modifier given by the aligning mechanic, and cut every generator link
        private void TakeBack()
        {
            foreach ((Hero receiver, ICustomModifier modifier) in _given)
            {
                if (receiver == null) continue;

                receiver.RemoveModifier(modifier);
            }

            _given.Clear();

            foreach ((Hero generator, Action<DamageEvent> heal) in _links)
            {
                if (generator != null) generator.OnDamageDealt -= heal;
            }

            _links.Clear();
        }
    }
}
