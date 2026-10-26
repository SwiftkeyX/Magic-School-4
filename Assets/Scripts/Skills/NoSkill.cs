namespace MagicSchool.Skills
{
    // The skill of a hero that has none (a dummy, Hero A): every hook does nothing, so a hero never checks for null.
    public sealed class NoSkill : SkillDefinition
    {
        public NoSkill() : base(null) { }

        protected override SkillFlow BuildActiveFlow(TemplateActionRegistrySO registry) => null;
        public override string SkillName => string.Empty;
        public override string Description => string.Empty;
    }
}
