using System.Collections.Generic;
using MagicSchool.Contracts;
using static MagicSchool.Skills.SkillFactory;

namespace MagicSchool.Skills
{
    internal class RockGolemSkill : SkillDefinition
    {
        private static float ShieldAmount => Tuned(500f);
        private static float ShieldDuration => Tuned(4f);
        private readonly ICustomModifier _shield = BundleRefresh(ShieldDuration, Shield(ShieldAmount));

        // the allies standing on this mark are his wards (See AlignMark)
        private const string WardMark = "Ward";
        public override IReadOnlyList<string> SpecialMarks => new[] { WardMark };


        public static SkillDefinition Build(TemplateActionRegistrySO registry) => new RockGolemSkill(registry);

        private RockGolemSkill(TemplateActionRegistrySO registry) : base(registry) { }

        public override string SkillName => "Stone Oath";

        public override string Description
            => $"Gives a {ShieldAmount} shield to himself and his wards for {ShieldDuration} seconds.";

        public override string PassiveDescription
            => $"The allies on his {WardMark} mark are his wards.";

        // ============================== passive ==============================
        public override bool HasAutoAttackPassive => true;

        // ============================== active ==============================
        public override bool OnCast()
        {
            if (!base.OnCast()) return false;

            Caster.AddModifier(_shield, Caster as IHeroStats);

            // his wards: the allies on his Ward mark (See IAligned)
            if (Caster is IAligned aligned)
            {
                foreach (ICombatant ward in aligned.AlliesOnSpecialMark(WardMark))
                {
                    if (ward != null && ward.IsAlive) ward.AddModifier(_shield, Caster as IHeroStats);
                }
            }

            return true;
        }

        // the cast itself only plays the animation; the shields land in OnCast
        protected override SkillFlow BuildActiveFlow(TemplateActionRegistrySO registry)
        {
            SkillPart cast = Part(
                registry: registry,
                source: ActionSourceEnum.Self,
                action: TemplateActionEnum.Cast,
                target: AimTargetEnum.Self);

            return Flow(onStart: cast);
        }
    }
}
