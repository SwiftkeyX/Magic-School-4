using System.Collections.Generic;
using UnityEngine;
using MagicSchool.Combat.Heroes.States;
using MagicSchool.Combat.Heroes.Stats;
using MagicSchool.Contracts;
using MagicSchool.Combat.Placements;
using MagicSchool.Skills;

namespace MagicSchool.Combat.Heroes
{
    /// <summary>
    /// Hero don't have any logic inside it BUT:
    /// 1) It's the ONLY Monobehavior for the Hero, so it's here so we could make hero interact with Unity.
    /// 2) it act like a glue, which mean itself don't contain any real logic.
    ///
    /// One class, six files:
    ///     Hero.cs             => fields, Init, Update, and resetting/upgrading between stages
    ///     Hero.Board.cs       => the board, its placement and reservation, aligning
    ///     Hero.Combat.cs      => IEffectable (damage, heal, modifiers) and the combat events
    ///     Hero.Stats.cs       => IHeroStats, mana, the attack cooldown
    ///     Hero.Skill.cs       => skill triggers, and the text the Hero Panel reads
    ///     Hero.Targeting.cs   => ITargeter, handed to FindTarget
    /// </summary>
    // IsDrawn is false while a benchmark runs with nothing drawn: a hero made then has no bars and casts no floating
    // skill name, none of which changes a fight. ResetForNewStage changes state rather than starting it: the Dead
    // state has to be left properly, and a survivor is mid-state, not un-started. SetLevel and UpgradeTo rebuild the
    // stats, so they are for before a fight; current HP and mana start over and items are granted again.
    public partial class Hero : MonoBehaviour, ICombatant, IHexPlaceable, IHeroStats, IInspectableHero, IDraggable, ISellable, ISkillCaster
    {
        private HeroDataSO _SOData;
        private HeroStateMachine _stateMachine;
        private BattleBoard _board;
        private FindTarget _findEnemy;
        private HeroVisuals _visuals;
        private SkillDefinition _skill;
        private int _skillTuningVersion;
        private HeroItems _items;
        private HeroSpawn _spawn;
        private AttackCooldown _attackCooldown;
        private Stat _stat;
        private TeamEnum _team;
        private int _star = HeroStar.Min;
        private int _level = HeroLevel.Min;
        private int _alignPattern;
        private Dictionary<string, List<ICombatant>> _alliesOnSpecialMark = new Dictionary<string, List<ICombatant>>();
        private Vector3 _baseScale;
        private bool _isDummy;

        [SerializeField] private float _moveSpeed = 1f;
        [SerializeField] private AnimationCurve _walkCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private AnimationCurve _attackCurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.5f, 1f), new Keyframe(1f, 0f));

        private Stat Stat => _stat;
        private IPlacement _currentPlacement;
        private Hex _reservedHex;

        public bool IsInitialized => _stat != null;
        public TeamEnum Team => _team;
        public HeroStateEnum StateType => _stateMachine.CurrentType;
        public bool IsDummy => _isDummy;
        public HeroDataSO Data => _SOData;
        public int Star => _star;
        public bool CanUpgrade => _star < HeroStar.Max;
        public int Level => _level;

        public static bool IsDrawn { get; set; } = true;

        public int Price => _SOData != null ? _SOData.Price * HeroStar.Copies(_star) : 0;

        public void ChangeState(HeroStateEnum next) => _stateMachine.ChangeState(next);
        public HeroStateEnum PreviousStateType => _stateMachine.PreviousType;

        public void PlaySkillCastEffect(string skillName) => _visuals.PlaySkillCastEffect(skillName);

        public IReadOnlyList<IEquipment> WornItems => _items.Worn;
        public int WornItemCount => _items.Count;
        public bool HasItemRoom => _items != null && _items.HasRoom;
        public bool TryWear(IEquipment item) => _items != null && _items.TryWear(item);
        public bool TryTakeOff(IEquipment item) => _items != null && _items.TryTakeOff(item);

        public HeroRoleEnum Role => _SOData != null ? _SOData.Role : HeroRoleEnum.None;
        public event System.Action<Hero, HeroRoleEnum, int, ICombatant> OnForgedItemRequested;
        public void ReceiveForgedItem(HeroRoleEnum forRole, int tier, ICombatant forger)
            => OnForgedItemRequested?.Invoke(this, forRole, tier, forger);

        public bool CanForgeFor(HeroRoleEnum role) => _SOData != null && _SOData.CanForgeFor(role);

        public void WearForged(IEquipment item) => _items?.WearForged(item);

        public void Init(HeroDataSO data, BattleBoard board, TeamEnum team, SkillDefinition skill = null)
        {
            _SOData = data;
            _board = board;
            _team = team;

            _stat = new Stat(_SOData, _star, _level);
            _baseScale = transform.localScale;
            _isDummy = _SOData.IsDummy;

            _visuals = GetComponent<HeroVisuals>();
            SetSkill(skill);
            _skillTuningVersion = SkillTuning.Version;
            _items = new HeroItems(this);
            _spawn = new HeroSpawn(this, _board, (unit, unitTeam, placement, onBoard) => OnSpawnRequested?.Invoke(unit, unitTeam, placement, onBoard));
            _findEnemy = new FindTarget(this, _board);
            RollPathLean();
            _attackCooldown = new AttackCooldown();
            _stateMachine = new HeroStateMachine(this, new MovementConfig(_moveSpeed, _walkCurve, _attackCurve));
        }

        void Start()
        {
            if (!IsInitialized) return;

            _stateMachine.Start(HeroStateEnum.Idle);
        }

        void Update()
        {
            if (!IsInitialized) return;

            if (!IsBattleOn) return;

            if (!IsInCombat) return;

            TickModifiers(Time.deltaTime);

            _attackCooldown.Tick(Time.deltaTime, AttackSpeed);

            _stateMachine.Tick();
        }

        void OnDrawGizmos()
        {
            if (!Application.isPlaying || !IsInitialized) return;
            if (StateType != HeroStateEnum.Attack) return;

            ICombatant target = _findEnemy.CurrentTarget;
            if (target == null || !target.IsAlive) return;

            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, target.transform.position);
        }

        public void ResetForNewStage()
        {
            if (!IsInitialized) return;

            _stat = new Stat(_SOData, _star, _level);
            _attackCooldown = new AttackCooldown();
            CancelRevive();

            _items.ClearForged();
            _items.ReGrantAll();

            _stateMachine.ChangeState(HeroStateEnum.Idle);
        }

        public void SetLevel(int level)
        {
            level = HeroLevel.Clamp(level);
            if (!IsInitialized || level == _level) return;

            _level = level;
            _stat = new Stat(_SOData, _star, _level);
            _items.ReGrantAll();
        }

        public bool Upgrade()
        {
            if (!IsInitialized || !CanUpgrade) return false;

            _star++;
            _stat = new Stat(_SOData, _star, _level);
            _items.ReGrantAll();
            transform.localScale = _baseScale * HeroStar.Scale(_star);
            return true;
        }

        public void UpgradeTo(int star)
        {
            while (_star < star && Upgrade()) { }
        }
    }
}
