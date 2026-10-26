namespace MagicSchool.Contracts
{
    // Healed: HP actually gained. Overhealed: the part past max HP. LostToWound: what Wound halved away.
    public readonly struct HealOutcome
    {
        public readonly int NewHP;
        public readonly int Healed;
        public readonly int Overhealed;
        public readonly int LostToWound;

        public HealOutcome(int newHP, int healed, int overhealed, int lostToWound)
        {
            NewHP = newHP;
            Healed = healed;
            Overhealed = overhealed;
            LostToWound = lostToWound;
        }
    }
}
