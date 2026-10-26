namespace MagicSchool.Core.States
{

    /// <summary>
    /// Control life cycle of GameManager's phase
    /// The phase = { Preparation, Combat, Result }
    /// </summary>
    // Transitions every state would repeat (restart) are global and live here.
    internal class GameStateMachine
    {
        private readonly GameManager _game;
        private readonly PreparationState _preparation;
        private readonly CombatState _combat;
        private readonly ResultState _result;

        public GameState Current { get; private set; }

        public GamePhaseEnum CurrentType => Current == null ? GamePhaseEnum.Preparation : Current.StateType;

        public GameStateMachine(GameManager game)
        {
            _game = game;
            _preparation = new PreparationState(game);
            _combat = new CombatState(game);
            _result = new ResultState(game);
        }

        public void Start(GamePhaseEnum initial)
        {
            Current = GetState(initial);
            Current.OnEnter();
        }

        public void ChangeState(GamePhaseEnum next)
        {
            if (Current != null && next == CurrentType) return;

            Current?.OnExit();
            Current = GetState(next);
            Current.OnEnter();
        }

        public void Reenter()
        {
            if (Current == null) return;

            Current.OnExit();
            Current.OnEnter();
        }

        public void Tick()
        {
            if (Current == null) return;

            if (TryResolveInterrupt(out GamePhaseEnum forced))
            {
                ChangeState(forced);

                return;
            }

            Current.OnUpdate();
        }

        private bool TryResolveInterrupt(out GamePhaseEnum forced)
        {
            forced = default;

            if (_game.Requests.ConsumeRestart())
            {
                forced = GamePhaseEnum.Preparation;
                return true;
            }

            return false;
        }

        private GameState GetState(GamePhaseEnum type)
        {
            switch (type)
            {
                case GamePhaseEnum.Preparation: return _preparation;
                case GamePhaseEnum.Combat: return _combat;
                case GamePhaseEnum.Result: return _result;
                default: return null;
            }
        }
    }
}
