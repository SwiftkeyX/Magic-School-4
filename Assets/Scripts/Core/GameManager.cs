using System.Linq;
using System.IO;
using UnityEngine;
using MagicSchool.Contracts;
using MagicSchool.Combat.Placements;
using MagicSchool.CombatRecording;
using MagicSchool.Core.States;
using MagicSchool.Skills;

namespace MagicSchool.Core
{
    // FLAGGING: The hero limit should stay in PlayerController.cs, 
    // it was here now temporarily for quick demo.
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        // ============================================= dependency =============================================
        [SerializeField] private BattleBoard _board;
        [SerializeField] private Bench _bench;
        [SerializeField] private BattlePlacementSO[] _stages;
        [SerializeField] private TemplateActionRegistrySO _templateActions;
        [SerializeField] private bool _isPlayerSeed;

        [Tooltip("Write every round's numbers to a CSV for balancing. Off in a real build - it writes a file.")]
        [SerializeField] private bool _test;
        [SerializeField] private int _startingHeroLimit = 3;   // how many heroes the player may field on stage 1
        private BattlePlacementSO _currentStage;
        private HeroMover _heroMover;
        private HeroSeller _heroSeller;
        private HeroFormation _heroFormation;
        private HeroSeed _seed;
        private HeroSpawner _heroSpawner;
        private CombatRecorder _recorder;
        private GameSpeed _speed;
        private CombatCsvLog _balanceLog;
        private GameStateMachine _stateMachine;
        private IBannerPanel _banner;
        private IHintPanel _hint;
        private IRewardPanel _reward;
        private IScoreboardPanel _scoreboard;
        private int _stageIndex;
        private int _heroLimit;
        private bool _startCombatRequested;
        private bool _continueRequested;
        private bool _restartRequested;

        // ======================================== getter ========================================
        public GamePhaseEnum Phase => _stateMachine == null ? GamePhaseEnum.Preparation : _stateMachine.CurrentType;
        internal TeamEnum? Winner { get; private set; }
        internal bool IsPlayerSeed => _isPlayerSeed;
        internal int StageIndex => _stageIndex;
        internal bool HasStages => _stages != null && _stages.Length > 0;
        internal int StageCount => HasStages ? _stages.Length : 1;
        internal int StageNumber => _stageIndex + 1;
        public int HeroLimit => _heroLimit;
        public int SpeedMultiplier => _speed == null ? 1 : _speed.Multiplier;
        internal bool IsRunCleared => Winner == TeamEnum.Blue && _stageIndex + 1 >= StageCount;

        // ======================================== setter ========================================
        internal BattlePlacementSO GetStage(int index) => HasStages ? _stages[index] : _currentStage;
        internal void SetWinner(TeamEnum? winner) => Winner = winner;
        internal void SetStageIndex(int index) => _stageIndex = Mathf.Clamp(index, 0, StageCount - 1);
        internal void GrowHeroLimit() => _heroLimit++;
        internal void ResetHeroLimit() => _heroLimit = _startingHeroLimit;

        // === forwarding ===
        internal BattleBoard Board => _board;
        internal HeroSeed Seed => _seed;
        internal HeroFormation Formation => _heroFormation;
        internal CombatRecorder Recorder => _recorder;
        internal CombatCsvLog BalanceLog => _balanceLog;
        internal IBannerPanel Banner => _banner;
        internal IHintPanel Hint => _hint;
        internal IRewardPanel Reward => _reward;
        internal IScoreboardPanel Scoreboard => _scoreboard;
        internal void ChangeState(GamePhaseEnum next) => _stateMachine.ChangeState(next);
        public void MoveHero(ICombatant hero, IPlacement placement) => _heroMover.MoveThisHeroTo(hero, placement);
        public void SellHero(ICombatant hero)
        {
            if (Phase != GamePhaseEnum.Preparation) return;

            _heroSeller.Sell(hero);
        }

        // ======================================== life cycle ========================================
        // init dependency
        void Awake()
        {
            Instance = this;

            _heroMover = new HeroMover();
            _seed = new HeroSeed(GetStage(_stageIndex), _board);
            _recorder = new CombatRecorder();

            _speed = new GameSpeed();
            _speed.Reset();

            if (_test) _balanceLog = new CombatCsvLog(Path.Combine(Application.dataPath, "..", "BalanceLogs"));

            _heroSpawner = new HeroSpawner(_heroMover, _bench, _seed, _templateActions, _recorder);
            _heroSeller = new HeroSeller();
            _heroFormation = new HeroFormation(_heroMover);
            _heroLimit = _startingHeroLimit;
            _stateMachine = new GameStateMachine(this);

            MonoBehaviour[] behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            _banner = behaviours.OfType<IBannerPanel>().FirstOrDefault();
            _hint = behaviours.OfType<IHintPanel>().FirstOrDefault();
            _reward = behaviours.OfType<IRewardPanel>().FirstOrDefault();
            _scoreboard = behaviours.OfType<IScoreboardPanel>().FirstOrDefault();
        }

        // start the game at preparation state
        void Start()
        {
            _stateMachine.Start(GamePhaseEnum.Preparation);
        }

        void Update() => _stateMachine.Tick();

        // ========================================= request handler =========================================
        // set game's speed to x1 / x2 / x3.
        public void SetSpeed(int multiplier) => _speed?.Set(multiplier);

        public void StartCombat()
        {
            if (Phase != GamePhaseEnum.Preparation) return;

            _startCombatRequested = true;
        }

        public void ContinueFromResult()
        {
            if (Phase != GamePhaseEnum.Result) return;

            _continueRequested = true;
        }

        public void Restart()
        {
            _restartRequested = true;
        }

        // consume a request, then clear the flag to false immediately:
        // 1) start combat => let hero fight between heroes
        // 2) continue => move to a next stage, or repeat the same stage
        // 3) restart => repeat this stage
        internal bool ConsumeStartCombatRequest() => Consume(ref _startCombatRequested);
        internal bool ConsumeContinueRequest() => Consume(ref _continueRequested);
        internal bool ConsumeRestartRequest() => Consume(ref _restartRequested);

        // reset all request
        internal void ClearPendingRequests()
        {
            _startCombatRequested = false;
            _continueRequested = false;
            _restartRequested = false;
        }

        // if request exist, consume it, and clear the flag
        private static bool Consume(ref bool requested)
        {
            if (!requested) return false;

            requested = false;
            return true;
        }
    }
}
