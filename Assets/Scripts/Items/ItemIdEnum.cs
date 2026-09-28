namespace MagicSchool.Items
{
    // enum that pair a item SO => with a item code
    // e.g. ItemDataSO contain enum "IronPlate" => use the enum to find "IronPlate.cs"
    public enum ItemIdEnum
    {
        None = 0,

        // ======================================= Attack =======================================
        Whetstone = 2,
        DuelistGauntlet = 3,
        HuntersCord = 4,
        ReaversEdge = 5,
        RunedEdge = 6,

        // ======================================= Magic =======================================
        ApprenticeWand = 7,
        ArchmageTome = 8,
        SagesFocus = 9,
        Stormglass = 10,
        LeyBattery = 11,

        // ======================================= Defense =======================================
        IronPlate = 1,
        OakenCharm = 12,
        BulwarkCrest = 13,
        AegisShard = 14,
        WardensVow = 15,

        // ======================================= Utility =======================================
        ChaliceOfDawn = 16,
        ScholarsSash = 17,
        FarsightLens = 18,
        LongshotQuiver = 19,
        VitalisWeave = 20,
    }
}
