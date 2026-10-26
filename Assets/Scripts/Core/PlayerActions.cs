using System;
using UnityEngine;
using MagicSchool.Contracts;
using MagicSchool.Combat.Align;
using MagicSchool.Combat.Heroes;
using MagicSchool.Combat.Placements;
using MagicSchool.Economy;
using MagicSchool.Items;

namespace MagicSchool.Core
{
    // What the player does to the team and its items: move, sell, wear, lay items on hexes, turn a pattern, buy or put
    // back a hero held out of the shop, combine copies. Built and held by GameManager (GameManager.Instance.Actions).
    // Every one works aligning out again, since standing and wearing hexes are what the bonuses come from. Turning a
    // pattern is Preparation only: bonuses are fixed by where everyone stood when the fight started.
    // Combining: only Preparation touches the board; mid-fight the benches combine alone and meet the board at the
    // next Preparation. A combine rebuilds stats, which drops aligning bonuses, so they are given again. A hero
    // spawned on a hex (a seed, a team comp, an enemy) never starts one.
    public class PlayerActions
    {
        private readonly Func<GamePhaseEnum> _phase;
        private readonly BattleBoard _board;
        private readonly Bench _bench;
        private readonly HeroMover _heroMover;
        private readonly Seller _seller;
        private readonly Shop _shop;
        private readonly HeroUpgrader _upgrader;
        private readonly AlignBonuses _alignBonuses;
        private readonly HeroSpawner _heroSpawner;

        public event Action<Hero, int> OnShopHeroPickedUp;

        internal PlayerActions(Func<GamePhaseEnum> phase, BattleBoard board, Bench bench, HeroMover heroMover, Seller seller,
                               Shop shop, HeroUpgrader upgrader, AlignBonuses alignBonuses, HeroSpawner heroSpawner)
        {
            _phase = phase;
            _board = board;
            _bench = bench;
            _heroMover = heroMover;
            _seller = seller;
            _shop = shop;
            _upgrader = upgrader;
            _alignBonuses = alignBonuses;
            _heroSpawner = heroSpawner;

            _heroSpawner.OnSpawned += CombineIfBenched;
            _shop.OnHeroPickedUp += HandOutShopHero;
        }

        private GamePhaseEnum Phase => _phase();

        public void MoveHero(ICombatant hero, IPlacement placement)
        {
            _heroMover.MoveThisHeroTo(hero, placement);
            RefreshAligning();
        }

        public void Sell(ISellable sellable)
        {
            if (sellable is ICombatant && Phase != GamePhaseEnum.Preparation) return;

            _seller.Sell(sellable);
            RefreshAligning();
        }

        public bool AddHeroToBench(HeroDataSO data) => data != null && _bench.SpawnHeroOnBench(data);

        public bool AddItemToLooseRow(ScriptableObject data) => data is ItemDataSO item && ItemDrop.Spawn(item) != null;

        public bool PutItemOnHex(IEquipment item, Hex hex)
        {
            if (!_board.Items.Place(item, hex)) return false;

            RefreshAligning();
            return true;
        }

        public void TakeItemOffHex(IEquipment item)
        {
            if (_board.Items.Remove(item)) RefreshAligning();
        }

        public bool WearItem(Hero hero, IEquipment item)
        {
            if (!hero.TryWear(item)) return false;

            RefreshAligning();
            return true;
        }

        public void TakeItemOffHero(Hero hero, IEquipment item)
        {
            if (hero.TryTakeOff(item)) RefreshAligning();
        }

        private void HandOutShopHero(int slot, HeroDataSO data)
            => OnShopHeroPickedUp?.Invoke(_heroSpawner.SpawnUnboughtHero(data, _board), slot);

        public bool BuyShopHero(Hero hero, int slot, IPlacement placement)
        {
            placement ??= _bench.FindFreeSlot();
            if (placement == null)
            {
                Debug.Log("Bench is full - nowhere to put the hero.");
                ReturnShopHero(hero);
                return false;
            }

            if (!_shop.TryBuyPickedUp(slot, hero.Data))
            {
                ReturnShopHero(hero);
                return false;
            }

            MoveHero(hero, placement);

            CombineCopies();
            return true;
        }

        public void ReturnShopHero(Hero hero)
        {
            if (hero != null) UnityEngine.Object.Destroy(hero.gameObject);
        }

        public bool RotateAlign(Hero hero, int step)
        {
            if (Phase != GamePhaseEnum.Preparation || hero == null || hero.Team != TeamEnum.Blue) return false;
            if (!hero.RotateAlignMark(step)) return false;

            RefreshAligning();
            return true;
        }

        internal void RefreshAligning() => _alignBonuses.Refresh(_board, TeamEnum.Blue);

        internal void CombineCopies()
        {
            bool isPreparation = Phase == GamePhaseEnum.Preparation;

            if (_upgrader.CombineAll(includeBoard: isPreparation) && isPreparation) RefreshAligning();
        }

        private void CombineIfBenched(Hero hero)
        {
            if (hero.Team == TeamEnum.Blue && hero.CurrentPlacement is BenchSlot) CombineCopies();
        }
    }
}
