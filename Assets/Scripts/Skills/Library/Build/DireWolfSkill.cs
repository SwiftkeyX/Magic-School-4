using System.Collections.Generic;
using MagicSchool.Contracts;
using static MagicSchool.Skills.SkillFactory;

namespace MagicSchool.Skills
{
    internal class DireWolfSkill : SkillDefinition
    {
        // buff
        private const float AttackSpeedBuff = 200f;   // +200%
        private const float BuffDuration = 5f;

        // stun of DireWolf can grows with every x auto attacks
        private const float StunDuration = 0.5f;
        private const float StunPerGrowth = 0.5f;
        private const int AttacksPerGrowth = 5;
        private int _attackCount;

        // passive
        private const float AttackCut = 50f;   // -50% base AD
        private const float HealOnAA = 30f;
        private const float RestOfFight = -1f;

        public static SkillDefinition Build(TemplateActionRegistrySO registry) => new DireWolfSkill(registry);

        private DireWolfSkill(TemplateActionRegistrySO registry) : base(registry) { }

        public override string SkillName => "Frenzy";

        public override string Description
            => $"Works himself into a frenzy for {BuffDuration} seconds, gaining {AttackSpeedBuff}% "
             + "attack speed. When it wears off he lets out a howl that stuns every enemy around him "
             + $"for {StunDuration} seconds. Every {AttacksPerGrowth} auto attacks add {StunPerGrowth} seconds to the stun.";

        public override string PassiveDescription
            => $"His AD is cut by {AttackCut}%. Every auto attack he lands heals him for {HealOnAA}% AP.";

        private float CurrentStun => StunDuration + StunPerGrowth * (_attackCount / AttacksPerGrowth);

        // ============================== active ==============================
        protected override List<SkillStep> Active(TemplateActionRegistrySO registry)
            => new List<SkillStep> { Cast(registry), OnCastExpired(registry) };

        private static SkillStep Cast(TemplateActionRegistrySO registry)
        {
            // +200% of his own attack speed - a self-referential ratio, resolved once when it lands
            ICustomModifier modifiers = Bundle(
                BuffDuration,
                Buff(ModifierEnum.AS, (StatEnum.AS, AttackSpeedBuff, ScaleFromEnum.Base)),
                // FIXLATER: I notice that there's not anything to prevent the Modifier.ManaBlocked to be use with Buff or Debuff. 
                // Eventhough it should olonly be used by Status().
                Debuff(ModifierEnum.ManaBlocked)
            );

            // cast buff
            SkillActionGroup cast = ActionGroup(registry,
                source: ActionSourceEnum.Self,
                action: TemplateActionEnum.Cast,
                target: AimTargetEnum.Self,
                Apply(EffectRecipientEnum.Self, modifiers));

            return Step(trigger: TriggerEnum.OnCast, groups: cast);
        }

        private SkillStep OnCastExpired(TemplateActionRegistrySO registry)
        {
            // stun
            ICustomModifier stun = Bundle(() => CurrentStun, Status(ModifierEnum.Stun));

            // aoe on self
            SkillActionGroup AOE = ActionGroup(registry,
                source: ActionSourceEnum.Self,
                action: TemplateActionEnum.CircleAOE,
                target: AimTargetEnum.Self,
                Apply(EffectRecipientEnum.EnemiesInArea, stun));

            return Step(trigger: TriggerEnum.OnExpired, groups: AOE);
        }

        // ============================== passive ==============================
        // at combat start, the count starts over and his AD is halved
        public override bool OnCombatStart()
        {
            _attackCount = 0;

            Caster.AddModifier(
                Bundle(
                    RestOfFight,
                    Debuff(
                        ModifierEnum.ATK,
                        (StatEnum.ATK, AttackCut, ScaleFromEnum.Base))
                ),
                Caster as IHeroStats
            );

            return base.OnCombatStart();
        }

        // on attack, increase attackCount
        // attackCount increase stun duration
        public override bool OnAttack(ICombatant target)
        {
            _attackCount++;

            return base.OnAttack(target);
        }

        protected override List<SkillStep> Passive(TemplateActionRegistrySO registry)
        {
            // cast
            SkillActionGroup cast = ActionGroup(registry,
                source: ActionSourceEnum.Self,
                action: TemplateActionEnum.Cast,
                target: AimTargetEnum.Self,
                // sheet: 30% AP
                Heal(EffectRecipientEnum.Self, (StatEnum.AP, HealOnAA)));

            return new List<SkillStep> { Step(trigger: TriggerEnum.OnAttack, groups: cast) };
        }
    }
}
