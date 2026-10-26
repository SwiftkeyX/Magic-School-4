namespace MagicSchool.Contracts
{
    // IModifier to answer: How much this modifier give stat (stat after calcuation)?
    // e.g. Hero ask "hey, I have like 10 modifiers, give me how much stat, each modifier actualy give me"
    public interface IModifier
    {
        public float GetBonusAmount(IHeroStats stats);
        public ModifierEnum GetModifierEnum();
        public ScalingSourceEnum GetScalingSource();
    }
}
