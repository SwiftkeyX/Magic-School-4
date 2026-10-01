
namespace MagicSchool.Skills
{
    // a enum that used to pair a hero SO => with a skill code
    // e.g. HeroSO contain enum "Elf" => use the enum to find "ElfSkill.cs"
    // Serialized into HeroDataSO assets as a raw int. The numbers were kept when the roster was
    // renamed (Vesper -> Elf, ...), so never reuse a retired one - an old asset may still hold it.
    public enum SkillIdEnum
    {
        None = 0,

        Werewolf = 1,
        Naga = 2,
        ShieldKnight = 3,
        OrcBlademaster = 4,
        Ranger = 5,
        // 6 retired (Solace)
        Elf = 7,
        // 8 retired (Pip)
        DireWolf = 9,
        Dryad = 10,
        Knight = 11,
        Centaur = 12,
        Dwarf = 13,
        SkeletonArcher = 14,
        Imp = 15,
        Reaper = 16,
        // 17 retired (Verity)
        Myconid = 18,
        Monk = 19,
        Blacksmith = 20,
        Bandit = 21,
        FrostWitch = 22,
        Swordsman = 23,
        Lich = 24,
        Dragon = 25,
        Ettin = 26,
        Harpy = 27,
        Husk = 28,
        RockGolem = 29,
        Templar = 30,
    }
}
