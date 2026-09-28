namespace MagicSchool.Contracts
{
    // ICombatRecord answers: over one round of combat, show this unit's performance to the player.
    // e.g. Vharn do 400 damage, take 300 damage, and healed for 120
    public interface ICombatRecord
    {
        // === what this unit did to others ===
        int DamageDealt { get; }            // AutoAttackDamage + SkillDamage
        int AutoAttackDamage { get; }
        int SkillDamage { get; }
        int Overkill { get; }
        int HealingDone { get; }
        int Overhealing { get; }

        // === what others did to this unit ===
        int DamageTaken { get; }
        int DamageMitigated { get; }
        int HealingReceived { get; }
        int HealingLostToWound { get; }
    }
}
