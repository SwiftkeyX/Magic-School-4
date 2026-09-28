namespace MagicSchool.Contracts
{
    // Whose stats a StatRatio reads.
    //
    // Nearly everything scales off the caster: Quatre's 744% AD is a share of Quatre's attack. But a
    // buff handed to someone else usually means a share of THEIR stat - "allies gain 25% Attack
    // Speed" is 25% of the ally's attack speed, not of the caster's - and without this the two
    // cases are indistinguishable.
    //
    // Serialized nowhere today, but assign explicit values so it stays safe if it ever is.
    public enum ScalingSourceEnum
    {
        Caster = 0,
        Recipient = 1,
    }
}
