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
    ///     load stage          - LoadStage() makes the run play a stage of GameManager._stages (See GameManager.JumpToStage);
    ///                           LoadEnemyTeam() only puts a stage's enemies (and its deploy zone) on the board, for one outside the run
    ///     team comp           - ReadPlayerTeam() and ReadItemsOnHexes() save the player's heroes on the board, their items and
    ///                           the items on hexes; LoadPlayerTeam() puts a saved comp back
    ///
    /// Tick GameManager._stageEditMode to use this tool.
    /// </summary>
    // A team comp is a BattlePlacementSO of blue entries plus the board they were placed on: a hex number only means
    // something on its own layout. A quick save holds both teams; on load the player's heroes go first, so none stands
    // on a hex a saved enemy needs.
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

        public BattlePlacementSO CurrentStage => _game.GetStage(_game.StageIndex);
        public bool CanEditNow => _game.Phase == GamePhaseEnum.Preparation;
        public int EnemyCountOnBoard => _game.Board.CountTeamOnBoard(EnemyTeam);

        public bool EditsDeployZone { get; set; } = true;

        public int NewEnemyLevel { get; set; } = HeroLevel.Min;

        public List<Hero> EnemiesOnBoard => _game.Board.HeroesOnBoard
            .OfType<Hero>()
            .Where(hero => hero != null && hero.Team == EnemyTeam && hero.CurrentHex != null)
            .OrderBy(hero => hero.CurrentHex.Number.column)
            .ThenBy(hero => hero.CurrentHex.Number.row)
            .ToList();

        public void SetLevel(Hero enemy, int level)
        {
            if (enemy == null || !CanEditNow || enemy.Team != EnemyTeam) return;
            enemy.SetLevel(level);
        }

        public bool TryAddEnemy(HeroDataSO data)
        {
            if (data == null || !CanEditNow) return false;
            if (OnSpawnRequested == null) { Debug.LogError("Can't add the enemy. Nothing is listening for spawn requests."); return false; }

            Hex free = FirstFreeEnemyHex();
            if (free == null) return false;

            OnSpawnRequested.Invoke(data, EnemyTeam, free, _game.Board);
            if (_game.Board.WhoReservedThisHex(free) is Hero added) added.SetLevel(NewEnemyLevel);
            return true;
        }

        public List<HeroPlacement> ReadEnemyTeam() => ReadTeam(EnemyTeam);

        public List<HeroPlacement> ReadPlayerTeam() => ReadTeam(PlayerTeam);

        private List<HeroPlacement> ReadTeam(TeamEnum team)
        {
            return _game.Board.HeroesOnBoard
                .OfType<Hero>()
                .Where(hero => hero != null && hero.Team == team && hero.CurrentHex != null)
                .Select(hero => new HeroPlacement(hero.Data, hero.Team, hero.CurrentHex.Number, WornItemData(hero), hero.AlignPattern, hero.Level, hero.Star))
                .OrderBy(placement => placement.hexPlacement.column)
                .ThenBy(placement => placement.hexPlacement.row)
                .ToList();
        }

        private static ItemDataSO WornItemData(Hero hero)
            => hero.WornItems.OfType<Item>().FirstOrDefault(item => item != null)?.Data;

        public List<ItemPlacement> ReadItemsOnHexes()
        {
            return _game.Board.Items.All()
                .Where(pair => pair.item is Item item && item.Data != null)
                .Select(pair => new ItemPlacement(((Item)pair.item).Data, pair.hex.Number))
                .ToList();
        }

        public List<HexNumber> ReadDeployZone() => _game.Board.DeployZone.GetDeployZone();

        public bool TryToggleDeployHex(Hex hex)
        {
            if (hex == null || !CanEditNow || !EditsDeployZone) return false;
            if (_game.Board.WhoReservedThisHex(hex) != null) return false;

            _game.Board.DeployZone.Toggle(hex);
            return true;
        }

        public int EnemyCountIn(BattlePlacementSO stage)
            => stage.HeroesPlacement.Count(placement => placement.team == EnemyTeam);

        public int DeployZoneCountIn(BattlePlacementSO stage) => stage.DeployZone.Count;

        public void WriteEnemyTeam(BattlePlacementSO stage, IEnumerable<HeroPlacement> team)
            => stage.ReplaceTeam(EnemyTeam, team);

        public void WriteDeployZone(BattlePlacementSO stage, IEnumerable<HexNumber> zone)
            => stage.ReplaceDeployZone(zone);

        public int PlayerCountOnBoard => _game.Board.CountTeamOnBoard(PlayerTeam);
        public int PlayerCountIn(BattlePlacementSO comp) => comp.HeroesPlacement.Count(placement => placement.team == PlayerTeam);

        public void WritePlayerTeam(BattlePlacementSO comp, IEnumerable<HeroPlacement> team, IEnumerable<ItemPlacement> itemsOnHexes)
        {
            comp.ReplaceTeam(PlayerTeam, team);
            comp.ReplaceItemsOnHexes(itemsOnHexes);
            comp.SetBoardLayout(_game.Board.Layout);
        }

        public bool IsInRun(BattlePlacementSO stage) => _game.IsInRun(stage);

        public bool LoadStage(BattlePlacementSO stage) => CanEditNow && _game.JumpToStage(stage);

        public bool FitsThisBoard(BattlePlacementSO comp) => comp.BoardLayout == null || _game.Board.HasLayout(comp.BoardLayout);

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

                if (board.WhoReservedThisHex(hex) is Hero turned) turned.SetAlignMarkPattern(placement.alignPattern);

                if (board.WhoReservedThisHex(hex) is Hero upgraded) upgraded.UpgradeTo(placement.Star);

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

        public int LoadEnemyTeam(BattlePlacementSO stage, bool withZone)
        {
            if (stage == null || !CanEditNow) return 0;
            if (OnSpawnRequested == null) { Debug.LogError("Can't load the enemies. Nothing is listening for spawn requests."); return 0; }

            BattleBoard board = _game.Board;
            board.ClearTeam(EnemyTeam);
            if (withZone)
            {
                board.DeployZone.Set(stage.DeployZone);
                board.Colours.ShowDeployZone(true);
            }

            int placed = 0;
            foreach (HeroPlacement placement in stage.HeroesPlacement)
            {
                if (placement.team != EnemyTeam || placement.dataSO == null) continue;

                if (!board.Hexs.TryGetValue(placement.hexPlacement, out Hex hex) || board.WhoReservedThisHex(hex) != null)
                {
                    Debug.LogWarning($"[StageEditor] skipped {placement.dataSO.name}: hex {placement.hexPlacement.column},{placement.hexPlacement.row} is missing or taken.");
                    continue;
                }

                OnSpawnRequested.Invoke(placement.dataSO, EnemyTeam, hex, board);
                if (board.WhoReservedThisHex(hex) is Hero enemy) enemy.SetLevel(placement.Level);
                placed++;
            }

            _game.RefreshAligning();
            return placed;
        }

        public void WriteQuickSave(BattlePlacementSO save)
        {
            WritePlayerTeam(save, ReadPlayerTeam(), ReadItemsOnHexes());
            WriteEnemyTeam(save, ReadEnemyTeam());
            WriteDeployZone(save, ReadDeployZone());
        }

        public (int players, int enemies) LoadQuickSave(BattlePlacementSO save)
        {
            if (save == null || !CanEditNow) return (0, 0);

            _game.Board.ClearTeam(PlayerTeam);
            int enemies = LoadEnemyTeam(save, withZone: true);
            int players = LoadPlayerTeam(save);
            return (players, enemies);
        }

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
