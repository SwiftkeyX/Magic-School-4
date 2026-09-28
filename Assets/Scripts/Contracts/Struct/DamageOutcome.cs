namespace MagicSchool.Contracts
{
    public readonly struct DamageOutcome
    {
        public readonly int NewHP;
        public readonly int Landed;         // HP actually removed
        public readonly int Overkill;       // the part of the hit that fell past 0 HP
        public readonly int Mitigated;      // what DF and Damage Reduction saved

        public DamageOutcome(int newHP, int landed, int overkill, int mitigated)
        {
            NewHP = newHP;
            Landed = landed;
            Overkill = overkill;
            Mitigated = mitigated;
        }
    }
}
