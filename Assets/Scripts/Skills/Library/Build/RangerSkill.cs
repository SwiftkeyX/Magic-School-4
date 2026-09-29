using System.Collections.Generic;
using MagicSchool.Contracts;
using static MagicSchool.Skills.SkillFactory;

namespace MagicSchool.Skills
{
    internal class RangerSkill : SkillDefinition
    {
        private const float DamageRatio = 744f;

        public static SkillDefinition Build(TemplateActionRegistrySO registry) => new RangerSkill(registry);

        private RangerSkill(TemplateActionRegistrySO registry) : base(registry) { }

        public override string SkillName => "Longshot";

        public override string Description
            => $"Fires a shot that carries straight on through the target, dealing {DamageRatio}% AD to "
             + "every enemy caught along its path.";

        // ============================== active ==============================
        protected override List<SkillStep> Active(TemplateActionRegistrySO registry)
        {
            SkillActionGroup shot = ActionGroup(registry,
                source: ActionSourceEnum.Self,
                action: TemplateActionEnum.PiercingProjectile,
                target: AimTargetEnum.Current,
                Damage(EffectRecipientEnum.EnemiesInPath, (StatEnum.ATK, DamageRatio))
            );

            return new List<SkillStep> { Step(trigger: TriggerEnum.OnCast, groups: shot) };
        }
    }
}
