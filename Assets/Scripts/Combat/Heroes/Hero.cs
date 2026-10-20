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
    public partial class Hero : MonoBehaviour, ICombatant, IHexPlaceable, IHeroStats, IInspectableHero, IDraggable, ISellable, ISpawner, IAligned, IHexMeasurable, IFallen, IFreeCaster
    {
        // ======================================== Dependency ========================================
        private HeroDataSO _SOData;
        private HeroStateMachine _stateMachine;
        private BattleBoard _board;
        private FindTarget _findEnemy;
        private HeroVisuals _visuals;
        private HeroSkill _skill;
        private int _skillTuningVersion;     // the SkillTuning.Version the skill was built with
        private HeroItems _items;
        private HeroSpawn _spawn;
        private AttackCooldown _attackCooldown;
        private Stat _stat;
        private TeamEnum _team;
        private int _star = HeroStar.Min;       // upgraded by combining copies (See HeroStar)
        private int _alignPattern;              // which of its align patterns the hero is turned to (See AlignPattern)
        private Dictionary<string, List<ICombatant>> _alliesOnSpecialMark = new Dictionary<string, List<ICombatant>>();  // allies on each special mark's hexes (See IAligned)
        private Vector3 _baseScale;
        private bool _isDummy;                 // Temporary, tagged once at its source - see the FLAGGING on HeroDataSO._isDummy.

        // ======================================== Etc ========================================
        [SerializeField] private float _moveSpeed = 1f;
        [SerializeField] private AnimationCurve _walkCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        // Hump-shaped (0 -> 1 -> 0): drives the attack dash out toward the enemy and back, not a one-way ease like _walkCurve.
        [SerializeField] private AnimationCurve _attackCurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.5f, 1f), new Keyframe(1f, 0f));

        // ======================================== Runtime data ========================================
        private Stat Stat => _stat;
        private IPlacement _currentPlacement;   // placement hero stand on e.g. hex, benchslot
        private Hex _reservedHex;               // hex that hero reserved. use while battle

        // ======================================== other getter ========================================
        public bool IsInitialized => _stat != null;
        public TeamEnum Team => _team;
        public HeroStateEnum StateType => _stateMachine.CurrentType;
        public bool IsDummy => _isDummy;
        public HeroDataSO Data => _SOData;
        public int Star => _star;
        public bool CanUpgrade => _star < HeroStar.Max;

        // what the shop charged for every copy that went into him, and what selling him refunds
        public int Price => _SOData != null ? _SOData.Price * HeroStar.Copies(_star) : 0;

        // ======================================== state ========================================
        public void ChangeState(HeroStateEnum next) => _stateMachine.ChangeState(next);
        public HeroStateEnum PreviousStateType => _stateMachine.PreviousType;

        // ======================================== visuals ========================================
        public void PlaySkillCastEffect(string skillName) => _visuals.PlaySkillCastEffect(skillName);

        // ======================================== items ========================================
        public IReadOnlyList<IEquipment> WornItems => _items.Worn;
        public int WornItemCount => _items.Count;
        public bool HasItemRoom => _items != null && _items.HasRoom;
        public bool TryWear(IEquipment item) => _items != null && _items.TryWear(item);
        public bool TryTakeOff(IEquipment item) => _items != null && _items.TryTakeOff(item);

        // ======================================== life cycle ========================================
        // Give everything a hero needs to exist, in one call. 
        public void Init(HeroDataSO data, BattleBoard board, TeamEnum team, SkillDefinition skill = null)
        {
            _SOData = data;
            _board = board;
            _team = team;

            _stat = new Stat(_SOData, _star);
            _baseScale = transform.localScale;
            _isDummy = _SOData.IsDummy;

            _visuals = GetComponent<HeroVisuals>();
            _skill = new HeroSkill(this, skill);
            _skillTuningVersion = SkillTuning.Version;
            _items = new HeroItems(this);
            _spawn = new HeroSpawn(this, _board, (unit, unitTeam, placement, onBoard) => OnSpawnRequested?.Invoke(unit, unitTeam, placement, onBoard));
            _findEnemy = new FindTarget(this, _board);
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

            // if combat not start, return
            if (!IsBattleOn) return;

            // Some hero are not on BattleBoard but was in the bench. They don't consider in combat.
            if (!IsInCombat) return;

            TickModifiers(Time.deltaTime);

            // update auto attack cooldown
            _attackCooldown.Tick(Time.deltaTime, AttackSpeed);

            _stateMachine.Tick();
        }

        // draw gize between attacker and receiver to show which hero is attacking.
        void OnDrawGizmos()
        {
            if (!Application.isPlaying || !IsInitialized) return;
            if (StateType != HeroStateEnum.Attack) return;

            ICombatant target = _findEnemy.CurrentTarget;
            if (target == null || !target.IsAlive) return;

            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, target.transform.position);
        }

        // when the stage is reset, make hero alive again.
        public void ResetForNewStage()
        {
            if (!IsInitialized) return;

            _stat = new Stat(_SOData, _star);
            _attackCooldown = new AttackCooldown();

            // the modifier from items is also reset, so re-grant it.
            _items.ReGrantAll();

            // ChangeState, not Start: the Dead state has to be left properly, and a hero that
            // survived is already mid-state rather than un-started.
            _stateMachine.ChangeState(HeroStateEnum.Idle);
        }

        public bool Upgrade()
        {
            if (!IsInitialized || !CanUpgrade) return false;

            _star++;
            _stat = new Stat(_SOData, _star);
            _items.ReGrantAll();
            transform.localScale = _baseScale * HeroStar.Scale(_star);
            return true;
        }
    }
}
