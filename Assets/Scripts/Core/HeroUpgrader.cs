using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MagicSchool.Contracts;
using MagicSchool.Combat.Heroes;
using MagicSchool.Combat.Placements;
using MagicSchool.Items;

namespace MagicSchool.Core
{
    /// <summary>
    /// Upgrading (TFT's star-up): two copies of the same hero at the same star combine into one, a star higher.
    /// e.g.    buy a 2nd Orc Blademaster                   => one 2-star Orc
    ///         a 2nd 2-star Orc (two more copies bought)   => one 3-star Orc
    /// The copy on the board is the one that stays, so a placed hero keeps its hex.
    /// The other copy goes; its item moves to the one that stays, or drops to the loose row if that one already wears one.
    /// (See HeroStar for what a star changes)
    /// </summary>
    public class HeroUpgrader
    {
        private const TeamEnum PlayerTeam = TeamEnum.Blue;

        private readonly BattleBoard _board;
        private readonly Bench _bench;
        private readonly Bench _spareBench;

        public HeroUpgrader(BattleBoard board, Bench bench, Bench spareBench)
        {
            _board = board;
            _bench = bench;
            _spareBench = spareBench;
        }

        // Combine every pair there is, until none is left. Returns whether anything combined.
        // includeBoard false = mid-fight: the board's heroes are fighting, so only the benches combine.
        // They combine with the board once the next Preparation starts.
        public bool CombineAll(bool includeBoard)
        {
            bool anyCombined = false;

            // a combine can make a new pair (two 2-stars), so look again after each one
            while (TryCombineOnePair(includeBoard)) anyCombined = true;

            return anyCombined;
        }

        private bool TryCombineOnePair(bool includeBoard)
        {
            var pair = Owned(includeBoard)
                .Where(hero => hero.CanUpgrade)
                .GroupBy(hero => (hero.Data, hero.Star))
                .FirstOrDefault(group => group.Count() >= HeroStar.CopiesToCombine);

            if (pair == null) return false;

            // Owned lists the board first, so a hero standing on the board is the one that stays
            Hero keeper = pair.First();
            Hero copy = pair.Skip(1).First();

            Combine(keeper, copy);
            return true;
        }

        private void Combine(Hero keeper, Hero copy)
        {
            // the copy's item: worn by the keeper if there's room, otherwise back to the loose row
            foreach (IEquipment item in copy.WornItems.ToList())
            {
                copy.TryTakeOff(item);
                if (keeper.TryWear(item)) continue;
                if (item is Item looseItem) ItemDrop.MoveToFreeSpot(looseItem);
            }

            Remove(copy);
            keeper.Upgrade();

            Debug.Log($"[Upgrade] {keeper.DisplayName}");
        }

        // the player's heroes: the board first, then the bench, then the spare bench
        private IEnumerable<Hero> Owned(bool includeBoard)
        {
            IEnumerable<Hero> onBoard = includeBoard
                ? _board.HeroesOnBoard.OfType<Hero>().Where(hero => hero != null && hero.Team == PlayerTeam)
                : Enumerable.Empty<Hero>();

            IEnumerable<Hero> onBenches = (_bench != null ? _bench.Heroes : Enumerable.Empty<Hero>())
                .Concat(_spareBench != null ? _spareBench.Heroes : Enumerable.Empty<Hero>());

            return onBoard.Concat(onBenches).Where(hero => hero.IsInitialized);
        }

        // take the copy off whatever it stands on and delete it (the same as selling, with no gold)
        private static void Remove(Hero hero)
        {
            IPlacement placement = hero.CurrentPlacement;
            if (placement != null) placement.OnUnitUnplaced(hero);
            hero.SetCurrentPlacement(null);
            hero.UntrackFromBoard();

            Object.Destroy(hero.gameObject);
        }
    }
}
