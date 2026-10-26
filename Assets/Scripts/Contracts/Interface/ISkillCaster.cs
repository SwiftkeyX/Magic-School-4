using System.Collections.Generic;

namespace MagicSchool.Contracts
{
    // ISkillCaster answers everything a skill may ask of its caster, or of another unit it reads: one interface instead
    // of a runtime check per question, so a missing answer is a compile error rather than a bonus of 0.
    //     SpawnUnitsNearby      put new units on the board next to itself (Big Slime splits)
    //     AlliesOnSpecialMark   the allies on its special mark of that name, as AlignBonuses.Refresh counted them when the
    //                           fight started; empty when it has no such mark (Rock Golem's wards, Skeleton Archer's long shots)
    //     WornItemCount, Role, ReceiveForgedItem
    //                           what it wears, and an item forged onto it mid-fight: the one the forger's table lists for
    //                           that role and tier, worn on top of the one slot for the rest of the fight (Blacksmith)
    //     CanForgeFor           does its forge table have a row for that role? Items are only named on its HeroDataSO
    //     IsAtLeastHexesAway    counted in hexes; false when either unit stands on no hex
    //     KilledBy, IsSummon    who landed the killing hit (null when nothing authored it), and whether another unit's
    //                           skill put it on the board mid-fight (Troll never grows from a summon's death)
    //     GrantFreeCast         cast once, as soon as it can, without spending or needing mana; grants are kept (Reaper)
    //     ReviveAfter, IsReviving
    //                           dead now, back up after the seconds with that share of max HP (0.3 = 30%); while waiting
    //                           it still counts as standing, so the fight does not end on it (Reaper)
    public interface ISkillCaster : ICombatant, IHeroStats
    {
        int SpawnUnitsNearby(int count);

        IReadOnlyList<ICombatant> AlliesOnSpecialMark(string mark);

        int WornItemCount { get; }
        HeroRoleEnum Role { get; }
        void ReceiveForgedItem(HeroRoleEnum forRole, int tier, ICombatant forger);
        bool CanForgeFor(HeroRoleEnum role);

        bool IsAtLeastHexesAway(IPlaceable other, int hexes);

        IEffectable KilledBy { get; }
        bool IsSummon { get; }

        void GrantFreeCast();

        void ReviveAfter(float seconds, float hpRatio);
        bool IsReviving { get; }
    }
}
