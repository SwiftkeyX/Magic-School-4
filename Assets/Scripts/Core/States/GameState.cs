namespace MagicSchool.Core.States
{
    internal abstract class GameState
    {
        protected readonly GameManager _game;

        protected GameState(GameManager game)
        {
            _game = game;
        }

        public abstract GamePhaseEnum StateType { get; }

        public virtual void OnEnter() { }
        public virtual void OnExit() { }
        public virtual void OnUpdate() { CheckSwitchState(); }
        protected virtual void CheckSwitchState() { }
    }
}
