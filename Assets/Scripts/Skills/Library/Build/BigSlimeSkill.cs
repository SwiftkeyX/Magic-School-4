using MagicSchool.Contracts;

namespace MagicSchool.Skills
{
    internal class BigSlimeSkill : SkillDefinition
    {
        private static int SplitCount => Tuned(2);

        public static SkillDefinition Build(TemplateActionRegistrySO registry) => new BigSlimeSkill(registry);

        private BigSlimeSkill(TemplateActionRegistrySO registry) : base(registry) { }

        public override string SkillName => "Split";

        public override string Description => "";

        public override string PassiveDescription
            => $"When he dies, he splits into {SplitCount} Small Slimes.";

        public override void OnDeath()
        {
            Caster?.SpawnUnitsNearby(SplitCount);
        }

        protected override SkillFlow BuildActiveFlow(TemplateActionRegistrySO registry) => null;
    }
}
