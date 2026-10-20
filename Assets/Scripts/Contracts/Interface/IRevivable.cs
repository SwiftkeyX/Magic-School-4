namespace MagicSchool.Contracts
{
    // IRevivable answers: can a skill bring this unit back after it dies?
    // e.g.     Reaper, once a fight, gets back up 3 seconds after she dies
    public interface IRevivable
    {
        // dead now: get back up after [seconds], with [hpRatio] of max HP (0.3 = 30%)
        void ReviveAfter(float seconds, float hpRatio);

        // dead, but getting back up - still counts as standing, so the fight does not end on it
        bool IsReviving { get; }
    }
}
