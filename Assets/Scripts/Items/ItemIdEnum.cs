namespace MagicSchool.Items
{
    // enum that pair a item SO => with a item code
    // e.g. ItemDataSO contain enum "IronPlate" => use the enum to find "IronPlate.cs"
    // Assets store these ints: never reuse one. ForgedIronPlate, MasterworkIronPlate, TemperedIronPlate and
    // ReinforcedIronPlate are Blacksmith's forged items, never in the shop.
    public enum ItemIdEnum
    {
        None = 0,

        Whetstone = 2,
        DuelistGauntlet = 3,
        HuntersCord = 4,
        ReaversEdge = 5,
        RunedEdge = 6,

        ApprenticeWand = 7,
        ArchmageTome = 8,
        SagesFocus = 9,
        Stormglass = 10,
        LeyBattery = 11,

        IronPlate = 1,
        OakenCharm = 12,
        BulwarkCrest = 13,
        AegisShard = 14,
        WardensVow = 15,
        ForgedIronPlate = 21,
        MasterworkIronPlate = 22,
        TemperedIronPlate = 29,
        ReinforcedIronPlate = 30,

        ChaliceOfDawn = 16,
        ScholarsSash = 17,
        FarsightLens = 18,
        LongshotQuiver = 19,
        VitalisWeave = 20,

        DewdropCharm = 23,
        MoonwellPendant = 24,
        StarwellOrb = 25,

        SerratedFang = 26,
        PiercingBolt = 27,
        GiantsBane = 28,
    }
}
