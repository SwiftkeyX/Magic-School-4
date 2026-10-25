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

        // ======================================== IFallen ========================================
        public IEffectable KilledBy { get; private set; }
        public bool IsSummon { get; private set; }

        // called by whoever builds a unit another unit asked for (See OnSpawnRequested)
        public void MarkAsSummon() => IsSummon = true;

        // ======================================== IRevivable ========================================
        // a skill that brings this hero back after it dies (e.g. Reaper). Ticked by HeroDead (See Revive)
        private float _reviveIn = -1f;
        private float _reviveHPRatio;

        public bool IsReviving => _reviveIn >= 0f;

        public void ReviveAfter(float seconds, float hpRatio)
        {
            _reviveIn = Math.Max(0f, seconds);
            _reviveHPRatio = hpRatio;
        }

        public void CancelRevive() => _reviveIn = -1f;

        // count down while dead; true once it is time to get back up
        internal bool TickRevive(float deltaTime)
        {
            if (!IsReviving) return false;

            _reviveIn -= deltaTime;
            return _reviveIn <= 0f;
        }

        // back up on the hex it died on, with part of its max HP. Mana and modifiers are kept.
        internal void Revive()
        {
            float ratio = _reviveHPRatio;
            CancelRevive();
            KilledBy = null;

            Stat.SetCurrentHP(Math.Max(1, (int)Math.Round(Stat.MaxHP * ratio)));

            // it may have died mid-step or mid-dash: stand on its hex again and drop the step it had reserved
            if (CurrentHex != null) transform.position = CurrentHex.transform.position;
            SetReservedHex(null);

            ChangeState(HeroStateEnum.Idle);
        }

        // ======================================== IEffectable ========================================
        // 0 HP is dead at once, not only from the hero's next update, when the state machine moves it to Dead -
        // until then a shot landing in the same frame would hit it and vanish
        // e.g.     Skeleton Archer's second arrow landing on the hero his first one just killed
        public bool IsAlive => this != null && IsInitialized && StateType != HeroStateEnum.Dead && CurrentHP > 0;
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
            // the attacker's armor penetration and lethality lower the DF this hit meets - autos, skills and burn alike
            int defense = source is IHeroStats attacker
                ? CombatMath.DFAfterPenetration(Stat.DF, attacker.GetStat(StatEnum.ArmorPen), attacker.GetStat(StatEnum.Lethality))
                : Stat.DF;
            DamageOutcome outcome = CombatMath.ResolveDamage(damage, defense, Stat.DamageReductionPercent, Stat.CurrentHP, Stat.Shield, out int absorbed);
            bool wasAlive = Stat.CurrentHP > 0;
            Stat.ConsumeShield(absorbed);
            Stat.SetCurrentHP(outcome.NewHP);

            // the hit that takes it to 0 HP is the kill: remember whose it was
            if (wasAlive && outcome.NewHP <= 0) KilledBy = source;

            var damageEvent = new DamageEvent(source, this, kind, outcome);
            OnDamaged?.Invoke(damageEvent);

            // tell my own skill
            // e.g.     Shield Knight counts what he takes while bracing
            _skill?.TriggerOnDamageTaken(damageEvent);

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
