using System.Collections.Generic;
using MagicSchool.Contracts;
using static MagicSchool.Skills.SkillFactory;

namespace MagicSchool.Skills
{
    internal class TemplarSkill : SkillDefinition
    {
        // swipe
        private static float DamageRatio => Tuned(150f);
        private static float ConeLength => Tuned(2.5f);
        private static float ConeWidth => Tuned(3f);

        // shield
        private static float ShieldRatio => Tuned(300f);
        private static float ShieldRatioPerHit => Tuned(50f);
        private static float ShieldDuration => Tuned(4f);        // FLAGGING: the roster sheet gives no length - 4s is Rock Golem's

        private readonly ICustomModifier _shield = BundleRefresh(ShieldDuration, Shield((StatEnum.DF, ShieldRatio)));

        public static SkillDefinition Build(TemplateActionRegistrySO registry) => new TemplarSkill(registry);

        private TemplarSkill(TemplateActionRegistrySO registry) : base(registry) { }

        public override string SkillName => "Shield Swipe";

        public override string Description
            => $"Swipes in a cone in front of him for {AD(DamageRatio)}, and gains a shield worth {DF(ShieldRatio)} "
             + $"for {ShieldDuration} seconds, +{ShieldRatioPerHit}% for each enemy the swipe hit.";

        // ============================== active ==============================
        // on cast, swipe and take the base shield. The rest of the shield comes in as the swipe lands
        public override bool OnCast()
        {
            if (!base.OnCast()) return false;

            Caster.AddModifier(_shield, Caster as IHeroStats);

            return true;
        }

        protected override SkillFlow BuildActiveFlow(TemplateActionRegistrySO registry)
        {
            SkillPart swipe = Part(registry,
                source: ActionSourceEnum.Self,
                action: TemplateActionEnum.TriangleAOE,
                target: AimTargetEnum.Current,
                tuning: TuneAOE(length: ConeLength, width: ConeWidth, offset: AOEOffsetEnum.Tip),
                Damage(EffectRecipientEnum.EnemiesInArea, (StatEnum.ATK, DamageRatio))
            );

            swipe.OnSkillHit = GrowShield;

            return Flow(onStart: swipe);
        }

        // the swipe hit an enemy - the shield grows.
        // A fresh bundle every time, so the hits stack on top of the base shield
        private void GrowShield(ICombatant enemy)
        {
            Caster.AddModifier(
                BundleStack(ShieldDuration, Shield((StatEnum.DF, ShieldRatioPerHit))),
                Caster as IHeroStats);
        }
    }
}
