namespace MagicSchool.Contracts
{
    // Landed: HP actually removed. Overkill: the part past 0 HP. Mitigated: what DF and Damage Reduction saved.
    public readonly struct DamageOutcome
    {
        public readonly int NewHP;
        public readonly int Landed;
        public readonly int Overkill;
        public readonly int Mitigated;

        public DamageOutcome(int newHP, int landed, int overkill, int mitigated)
        {
            NewHP = newHP;
            Landed = landed;
            Overkill = overkill;
            Mitigated = mitigated;
        }
    }
}
