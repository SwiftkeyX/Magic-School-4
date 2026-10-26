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
    // The board is listed first, so the copy standing on the board is the one kept. A combine can make a new pair (two
    // 2-stars), so it looks again after each. The other copy's item goes to the keeper if it has room, else to the
    // loose row.
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

        public bool CombineAll(bool includeBoard)
        {
            bool anyCombined = false;

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

            Hero keeper = pair.First();
            Hero copy = pair.Skip(1).First();

            Combine(keeper, copy);
            return true;
        }

        private void Combine(Hero keeper, Hero copy)
        {
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

        private IEnumerable<Hero> Owned(bool includeBoard)
        {
            IEnumerable<Hero> onBoard = includeBoard
                ? _board.HeroesOnBoard.OfType<Hero>().Where(hero => hero != null && hero.Team == PlayerTeam)
                : Enumerable.Empty<Hero>();

            IEnumerable<Hero> onBenches = (_bench != null ? _bench.Heroes : Enumerable.Empty<Hero>())
                .Concat(_spareBench != null ? _spareBench.Heroes : Enumerable.Empty<Hero>());

            return onBoard.Concat(onBenches).Where(hero => hero.IsInitialized);
        }

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
