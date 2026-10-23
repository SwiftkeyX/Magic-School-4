using System.Collections.Generic;
using MagicSchool.Contracts;
using static MagicSchool.Skills.SkillFactory;

namespace MagicSchool.Skills
{
    internal class BlacksmithSkill : SkillDefinition
    {
        private static float DamageRatio => Tuned(180f);
        private static float ShredDuration => Tuned(-1f);
        private static float ShredFromAP => Tuned(25f);

        public static SkillDefinition Build(TemplateActionRegistrySO registry) => new BlacksmithSkill(registry);

        private BlacksmithSkill(TemplateActionRegistrySO registry) : base(registry) { }

        public override string SkillName => "Forge Hammer";

        public override string Description
            => $"Hammers the current target for {AD(DamageRatio)}, "
             + $"permanently removing armour equal to {AP(ShredFromAP)}.";

        // ============================== active ==============================
        protected override SkillFlow BuildActiveFlow(TemplateActionRegistrySO registry)
        {
            SkillPart hammer = Part(registry,
                source: ActionSourceEnum.Current,
                action: TemplateActionEnum.CircleAOE,
                target: AimTargetEnum.Current,
                Damage(EffectRecipientEnum.EnemiesInArea, (StatEnum.ATK, DamageRatio)),
                Apply(EffectRecipientEnum.EnemiesInArea,
                      BundleRefresh(ShredDuration,
                             // 25% of the total AP is taken off the target's DF, for the rest of the fight
                             Debuff(ModifierEnum.DefendShred, (StatEnum.AP, ShredFromAP, ScaleFromEnum.Total))))
            );

            return Flow(onStart: hammer);
        }
    }
}
