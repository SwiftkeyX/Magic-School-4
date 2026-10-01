namespace MagicSchool.Contracts
{
    public interface IInspectableHero : IInspectable, IHeroStats
    {
        TeamEnum Team { get; }
        bool HasSkill { get; }
        string SkillName { get; }
        string SkillDescription { get; }
        bool HasAttackPassive { get; }
        string PassiveDescription { get; }
    }
}
