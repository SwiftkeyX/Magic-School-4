using System.Linq;
using UnityEngine;
using MagicSchool.Contracts;
using MagicSchool.Core;
using MagicSchool.Combat.Placements;
using MagicSchool.Input;

namespace MagicSchool.Player
{
    /// FLAGGING: PlayerController ref to many module without using contract.
    /// But I can't do anything since I don't see the pattern well enough to start using the interface.
    /// So I just let it ref to other module directly.
    /// BUT I think if I start making other game, I would see the pattern more clear, and understand what interface player should use
    internal class PlayerController : MonoBehaviour
    {
        [SerializeField] private Camera _cam;
        [SerializeField] private BattleBoard _board;
        private IInspectorPanel _inspector;
        private IHeroCountPanel _heroCountPanel;
        private IRewardPanel _reward;
        private ISellZone _sellZone;
        private Dragging _dragging;
        private TeamEnum _team;
        private PlayerTeamSize _teamSize;


        void Awake()
        {
            _team = TeamEnum.Blue;
        }

        // FLAGGING: Use findObjectByType maybe mess up later.
        void Start()
        {
            var behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            _inspector = behaviours.OfType<IInspectorPanel>().FirstOrDefault();
            _heroCountPanel = behaviours.OfType<IHeroCountPanel>().FirstOrDefault();
            _reward = behaviours.OfType<IRewardPanel>().FirstOrDefault();
            _sellZone = behaviours.OfType<ISellZone>().FirstOrDefault();
            _teamSize = new PlayerTeamSize(_heroCountPanel, _board);
            _dragging = new Dragging(_cam, _board, _sellZone, _team, _teamSize);
        }

        void Update()
        {
            if (GameManager.Instance == null) return;

            TryStartCombat();
            TryRestart();
            TrySetSpeed();
            TryInspect();
            _dragging?.Tick();
            _teamSize?.RefreshHeroCountPanel();
        }

        // BLOCKED on: a "Start Battle" UI button. => Now use manual space-bar to trigger the game for easy testing.
        // Lives on the player side so starting a battle is something input asks for, rather than
        // something GameManager reaches into input to discover.
        private void TryStartCombat()
        {
            if (!PlayerInputSystem.SpacePressedThisFrame) return;

            // if the reward isn't choose yet, choose reward first.
            if (TryReopenReward()) return;

            // if in result state, continue the game, by going to preparation state
            if (GameManager.Instance.Phase == GamePhaseEnum.Result)
            {
                GameManager.Instance.ContinueFromResult();
                return;
            }

            // if in preparation state, start the combat
            else if (GameManager.Instance.Phase == GamePhaseEnum.Preparation)
            {
                GameManager.Instance.StartCombat();
            }
        }

        // if the reward available and isn't choose yet, show the reward panel.
        private bool TryReopenReward()
        {
            if (_reward == null || !_reward.IsChoosing) return false;

            _reward.SetShown(true);
            return true;
        }

        // set the speed of the game
        private void TrySetSpeed()
        {
            int speed = PlayerInputSystem.SpeedPressedThisFrame();
            if (speed == 0) return;

            GameManager.Instance.SetSpeed(speed);
        }

        // quick restart
        private void TryRestart()
        {
            if (!PlayerInputSystem.RestartPressedThisFrame) return;

            GameManager.Instance.Restart();
        }

        // Right-click a hero or an item to inspect it.
        private void TryInspect()
        {
            if (!PlayerInputSystem.InspectPressedThisFrame) return;

            // get which IInspectable that the pointer hit on
            Vector3 worldPos = PlayerInputSystem.GetMouseWorldPosition(_cam);
            IInspectable target = Picker.At<IInspectable>(worldPos);

            // inspect the target
            _inspector?.Inspect(target);
        }
    }
}
