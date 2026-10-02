
using System.Collections.Generic;
using MagicSchool.Contracts;

namespace MagicSchool.Modifiers
{
    // Contain "a" active group of modifiers on the hero.
    // 1) To track how long this group of modifiers last.
    // 2) To contain how much bonus stat this modifier'll give.
    //
    // e.g. Werewolf's transform. a CustomModifier:
    //
    //      BundleRefresh(10s,
    //          Buff(Omnivamp, 10% of AP),          [0]
    //          Buff(ATK, 500% of AS),              [1]
    //          Status(Transformed),                [2]
    //          Status(ManaBlocked),                [3]
    //          Status(AutoAttackWasReplaced))      [4]
    //
    // When he casts, those modifier was group into one ActiveCustomModifier
    //
    // Variable:
    //      _bonusStat     = [10, 3.75, 0, 0, 0]   (one entry per modifier, same order; a status gives 0 stat)
    //      Remaining      = 10s, ticking down     (the whole group leaves together when timer reach 0)
    internal class ActiveCustomModifier
    {
        private const float Permanent = -1f;
        private float _remaining;

        // pair of modifier & stat - tell which stat is increase by this modifier
        private static readonly Dictionary<ModifierEnum, StatEnum> _lookup = new Dictionary<ModifierEnum, StatEnum>
        {
            // buff
            { ModifierEnum.BonusHP        , StatEnum.MaxHP }          ,
            { ModifierEnum.ATK            , StatEnum.ATK }            ,
            { ModifierEnum.AS             , StatEnum.AS}              ,
            { ModifierEnum.DF             , StatEnum.DF }             ,
            { ModifierEnum.DamageReduction, StatEnum.DamageReduction },
            { ModifierEnum.AP             , StatEnum.AP }             ,
            { ModifierEnum.Range          , StatEnum.Range }          ,
            { ModifierEnum.StartMana      , StatEnum.StartMana }      ,
            { ModifierEnum.MR             , StatEnum.MR }             ,
            { ModifierEnum.CritChance     , StatEnum.CritChance }     ,
            // ...

            // debuff
            { ModifierEnum.DefendShred    , StatEnum.DF }             ,
            // ...
        };

        // ============================================ getter ============================================
        public readonly ICustomModifier CustomModifier;     // the group of modifier - buff, debuff, status, etc...
        private readonly float[] _bonusStat;                // the amount of total stat that will be added to hero, amplifier included
        public float Remaining => _remaining;               // remember its remaining duration of the modifier - The group share the same remaining

        public ActiveCustomModifier(ICustomModifier source, float amplifier, IHeroStats casterStats, IHeroStats recipientStats)
        {
            CustomModifier = source;

            // get bonus stat from each modifier
            IReadOnlyList<IModifier> modifiers = source.GetModifiers();
            _bonusStat = new float[modifiers.Count];
            for (int i = 0; i < modifiers.Count; i++)
            {
                // the bonus stat could derive from caster itself or others hero that being hit by the skill.
                IHeroStats from;
                if (modifiers[i].GetScalingSource() == ScalingSourceEnum.Caster)
                {
                    from = casterStats;
                }
                else if (modifiers[i].GetScalingSource() == ScalingSourceEnum.Recipient)
                {
                    from = recipientStats;
                }
                // fallback
                else from = casterStats;

                // get bonus stat, amplified if the effect's conditions is true.
                // e.g. +30% when the target was wounded.
                _bonusStat[i] = modifiers[i].GetBonusAmount(from) * amplifier;
            }

            // start the modifier's timer
            _remaining = StartingRemaining(source.GetDuration());
        }

        public void Tick(float deltaTime)
        {
            _remaining -= deltaTime;
        }

        // Get the specify stat from this ActiveCustomModifer
        public float GetStat(StatEnum stat)
        {
            float total = 0f;
            IReadOnlyList<IModifier> modifiers = CustomModifier.GetModifiers();
            for (int i = 0; i < modifiers.Count; i++)
            {
                if (_lookup.TryGetValue(modifiers[i].GetModifierEnum(), out StatEnum feeds) && feeds == stat)
                    total += _bonusStat[i];
            }
            return total;
        }

        // Get the specify modifier amount from this ActiveCustomModifer
        // e.g. if this group, have Shield(500) and Shield(200)     =    GetAmount(Shield) = 700.
        public float GetAmount(ModifierEnum type)
        {
            float total = 0f;
            IReadOnlyList<IModifier> modifiers = CustomModifier.GetModifiers();
            for (int i = 0; i < modifiers.Count; i++)
                if (modifiers[i].GetModifierEnum() == type) total += _bonusStat[i];
            return total;
        }

        // ======================================== other ========================================
        // ====== shield ======
        // when shield absorb damage, the shield amount is taken from the modifer.
        public float ConsumeShield(float amount)
        {
            float taken = 0f;
            IReadOnlyList<IModifier> modifiers = CustomModifier.GetModifiers();
            for (int i = 0; i < modifiers.Count && taken < amount; i++)
            {
                if (modifiers[i].GetModifierEnum() != ModifierEnum.Shield) continue;

                float take = System.Math.Min(_bonusStat[i], amount - taken);
                _bonusStat[i] -= take;
                taken += take;
            }

            return taken;
        } 

        // ======================================== private ========================================
        private static float StartingRemaining(float duration)
            => (duration == Permanent) ? float.PositiveInfinity : duration;
    }
}