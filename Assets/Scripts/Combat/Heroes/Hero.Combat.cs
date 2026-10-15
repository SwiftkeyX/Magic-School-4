using System;
using System.Collections.Generic;
using MagicSchool.Contracts;
using MagicSchool.Combat.Placements;

namespace MagicSchool.Combat.Heroes
{
    // Hero: being hit and healed (IEffectable), the events that report it, and the units it can ask for.
    public partial class Hero
    {
        // ======================================== combat event ========================================
        public event Action<DamageEvent> OnDamaged;
        public event Action<HealEvent> OnHealed;
        public event Action<DamageEvent> OnDamageDealt;     // this hero hurt someone - raised by the one who took it
        public event Action<HeroDataSO, TeamEnum, IPlacement, BattleBoard> OnSpawnRequested;

        // ======================================== IEffectable ========================================
        public bool IsAlive => this != null && IsInitialized && StateType != HeroStateEnum.Dead;
        public void AddModifier(ICustomModifier modifier, IHeroStats casterStats, float amplifier = 1f) => Stat.AddModifier(modifier, amplifier, casterStats, this);
        public bool RemoveModifier(ICustomModifier modifier) => Stat.RemoveModifier(modifier);
        public bool HasStatus(ModifierEnum status) => Stat.HasStatus(status);
        public int ActiveModifierCount => Stat.ActiveModifierCount;
        public float ModifierRemaining(int index) => Stat.ModifierRemaining(index);

        public void Heal(float amount, IEffectable source)
        {
            HealOutcome outcome = CombatMath.ResolveHeal(amount, Stat.IsWounded, Stat.CurrentHP, Stat.MaxHP);
            Stat.SetCurrentHP(outcome.NewHP);

            OnHealed?.Invoke(new HealEvent(source, this, outcome));
        }

        public int TakeDamage(int damage, IEffectable source, DamageKindEnum kind)
        {
            // if target has vanish, the target don't take damage
            if (HasStatus(ModifierEnum.Untargetable)) return 0;

            // calculate damage
            DamageOutcome outcome = CombatMath.ResolveDamage(damage, Stat.DF, Stat.DamageReductionPercent, Stat.CurrentHP, Stat.Shield, out int absorbed);
            Stat.ConsumeShield(absorbed);
            Stat.SetCurrentHP(outcome.NewHP);

            var damageEvent = new DamageEvent(source, this, kind, outcome);
            OnDamaged?.Invoke(damageEvent);

            // tell the dealer too
            // e.g.     aligning: Shield Knight heals off the damage his generator deals
            if (source is Hero dealer) dealer.OnDamageDealt?.Invoke(damageEvent);

            // if take burn, annouce it.
            if (kind == DamageKindEnum.Burn) AnnounceBurn();

            // if (other status) add here;
            // ...

            // return how much damage was done
            return outcome.Landed;
        }

        // ======================================== ISpawner ========================================
        public int SpawnUnitsNearby(int count) => _spawn.SpawnNearby(_SOData.SpawnUnit, count);

        // tell every other hero on the board this one took a burn tick
        private void AnnounceBurn()
        {
            foreach (ICombatant combatant in new List<ICombatant>(HeroesOnBoard))
            {
                if (combatant is Hero hero && hero != this && hero.IsAlive)
                    hero.TriggerOnHeroBurned(this);
            }
        }
    }
}
