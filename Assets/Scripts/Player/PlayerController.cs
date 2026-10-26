using System.Linq;
using UnityEngine;
using MagicSchool.Contracts;
using MagicSchool.Core;
using MagicSchool.Combat.Heroes;
using MagicSchool.Combat.Placements;
using MagicSchool.Input;

namespace MagicSchool.Player
{
    /// FLAGGING: PlayerController ref to many module without using contract.
    /// But I can't do anything since I don't see the pattern well enough to start using the interface.
    /// So I just let it ref to other module directly.
    /// BUT I think if I start making other game, I would see the pattern more clear, and understand what interface player should use
    // Starting a battle lives on the player side so it is something input asks for, not something GameManager reaches
    // into input to discover. The space bar stands in for a Start Battle button.
    internal class PlayerController : MonoBehaviour
    {
        [SerializeField] private Camera _cam;
        [SerializeField] private BattleBoard _board;
        private IInspectorPanel _inspector;
        private IHeroCountPanel _heroCountPanel;
        private IRewardPanel _reward;
        private ISellZone _sellZone;
        private Dragging _dragging;
        private ZonePainting _zonePainting;
        private AlignHighlight _alignHighlight;
        private TeamEnum _team;
        private PlayerTeamSize _teamSize;


        void Awake()
        {
            _team = TeamEnum.Blue;
        }

        void Start()
        {
            var behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            _inspector = behaviours.OfType<IInspectorPanel>().FirstOrDefault();
            _heroCountPanel = behaviours.OfType<IHeroCountPanel>().FirstOrDefault();
            _reward = behaviours.OfType<IRewardPanel>().FirstOrDefault();
            _sellZone = behaviours.OfType<ISellZone>().FirstOrDefault();
            _teamSize = new PlayerTeamSize(_heroCountPanel, _board);
            _dragging = new Dragging(_cam, _board, _sellZone, _team, _teamSize);
            _zonePainting = new ZonePainting(_cam, _dragging);
            _alignHighlight = new AlignHighlight(_cam, _dragging, _board, _team);
        }

        void Update()
        {
            if (GameManager.Instance == null) return;

            TryStartCombat();
            TryRestart();
            TrySkipStage();
            TrySetSpeed();
            if (!TryRotateAlign()) TryInspect();
            _dragging?.Tick();
            _zonePainting?.Tick();
            _alignHighlight?.Tick();
            _teamSize?.RefreshHeroCountPanel();
        }

        private void TryStartCombat()
        {
            if (!PlayerInputSystem.SpacePressedThisFrame) return;

            if (TryReopenReward()) return;

            if (GameManager.Instance.Phase == GamePhaseEnum.Result)
            {
                GameManager.Instance.ContinueFromResult();
                return;
            }

            else if (GameManager.Instance.Phase == GamePhaseEnum.Preparation)
            {
                GameManager.Instance.StartCombat();
            }

            else if (GameManager.Instance.Phase == GamePhaseEnum.Combat)
            {
                GameManager.Instance.TogglePause();
            }
        }

        private bool TryReopenReward()
        {
            if (_reward == null || !_reward.IsChoosing) return false;

            _reward.SetShown(true);
            return true;
        }

        private void TrySetSpeed()
        {
            int speed = PlayerInputSystem.SpeedPressedThisFrame();
            if (speed == 0) return;

            GameManager.Instance.SetSpeed(speed);
        }

        private void TryRestart()
        {
            if (!PlayerInputSystem.RestartPressedThisFrame) return;

            GameManager.Instance.Restart();
        }

        private void TrySkipStage()
        {
            if (!PlayerInputSystem.SkipStagePressedThisFrame) return;

            GameManager.Instance.SkipStage();
        }

        private bool TryRotateAlign()
        {
            Hero held = _dragging?.HeldHero;
            if (!PlayerInputSystem.InspectPressedThisFrame || held == null) return false;

            GameManager.Instance.RotateAlign(held, 1);
            return true;
        }

        private void TryInspect()
        {
            if (!PlayerInputSystem.InspectPressedThisFrame) return;

            Vector3 worldPos = PlayerInputSystem.GetMouseWorldPosition(_cam);
            IInspectable target = Picker.At<IInspectable>(worldPos);

            if (TrySwitchTarget(target)) return;

            _inspector?.Inspect(target);
        }

        private bool TrySwitchTarget(IInspectable target)
        {
            if (GameManager.Instance.Phase != GamePhaseEnum.Preparation) return false;
            if (!(target is Hero hero) || hero.Team != _team || hero.CurrentHex == null) return false;
            if (_inspector == null || !ReferenceEquals(_inspector.Shown, target)) return false;

            return hero.SwitchChosenTarget();
        }
    }
}
