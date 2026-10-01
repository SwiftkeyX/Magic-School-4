using System.Collections.Generic;
using MagicSchool.Contracts;
using static MagicSchool.Skills.SkillFactory;

namespace MagicSchool.Skills
{
    internal class RangerSkill : SkillDefinition
    {
        private const float DamageRatio = 744f;

        // buff - one for every enemy the shot hits
        private const float AttackSpeedPerHit = 20f;   // +20% base AS
        private const float BuffDuration = 7f;

        public static SkillDefinition Build(TemplateActionRegistrySO registry) => new RangerSkill(registry);

        private RangerSkill(TemplateActionRegistrySO registry) : base(registry) { }

        public override string SkillName => "Longshot";

        public override string Description
            => $"Fires a shot that carries straight on through the target, dealing {DamageRatio}% AD to "
             + $"every enemy caught along its path. Gains {AttackSpeedPerHit}% attack speed for "
             + $"{BuffDuration} seconds for each enemy it hits.";

        // ============================== active ==============================
        protected override List<SkillFlow> Active(TemplateActionRegistrySO registry)
        {
            SkillPart shot = Part(registry,
                source: ActionSourceEnum.Self,
                action: TemplateActionEnum.PiercingProjectile,
                target: AimTargetEnum.Current,
                Damage(EffectRecipientEnum.EnemiesInPath, (StatEnum.ATK, DamageRatio))
            );

            shot.OnSkillHit = GainAttackSpeed;

            return new List<SkillFlow> { Flow(trigger: TriggerEnum.OnCast, groups: shot) };
        }

        // the arrow hit an enemy - gain attack speed.
        // A fresh bundle every time, so the hits stack instead of refreshing
        private void GainAttackSpeed(ICombatant enemy)
        {
            Caster.AddModifier(
                Bundle(BuffDuration,
                    Buff(
                        ModifierEnum.AS, (StatEnum.AS, AttackSpeedPerHit, ScaleFromEnum.Base)
                    )),
                Caster as IHeroStats);
        }
    }
}
