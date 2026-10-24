using MagicSchool.Contracts;

namespace MagicSchool.Combat.Heroes.States
{
    /// <summary>
    /// A hero's HP over time: StatEnum.HPPerSecond, healed once a second of the fight. It is 0 by default, so only a
    /// hero something gave it to heals (e.g. Blacksmith, +10 every cast).
    /// Unlike mana it keeps going while stunned or casting; only death stops it. It goes through Heal, so a wound cuts it
    /// and the damage meter counts it as the hero's own healing.
    /// </summary>
    internal class HPRegen
    {
        private const float TickInterval = 1f;

        private readonly Hero _me;
        private float _sinceTick;

        public HPRegen(Hero hero)
        {
            _me = hero;
        }

        public void Tick(float deltaTime, HeroStateEnum state)
        {
            if (state == HeroStateEnum.Dead || !_me.IsAlive) return;

            _sinceTick += deltaTime;
            if (_sinceTick < TickInterval) return;
            _sinceTick -= TickInterval;

            float perTick = _me.GetStat(StatEnum.HPPerSecond) * TickInterval;
            if (perTick <= 0f || _me.CurrentHP >= _me.MaxHP) return;

            _me.Heal(perTick, _me);
        }
    }
}
