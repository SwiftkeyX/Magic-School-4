namespace MagicSchool.Contracts
{
    // IHexMeasurable answer: how far apart two units stand, counted in hexes - Contracts holds no Hex to measure with.
    // e.g.     Skeleton Archer's shot is stronger against a target 3 or more hexes away
    public interface IHexMeasurable
    {
        // [other] stands at least [hexes] hexes from here. False when either of them stands on no hex.
        bool IsAtLeastHexesAway(IPlaceable other, int hexes);
    }
}
