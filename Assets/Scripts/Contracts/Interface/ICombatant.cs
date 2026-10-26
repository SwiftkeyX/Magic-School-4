namespace MagicSchool.Contracts
{
    // ICombatant answers: a unit fighting on the board, without needing the whole Hero.
    // e.g.     BattleBoard lists the heroes on it; a template action asks where its caster stands and whom to aim at.
    // It bundles a lot (a known god interface, see .claude/docs/flagging.md).
    public interface ICombatant : IPlaceable, IEffectable, ITargeter
    {
        TeamEnum Team { get; }
        HeroStateEnum StateType { get; }

        void TrackOnBoard();
        void UntrackFromBoard();
    }
}
