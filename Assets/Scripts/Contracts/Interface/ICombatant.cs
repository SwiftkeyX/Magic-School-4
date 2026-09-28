namespace MagicSchool.Contracts
{
    // Contract for anything that needs a unit fighting on the board, without needing the whole Hero.
    // E.g.
    // BattleBoard asks "what is all the hero on the board"
    // a template action asks "where its caster stands and who to aim at" 

    // FLAGGING: maybe we have to rethink about the interface. 
    // ICombatant is overused which is a signal for it being god interface.

    // ICombatant answer: ???
    // it is a package that do so much thing. Read FLAGGING.
    public interface ICombatant : IPlaceable, IEffectable, ITargeter
    {
        TeamEnum Team { get; }
        HeroStateEnum StateType { get; }

        // A unit should knows which board it's on
        void TrackOnBoard();
        void UntrackFromBoard();
    }
}
