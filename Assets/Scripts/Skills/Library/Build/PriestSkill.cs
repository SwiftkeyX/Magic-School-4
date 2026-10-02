using MagicSchool.Contracts;
using static MagicSchool.Skills.SkillFactory;

namespace MagicSchool.Skills
{
    internal class PriestSkill : SkillDefinition
    {
        private const float FairyDuration = 6f;
        private const float FairyInterval = 1f;
        private const float StingDamage = 60f;
        private const float HealPercent = 100f;      

        // what the fairy does every second - played by the Companion.cs, not by the flow
        private SkillPart _sting;

        public static SkillDefinition Build(TemplateActionRegistrySO registry) => new PriestSkill(registry);

        private PriestSkill(TemplateActionRegistrySO registry) : base(registry) { }

        public override string SkillName => "Fairy Companion";

        public override string Description
            => $"Summons a fairy and gives it to the closest ally. For {FairyDuration} seconds the fairy deals "
             + $"{StingDamage}% AP every second to the enemy its owner is fighting, and heals the owner for "
             + $"{HealPercent}% of the damage it deals.";

        // ============================== init ==============================
        // the sting is played on its own rather than inside the flow, so it is initialized here
        public override void Init(ICombatant caster)
        {
            base.Init(caster);

            _sting.Init(caster);
        }

        // ============================== active ==============================
        protected override SkillFlow BuildActiveFlow(TemplateActionRegistrySO registry)
        {
            // the sting. The fairy plays it as its owner, so Current is whoever the owner is fighting,
            // and the lifesteal heals the owner.
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

            // FLAGGING: temporarily. The fairy goes to the closest ally, the same way Rock Golem picks his ward.
            // Who should really get it is not decided yet.
            SkillPart fairy = Part(
                registry: registry,
                source: ActionSourceEnum.Self,
                action: TemplateActionEnum.Summon,
                target: AimTargetEnum.NearestAlly,
                tuning: TuneSummon(act: _sting, duration: FairyDuration, interval: FairyInterval)
            );

            return Flow(onStart: fairy);
        }
    }
}
