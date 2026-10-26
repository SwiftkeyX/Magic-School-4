using System.Collections.Generic;
using MagicSchool.Contracts;
using static MagicSchool.Skills.SkillFactory;

namespace MagicSchool.Skills
{
    // The shot pierces with FalloffPerEnemy% less for each enemy already hit; every enemy hit gives
    // +AttackSpeedPerHit% base AS, a fresh bundle each so they stack.
    internal class RangerSkill : SkillDefinition
    {
        private static float DamageRatio => Tuned(500f);
        private static float FalloffPerEnemy => Tuned(33f);

        private static float AttackSpeedPerHit => Tuned(20f);
        private static float BuffDuration => Tuned(7f);

        public static SkillDefinition Build(TemplateActionRegistrySO registry) => new RangerSkill(registry);

        private RangerSkill(TemplateActionRegistrySO registry) : base(registry) { }

        public override string SkillName => "Longshot";

        public override string Description
            => $"Fires a shot that carries straight on through the target, dealing {AD(DamageRatio)} to "
             + $"every enemy caught along its path, {FalloffPerEnemy}% less for each enemy it already went through. "
             + $"Gains {AttackSpeedPerHit}% attack speed for "
             + $"{BuffDuration} seconds for each enemy it hits.";

        protected override SkillFlow BuildActiveFlow(TemplateActionRegistrySO registry)
        {
            SkillPart shot = Part(registry,
                source: ActionSourceEnum.Self,
                action: TemplateActionEnum.PiercingProjectile,
                target: AimTargetEnum.Current,
                tuning: TuneProjectile(falloff: FalloffPerEnemy / 100f, speed: 16f),
                Damage(EffectRecipientEnum.EnemiesInPath, (StatEnum.ATK, DamageRatio))
            );

            shot.OnSkillHit = GainAttackSpeed;

            return Flow(onStart: shot);
        }

        private void GainAttackSpeed(ICombatant enemy)
        {
            Caster.AddModifier(
                BundleStack(BuffDuration,
                    Buff(
                        ModifierEnum.AS, (StatEnum.AS, AttackSpeedPerHit, ScaleFromEnum.Base)
                    )),
                Caster as IHeroStats);
        }
    }
}
