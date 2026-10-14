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

                // loop through every mark own by this hero
                foreach (AlignMark mark in me.Data.AlignMarks)
                {
                    // an item lying on a wearing hex: this hero wears it.
                    // An item is not an ally, so it fills nothing else: no ally bonus, no self bonus.
                    foreach (Hex wearing in mark.GetWearingHexes(standingOn))
                    {
                        Wear(me, board.Items.ItemOn(wearing));
                    }

                    // loop through every hex that was marked
                    foreach (Hex marked in mark.GetMarkedHexes(standingOn))
                    {
                        // get who is standing on the marked hex
                        Hero receiver = board.WhoReservedThisHex(marked) as Hero;
                        bool isAlly = receiver != null && receiver.Team == team;

                        // this mark give bonus to the receiver
                        if (isAlly && mark.HasAllyBonus) Give(receiver, mark.BuildAllyModifier(), me);

                        // this mark give bonus to self: for an ally standing there,
                        // or the opposite - for a hex left empty
                        bool paysSelf = mark.selfWhenEmpty ? IsEmpty(board, marked) : isAlly;
                        if (paysSelf && mark.HasSelfBonus) Give(me, mark.BuildSelfModifier(), me);

                        // the ally is a generator: its damage heals this hero
                        if (isAlly && mark.HasDamageHeal) LinkDamageHeal(receiver, me, mark.healFromAllyDamage);
                    }
                }
            }
        }

        // Nobody stands on it, and the player could have put someone there.
        // Outside the deploy zone a hex is always empty of the player's team, so it would pay for nothing.
        // An item lying there is not someone.
        private static bool IsEmpty(BattleBoard board, Hex hex)
            => hex.IsInDeployZone && board.WhoReservedThisHex(hex) == null;

        // The hero wears an item that lies on one of its wearing hexes
        private void Wear(Hero wearer, IEquipment item)
        {
            if (item == null || item.Modifier == null) return;

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
