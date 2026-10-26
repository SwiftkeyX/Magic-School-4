using System.Collections.Generic;
using MagicSchool.Contracts;
using static MagicSchool.Skills.SkillFactory;

namespace MagicSchool.Skills
{
    // His AS buff is a self-referential ratio (+200% of his own AS), resolved once as it lands. His stun grows with
    // every few auto attacks; the count starts over, and his AD is halved, at combat start.
    internal class DireWolfSkill : SkillDefinition
    {
        private static float AttackSpeedBuff => Tuned(200f);
        private static float BuffDuration => Tuned(5f);

        private static float StunDuration => Tuned(0.5f);
        private static float StunPerGrowth => Tuned(0.5f);
        private static int AttacksPerGrowth => Tuned(5);
        private int _attackCount;

        private static float AttackCut => Tuned(50f);
        private static float HealOnAA => Tuned(30f);
        private static float RestOfFight => Tuned(-1f);

        public static SkillDefinition Build(TemplateActionRegistrySO registry) => new DireWolfSkill(registry);

        private DireWolfSkill(TemplateActionRegistrySO registry) : base(registry) { }

        public override string SkillName => "Frenzy";

        public override string Description
            => $"Works himself into a frenzy for {BuffDuration} seconds, gaining {AttackSpeedBuff}% "
             + "attack speed. When it wears off he lets out a howl that stuns every enemy around him "
             + $"for {StunDuration} seconds. Every {AttacksPerGrowth} auto attacks add {StunPerGrowth} seconds to the stun.";

        public override string PassiveDescription
            => $"His AD is cut by {AttackCut}%. Every auto attack he lands heals him for {AP(HealOnAA)}.";

        private float CurrentStun => StunDuration + StunPerGrowth * (_attackCount / AttacksPerGrowth);

        protected override SkillFlow BuildActiveFlow(TemplateActionRegistrySO registry)
            => Flow(onStart: Cast(registry), onExpired: OnCastExpired(registry));

        private static SkillPart Cast(TemplateActionRegistrySO registry)
        {
            ICustomModifier modifiers = BundleRefresh(
                BuffDuration,
                Buff(ModifierEnum.AS, (StatEnum.AS, AttackSpeedBuff, ScaleFromEnum.Base)),
                Debuff(ModifierEnum.ManaBlocked)
            );

            SkillPart cast = Part(registry,
                source: ActionSourceEnum.Self,
                action: TemplateActionEnum.Cast,
                target: AimTargetEnum.Self,
                Apply(EffectRecipientEnum.Self, modifiers));

            return cast;
        }

        private SkillPart OnCastExpired(TemplateActionRegistrySO registry)
        {
            ICustomModifier stun = BundleRefresh(() => CurrentStun, Status(ModifierEnum.Stun));

            SkillPart AOE = Part(registry,
                source: ActionSourceEnum.Self,
                action: TemplateActionEnum.CircleAOE,
                target: AimTargetEnum.Self,
                Apply(EffectRecipientEnum.EnemiesInArea, stun));

            return AOE;
        }

        public override bool OnCombatStart()
        {
            _attackCount = 0;

            Caster.AddModifier(
                BundleStack(
                    RestOfFight,
                    Debuff(
                        ModifierEnum.ATK,
                        (StatEnum.ATK, AttackCut, ScaleFromEnum.Base))
                ),
                Caster as IHeroStats
            );

            return base.OnCombatStart();
        }

        public override bool OnAutoAttack(ICombatant target)
        {
            _attackCount++;

            return base.OnAutoAttack(target);
        }

        protected override SkillFlow BuildAutoAttackFlow(TemplateActionRegistrySO registry)
        {
            SkillPart cast = Part(registry,
                source: ActionSourceEnum.Self,
                action: TemplateActionEnum.Cast,
                target: AimTargetEnum.Self,
                Heal(EffectRecipientEnum.Self, (StatEnum.AP, HealOnAA)));

            return Flow(onStart: cast);
        }
    }
}
