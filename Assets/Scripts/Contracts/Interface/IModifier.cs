namespace MagicSchool.Contracts
{
    // IModifier to answer: How much this modifier give stat (stat after calcuation)?
    // e.g. Hero ask "hey, I have like 10 modifiers, give me how much stat, each modifier actualy give me"
    public interface IModifier
    {
        public float GetBonusAmount(IHeroStats stats);      // get bonus amount from this modifier (bonus amount is the final amount after modifier calculation)
        public ModifierEnum GetModifierEnum();              // get modifier enum - to know what this modifier should behave
        public ScalingSourceEnum GetScalingSource();        // get whose stats it should be scaling off
    }
}
