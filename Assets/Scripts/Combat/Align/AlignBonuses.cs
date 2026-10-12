using System.Collections.Generic;
using MagicSchool.Contracts;
using MagicSchool.Combat.Heroes;
using MagicSchool.Combat.Placements;

namespace MagicSchool.Combat.Align
{
    /// <summary>
    /// Remember the bonuses from the aligning mechanic are giving right now. 
    /// So we can control when to give the bonus or take bonus back.
    /// e.g.    the player drags an Elf behind Ettin    => Refresh: the Elf gains +30% attack speed
    ///         the player drags the Elf away again     => Refresh: the Elf loses the bonus
    /// </summary>
    public class AlignBonuses
    {
        private readonly List<(Hero receiver, ICustomModifier modifier)> _given = new List<(Hero, ICustomModifier)>();

        // FLAGGING: This isn't optimal obviously. But let leave it alone for the speed.
        // Take back every bonus, then give every ally standing on a marked hex its bonus.
        // This method was called when:
        //      during prepartion, when the heroes's position is changed, refresh
        //      when the fight starts, refresh
        public void Refresh(BattleBoard board, TeamEnum team)
        {
            TakeBack();

            foreach (ICombatant combatant in new List<ICombatant>(board.HeroesOnBoard))
            {
                if (!(combatant is Hero giver) || giver.Team != team || !giver.IsAlive) continue;

                Hex standingOn = giver.CurrentHex;
                if (standingOn == null) continue;

                foreach (AlignMark mark in giver.Data.AlignMarks)
                {
                    // the board's edge: no hex on that side
                    Hex marked = standingOn.GetNeighbor(mark.direction);
                    if (marked == null) continue;

                    if (!(board.WhoReservedThisHex(marked) is Hero receiver) || receiver.Team != team) continue;

                    ICustomModifier bonus = mark.BuildModifier();
                    receiver.AddModifier(bonus, giver);
                    _given.Add((receiver, bonus));
                }
            }
        }

        // Wipe every modifier given by the aligning mechanic  
        private void TakeBack()
        {
            foreach ((Hero receiver, ICustomModifier modifier) in _given)
            {
                if (receiver == null) continue;

                receiver.RemoveModifier(modifier);
            }

            _given.Clear();
        }
    }
}
