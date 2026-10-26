using System;

namespace MagicSchool.Core
{
    // What the player asked the game to do next - start the fight, continue from the result, restart, skip the stage -
    // held until the state it is for reads it on its next tick. Each request is taken once (Consume...), and a request
    // for another phase is refused when asked. Built and held by GameManager (GameManager.Instance.Requests).
    // KeepShopOnce is set by GameManager.JumpToStage only, so it is not one of ClearPending's requests: Preparation
    // clears those first.
    public class PhaseRequests
    {
        private readonly Func<GamePhaseEnum> _phase;

        private bool _startCombat;
        private bool _continue;
        private bool _restart;
        private bool _skipStage;
        private bool _keepShopOnce;

        internal PhaseRequests(Func<GamePhaseEnum> phase) => _phase = phase;

        public void StartCombat()
        {
            if (_phase() != GamePhaseEnum.Preparation) return;

            _startCombat = true;
        }

        public void ContinueFromResult()
        {
            if (_phase() != GamePhaseEnum.Result) return;

            _continue = true;
        }

        public void Restart() => _restart = true;

        public void SkipStage()
        {
            GamePhaseEnum phase = _phase();
            if (phase != GamePhaseEnum.Preparation && phase != GamePhaseEnum.Combat) return;

            _skipStage = true;
        }

        internal void KeepShopOnce() => _keepShopOnce = true;

        internal bool ConsumeKeepShop() => Consume(ref _keepShopOnce);
        internal bool ConsumeStartCombat() => Consume(ref _startCombat);
        internal bool ConsumeContinue() => Consume(ref _continue);
        internal bool ConsumeRestart() => Consume(ref _restart);
        internal bool ConsumeSkipStage() => Consume(ref _skipStage);

        internal void ClearPending()
        {
            _startCombat = false;
            _continue = false;
            _restart = false;
            _skipStage = false;
        }

        private static bool Consume(ref bool requested)
        {
            if (!requested) return false;

            requested = false;
            return true;
        }
    }
}
