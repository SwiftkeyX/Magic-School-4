using System.Collections.Generic;
using MagicSchool.Contracts;
using MagicSchool.Modifiers;
using static MagicSchool.Skills.SkillFactory;

namespace MagicSchool.Skills
{
    internal class BanditSkill : SkillDefinition
    {
        private const float DamageRatio = 160f;
        private const float StealPercent = 30f;
        private const float StealDuration = -1f;   // until the end of the fight

        public static SkillDefinition Build(TemplateActionRegistrySO registry) => new BanditSkill(registry);

        private BanditSkill(TemplateActionRegistrySO registry) : base(registry) { }

        public override string SkillName => "Pickpocket";

        public override string Description
            => $"Strikes the current target for {DamageRatio}% AD and steals {StealPercent}% of its "
             + "attack speed until the end of the fight.";

        // ============================== active ==============================
        // Steals before the strike
        public override bool OnCast()
        {
            StatSteal.Take(Caster, Caster.FindCurrentTarget(), StealPercent, StealDuration, StatEnum.AS);

            return base.OnCast();
        }

        protected override List<SkillFlow> Active(TemplateActionRegistrySO registry)
        {
            SkillPart strike = Part(registry,
                source: ActionSourceEnum.Self,
                action: TemplateActionEnum.Cast,
                target: AimTargetEnum.Current,
                Damage(EffectRecipientEnum.SameToAimTarget, (StatEnum.ATK, DamageRatio)));

            return new List<SkillFlow> { Flow(trigger: TriggerEnum.OnCast, groups: strike) };
        }
    }
}
