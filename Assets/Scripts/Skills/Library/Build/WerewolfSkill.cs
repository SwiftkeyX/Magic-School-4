using System.Collections.Generic;
using MagicSchool.Contracts;
using static MagicSchool.Skills.SkillFactory;

namespace MagicSchool.Skills
{
    internal class WerewolfSkill : SkillDefinition
    {
        // werewolf play 3 different attack
        // box, triangle, circle - in the order 
        private readonly SkillActionGroup[] _beats;
        private int _currentBeat;

        private const float OmnivampFromAP = 10f;  
        // FLAGGING: sheet: 80% of bonus AS, converted to AD 
        private const float AttackFromAS = 500f;   
        private const float TransformDuration = 10f;

        // each beat (attack) have different damage amount
        private const float BoxDamage = 200f;
        private const float TriangleDamage = 300f;
        private const float CircleDamage = 400f;


        // FIXLATER: turn every hero into skilldefinition. and move Build inside the skilldefinition.
        public static SkillDefinition Build(TemplateActionRegistrySO registry) => new WerewolfSkill(registry);

        private WerewolfSkill(TemplateActionRegistrySO registry)
            : base(
                skillName: "Moonrage",
                activeSteps: new List<SkillStep> { Transform(registry) },
                description: GetSkillDescription(),
                passiveDescription: GetPassiveDescription())
        {
            _beats = new[]
            {
                Beat(registry, TemplateActionEnum.BoxAOE,      BoxDamage),
                Beat(registry, TemplateActionEnum.TriangleAOE, TriangleDamage),
                Beat(registry, TemplateActionEnum.CircleAOE,   CircleDamage),
            };
        }

        // ============================== init ==============================
        private static string GetSkillDescription()
        {
            return $"Transforms for {TransformDuration} seconds, draining {OmnivampFromAP}% AP of the damage "
                 + "he deals back as life and turning his bonus attack speed into raw attack damage.";
        }

        private static string GetPassiveDescription()
        {
            return "While transformed his auto attack becomes a three beat combo - box, then triangle, "
                 + $"then circle - landing for {BoxDamage}% / {TriangleDamage}% / {CircleDamage}% AD "
                 + "in turn.";
        }

        public override void Init(ICombatant caster)
        {
            base.Init(caster);

            foreach (SkillActionGroup beat in _beats) beat.Init(caster);
        }

        // ============================== passive ==============================
        public override bool HasPassive => true;

        public override bool OnAttack(ICombatant target)
        {
            if (!Caster.HasStatus(ModifierEnum.Transformed)) return false;

            if (!PlayGroup(_beats[_currentBeat])) return false;

            _currentBeat = (_currentBeat + 1) % _beats.Length;
            return true;
        }

        // ============================== active ==============================
        private static SkillStep Transform(TemplateActionRegistrySO registry)
        {
            // one group, one timer - the whole transform ends on the same tick
            ICustomModifier WorldEnderBuff = Bundle(
                duration: TransformDuration,

                Buff(
                    modifier: ModifierEnum.Omnivamp,
                    ratios: (StatEnum.AP, OmnivampFromAP, ScaleFromEnum.Base)),

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

        private static SkillActionGroup Beat(TemplateActionRegistrySO registry, TemplateActionEnum action, float damage)
            => ActionGroup(
                registry: registry,
                source: ActionSourceEnum.Self,
                action: action,
                target: AimTargetEnum.Current,
                tuning: TuneAOE(offset: AOEOffsetEnum.Tip),
                Damage(
                    recipient: EffectRecipientEnum.EnemiesInArea,
                    ratios: (StatEnum.ATK, damage))
            );
    }
}
