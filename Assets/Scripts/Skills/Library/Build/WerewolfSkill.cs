using System.Collections.Generic;
using MagicSchool.Contracts;
using static MagicSchool.Skills.SkillFactory;

namespace MagicSchool.Skills
{
    internal class WerewolfSkill : SkillDefinition
    {
        // werewolf skill play 3 different attack
        // box, triangle, circle - in the order 
        private readonly SkillPart[] _beats;
        private int _currentBeat;

        // each beat (attack) have different damage amount
        private const float BoxDamage = 200f;
        private const float TriangleDamage = 300f;
        private const float CircleDamage = 400f;

        private const float OmnivampFromAP = 10f;
        // FLAGGING: sheet: 80% of bonus AS, converted to AD 
        private const float AttackFromAS = 500f;
        private const float TransformDuration = 10f;


        // FIXLATER: turn every hero into skilldefinition. and move Build inside the skilldefinition.
        public static SkillDefinition Build(TemplateActionRegistrySO registry) => new WerewolfSkill(registry);

        private WerewolfSkill(TemplateActionRegistrySO registry) : base(registry)
        {
            _beats = new[]
            {
                Beat(registry, TemplateActionEnum.BoxAOE,      BoxDamage),
                Beat(registry, TemplateActionEnum.TriangleAOE, TriangleDamage),
                Beat(registry, TemplateActionEnum.CircleAOE,   CircleDamage),
            };
        }

        public override string SkillName => "Moonrage";

        public override string Description
            => $"Transforms for {TransformDuration} seconds, draining {OmnivampFromAP}% AP of the damage "
             + "he deals back as life and turning his bonus attack speed into raw attack damage.";

        public override string PassiveDescription
            => "While transformed his auto attack becomes a three beat combo - box, then triangle, "
             + $"then circle - landing for {BoxDamage}% / {TriangleDamage}% / {CircleDamage}% AD "
             + "in turn.";

        // ============================== init ==============================

        public override void Init(ICombatant caster)
        {
            base.Init(caster);

            foreach (SkillPart beat in _beats) beat.Init(caster);
        }

        // ============================== passive ==============================
        public override bool HasAttackPassive => true;

        // at combat start, reset the beat
        public override bool OnCombatStart()
        {
            _currentBeat = 0;
            return base.OnCombatStart();
        }

        // if transform, auto-attack play the current beat
        public override bool OnAttack(ICombatant target)
        {
            if (!Caster.HasStatus(ModifierEnum.Transformed)) return false;

            if (!PlayOnePart(_beats[_currentBeat])) return false;

            _currentBeat = (_currentBeat + 1) % _beats.Length;
            return true;
        }

        // ============================== active ==============================
        protected override SkillFlow BuildActiveFlow(TemplateActionRegistrySO registry)
        {
            return Transform(registry);
        }

        private static SkillFlow Transform(TemplateActionRegistrySO registry)
        {
            // one group, one timer - the whole transform ends on the same tick
            ICustomModifier WorldEnderBuff = BundleRefresh(
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

            SkillPart cast = Part(
                registry: registry,
                source: ActionSourceEnum.Self,
                action: TemplateActionEnum.Cast,
                target: AimTargetEnum.Self,
                Apply(
                    recipient: EffectRecipientEnum.Self,
                    modifier: WorldEnderBuff)
            );

            return Flow(onStart: cast);
        }

        private static SkillPart Beat(TemplateActionRegistrySO registry, TemplateActionEnum action, float damage)
            => Part(
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
