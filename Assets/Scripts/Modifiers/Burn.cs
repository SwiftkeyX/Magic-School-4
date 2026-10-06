using System;
using MagicSchool.Contracts;

namespace MagicSchool.Modifiers
{
    // Burn is a status that deals damage every second for as long as it lasts.
    // A hero is burning or he is not: there is one burn per hero, it never stacks.
    //
    // Set a burning hero on fire again:
    //      a stronger or equal fire    = replaces the old one, so the timer restarts and the new owner gets the credit.
    //      a weaker fire               = ignored until the stronger one is out.
    internal class Burn
    {
        private const float TickInterval = 1f;

        private float _damagePerSecond;
        private IEffectable _source;        // who set the fire - the damage is his
        private int _ticksLeft;
        private float _sinceLastTick;

        public bool IsBurning => _ticksLeft > 0;

        public void Ignite(float damagePerSecond, float duration, IEffectable source)
        {
            int ticks = (int)Math.Round(duration / TickInterval);
            if (ticks <= 0 || damagePerSecond <= 0f) return;

            // a weaker fire does nothing to a hero who already burns hotter
            if (IsBurning && damagePerSecond < _damagePerSecond) return;

            // a fresh fire starts its own rhythm, a fire that is replaced keeps the rhythm it has
            if (!IsBurning) _sinceLastTick = 0f;

            _damagePerSecond = damagePerSecond;
            _source = source;
            _ticksLeft = ticks;
        }

        // Count the time. Returns true when a tick is due, with what it deals and who dealt it.
        // Ticks are counted rather than timed against the duration, so the last one is never dropped.
        public bool Tick(float deltaTime, out int damage, out IEffectable source)
        {
            damage = 0;
            source = null;
            if (!IsBurning) return false;

            _sinceLastTick += deltaTime;
            if (_sinceLastTick < TickInterval) return false;

            _sinceLastTick -= TickInterval;
            _ticksLeft--;

            damage = (int)Math.Round(_damagePerSecond * TickInterval);
            source = _source;
            return true;
        }
    }
}
