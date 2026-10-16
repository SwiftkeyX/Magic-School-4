using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;
using MagicSchool.Contracts;
using MagicSchool.Combat.Heroes;
using MagicSchool.Combat.Placements;
using MagicSchool.Items;

namespace MagicSchool.Core
{
    /// <summary>
    /// Dev tool: during play mode, build a enemy team, add/remove deploy zone. 
    /// This fix the problem of manualy typing enemy team into a BattlePlacementSO.
    ///     move enemy          - drag an enemy to any free hex (not deploy zone)
    ///     add new enemy       - TryAddEnemy puts a new enemy on a free hex (not deploy zone)
    ///     remove enemy        - drop an enemy on the shop
    ///     paint deploy zone   - click an empty hex to put it in or out of the zone
    ///     save formation      - ReadEnemyTeam() and ReadDeployZone() are what the board holds right now; the menu writes them into an asset
    ///     team comp           - ReadPlayerTeam() and ReadItemsOnHexes() save the player's heroes on the board, their items and
    ///                           the items on hexes; LoadPlayerTeam() puts a saved comp back
    ///
    /// Tick GameManager._stageEditMode to use this tool.
    /// </summary>
    public class StageEditor
    {
        private const TeamEnum EnemyTeam = TeamEnum.Red;
        private const TeamEnum PlayerTeam = TeamEnum.Blue;

        private readonly GameManager _game;

        public event Action<HeroDataSO, TeamEnum, IPlacement, BattleBoard> OnSpawnRequested;

        internal StageEditor(GameManager game)
        {
            _game = game;
        }

        // ======================================== getter ========================================
        public BattlePlacementSO CurrentStage => _game.GetStage(_game.StageIndex);
        public bool CanEditNow => _game.Phase == GamePhaseEnum.Preparation;

        // ======================================== add ========================================
        // put a new enemy on the first free enemy hex
        public bool TryAddEnemy(HeroDataSO data)
        {
            if (data == null || !CanEditNow) return false;
            if (OnSpawnRequested == null) { Debug.LogError("Can't add the enemy. Nothing is listening for spawn requests."); return false; }

            Hex free = FirstFreeEnemyHex();
            if (free == null) return false;

            OnSpawnRequested.Invoke(data, EnemyTeam, free, _game.Board);
            return true;
        }

        // ======================================== read ========================================
        // get every enemy on the board and the hex it stands on
        public List<HeroPlacement> ReadEnemyTeam() => ReadTeam(EnemyTeam);

        // get every player hero on the board and the hex it stands on (the bench is not part of a comp)
        public List<HeroPlacement> ReadPlayerTeam() => ReadTeam(PlayerTeam);

        private List<HeroPlacement> ReadTeam(TeamEnum team)
        {
            return _game.Board.HeroesOnBoard
                .OfType<Hero>()
                .Where(hero => hero != null && hero.Team == team && hero.CurrentHex != null)
                .Select(hero => new HeroPlacement(hero.Data, hero.Team, hero.CurrentHex.Number, WornItemData(hero), hero.AlignPattern))
                .OrderBy(placement => placement.hexPlacement.column)
                .ThenBy(placement => placement.hexPlacement.row)
                .ToList();
        }

        // the item the hero wears, as data. A hero wears one item at most (See HeroItems.Capacity)
        private static ItemDataSO WornItemData(Hero hero)
            => hero.WornItems.OfType<Item>().FirstOrDefault(item => item != null)?.Data;

        // get every item lying on a hex and the hex it lies on
        public List<ItemPlacement> ReadItemsOnHexes()
        {
            return _game.Board.Items.All()
                .Where(pair => pair.item is Item item && item.Data != null)
                .Select(pair => new ItemPlacement(((Item)pair.item).Data, pair.hex.Number))
                .ToList();
        }

        // get current deploy zone 
        public List<HexNumber> ReadDeployZone() => _game.Board.DeployZone.GetDeployZone();

        // ======================================== deploy zone ========================================
        // Toggle hex into deploy zone
        public bool TryToggleDeployHex(Hex hex)
        {
            if (hex == null || !CanEditNow) return false;
            if (_game.Board.WhoReservedThisHex(hex) != null) return false;

            _game.Board.DeployZone.Toggle(hex);
            return true;
        }

        // ======================================== write ========================================
        // how many enemies now?
        public int EnemyCountIn(BattlePlacementSO stage)
            => stage.HeroesPlacement.Count(placement => placement.team == EnemyTeam);

        // how many hexes in the deploy zone now?
        public int DeployZoneCountIn(BattlePlacementSO stage) => stage.DeployZone.Count;

