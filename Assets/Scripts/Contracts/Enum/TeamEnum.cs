namespace MagicSchool.Contracts
{
    // Serialized into HexPlacement (BattleBoard/BattlePlacementSO assets) as a raw int - always
    // assign explicit values so inserting a new member later can't silently remap existing assets.
    public enum TeamEnum
    {
        Blue = 0,
        Red = 1,
    }

}
