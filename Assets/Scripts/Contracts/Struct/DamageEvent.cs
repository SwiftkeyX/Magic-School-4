namespace MagicSchool.Contracts
{
    // Source is null when nothing authored the hit.
    public readonly struct DamageEvent
    {
        public readonly IEffectable Source;
        public readonly IEffectable Target;
        public readonly DamageKindEnum Kind;
        public readonly DamageOutcome Outcome;

        public DamageEvent(IEffectable source, IEffectable target, DamageKindEnum kind, DamageOutcome outcome)
        {
            Source = source;
            Target = target;
            Kind = kind;
            Outcome = outcome;
        }
    }
}
