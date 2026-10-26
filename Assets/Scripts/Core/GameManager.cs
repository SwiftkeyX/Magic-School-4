using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using MagicSchool.Contracts;
using MagicSchool.Combat.Align;
using MagicSchool.Combat.Heroes;
using MagicSchool.Combat.Placements;
using MagicSchool.CombatRecording;
using MagicSchool.Items;
using MagicSchool.Core.States;
using MagicSchool.Economy;
using MagicSchool.Skills;

namespace MagicSchool.Core
{
    // The composition root: builds every service in Awake and wires the panels through Contracts interfaces, and owns
    // the phase, the stage and the player's actions.
    // Every action that moves a hero or an item, wears or takes one off, sells, or turns a pattern works aligning out
    // again: standing and wearing hexes are what the bonuses come from. Turning a pattern is Preparation only, since
    // bonuses are fixed by where everyone stood when the fight started.
    // Combining: only Preparation touches the board; mid-fight the benches combine alone and meet the board at the
    // next Preparation. A combine rebuilds stats, which drops aligning bonuses, so they are given again. A hero
    // spawned on a hex (a seed, a team comp, an enemy) never starts one.
    // Only a fight pauses; CombatState unpauses as it ends, so no other phase is left paused. JumpToStage's flag is
    // not one of ClearPendingRequests' requests, because Preparation clears those first.
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Scene")]
        [SerializeField] private BattleBoard _board;
        [SerializeField] private Bench _bench;
        [Tooltip("Where the heroes that fought are sent after every fight. The player can take heroes off it but not put them on.")]
        [SerializeField] private Bench _spareBench;
        [SerializeField] private TemplateActionRegistrySO _templateActions;

        [Header("Stages")]
        [SerializeField] private BattlePlacementSO[] _stages;
        [SerializeField] private int _startingHeroLimit = 3;

        [Header("Gold")]
        [SerializeField] private int _startingGold = 10;
        [SerializeField] private int _stageIncome = 5;

        [Header("Shop")]
        [SerializeField] private int _shopSlots = 5;
        [SerializeField] private int _refreshCost = 1;
        [SerializeField] private List<HeroDataSO> _shopRoster;
        [SerializeField] private List<ItemDataSO> _shopItems;
        [SerializeField] private ShopOddsSO _shopOdds;

        [Header("Reward")]
        [Tooltip("Offer an item card after every win. Now are exclude from the game")]
        [SerializeField] private bool _useRewardPanel = false;

        [Header("Testing")]
        [SerializeField] private bool _isPlayerSeed;
        [Tooltip("Write every round's numbers to a CSV for balancing. Off in a real build - it writes a file.")]
        [SerializeField] private bool _test;
        [Tooltip("Dev tool: drag, add and remove enemies in Preparation, then save them from Magic School 4 > Stage Editor.")]
        [SerializeField] private bool _stageEditMode;

        private BattlePlacementSO _currentStage;
        private Wallet _wallet;
        private Shop _shop;
        private HeroMover _heroMover;
        private Seller _seller;
        private HeroFormation _heroFormation;
        private AlignBonuses _alignBonuses;
        private HeroSeed _seed;
        private HeroSpawner _heroSpawner;
        private HeroUpgrader _upgrader;
        private StageEditor _stageEditor;
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
        private bool _skipStageRequested;
        private bool _keepShopOnce;

        public GamePhaseEnum Phase => _stateMachine == null ? GamePhaseEnum.Preparation : _stateMachine.CurrentType;
        internal TeamEnum? Winner { get; private set; }
        internal bool IsPlayerSeed => _isPlayerSeed;
        internal int StageIndex => _stageIndex;
        internal bool HasStages => _stages != null && _stages.Length > 0;
        internal int StageCount => HasStages ? _stages.Length : 1;
        internal int StageNumber => _stageIndex + 1;
        public int HeroLimit => _heroLimit;
        public int SpeedMultiplier => _speed == null ? 1 : _speed.Multiplier;
        public bool IsPaused => _speed != null && _speed.IsPaused;
        public StageEditor StageEditor => _stageEditMode ? _stageEditor : null;
        internal bool IsRunCleared => Winner == TeamEnum.Blue && _stageIndex + 1 >= StageCount;

        internal BattlePlacementSO GetStage(int index) => HasStages ? _stages[index] : _currentStage;
        internal bool IsInRun(BattlePlacementSO stage) => HasStages && stage != null && System.Array.IndexOf(_stages, stage) >= 0;
        internal void SetWinner(TeamEnum? winner) => Winner = winner;
        internal void SetStageIndex(int index) => _stageIndex = Mathf.Clamp(index, 0, StageCount - 1);
        internal void GrowHeroLimit() => _heroLimit++;
        internal void ResetHeroLimit() => _heroLimit = _startingHeroLimit;
        internal void PayStageIncome() => _wallet.Earn(_stageIncome);

