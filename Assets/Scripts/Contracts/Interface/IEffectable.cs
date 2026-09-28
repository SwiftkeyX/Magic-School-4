namespace MagicSchool.Contracts
{
    // IEffectable answer: to make a unit that can be affected by a attack & skill.
    // This unit can take damage, can be healed, can add modifier.
    // e.g. heroA hit heroB - so it ask IEffectable to take damage.
    // e.g. skill want to add a new modifier to a hit target - so it ask IEffectable to add new modifier.
    public interface IEffectable
    {
        void TakeDamage(int damage, IEffectable source, DamageKindEnum kind);
        void Heal(float amount, IEffectable source);
        void AddModifier(ICustomModifier modifier, float amplifier, IHeroStats casterStats);
        bool HasStatus(ModifierEnum status);    // "is this one transformed / wounded / stunned".

        bool IsAlive { get; }
    }
}