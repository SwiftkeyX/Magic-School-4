using UnityEngine;
using MagicSchool.Contracts;
using MagicSchool.Skills;

namespace MagicSchool.Combat.Heroes.States
{
    /// <summary>
    /// HeroStateMachine is state machine that control hero's behaviour.
    /// It's vanilla state machine, nothing special.
    /// </summary>
    // Global transitions every state would repeat (stun, death, cast) live here. Mana ticks before the cast check, so
    // a full bar casts the same frame. A full bar waits for a step under way to land: cast halfway, the hero kept the
    // hex he was stepping onto booked and never arrived, and enemies next to it waited forever. A free cast never
    // lands on top of a cast in progress or while stunned.
    internal class HeroStateMachine
    {
        private readonly Hero _me;

        private readonly HeroIdle _idle;
        private readonly HeroAttack _attack;
        private readonly HeroDead _dead;
        private readonly HeroWalk _walk;
        private readonly HeroStunned _stunned;
        private readonly HeroCast _cast;
        private readonly ManaRegen _manaRegen;
        private readonly HPRegen _hpRegen;

        public HeroState Current { get; private set; }

        public HeroStateEnum CurrentType => Current == null ? HeroStateEnum.Idle : Current.StateType;
        public HeroStateEnum PreviousType { get; private set; }

        public HeroStateMachine(Hero hero, MovementConfig movement)
        {
            _me = hero;
            Transition transition = new Transition(hero, movement);

            _idle = new HeroIdle(hero, transition);
            _walk = new HeroWalk(hero, movement, transition);
            _attack = new HeroAttack(hero, movement, transition);
            _dead = new HeroDead(hero, transition);
            _stunned = new HeroStunned(hero, transition);
            _cast = new HeroCast(hero, movement, transition);
            _manaRegen = new ManaRegen(hero);
            _hpRegen = new HPRegen(hero);
        }

        public void Start(HeroStateEnum initial)
        {
            Current = GetState(initial);
            Current.OnEnter();
        }

        public void ChangeState(HeroStateEnum next)
        {
            if (Current != null && next == CurrentType) return;

            PreviousType = CurrentType;

            Current?.OnExit();
            Current = GetState(next);
            Current.OnEnter();
        }

        public void Tick()
        {
            if (Current == null) return;

            _manaRegen.Tick(Time.deltaTime, CurrentType);
            _hpRegen.Tick(Time.deltaTime, CurrentType);

            if (TryResolveInterrupt(out HeroStateEnum forced))
            {
                ChangeState(forced);

                return;
            }

            Current.OnUpdate();
        }

        private bool TryResolveInterrupt(out HeroStateEnum forced)
        {
            forced = default;

            if (CurrentType == HeroStateEnum.Dead) return false;

            if (_me.CurrentHP <= 0)
            {
                forced = HeroStateEnum.Dead;
                return true;
            }

            bool notStun = CurrentType != HeroStateEnum.Stunned;
            if (_me.IsStunned && notStun)
            {
                forced = HeroStateEnum.Stunned;
                return true;
            }

            if (CurrentType == HeroStateEnum.Walk && _walk.IsMidStep) return false;

            bool success = _me.TriggerActiveSkill(_me.IsManaCapped());
            if (success)
            {
                forced = HeroStateEnum.Cast;
                return true;
            }

            bool canCastFree = _me.HasFreeCast && CurrentType != HeroStateEnum.Cast && CurrentType != HeroStateEnum.Stunned;
            if (canCastFree && _me.TriggerActiveSkill(isManaCapped: true))
            {
                _me.UseFreeCast();
                forced = HeroStateEnum.Cast;
                return true;
            }

            return false;
        }

        private HeroState GetState(HeroStateEnum type)
        {
            switch (type)
            {
                case HeroStateEnum.Idle: return _idle;
                case HeroStateEnum.Walk: return _walk;
                case HeroStateEnum.Attack: return _attack;
                case HeroStateEnum.Dead: return _dead;
                case HeroStateEnum.Stunned: return _stunned;
                case HeroStateEnum.Cast: return _cast;
                default: return null;
            }
        }
    }
}
