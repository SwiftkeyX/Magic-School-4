using MagicSchool.Contracts;
using static MagicSchool.Skills.SkillFactory;

namespace MagicSchool.Skills
{
    internal class PriestSkill : SkillDefinition
    {
        private const float FairyDuration = 6f;
        private const float FairyInterval = 1f;
        private const float DamagePerTick = 60f;
        private const float HealPercent = 100f;      // of the damage the fairy deals

        // DamageOverTime takes the total over the whole duration, not the amount per tick
        private const float DamageTotal = DamagePerTick * FairyDuration / FairyInterval;

        public static SkillDefinition Build(TemplateActionRegistrySO registry) => new PriestSkill(registry);

        private PriestSkill(TemplateActionRegistrySO registry) : base(registry) { }

        public override string SkillName => "Fairy Companion";

        public override string Description
            => $"Summons a fairy and gives it to the closest ally. For {FairyDuration} seconds the fairy deals "
             + $"{DamagePerTick}% AP every second to the enemy closest to its owner, and heals the owner for "
             + $"{HealPercent}% of the damage it deals.";

        // ============================== active ==============================
        protected override SkillFlow BuildActiveFlow(TemplateActionRegistrySO registry)
        {
            // FLAGGING: temporarily. The fairy goes to the closest ally, the same way Rock Golem picks his ward.
            // Who should really get it is not decided yet.
            SkillPart fairy = Part(
                registry: registry,
                source: ActionSourceEnum.Self,
                action: TemplateActionEnum.Companion,
                target: AimTargetEnum.NearestAlly,
                tuning: TuneCompanion(lifesteal: HealPercent / 100f),

                DamageOverTime(
                    recipient: EffectRecipientEnum.ClosestEnemyToAimTarget,
                    interval: FairyInterval,
                    duration: FairyDuration,
                    ratios: (StatEnum.AP, DamageTotal))
            );

            return Flow(onStart: fairy);
        }
    }
}
