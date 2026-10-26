namespace MagicSchool.Combat.Heroes
{

    internal class AttackCooldown
    {
        private float _elapsed;

        public bool IsReady(float attackSpeed) => _elapsed >= 1f / attackSpeed;

        public void Tick(float deltaTime, float attackSpeed)
        {
            if (IsReady(attackSpeed)) return;

            _elapsed += deltaTime;
        }

        public void Spend() => _elapsed = 0f;
    }
}
