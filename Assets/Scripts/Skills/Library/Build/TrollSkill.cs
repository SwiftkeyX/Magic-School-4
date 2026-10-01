using System.Collections.Generic;
using MagicSchool.Contracts;
using static MagicSchool.Skills.SkillFactory;

namespace MagicSchool.Skills
{
    internal class TrollSkill : SkillDefinition
    {
        // slam
        private const float DamageRatio = 150f;
        private const float WoundDuration = 6f;

        // area - the centre hex plus that many rings around it (hexes are ~1 apart)
        private const int AreaHexes = 1;
        private const float AreaDiameter = AreaHexes * 2f + 0.5f;

        // passive
        private const float BonusHPPerDeath = 300f;
        private const float RestOfFight = -1f;

        public static SkillDefinition Build(TemplateActionRegistrySO registry) => new TrollSkill(registry);

        private TrollSkill(TemplateActionRegistrySO registry) : base(registry) { }

        public override string SkillName => "Rotting Slam";

        public override string Description
            => $"Slams the ground for {DamageRatio}% AD to enemies around him and wounds them for "
             + $"{WoundDuration} seconds (reduced healing).";

        public override string PassiveDescription
            => $"Whenever an enemy dies, he gains {BonusHPPerDeath} bonus health for the rest of the fight and heals for as much.";

        // ============================== passive ==============================
        public override bool HasAttackPassive => true;

        // an enemy died - gain bonus health, and the HP to fill it.
        // A fresh bundle every time, so the deaths stack
        public override void OnHeroDied(ICombatant dead)
        {
            if (dead.Team == Caster.Team) return;

            Caster.AddModifier(
                Bundle(RestOfFight, Buff(ModifierEnum.BonusHP, BonusHPPerDeath)),
                Caster as IHeroStats);

            Caster.Heal(BonusHPPerDeath, Caster);
        }

        // ============================== active ==============================
        protected override SkillFlow BuildActiveFlow(TemplateActionRegistrySO registry)
        {
            ICustomModifier wound = Bundle(WoundDuration, Status(ModifierEnum.Wound));

            // a circle around himself
            SkillPart slam = Part(
                registry: registry,
                source: ActionSourceEnum.Self,
                action: TemplateActionEnum.CircleAOE,
                target: AimTargetEnum.Self,
                tuning: TuneAOE(length: AreaDiameter, width: AreaDiameter),

                Damage(EffectRecipientEnum.EnemiesInArea, (StatEnum.ATK, DamageRatio)),
                Apply(EffectRecipientEnum.EnemiesInArea, wound)
            );

            return Flow(onStart: slam);
        }
    }
}
