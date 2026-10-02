using System;
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
    /// </summary>
    public class Hero : MonoBehaviour, ICombatant, IHexPlaceable, IHeroStats, IInspectableHero, IDraggable
    {
        // ======================================== Dependency ========================================
        private HeroDataSO _SOData;
        private HeroStateMachine _stateMachine;
        private BattleBoard _board;
        private FindTarget _findEnemy;
        private HeroVisuals _visuals;
        private HeroSkill _skill;
        private HeroItems _items;
        private AttackCooldown _attackCooldown;
        private Stat _stat;
        private TeamEnum _team;
        private bool _isDummy;                  // Temporary, tagged once at its source - see the FLAGGING on HeroDataSO._isDummy.

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

        // ======================================== state ========================================
        public void ChangeState(HeroStateEnum next) => _stateMachine.ChangeState(next);
        public HeroStateEnum PreviousStateType => _stateMachine.PreviousType;

        // ======================================== board ========================================
        public ICombatant WhoReservedThisHex(Hex hex) => _board.WhoReservedThisHex(hex);
        public bool IsHexReservedByOther(Hex hex) => _board.IsReservedByOther(hex, this);
        public bool IsBattleOn => _board == null || _board.IsBattleOn;

        public IReadOnlyList<ICombatant> HeroesOnBoard => _board != null ? _board.HeroesOnBoard : new List<ICombatant>();
        public void TrackOnBoard() { if (_board != null) _board.TrackThisHero(this); }
        public void UntrackFromBoard() { if (_board != null) _board.UntrackThisHero(this); }

        // ======================================== visuals ========================================
        public void SetDeadVisual() => _visuals.SetDeadVisual();
        public void SetAliveVisual() => _visuals.SetAliveVisual();
        public void PlaySkillCastEffect(string skillName) => _visuals.PlaySkillCastEffect(skillName);

        // ======================================== items ========================================
        public IReadOnlyList<IEquipment> WornItems => _items.Worn;
        public int WornItemCount => _items.Count;
        public bool HasItemRoom => _items != null && _items.HasRoom;
        public bool TryWear(IEquipment item) => _items != null && _items.TryWear(item);
        public bool TryTakeOff(IEquipment item) => _items != null && _items.TryTakeOff(item);

        // ======================================== skill ========================================
        public bool TriggerActiveSkill(bool isManaCapped) => _skill.TriggerOnCastSkill(isManaCapped);
        public bool TriggerOnAutoAttack(ICombatant target) => _skill.TriggerOnAutoAttack(target);
        public ICombatant PickAutoAttackTarget(ICombatant target) => _skill.PickAutoAttackTarget(target);
        public bool TriggerOnCombatStart() => _skill.TriggerOnCombatStart();
        public void TriggerOnHeroDied(ICombatant dead) => _skill.TriggerOnHeroDied(dead);
        public void TriggerOnHeroBurned(ICombatant burned) => _skill.TriggerOnHeroBurned(burned);
        public float GetCastTime() => _skill.GetCastTime();
        public bool IsSkillRepeating() => _skill.IsSkillRepeating();

        // what the Hero Panel reads
        public bool HasSkill => _skill != null && _skill.HasSkill;
        public bool HasAutoAttackPassive => _skill != null && _skill.HasAutoAttackPassive;
        public string SkillName => _skill != null ? _skill.SkillName : string.Empty;
        public string SkillDescription => _skill != null ? _skill.Description : string.Empty;
        public string PassiveDescription => _skill != null ? _skill.PassiveDescription : string.Empty;
        public string DisplayName => _SOData != null ? _SOData.Name : name;

        // ======================================== attack ========================================
        public bool IsAttackReady => _attackCooldown.IsReady(AttackSpeed);
        public void SpendAttack() => _attackCooldown.Spend();

        // ======================================== stat ========================================
        public void GainMana(int amount) => Stat.AddMana(amount);      // return true if mana if capped
        public bool IsManaCapped() => Stat.IsManaCapped();
        public void SpendMana() => Stat.SpendMana();                   // called once a cast actually happened
        public void TickModifiers(float deltaTime) => Stat.TickModifiers(deltaTime);

        // ======================================== combat event ========================================
        public event Action<DamageEvent> OnDamaged;
        public event Action<HealEvent> OnHealed;

        // ======================================== interface method ========================================
        // === IEffectable ===
        public bool IsAlive => this != null && IsInitialized && StateType != HeroStateEnum.Dead;
        public void AddModifier(ICustomModifier modifier, IHeroStats casterStats, float amplifier = 1f) => Stat.AddModifier(modifier, amplifier, casterStats, this);
        public bool RemoveModifier(ICustomModifier modifier) => Stat.RemoveModifier(modifier);
        public bool HasStatus(ModifierEnum status) => Stat.HasStatus(status);
        public int ActiveModifierCount => Stat.ActiveModifierCount;
        public float ModifierRemaining(int index) => Stat.ModifierRemaining(index);

        public void Heal(float amount, IEffectable source)
        {
            HealOutcome outcome = CombatMath.ResolveHeal(amount, Stat.IsWounded, Stat.CurrentHP, Stat.MaxHP);
            Stat.SetCurrentHP(outcome.NewHP);

            OnHealed?.Invoke(new HealEvent(source, this, outcome));
        }

        public int TakeDamage(int damage, IEffectable source, DamageKindEnum kind)
        {
            // if target has vanish, the target don't take damage
            if (HasStatus(ModifierEnum.Untargetable)) return 0;

            // calculate damage
            DamageOutcome outcome = CombatMath.ResolveDamage(damage, Stat.DF, Stat.DamageReductionPercent, Stat.CurrentHP, Stat.Shield, out int absorbed);
            Stat.ConsumeShield(absorbed);
            Stat.SetCurrentHP(outcome.NewHP);

            OnDamaged?.Invoke(new DamageEvent(source, this, kind, outcome));

            // if take burn, annouce it.
            if (kind == DamageKindEnum.Burn) AnnounceBurn();

            // if (other status) add here;
            // ...

            // return how much damage was done
            return outcome.Landed;
        }

        // === IHeroStats ===
        public int CurrentHP => Stat.CurrentHP;
        public int MaxHP => Stat.MaxHP;
        public int Shield => Stat.Shield;
        public int CurrentMana => Stat.CurrentMana;
        public int MaxMana => Stat.MaxMana;
        public int AttackDamage => Stat.Atk;
        public int Defence => Stat.DF;
        public int Magic => Stat.MG;
        public int MagicResist => Stat.MR;
        public float AttackSpeed => Stat.AttackSpeed;
        public int Range => Stat.Range;
        public bool IsStunned => Stat.IsStunned;
        public bool IsWounded => Stat.IsWounded;
        public float GetStat(StatEnum type) => Stat.GetFinalStat(type);
        public float GetBaseStat(StatEnum type) => Stat.GetBaseStat(type);

        // === IPlaceable ===
        public Hex CurrentHex => _currentPlacement as Hex;
        public Hex ReservedHex => _reservedHex;
        public IPlacement CurrentPlacement => _currentPlacement;
        public bool IsInCombat => _currentPlacement is Hex;
        public int LinesFromFront => CurrentHex != null ? CurrentHex.LinesFromFront : -1;
        public void SetReservedHex(Hex hex)
        {
            if (_board != null) _board.UpdateReservation(this, _reservedHex, hex);

            _reservedHex = hex;
        }
        public void SetCurrentPlacement(IPlacement placement) => _currentPlacement = placement;

        // === ITargeter ===
        public ICombatant FindCurrentTarget() => _findEnemy.FindCurrentTarget();
        public ICombatant FindNearestEnemy() => _findEnemy.FindNearestEnemy();
        public ICombatant FindNearestEnemyTo(ICombatant target) => _findEnemy.FindNearestEnemyTo(target);
        public ICombatant FindFurthestEnemy(int reachRange) => _findEnemy.FindFurthestEnemy(reachRange);
        public IPlacement FindClusteredCircle(int reachRange, float blastRadius, bool isJump) => _findEnemy.FindClusteredCircle(reachRange, blastRadius, isJump);
        public IPlacement FindClusteredCharge(int reachRange, float chargeHalfWidth, bool maxRange = false) => _findEnemy.FindClusteredCharge(reachRange, chargeHalfWidth, maxRange);
        public IPlacement FindRandomFreeHex(int reachRange) => _findEnemy.FindRandomFreeHex(reachRange);
        public ICombatant FindClusteredLaser(int reachRange, float beamHalfWidth) => _findEnemy.FindClusteredLaser(reachRange, beamHalfWidth);
        public IReadOnlyList<ICombatant> FindEnemiesNear(ICombatant target, int reachRange) => _findEnemy.FindEnemiesNear(target, reachRange);
        public IReadOnlyList<ICombatant> FindAllEnemies() => _findEnemy.FindAllEnemies();
        public IReadOnlyList<ICombatant> FindAllAllies() => _findEnemy.FindAllAllies();
        public IReadOnlyList<ICombatant> FindRandomEnemies(int count) => _findEnemy.FindRandomEnemies(count);
        public ICombatant FindNearestAlly() => _findEnemy.FindNearestAlly();


        // ======================================== life cycle ========================================
        #region Life Cycle
        // Give everything a hero needs to exist, in one call. 
        public void Init(HeroDataSO data, BattleBoard board, TeamEnum team, SkillDefinition skill = null)
        {
            _SOData = data;
            _board = board;
            _team = team;

            _stat = new Stat(_SOData);
            _isDummy = _SOData.IsDummy;

            _visuals = GetComponent<HeroVisuals>();
            _skill = new HeroSkill(this, skill);
            _items = new HeroItems(this);
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
        #endregion

        // ======================================== gizmo ========================================
        #region Gizmo
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
        #endregion

        // This is where a function with no where to live yet. group together.
        #region Temporarily
        // when the stage is reset, make hero alive again.
        public void ResetForNewStage()
        {
            if (!IsInitialized) return;

            _stat = new Stat(_SOData);
            _attackCooldown = new AttackCooldown();

            // the modifier from items is also reset, so re-grant it.
            _items.ReGrantAll();

            SetAliveVisual();

            // ChangeState, not Start: the Dead state has to be left properly, and a hero that
            // survived is already mid-state rather than un-started.
            _stateMachine.ChangeState(HeroStateEnum.Idle);
        }

        private void AnnounceBurn()
        {
            foreach (ICombatant combatant in new List<ICombatant>(HeroesOnBoard))
            {
                if (combatant is Hero hero && hero != this && hero.IsAlive)
                    hero.TriggerOnHeroBurned(this);
            }
        }
        #endregion
    }
}
