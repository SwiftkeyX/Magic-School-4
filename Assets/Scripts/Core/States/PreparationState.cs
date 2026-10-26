using System.Linq;
using MagicSchool.Contracts;
using MagicSchool.Combat.Heroes;
using MagicSchool.Combat.Placements;
using MagicSchool.Engine;
using MagicSchool.Economy;
using MagicSchool.Items;

namespace MagicSchool.Core.States
{
    /// <summary>
    /// Buying, and putting heroes on the board. Nothing fights.
    /// Left only when the player asks for it - see GameManager.StartCombat.
    /// </summary>
    // Copies bought mid-fight sat on the bench and combine with the board here. Items on hexes stay, as the team does,
    // unless the next board differs. SkipStage does what CombatState.OnEnter would, so the result and the next
    // Preparation see a normal win: the team is remembered where it stands, and the enemies leave without dying, so no
    // death skill fires (a Big Slime would split).
    internal class PreparationState : GameState
    {
        public override GamePhaseEnum StateType => GamePhaseEnum.Preparation;

        public PreparationState(GameManager game) : base(game) { }

        public override void OnEnter()
        {
            _game.Board.SetBattleOn(false);

            _game.SetWinner(null);
            _game.ClearPendingRequests();

            ClearBoard();

            _game.CombineCopies();

            ReturnItems();

            ApplyLayout();

            SeedTeams();

            ApplyDeployZone();

            _game.RefreshAligning();

            if (!_game.ConsumeKeepShop()) _game.Shop.Restock();

            _game.Hint?.ShowPreparation(_game.StageNumber, _game.StageCount);
            _game.Banner?.SetShown(false);
        }

        protected override void CheckSwitchState()
        {
            if (_game.ConsumeSkipStageRequest())
            {
                SkipStage();
                return;
            }

            if (!_game.ConsumeStartCombatRequest()) return;

            bool hasHeroesOnBoard = _game.Board.HeroesOnBoard.Any(h => h.Team == TeamEnum.Blue);
            if (!hasHeroesOnBoard)
            {
                DebugTool.LogWarning("Can't start combat - place at least one hero on the board first.");
                return;
            }

            _game.ChangeState(GamePhaseEnum.Combat);
        }

        private void SkipStage()
        {
            _game.Formation.Remember(_game.Board.HeroesOnBoard, TeamEnum.Blue);
            _game.Recorder?.BeginRound();
            _game.Board.Colours.ShowDeployZone(false);

            _game.Board.ClearTeam(TeamEnum.Red);
            _game.SetWinner(TeamEnum.Blue);
            _game.ChangeState(GamePhaseEnum.Result);
        }

        private void ClearBoard()
        {
            _game.Board.ClearTeam(TeamEnum.Red);

            if (_game.IsPlayerSeed)
            {
                _game.Board.ClearTeam(TeamEnum.Blue);
            }

            else
            {
                ResetTeam(TeamEnum.Blue);

                ReturnTeam();
            }
        }

        private void ReturnTeam()
        {
            if (IsNextStageOnSameBoard()) _game.Formation.ReturnToStartingPlacement();
            else _game.Formation.ReturnToSpareBench();
        }

        private void ReturnItems()
        {
            if (IsNextStageOnSameBoard()) return;

            foreach (IEquipment equipment in _game.Board.Items.TakeAll())
            {
                if (equipment is Item item) ItemDrop.MoveToFreeSpot(item);
            }
        }

        private bool IsNextStageOnSameBoard()
        {
            BattlePlacementSO stage = _game.GetStage(_game.StageIndex);

            return stage == null || stage.BoardLayout == null || _game.Board.HasLayout(stage.BoardLayout);
        }

        private void ApplyLayout()
        {
            BattlePlacementSO stage = _game.GetStage(_game.StageIndex);
            if (stage == null) return;

            if (stage.BoardLayout == null)
            {
                DebugTool.LogError($"{stage.name} has no board layout, so the board keeps the hexes it has. Set its Board Layout.");
                return;
            }

            _game.Board.SetLayout(stage.BoardLayout);
        }

        private void SeedTeams()
        {
            _game.Seed.SwitchSeed(_game.GetStage(_game.StageIndex));

            _game.Seed.SpawnTeamOnBoard(TeamEnum.Red);

            if (_game.IsPlayerSeed) _game.Seed.SpawnTeamOnBoard(TeamEnum.Blue);
        }

        private void ApplyDeployZone()
        {
            BattlePlacementSO stage = _game.GetStage(_game.StageIndex);
            if (stage == null) return;

            _game.Board.DeployZone.Set(stage.DeployZone);
            _game.Board.Colours.ShowDeployZone(true);

            if (stage.DeployZone.Count == 0)
            {
                DebugTool.LogError($"{stage.name} has no deploy zone, so no hero can be placed. Add hexes to its Deploy Zone list.");
            }
        }

        private void ResetTeam(TeamEnum team)
        {
            foreach (ICombatant combatant in _game.Board.HeroesOnBoard)
            {
                if (combatant == null || combatant.Team != team) continue;

                if (combatant is Hero hero) hero.ResetForNewStage();
            }
        }
    }
}
