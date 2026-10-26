using System.Collections.Generic;
using MagicSchool.Contracts;
using MagicSchool.StatScaling;
using static MagicSchool.Skills.SkillFactory;

namespace MagicSchool.Skills
{
    // Every burn tick on an enemy gives him +AttackSpeedPerBurnTick% base AS, a fresh bundle each so they stack. Shots
    // go to random enemies within RandomPoolRadius hexes of the current target.
    internal class ImpSkill : SkillDefinition
    {
        private static float ADDamagePerShot => Tuned(150f);
        private static float APDamagePerShot => Tuned(15f);
        private static int ShotCount => Tuned(5);
        private static float IntervalBetweenShot => Tuned(0.1f);
        private static int RandomPoolRadius => Tuned(2);
        private static float TotalCastTime => IntervalBetweenShot * (ShotCount - 1);

        private static float AttackSpeedPerBurnTick => Tuned(2f);
        private static float RestOfFight => Tuned(-1f);

        public static SkillDefinition Build(TemplateActionRegistrySO registry) => new ImpSkill(registry);

        private ImpSkill(TemplateActionRegistrySO registry) : base(registry) { }

        public override string SkillName => "Spear Barrage";

        public override string Description
            => $"Throws {ShotCount} spears one after another at random enemies within {RandomPoolRadius} "
             + $"hexes of the current target, each landing for {AD(ADDamagePerShot)} + "
             + $"{AP(APDamagePerShot)}, never hitting the same one twice until everyone in range has "
             + "been caught once.";

        public override string PassiveDescription
            => $"Gains {AttackSpeedPerBurnTick}% attack speed every time an enemy takes a burn tick.";

        public override bool HasAutoAttackPassive => true;

        public override void OnHeroBurned(ICombatant burned)
        {
            if (burned.Team == Caster.Team) return;

            Caster.AddModifier(
                BundleStack(RestOfFight, Buff(ModifierEnum.AS, (StatEnum.AS, AttackSpeedPerBurnTick, ScaleFromEnum.Base))),
                Caster as IHeroStats);
        }

        protected override SkillFlow BuildActiveFlow(TemplateActionRegistrySO registry)
            => Shoot(registry);

        private static SkillFlow Shoot(TemplateActionRegistrySO registry)
        {
            ProjectileTuning tune = TuneProjectile(castTime: 0f);

            SkillPart shoot = Part(
                registry: registry,
                source: ActionSourceEnum.Self,
                action: TemplateActionEnum.FireTimingRunnerHomingProjectile,
                target: AimTargetEnum.Random,
                tuning: TuneFireTimingRunnerProjectile(ShotCount, FireTimingModeEnum.Sequence, IntervalBetweenShot, tune,
                                                       randomPoolRadius: RandomPoolRadius, castTime: TotalCastTime),

                Damage(
                    recipient: EffectRecipientEnum.SameToAimTarget,
                    (StatEnum.ATK, ADDamagePerShot), (StatEnum.AP, APDamagePerShot)
                )
            );

            return Flow(onStart: shoot);
        }
    }
}
