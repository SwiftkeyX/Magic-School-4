using MagicSchool.Contracts;

namespace MagicSchool.Combat.Heroes.States
{

    internal abstract class HeroState
    {
        protected readonly Hero _me;
        protected readonly Transition _transition;

        protected HeroState(Hero hero, Transition transition)
        {
            _me = hero;
            _transition = transition;
        }

        public abstract HeroStateEnum StateType { get; }

        public virtual void OnEnter() { }
        public virtual void OnExit() { }
        public abstract void OnUpdate();
        protected abstract void CheckSwitchState();

    }
}
