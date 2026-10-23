using System.Collections.Generic;
using MagicSchool.Contracts;
using MagicSchool.Modifiers;
using static MagicSchool.Skills.SkillFactory;

namespace MagicSchool.Skills
{
    internal class BanditSkill : SkillDefinition
    {
        private static float DamageRatio => Tuned(160f);
        private static float StealPercent => Tuned(30f);
        private static float StealDuration => Tuned(-1f);   // until the end of the fight

        public static SkillDefinition Build(TemplateActionRegistrySO registry) => new BanditSkill(registry);

        private BanditSkill(TemplateActionRegistrySO registry) : base(registry) { }

        public override string SkillName => "Pickpocket";

        public override string Description
            => $"Strikes the current target for {AD(DamageRatio)} and steals {StealPercent}% of its "
             + "attack speed until the end of the fight.";

        // ============================== active ==============================
        // Steals before the strike
        public override bool OnCast()
        {
            StatSteal.Take(Caster, Caster.FindCurrentTarget(), StealPercent, StealDuration, StatEnum.AS);

            return base.OnCast();
        }

        protected override SkillFlow BuildActiveFlow(TemplateActionRegistrySO registry)
        {
            SkillPart strike = Part(registry,
                source: ActionSourceEnum.Self,
                action: TemplateActionEnum.Cast,
                target: AimTargetEnum.Current,
                Damage(EffectRecipientEnum.SameToAimTarget, (StatEnum.ATK, DamageRatio)));

            return Flow(onStart: strike);
        }
    }
}
