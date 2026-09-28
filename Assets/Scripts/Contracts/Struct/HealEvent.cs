namespace MagicSchool.Contracts
{
    public readonly struct HealEvent
    {
        public readonly IEffectable Source;      // who healed
        public readonly IEffectable Target;      // who was healed
        public readonly HealOutcome Outcome;

        public HealEvent(IEffectable source, IEffectable target, HealOutcome outcome)
        {
            Source = source;
            Target = target;
            Outcome = outcome;
        }
    }
}
