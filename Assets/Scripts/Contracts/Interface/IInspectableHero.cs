namespace MagicSchool.Contracts
{
    public interface IInspectableHero : IInspectable, IHeroStats
    {
        TeamEnum Team { get; }
        bool HasSkill { get; }
        string SkillName { get; }
        string SkillDescription { get; }
        bool HasAutoAttackPassive { get; }
        string PassiveDescription { get; }

        // the same texts with each amount as its number for this hero, or as its formula (See SkillDefinition.DescribeActive)
        string SkillDescriptionFor(bool formula);
        string PassiveDescriptionFor(bool formula);
        string AlignDescriptionFor(bool formula);
    }
}
