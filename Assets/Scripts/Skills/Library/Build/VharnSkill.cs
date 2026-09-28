using System.Collections.Generic;
using MagicSchool.Contracts;
using static MagicSchool.Skills.SkillFactory;

namespace MagicSchool.Skills
{
    /// <summary>
    /// Vharn: casting transforms him, and while transformed his auto attack is replaced by a three
    /// beat combo - box, then triangle, then circle - each hitting harder than the last.
    ///
    /// Ported from Assets/Data/Heroes/Skills/Vharn.asset, which stays as the reference until this is
    /// verified against it.
    /// </summary>
    internal static class VharnSkill
    {
        private const int ComboLength = 3;

        private const float OmnivampFromAP = 10f;   // sheet: 10% AP omnivamp
        private const float AttackFromAS = 500f;   // sheet: 80% of bonus AS, converted to AD
        private const float TransformDuration = 10f;

        // one per beat of the combo, in order. Named rather than passed inline so the description
        // below can quote the same figures the beats are built from.
        private const float BoxDamage = 200f;
        private const float TriangleDamage = 300f;
        private const float CircleDamage = 400f;

        public static SkillDefinition Build(TemplateActionRegistrySO registry)
        {
            // one count for the whole combo, handed to each beat below. Shared deliberately and
            // visibly, rather than a count inside each beat that nothing keeps in agreement.
            ComboTracker combo = new ComboTracker(ComboLength);

            SkillDefinition skill = new SkillDefinition(
                skillName: "Skill",
                activeSteps: new List<SkillStep> { Transform(registry) },
                passiveSteps: new List<SkillStep> { Combo(registry, combo) },
                description: $"Transforms for {TransformDuration} seconds, draining {OmnivampFromAP}% AP of the damage "
                           + "he deals back as life and turning his bonus attack speed into raw attack damage.",
                passiveDescription: "While transformed his auto attack becomes a three beat combo - box, then triangle, "
                                  + $"then circle - landing for {BoxDamage}% / {TriangleDamage}% / {CircleDamage}% AD "
                                  + "in turn.");

            // moves the combo on when he attacks - not when a condition is asked
            skill.Triggered += combo.Count;

            return skill;
        }

        // ============================== active: the transform ==============================
        private static SkillStep Transform(TemplateActionRegistrySO registry)
        {
            // one group, one timer - the whole transform ends on the same tick
            ICustomModifier WorldEnderBuff = Bundle(
                duration: TransformDuration,

                // sheet: 10% Ability Power Omnivamp
                Buff(
                    modifier: ModifierEnum.Omnivamp,
                    ratios: (StatEnum.AP, OmnivampFromAP, ScaleFromEnum.Base)),

                // FLAGGING: it consume 80% AS. which is only convert to 0.5 atk.
                // need change later
                Buff(
                    modifier: ModifierEnum.ATK,
                    ratios: (StatEnum.AS, AttackFromAS, ScaleFromEnum.Base)),

                Status(ModifierEnum.Transformed),

                // no mana while transformed, and the combo below stands in for the auto attack
                Status(ModifierEnum.ManaBlocked),

                Status(ModifierEnum.AutoAttackWasReplaced)
            );

            SkillActionGroup cast = ActionGroup(
                registry: registry,
                source: ActionSourceEnum.Self,
                action: TemplateActionEnum.Cast,
                target: AimTargetEnum.Self,
                Apply(
                    recipient: EffectRecipientEnum.Self,
                    modifier: WorldEnderBuff)
            );

            return Step(trigger: TriggerEnum.OnCast, groups: cast);
        }

        // ============================== passive: the combo ==============================
        private static SkillStep Combo(TemplateActionRegistrySO registry, ComboTracker combo)
        {
            List<SkillActionGroup> beats = new List<SkillActionGroup>
            {
                Beat(registry: registry, combo: combo, action: TemplateActionEnum.BoxAOE,      beat: 1, damage: BoxDamage,      tuning: TuneAOE(offset: AOEOffsetEnum.Tip)),
                Beat(registry: registry, combo: combo, action: TemplateActionEnum.TriangleAOE, beat: 2, damage: TriangleDamage, tuning: TuneAOE(offset: AOEOffsetEnum.Tip)),
                Beat(registry: registry, combo: combo, action: TemplateActionEnum.CircleAOE,   beat: 3, damage: CircleDamage,   tuning: TuneAOE(offset: AOEOffsetEnum.Tip)),
            };

            return new SkillStep(trigger: TriggerEnum.OnAttack, actionGroups: beats);
        }

        // One beat of the combo: play this shape when transformed and the combo is on this count.
        private static SkillActionGroup Beat(TemplateActionRegistrySO registry, ComboTracker combo,
                                             TemplateActionEnum action, int beat, float damage,
                                             Tuning tuning = null)
        {
            List<SkillCondition> conditions = new List<SkillCondition>
            {
                // must have status transform
                new HasStatusCondition(
                    subject:     ConditionSubjectEnum.Caster,
                    status:      ModifierEnum.Transformed,
                    wantPresent: true),

                // ask the user
                new NumberCondition(
                    subject:   ConditionSubjectEnum.Caster,
                    combo:     combo,
                    matchBeat: beat),
            };

            return ActionGroupWhen(
                registry: registry,
                source: ActionSourceEnum.Self,
                action: action,
                target: AimTargetEnum.Current,
                conditions: conditions,
                tuning: tuning,
                // sheet: Vharn is AD
                Damage(
                    recipient: EffectRecipientEnum.EnemiesInArea,
                    ratios: (StatEnum.ATK, damage))
            );
        }
    }
}
