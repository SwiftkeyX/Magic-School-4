namespace MagicSchool.Contracts
{
    // IHexMeasurable answer: how far apart two units stand, counted in hexes - Contracts holds no Hex to measure with.
    // e.g.     Skeleton Archer's shot is stronger against a target 3 or more hexes away
    // False when either unit stands on no hex.
    public interface IHexMeasurable
    {
        bool IsAtLeastHexesAway(IPlaceable other, int hexes);
    }
}
