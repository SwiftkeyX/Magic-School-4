using System.Collections.Generic;
using UnityEngine;
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
        private const float AttackSpeedPerBurningEnemy = 25f;   // +25% base AS
        private const int MaxBurningEnemies = 5;                // the top: +125%
        private const float RestOfFight = -1f;

        // one bundle, built once and granted again whenever the count changes. Its strength is the count.
        private readonly ICustomModifier _fanned = BundleRefresh(RestOfFight,
            Buff(ModifierEnum.AS, (StatEnum.AS, AttackSpeedPerBurningEnemy, ScaleFromEnum.Base)));

        public static SkillDefinition Build(TemplateActionRegistrySO registry) => new ImpSkill(registry);

        private ImpSkill(TemplateActionRegistrySO registry) : base(registry) { }

        public override string SkillName => "Spear Barrage";

        public override string Description
            => $"Throws {ShotCount} spears one after another at random enemies within {RandomPoolRadius} "
             + $"hexes of the current target, each landing for {ADDamagePerShot}% AD + "
             + $"{APDamagePerShot}% AP, never hitting the same one twice until everyone in range has "
             + "been caught once.";

        public override string PassiveDescription
            => $"Gains {AttackSpeedPerBurningEnemy}% attack speed for every enemy that is burning, "
             + $"up to {MaxBurningEnemies} enemies.";

        // ============================== passive ==============================
        public override bool HasAutoAttackPassive => true;

        // the number of burning enemies changes when one catches fire, stops burning, or dies.
        public override void OnBurnChanged(ICombatant hero) => FanTheFlames();
        public override void OnHeroDied(ICombatant dead) => FanTheFlames();

        // count the enemies burning right now, and be that much faster.
        private void FanTheFlames()
        {
            int burning = 0;
            foreach (ICombatant enemy in Caster.FindAllEnemies())
                if (enemy.IsAlive && enemy.HasStatus(ModifierEnum.Burn)) burning++;

            Caster.AddModifier(_fanned, Caster as IHeroStats, amplifier: Mathf.Min(burning, MaxBurningEnemies));
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
