namespace MagicSchool.Combat.Heroes
{

    internal class AttackCooldown
    {
        private float _elapsed;

        public bool IsReady(float attackSpeed) => _elapsed >= 1f / attackSpeed;

        // update cooldown
        public void Tick(float deltaTime, float attackSpeed)
        {
            if (IsReady(attackSpeed)) return;

            _elapsed += deltaTime;
        }

        // after attack, attack go cooldown
        public void Spend() => _elapsed = 0f;
    }
}
