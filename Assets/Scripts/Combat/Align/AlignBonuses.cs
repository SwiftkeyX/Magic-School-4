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
    // Refresh takes back every bonus it gave and gives them all again for where everyone stands now: after every move
    // in Preparation, and once more as the fight starts.
    // Orange pays the hero itself, green the ally standing there, white nobody (the skill reads who stands there). An
    // orange selfWhenEmpty hex pays only inside the deploy zone, where the player could have put someone; an item
    // lying there is not someone.
    // Wearing: a wearing hex in two marks is worn once. An item lying on one is worn, but an item is not an ally, so
    // it fills nothing else. An ally standing on one lends what it wears itself, never what it shares from its own
    // wearing hexes, so sharing never chains.
    // healFromAllyDamage links the ally to the owner through OnDamageDealt (15% of a 200 hit heals 30); the link is
    // cut on the next Refresh. ClearSpecialMarks forgets who stands on the special marks but keeps the bonuses given
    // (the hero benchmark's stand-ins leave before the fight).
    public class AlignBonuses
    {
        private readonly List<(Hero receiver, ICustomModifier modifier)> _given = new List<(Hero, ICustomModifier)>();
        private readonly List<(Hero generator, Action<DamageEvent> heal)> _links = new List<(Hero, Action<DamageEvent>)>();

        public void Refresh(BattleBoard board, TeamEnum team)
        {
            TakeBack();

            foreach (ICombatant combatant in new List<ICombatant>(board.HeroesOnBoard))
            {
                if (!(combatant is Hero me) || me.Team != team || !me.IsAlive) continue;

                Hex standingOn = me.CurrentHex;
                if (standingOn == null) continue;

                var alliesOnSpecialMark = new Dictionary<string, List<ICombatant>>();

                IReadOnlyList<AlignMark> marks = me.AlignMarks;

                var wearingHexes = new HashSet<Hex>();
                foreach (AlignMark mark in marks) wearingHexes.UnionWith(mark.GetWearingHexes(standingOn));

                foreach (Hex wearing in wearingHexes)
                {
                    Wear(me, board.Items.ItemOn(wearing));

                    if (board.WhoReservedThisHex(wearing) is Hero ally && ally != me && ally.Team == team)
                    {
                        foreach (IEquipment item in ally.WornItems) Wear(me, item);
                    }
                }

                for (int i = 0; i < marks.Count; i++)
                {
                    AlignMark mark = marks[i];

                    int colour = AligningHex.ColourOf(marks, i);
                    bool paysSelfOnly = colour == AligningHex.Orange;
                    bool paysAllyOnly = colour == AligningHex.Green;

                    if (mark.IsSpecial && !alliesOnSpecialMark.ContainsKey(mark.special)) alliesOnSpecialMark[mark.special] = new List<ICombatant>();

                    foreach (Hex marked in mark.GetMarkedHexes(standingOn))
                    {
                        Hero receiver = board.WhoReservedThisHex(marked) as Hero;
                        bool isAlly = receiver != null && receiver.Team == team;

                        if (isAlly && receiver != me && mark.IsSpecial)
                        {
                            alliesOnSpecialMark[mark.special].Add(receiver);
                            LinkSpecialMark(receiver, me, mark.special);
                        }

                        if (paysAllyOnly && isAlly && mark.HasAllyBonus) Give(receiver, mark.BuildAllyModifier(me.Magic), me);

                        bool paysSelf = mark.selfWhenEmpty ? IsEmpty(board, marked) : isAlly;
                        if (paysSelfOnly && paysSelf && mark.HasSelfBonus) Give(me, mark.BuildSelfModifier(), me);

                        if (paysSelfOnly && isAlly && mark.HasDamageHeal) LinkDamageHeal(receiver, me, mark.healFromAllyDamage);
                    }
                }

                me.SetAlliesOnSpecialMarks(alliesOnSpecialMark);
            }
        }

        public static void ClearSpecialMarks(Hero hero) => hero.SetAlliesOnSpecialMarks(new Dictionary<string, List<ICombatant>>());

        private static bool IsEmpty(BattleBoard board, Hex hex)
            => hex.IsInDeployZone && board.WhoReservedThisHex(hex) == null;

        private void Wear(Hero wearer, IEquipment item)
        {
            if (item as UnityEngine.Object == null || item.Modifier == null) return;

            Give(wearer, item.Modifier, wearer);
        }

        private void Give(Hero receiver, ICustomModifier bonus, Hero giver)
        {
            receiver.AddModifier(bonus, giver);
            _given.Add((receiver, bonus));
        }

        private void LinkDamageHeal(Hero generator, Hero payoff, float percent)
        {
            Action<DamageEvent> heal = hit => Omnivamp.HealFromDamage(payoff, hit.Outcome.Landed, percent, source: generator);

            generator.OnDamageDealt += heal;
            _links.Add((generator, heal));
        }

        private void LinkSpecialMark(Hero ally, Hero owner, string mark)
        {
            Action<DamageEvent> tell = hit => owner.TriggerOnMarkedAllyDealtDamage(mark, ally, hit);

            ally.OnDamageDealt += tell;
            _links.Add((ally, tell));
        }

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
