using System.Collections.Generic;
using MagicSchool.Contracts;
using static MagicSchool.Skills.SkillFactory;

namespace MagicSchool.Skills
{
    internal class MonkSkill : SkillDefinition
    {
        private static float DamageRatio => Tuned(150f);
        private static float StunDuration => Tuned(1.5f);
        private static float DamageReductionPercent => Tuned(70f);
        private static float GuardDuration => Tuned(3f);

        public static SkillDefinition Build(TemplateActionRegistrySO registry) => new MonkSkill(registry);

        private MonkSkill(TemplateActionRegistrySO registry) : base(registry) { }

        public override string SkillName => "Stunning Palm";

        public override string Description
            => $"Strikes the current target for {AP(DamageRatio)} and stuns it for {StunDuration} "
             + $"seconds, then takes {DamageReductionPercent}% less damage for {GuardDuration} seconds.";

        // ============================== active ==============================
        protected override SkillFlow BuildActiveFlow(TemplateActionRegistrySO registry)
        {
            // the palm - a box on the current target
            SkillPart palm = Part(registry,
                source: ActionSourceEnum.Current,
                action: TemplateActionEnum.BoxAOE,
                target: AimTargetEnum.Current,
                Damage(EffectRecipientEnum.EnemiesInArea, (StatEnum.AP, DamageRatio)),
                Apply(EffectRecipientEnum.EnemiesInArea, BundleRefresh(StunDuration, Status(ModifierEnum.Stun)))
            );

            // the guard - put up once the palm is gone
            SkillPart guard = Part(registry,
                source: ActionSourceEnum.Self,
                action: TemplateActionEnum.Cast,
                target: AimTargetEnum.Self,
                Apply(EffectRecipientEnum.Self,
                      BundleRefresh(GuardDuration, Buff(ModifierEnum.DamageReduction, DamageReductionPercent)))
            );

            return Flow(onStart: palm, onExpired: guard);
        }
    }
}
