namespace MagicSchool.Contracts
{
    public readonly struct HealOutcome
    {
        public readonly int NewHP;
        public readonly int Healed;         // HP actually gained
        public readonly int Overhealed;     // the part that fell past MaxHP
        public readonly int LostToWound;    // what Wound halved away

        public HealOutcome(int newHP, int healed, int overhealed, int lostToWound)
        {
            NewHP = newHP;
            Healed = healed;
            Overhealed = overhealed;
            LostToWound = lostToWound;
        }
    }
}
