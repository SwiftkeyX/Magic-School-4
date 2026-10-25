using System.Collections.Generic;
using MagicSchool.Contracts;
using static MagicSchool.Skills.SkillFactory;

namespace MagicSchool.Skills
{
    internal class KnightSkill : SkillDefinition
    {
        private static float StunDuration => Tuned(2f);
        private static float LandingDamage => Tuned(100f);
        private static float LandingDamagePerAlly => Tuned(50f);   // % AD more for every ally on his Valor mark
        private static float LandingDiameter => Tuned(4.5f);
        private static int JumpRange => Tuned(4);
        private static float ShieldFromDamage => Tuned(100f);   // % of the damage the landing deals
        private static float ShieldDuration => Tuned(4f);

        private static float LandingRadius => LandingDiameter / 2f;

        // the allies standing on this mark make his landing hit harder (See AlignMark)
        private const string ValorMark = "Valor";
        public override IReadOnlyList<string> SpecialMarks => new[] { ValorMark };

        public static SkillDefinition Build(TemplateActionRegistrySO registry) => new KnightSkill(registry);

        private KnightSkill(TemplateActionRegistrySO registry) : base(registry) { }

        public override string SkillName => "Valiant Leap";

        public override string Description
            => "Leaps into the densest part of the enemy formation. The landing deals "
             + $"{AD(LandingDamage)}, plus {AD(LandingDamagePerAlly)} for every ally on his {ValorMark} mark, "
             + $"to everyone caught around him and leaves them stunned for "
             + $"{StunDuration} seconds. He gains a shield worth {ShieldFromDamage}% of the damage it deals, "
             + $"for {ShieldDuration} seconds.";

        // ============================== active ==============================
        protected override SkillFlow BuildActiveFlow(TemplateActionRegistrySO registry)
            => Flow(onStart: Jump(registry), onExpired: Landing(registry));

        private static SkillPart Jump(TemplateActionRegistrySO registry)
        {
            SkillPart jump = Part(
                registry: registry,
                source: ActionSourceEnum.Self,
                action: TemplateActionEnum.Move,
                target: AimTargetEnum.ClusteredCircle,
                tuning: TuneMove(range: JumpRange, spread: LandingRadius)
            );

            return jump;
        }

        private SkillPart Landing(TemplateActionRegistrySO registry)
        {
            ICustomModifier stun = BundleRefresh(
                duration: StunDuration,
                modifiers: (
                    Status(ModifierEnum.Stun)
                )
            );

            SkillPart landing = Part(
                registry: registry,
                source: ActionSourceEnum.Self,
                action: TemplateActionEnum.CircleAOE,
                target: AimTargetEnum.Self,
                tuning: TuneAOE(length: LandingDiameter, width: LandingDiameter),

                Damage(
                    recipient: EffectRecipientEnum.EnemiesInArea,
                    ratios: (StatEnum.ATK, LandingDamage)
                ).AmplifiedBy(ValorBonus)
                 .WhenLanded(GainShield),
                Apply(
                    recipient: EffectRecipientEnum.EnemiesInArea,
                    modifier: stun
                )
            );

            return landing;
        }

        // [100% + 50% per ally on his Valor mark] AD, as a share of the base landing damage
        // e.g. 2 allies on it => 200% AD, the base 100% amplified by +100%
        private static float ValorBonus(IEffectable caster)
        {
            if (!(caster is IAligned aligned) || LandingDamage <= 0f) return 0f;
            return aligned.AlliesOnSpecialMark(ValorMark).Count * LandingDamagePerAlly / LandingDamage;
        }

        // every enemy the landing hits adds what it took to his shield - a fresh bundle each, so they stack
        // e.g. 90 landed on 4 enemies => a 360 shield, for 4 seconds
        private void GainShield(int landed)
        {
            if (landed <= 0) return;

            Caster.AddModifier(BundleStack(ShieldDuration, Shield(landed * ShieldFromDamage / 100f)), Caster as IHeroStats);
        }
    }
}
