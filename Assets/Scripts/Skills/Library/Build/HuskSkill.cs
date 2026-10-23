using System.Collections.Generic;
using MagicSchool.Contracts;
using MagicSchool.Modifiers;
using static MagicSchool.Skills.SkillFactory;

namespace MagicSchool.Skills
{
    internal class HuskSkill : SkillDefinition
    {
        private static readonly StatEnum[] StolenStats = { StatEnum.MaxHP, StatEnum.DF };
        private static float StealPercent => Tuned(10f);

        // the allies standing on this mark feed him (See AlignMark)
        private const string SiphonMark = "Siphon";
        public override IReadOnlyList<string> SpecialMarks => new[] { SiphonMark };

        // passive - the allies on his Siphon mark, for the rest of the fight
        private static float RestOfFight => Tuned(-1f);

        // active - random enemies, for a while: this many, plus one for every ally on his Siphon mark
        private static int EnemyCount => Tuned(4);
        private static float StealDuration => Tuned(7f);

        public static SkillDefinition Build(TemplateActionRegistrySO registry) => new HuskSkill(registry);

        private HuskSkill(TemplateActionRegistrySO registry) : base(registry) { }

        public override string SkillName => "Siphon Flesh";

        public override string Description
            => $"Steals {StealPercent}% of the HP and DF of {EnemyCount} random enemies, plus 1 for every ally "
             + $"on his {SiphonMark} mark, for {StealDuration} seconds. Stacks with every cast.";

        public override string PassiveDescription
            => $"When combat starts, every ally on his {SiphonMark} mark gives up {StealPercent}% of their HP and DF "
             + "to him for the rest of the fight.";

        // the allies on his Siphon mark (See IAligned); none for a hero not aligned, e.g. an enemy
        private IReadOnlyList<ICombatant> AlliesOnSiphon()
            => Caster is IAligned aligned ? aligned.AlliesOnSpecialMark(SiphonMark) : new ICombatant[0];

        // ============================== passive ==============================
        public override bool HasAutoAttackPassive => true;

        public override bool OnCombatStart()
        {
            foreach (ICombatant ally in AlliesOnSiphon())
            {
                if (ally != null && ally.IsAlive) StatSteal.Take(Caster, ally, StealPercent, RestOfFight, StolenStats);
            }

            return base.OnCombatStart();
        }

        // ============================== active ==============================
        public override bool OnCast()
        {
            if (!base.OnCast()) return false;

            int count = EnemyCount + AlliesOnSiphon().Count;
            foreach (ICombatant enemy in Caster.FindRandomEnemies(count))
                StatSteal.Take(Caster, enemy, StealPercent, StealDuration, StolenStats);

            return true;
        }

        // the cast itself only plays the animation; the steal happens in OnCast
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
