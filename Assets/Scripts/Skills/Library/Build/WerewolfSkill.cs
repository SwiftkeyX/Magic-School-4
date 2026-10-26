using System.Collections.Generic;
using MagicSchool.Contracts;
using static MagicSchool.Skills.SkillFactory;

namespace MagicSchool.Skills
{
    // Transformed, his auto attacks play a combo of three beats in order - box, triangle, circle - each with its own
    // damage, reset at combat start; he gains no mana meanwhile. The whole transform is one bundle, so it ends on one
    // tick.
    internal class WerewolfSkill : SkillDefinition
    {
        private readonly SkillPart[] _beats;
        private int _currentBeat;

        private static float BoxDamage => Tuned(200f);
        private static float TriangleDamage => Tuned(300f);
        private static float CircleDamage => Tuned(400f);

        private static float OmnivampFromAP => Tuned(10f);
        private static float AttackFromAS => Tuned(500f);
        private static float TransformDuration => Tuned(10f);

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
            => $"Transforms for {TransformDuration} seconds, draining {AP(OmnivampFromAP)}% of the damage "
             + "he deals back as life and turning his bonus attack speed into raw attack damage.";

        public override string PassiveDescription
            => "While transformed his auto attack becomes a three beat combo - box, then triangle, "
             + $"then circle - landing for {AD(BoxDamage)} / {AD(TriangleDamage)} / {AD(CircleDamage)} "
             + "in turn.";

        public override void Init(ISkillCaster caster)
        {
            base.Init(caster);

            foreach (SkillPart beat in _beats) beat.Init(caster);
        }

        public override bool HasAutoAttackPassive => true;

        public override bool OnCombatStart()
        {
            _currentBeat = 0;
            return base.OnCombatStart();
        }

        public override bool OnAutoAttack(ICombatant target)
        {
            if (!Caster.HasStatus(ModifierEnum.Transformed)) return false;

            if (!PlayOnePart(_beats[_currentBeat])) return false;

            _currentBeat = (_currentBeat + 1) % _beats.Length;
            return true;
        }

        protected override SkillFlow BuildActiveFlow(TemplateActionRegistrySO registry)
        {
            return Transform(registry);
        }

        private static SkillFlow Transform(TemplateActionRegistrySO registry)
        {
            ICustomModifier WorldEnderBuff = BundleRefresh(
                duration: TransformDuration,

                Buff(
                    modifier: ModifierEnum.Omnivamp,
                    ratios: (StatEnum.AP, OmnivampFromAP, ScaleFromEnum.Base)),

                Buff(
                    modifier: ModifierEnum.ATK,
                    ratios: (StatEnum.AS, AttackFromAS, ScaleFromEnum.Base)),

                Status(ModifierEnum.Transformed),

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
