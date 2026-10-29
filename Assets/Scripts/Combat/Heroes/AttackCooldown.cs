namespace MagicSchool.Combat.Heroes
{
    // Spend keeps the time that ran past the attack interval in the frame the hero became ready, so the attack rate
    // does not depend on the frame rate. Resetting to 0 dropped part of a frame per attack, so heroes attacked slower at
    // low frame rates, enough to swing a close fight (prog-5 won 85% at 1/30s steps, 25% at 1/100s).
    internal class AttackCooldown
    {
        private float _elapsed;

        public bool IsReady(float attackSpeed) => _elapsed >= 1f / attackSpeed;

        public void Tick(float deltaTime, float attackSpeed)
        {
            if (IsReady(attackSpeed)) return;

            _elapsed += deltaTime;
        }

        // Tick stops once ready, so what is left over is at most one frame - never a stored-up burst of attacks.
        public void Spend(float attackSpeed)
        {
            float interval = 1f / attackSpeed;
            _elapsed = _elapsed > interval ? _elapsed - interval : 0f;
        }
    }
}