        // overwrite enemy team's SO to the specify one
        public void WriteEnemyTeam(BattlePlacementSO stage, IEnumerable<HeroPlacement> team)
            => stage.ReplaceTeam(EnemyTeam, team);

        // overwrite the deploy zone's SO to the specify one
        public void WriteDeployZone(BattlePlacementSO stage, IEnumerable<HexNumber> zone)
            => stage.ReplaceDeployZone(zone);

        // ======================================== team comp ========================================
        // A team comp is a BattlePlacementSO holding blue entries only, plus the board they were placed on:
        // a hex number only means something on its own layout.
        public int PlayerCountOnBoard => _game.Board.CountTeamOnBoard(PlayerTeam);
        public int PlayerCountIn(BattlePlacementSO comp) => comp.HeroesPlacement.Count(placement => placement.team == PlayerTeam);

        // overwrite the player team of the SO (worn items included) and the items on hexes, and remember which board it stands on
        public void WritePlayerTeam(BattlePlacementSO comp, IEnumerable<HeroPlacement> team, IEnumerable<ItemPlacement> itemsOnHexes)
        {
            comp.ReplaceTeam(PlayerTeam, team);
            comp.ReplaceItemsOnHexes(itemsOnHexes);
            comp.SetBoardLayout(_game.Board.Layout);
        }

        // can the comp's hex numbers be read on the board as it is now?
        public bool FitsThisBoard(BattlePlacementSO comp) => comp.BoardLayout == null || _game.Board.HasLayout(comp.BoardLayout);

        // swap the player's team on the board for the comp's, with the items they wear and the items on hexes.
        // The heroes on the board go with the items they wear; items already lying on hexes go to the loose row.
        // The bench is left alone, and no gold changes hands. Returns how many heroes were put down.
        public int LoadPlayerTeam(BattlePlacementSO comp)
        {
            if (comp == null || !CanEditNow) return 0;
            if (OnSpawnRequested == null) { Debug.LogError("Can't load the team. Nothing is listening for spawn requests."); return 0; }

            BattleBoard board = _game.Board;
            board.ClearTeam(PlayerTeam);
            foreach (IEquipment lying in board.Items.TakeAll())
            {
                if (lying is Item item) ItemDrop.MoveToFreeSpot(item);
            }

            int placed = 0;
            foreach (HeroPlacement placement in comp.HeroesPlacement)
            {
                if (placement.team != PlayerTeam || placement.dataSO == null) continue;

                if (!board.Hexs.TryGetValue(placement.hexPlacement, out Hex hex) || board.WhoReservedThisHex(hex) != null)
                {
                    Debug.LogWarning($"[StageEditor] skipped {placement.dataSO.name}: hex {placement.hexPlacement.column},{placement.hexPlacement.row} is missing or taken.");
                    continue;
                }

                OnSpawnRequested.Invoke(placement.dataSO, PlayerTeam, hex, board);
                placed++;

                // turned the way it was saved
                if (board.WhoReservedThisHex(hex) is Hero turned) turned.SetAlignMarkPattern(placement.alignPattern);

                // the spawn is immediate, so the hero is on the hex now
                if (placement.item is ItemDataSO itemData && board.WhoReservedThisHex(hex) is Hero hero)
                {
                    Item worn = Item.Spawn(itemData, hex.transform.position);
                    if (worn != null && !hero.TryWear(worn)) ItemDrop.MoveToFreeSpot(worn);
                }
            }

            foreach (ItemPlacement lying in comp.ItemsOnHexes)
            {
                if (!(lying.item is ItemDataSO itemData)) continue;

                Item item = Item.Spawn(itemData, Vector3.zero);
                if (item == null) continue;

                // the hex must still be there and free - a hero or another item on it means it can't lie there
                bool isLaid = board.Hexs.TryGetValue(lying.hexPlacement, out Hex hex)
                              && board.WhoReservedThisHex(hex) == null
                              && board.Items.Place(item, hex);
                if (isLaid) continue;

                Debug.LogWarning($"[StageEditor] {itemData.name} could not lie on hex {lying.hexPlacement.column},{lying.hexPlacement.row} - it went to the loose row.");
                ItemDrop.MoveToFreeSpot(item);
            }

            _game.RefreshAligning();
            return placed;
        }

        // ======================================== private ========================================
        // an enemy may stand on any hex outside the deploy zone.
        private Hex FirstFreeEnemyHex()
        {
            BattleBoard board = _game.Board;

            return board.Hexs
                .Where(pair => !pair.Value.IsInDeployZone && board.WhoReservedThisHex(pair.Value) == null)
                .OrderByDescending(pair => pair.Key.column)
                .ThenBy(pair => pair.Key.row)
                .Select(pair => pair.Value)
                .FirstOrDefault();
        }
    }
}
