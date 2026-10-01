using System.Collections.Generic;
using MagicSchool.Contracts;
using static MagicSchool.Skills.SkillFactory;

namespace MagicSchool.Skills
{
    internal class RockGolemSkill : SkillDefinition
    {
        private const float ShieldAmount = 500f;
        private const float ShieldDuration = 4f;
        private readonly ICustomModifier _shield = BundleRefresh(ShieldDuration, Shield(ShieldAmount));


        // the ally he protects - picked once, when combat starts
        private ICombatant _ward;

        public static SkillDefinition Build(TemplateActionRegistrySO registry) => new RockGolemSkill(registry);

        private RockGolemSkill(TemplateActionRegistrySO registry) : base(registry) { }

        public override string SkillName => "Stone Oath";

        public override string Description
            => $"Gives a {ShieldAmount} shield to himself and his ward for {ShieldDuration} seconds.";

        public override string PassiveDescription
            => "When combat starts, he takes the closest ally as his ward.";

        // ============================== passive ==============================
        public override bool HasAttackPassive => true;

        public override bool OnCombatStart()
        {
            _ward = Caster.FindNearestAlly();

            return base.OnCombatStart();
        }

        // ============================== active ==============================
        public override bool OnCast()
        {
            if (!base.OnCast()) return false;

            Caster.AddModifier(_shield, Caster as IHeroStats);
            if (_ward != null && _ward.IsAlive) _ward.AddModifier(_shield, Caster as IHeroStats);

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
