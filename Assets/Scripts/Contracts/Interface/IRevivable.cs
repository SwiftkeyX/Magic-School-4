namespace MagicSchool.Contracts
{
    // IRevivable answers: can a skill bring this unit back after it dies?
    // e.g.     Reaper, once a fight, gets back up 3 seconds after she dies
    // ReviveAfter(seconds, hpRatio): dead now, back up after the seconds with that share of max HP (0.3 = 30%).
    // IsReviving: dead but getting back up - it still counts as standing, so the fight does not end on it.
    public interface IRevivable
    {
        void ReviveAfter(float seconds, float hpRatio);

        bool IsReviving { get; }
    }
}