        internal BattleBoard Board => _board;
        internal IReadOnlyList<HeroDataSO> ShopRoster => _shopRoster;
        internal TemplateActionRegistrySO TemplateActions => _templateActions;
        internal Shop Shop => _shop;
        internal HeroSeed Seed => _seed;
        internal HeroFormation Formation => _heroFormation;
        internal CombatRecorder Recorder => _recorder;
        internal CombatCsvLog BalanceLog => _balanceLog;
        internal IBannerPanel Banner => _banner;
        internal IHintPanel Hint => _hint;
        internal IRewardPanel Reward => _reward;
        internal IScoreboardPanel Scoreboard => _scoreboard;
        internal void ChangeState(GamePhaseEnum next) => _stateMachine.ChangeState(next);
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

        public event System.Action<Hero, int> OnShopHeroPickedUp;

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
            if (hero != null) Destroy(hero.gameObject);
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

        void Awake()
        {
            Instance = this;

            _heroMover = new HeroMover();
            _alignBonuses = new AlignBonuses();
            _seed = new HeroSeed(GetStage(_stageIndex), _board);
            _recorder = new CombatRecorder();

            _speed = new GameSpeed();
            _speed.Reset();

            if (_test) _balanceLog = new CombatCsvLog(Path.Combine(Application.dataPath, "..", "BalanceLogs"));

            _stageEditor = new StageEditor(this);
            _heroSpawner = new HeroSpawner(_heroMover, _templateActions, _recorder, _bench, _seed, _stageEditor);
            _upgrader = new HeroUpgrader(_board, _bench, _spareBench);
            _heroSpawner.OnSpawned += CombineIfBenched;
            _wallet = new Wallet(_startingGold);
            _seller = new Seller(_wallet);
            _shop = new Shop(_wallet, _bench, _shopRoster, _shopItems, _shopOdds, () => StageNumber, _shopSlots, _refreshCost,
                             (data, isStillOffered) => new HeroPreview(data, SkillLibrary.Resolve(data.SkillId, _templateActions), isStillOffered));
            _shop.OnHeroPickedUp += HandOutShopHero;
            _heroFormation = new HeroFormation(_heroMover, _spareBench, _bench);
            _heroLimit = _startingHeroLimit;
            _stateMachine = new GameStateMachine(this);

            MonoBehaviour[] behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            _banner = behaviours.OfType<IBannerPanel>().FirstOrDefault();
            _hint = behaviours.OfType<IHintPanel>().FirstOrDefault();
            _reward = _useRewardPanel ? behaviours.OfType<IRewardPanel>().FirstOrDefault() : null;
            _scoreboard = behaviours.OfType<IScoreboardPanel>().FirstOrDefault();

            behaviours.OfType<IShopPanel>().FirstOrDefault()?.BindShop(_shop);
        }

        void Start()
        {
            _stateMachine.Start(GamePhaseEnum.Preparation);
        }

        void Update() => _stateMachine.Tick();

        public void SetSpeed(int multiplier) => _speed?.Set(multiplier);

        public void TogglePause()
        {
            if (Phase != GamePhaseEnum.Combat) return;

            SetPaused(!IsPaused);
        }

        internal void SetPaused(bool isPaused)
        {
            if (_speed == null || _speed.IsPaused == isPaused) return;

            _speed.SetPaused(isPaused);
            if (isPaused) _hint?.ShowPaused(StageNumber, StageCount);
            else if (Phase == GamePhaseEnum.Combat) _hint?.ShowCombat(StageNumber, StageCount);
        }

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

        public void SkipStage()
        {
            if (Phase != GamePhaseEnum.Preparation && Phase != GamePhaseEnum.Combat) return;

            _skipStageRequested = true;
        }

        public bool JumpToStage(BattlePlacementSO stage)
        {
            if (Phase != GamePhaseEnum.Preparation || !IsInRun(stage)) return false;

            _heroFormation.Remember(_board.HeroesOnBoard, TeamEnum.Blue);
            SetStageIndex(System.Array.IndexOf(_stages, stage));
            _keepShopOnce = true;
            _stateMachine.Reenter();
            return true;
        }

        internal bool ConsumeKeepShop() => Consume(ref _keepShopOnce);

        internal bool ConsumeStartCombatRequest() => Consume(ref _startCombatRequested);
        internal bool ConsumeContinueRequest() => Consume(ref _continueRequested);
        internal bool ConsumeRestartRequest() => Consume(ref _restartRequested);
        internal bool ConsumeSkipStageRequest() => Consume(ref _skipStageRequested);

        internal void ClearPendingRequests()
        {
            _startCombatRequested = false;
            _continueRequested = false;
            _restartRequested = false;
            _skipStageRequested = false;
        }

        private static bool Consume(ref bool requested)
        {
            if (!requested) return false;

            requested = false;
            return true;
        }
    }
}
