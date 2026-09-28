using MagicSchool.Contracts;
using MagicSchool.Skills;

namespace MagicSchool.Combat.Heroes.States
{
    /// <summary>
    /// HeroStateMachine is state machine that control hero's behaviour.
    /// It's vanilla state machine, nothing special.
    /// </summary>
    internal class HeroStateMachine
    {
        private readonly Hero _me;

        private readonly HeroIdle _idle;
        private readonly HeroAttack _attack;
        private readonly HeroDead _dead;
        private readonly HeroWalk _walk;
        private readonly HeroStunned _stunned;
        private readonly HeroCast _cast;

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
            _cast = new HeroCast(hero, transition);
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

            // interrupt state e.g. stun, dead
            if (TryResolveInterrupt(out HeroStateEnum forced))
            {
                ChangeState(forced);

                // return early, so we don't update in the same frame
                return;
            }

            // update state
            Current.OnUpdate();
        }

        /// <summary>
        /// Global state transition.  
        /// Some of the transition are redundant in each state, make it a global transition by move it here.
        /// </summary>
        private bool TryResolveInterrupt(out HeroStateEnum forced)
        {
            forced = default;

            if (CurrentType == HeroStateEnum.Dead) return false;

            if (_me.CurrentHP <= 0)
            {
                forced = HeroStateEnum.Dead;
                return true;
            }

            // FLAGGING: the stun duration should be addition to previous exist stun running 
            bool notStun = CurrentType != HeroStateEnum.Stunned;
            if (_me.IsStunned && notStun)
            {
                forced = HeroStateEnum.Stunned;
                return true;
            }

            // FLAGGING: Being stun while casting skill is useless for the stun user, since skill effect is already fire.
            // if mana is full, trigger OnCast skill
            bool success = _me.TriggerActiveSkill(_me.IsManaCapped());
            if (success)
            {
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
