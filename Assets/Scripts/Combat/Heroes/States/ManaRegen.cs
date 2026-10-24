using UnityEngine;
using MagicSchool.Contracts;

namespace MagicSchool.Combat.Heroes.States
{
    /// <summary>
    /// A hero's mana over time: StatEnum.ManaPerSecond, every second of the fight. It is the only source of mana by default;
    /// an auto attack gives StatEnum.ManaPerAttack on top, 0 unless something raises it (See HeroAttack).
    /// Nothing is gained while the hero is stunned, casting, dead or mana blocked - the clock stops, it does not catch up.
    /// e.g.    5 mana/s, stunned for 2s    => 10 mana less by the time the stun ends
    /// A fraction is kept for the next frame, so 5 mana/s is 5 a second at any frame rate.
    /// </summary>
    internal class ManaRegen
    {
        private readonly Hero _me;
        private float _carry;   // mana earned but not yet a whole point

        public ManaRegen(Hero hero)
        {
            _me = hero;
        }

        public void Tick(float deltaTime, HeroStateEnum state)
        {
            if (!CanGain(state)) return;

            _carry += _me.GetStat(StatEnum.ManaPerSecond) * deltaTime;
            int whole = Mathf.FloorToInt(_carry);
            if (whole <= 0) return;

            _carry -= whole;
            _me.GainMana(whole);
        }

        private bool CanGain(HeroStateEnum state)
        {
            if (state == HeroStateEnum.Stunned || state == HeroStateEnum.Cast || state == HeroStateEnum.Dead) return false;

            // a stun that landed this frame, before the state machine moved to Stunned
            if (_me.IsStunned) return false;

            return !_me.HasStatus(ModifierEnum.ManaBlocked);
        }
    }
}
