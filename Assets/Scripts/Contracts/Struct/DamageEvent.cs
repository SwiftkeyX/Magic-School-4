namespace MagicSchool.Contracts
{
    public readonly struct DamageEvent
    {
        public readonly IEffectable Source;      // who dealt it. null if nothing authored the hit
        public readonly IEffectable Target;      // who took it
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
