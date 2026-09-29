using System.Collections.Generic;
using MagicSchool.Contracts;
using static MagicSchool.Skills.SkillFactory;

namespace MagicSchool.Skills
{
    internal static class BlacksmithSkill
    {
        private const float DamageRatio = 180f;
        private const float StunDuration = 1f;
        private const float ShredDuration = -1f;
        private const float ShredFromAP = 25f;

        public static SkillDefinition Build(TemplateActionRegistrySO registry)
        {
            SkillActionGroup hammer = ActionGroup(registry,
                source: ActionSourceEnum.Current,
                action: TemplateActionEnum.CircleAOE,
                target: AimTargetEnum.Current,
                Damage(EffectRecipientEnum.EnemiesInArea, (StatEnum.ATK, DamageRatio)),
                Apply(EffectRecipientEnum.EnemiesInArea, Bundle(StunDuration, Status(ModifierEnum.Stun))),
                Apply(EffectRecipientEnum.EnemiesInArea,
                      Bundle(ShredDuration,
                             // 25% of the total AP is taken off the target's DF, for the rest of the fight
                             Buff(ModifierEnum.DefendShred, (StatEnum.AP, -ShredFromAP, ScaleFromEnum.Total))))
            );

            return new SkillDefinition(
                skillName: "Forge Hammer",
                activeSteps: new List<SkillStep> { Step(trigger: TriggerEnum.OnCast, groups: hammer) },
                description: $"Hammers the current target for {DamageRatio}% AD, stunning it for {StunDuration} "
                             + $"seconds and permanently removing armour equal to {ShredFromAP}% AP.");
        }
    }
}
