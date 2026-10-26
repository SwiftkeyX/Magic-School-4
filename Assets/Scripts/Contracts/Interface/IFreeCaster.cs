namespace MagicSchool.Contracts
{
    // IFreeCaster answers: can another unit's skill make this unit cast its own skill, without mana?
    // e.g.     every 3 kills, Reaper lets the allies on her "Reap" mark cast once for free
    // A grant casts once, as soon as it can, without spending or needing mana. Several grants are kept, one cast each.
    public interface IFreeCaster
    {
        void GrantFreeCast();
    }
}
