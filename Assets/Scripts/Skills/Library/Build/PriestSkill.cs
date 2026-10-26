using MagicSchool.Contracts;
using static MagicSchool.Skills.SkillFactory;

namespace MagicSchool.Skills
{
    // The fairy (a Companion) plays the sting every second as its owner, so its target is whoever the owner fights and
    // the lifesteal heals the owner. The sting is played outside the flow, so it is initialized in Init.
    internal class PriestSkill : SkillDefinition
    {
        private static float FairyDuration => Tuned(6f);
        private static float FairyInterval => Tuned(1f);
        private static float StingDamage => Tuned(60f);
        private static float HealPercent => Tuned(100f);      

        private SkillPart _sting;

        public static SkillDefinition Build(TemplateActionRegistrySO registry) => new PriestSkill(registry);

        private PriestSkill(TemplateActionRegistrySO registry) : base(registry) { }

        public override string SkillName => "Fairy Companion";

        public override string Description
            => $"Summons a fairy and gives it to the lowest-health ally. For {FairyDuration} seconds the fairy deals "
             + $"{AP(StingDamage)} every second to the enemy its owner is fighting, and heals the owner for "
             + $"{HealPercent}% of the damage it deals.";

        public override void Init(ISkillCaster caster)
        {
            base.Init(caster);

            _sting.Init(caster);
        }

        protected override SkillFlow BuildActiveFlow(TemplateActionRegistrySO registry)
        {
            _sting = Part(
                registry: registry,
                source: ActionSourceEnum.Self,
                action: TemplateActionEnum.Cast,
                target: AimTargetEnum.Current,

                Damage(
                    recipient: EffectRecipientEnum.SameToAimTarget,
                    ratios: (StatEnum.AP, StingDamage)
                ).WithLifesteal(HealPercent)
            );

            SkillPart fairy = Part(
                registry: registry,
                source: ActionSourceEnum.Self,
                action: TemplateActionEnum.Summon,
                target: AimTargetEnum.LowestHealthAlly,
                tuning: TuneSummon(act: _sting, duration: FairyDuration, interval: FairyInterval)
            );

            return Flow(onStart: fairy);
        }
    }
}
