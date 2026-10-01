using System.Collections.Generic;
using MagicSchool.Contracts;
using MagicSchool.Modifiers;
using static MagicSchool.Skills.SkillFactory;

namespace MagicSchool.Skills
{
    internal class HuskSkill : SkillDefinition
    {
        private static readonly StatEnum[] StolenStats = { StatEnum.MaxHP, StatEnum.DF, StatEnum.MR };
        private const float StealPercent = 10f;

        // passive - every ally, for the rest of the fight
        private const float RestOfFight = -1f;

        // active - random enemies, for a while
        private const int EnemyCount = 4;
        private const float StealDuration = 7f;

        public static SkillDefinition Build(TemplateActionRegistrySO registry) => new HuskSkill(registry);

        private HuskSkill(TemplateActionRegistrySO registry) : base(registry) { }

        public override string SkillName => "Siphon Flesh";

        public override string Description
            => $"Steals {StealPercent}% of the HP, DF and MR of {EnemyCount} random enemies for {StealDuration} seconds. "
             + "Stacks with every cast.";

        public override string PassiveDescription
            => $"When combat starts, every ally gives up {StealPercent}% of their HP, DF and MR to him "
             + "for the rest of the fight.";

        // ============================== passive ==============================
        public override bool HasPassive => true;

        public override bool OnCombatStart()
        {
            foreach (ICombatant ally in Caster.FindAllAllies())
                StatSteal.Take(Caster, ally, StealPercent, RestOfFight, StolenStats);

            return base.OnCombatStart();
        }

        // ============================== active ==============================
        public override bool OnCast()
        {
            if (!base.OnCast()) return false;

            foreach (ICombatant enemy in Caster.FindRandomEnemies(EnemyCount))
                StatSteal.Take(Caster, enemy, StealPercent, StealDuration, StolenStats);

            return true;
        }

        // the cast itself only plays the animation; the steal happens in OnCast
        protected override List<SkillFlow> Active(TemplateActionRegistrySO registry)
        {
            SkillPart cast = Part(
                registry: registry,
                source: ActionSourceEnum.Self,
                action: TemplateActionEnum.Cast,
                target: AimTargetEnum.Self);

            return new List<SkillFlow> { Flow(trigger: TriggerEnum.OnCast, groups: cast) };
        }
    }
}
