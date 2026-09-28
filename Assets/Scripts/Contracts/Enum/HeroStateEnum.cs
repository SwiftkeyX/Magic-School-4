namespace MagicSchool.Contracts
{
    // Lives here rather than with the states themselves: ICombatant hands it out, and the skill
    // system reads it to tell a casting unit from a walking one. Both would otherwise have to
    // depend on the state machine just to name a state.
    public enum HeroStateEnum { Idle, Walk, Attack, Dead, Stunned, Cast }
}
