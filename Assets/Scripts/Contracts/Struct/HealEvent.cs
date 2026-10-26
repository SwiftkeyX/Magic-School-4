namespace MagicSchool.Contracts
{
    public readonly struct HealEvent
    {
        public readonly IEffectable Source;
        public readonly IEffectable Target;
        public readonly HealOutcome Outcome;

        public HealEvent(IEffectable source, IEffectable target, HealOutcome outcome)
        {
            Source = source;
            Target = target;
            Outcome = outcome;
        }
    }
}
