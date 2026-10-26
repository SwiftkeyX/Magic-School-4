namespace MagicSchool.Contracts
{
    // ICombatRecord answers: over one round of combat, show this unit's performance to the player.
    // e.g. Werewolf do 400 damage, take 300 damage, and healed for 120
    public interface ICombatRecord
    {
        int DamageDealt { get; }
        int AutoAttackDamage { get; }
        int SkillDamage { get; }
        int Overkill { get; }
        int HealingDone { get; }
        int Overhealing { get; }

        int DamageTaken { get; }
        int DamageMitigated { get; }
        int HealingReceived { get; }
        int HealingLostToWound { get; }

        float FightSeconds { get; }
        float SecondsAlive { get; }
    }
}
