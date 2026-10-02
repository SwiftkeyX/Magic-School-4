using System.Collections.Generic;
using MagicSchool.Contracts;
using MagicSchool.StatScaling;
using static MagicSchool.Skills.SkillFactory;

namespace MagicSchool.Skills
{
    internal class ImpSkill : SkillDefinition
    {
        private const float ADDamagePerShot = 150f;   // sheet: 150/155/160% AD
        private const float APDamagePerShot = 15f;    // sheet: 15/20/35% AP
        private const int ShotCount = 5;
        private const float IntervalBetweenShot = 0.1f;
        private const int RandomPoolRadius = 2;        // sheet: "random enemies within 2 hexes of the current target"
        private const float TotalCastTime = IntervalBetweenShot * (ShotCount - 1);

        // passive
        private const float AttackSpeedPerBurnTick = 2f;   // +2% base AS
        // FLAGGING: the roster sheet gives no length - the rest of the fight, same as Harpy's growth
        private const float RestOfFight = -1f;

        public static SkillDefinition Build(TemplateActionRegistrySO registry) => new ImpSkill(registry);

        private ImpSkill(TemplateActionRegistrySO registry) : base(registry) { }

        public override string SkillName => "Spear Barrage";

        public override string Description
            => $"Throws {ShotCount} spears one after another at random enemies within {RandomPoolRadius} "
             + $"hexes of the current target, each landing for {ADDamagePerShot}% AD + "
             + $"{APDamagePerShot}% AP, never hitting the same one twice until everyone in range has "
             + "been caught once.";

        public override string PassiveDescription
            => $"Gains {AttackSpeedPerBurnTick}% attack speed every time an enemy takes a burn tick.";

        // ============================== passive ==============================
        public override bool HasAutoAttackPassive => true;

        // an enemy took a burn tick - gain attack speed.
        // A fresh bundle every time, so the ticks stack
        public override void OnHeroBurned(ICombatant burned)
        {
            if (burned.Team == Caster.Team) return;

            Caster.AddModifier(
                BundleStack(RestOfFight, Buff(ModifierEnum.AS, (StatEnum.AS, AttackSpeedPerBurnTick, ScaleFromEnum.Base))),
                Caster as IHeroStats);
        }

        // ============================== active ==============================
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
