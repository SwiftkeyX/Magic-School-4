using MagicSchool.Contracts;

namespace MagicSchool.Combat.Placements
{
    // Anything that can stand on a board hex
    internal interface IHexPlaceable : IPlaceable
    {
        Hex ReservedHex { get; }

        void SetReservedHex(Hex hex);
    }

    internal static class PlaceableHexExtensions
    {
        // A hero's Placement is a Hex only while it's on the battlefield - null on the bench.
        // Mirrors what Hero.CurrentHex does, but reachable through the interface.
        public static Hex CurrentHex(this IPlaceable placeable) => placeable?.CurrentPlacement as Hex;

        // Where a unit is about to stand: the hex it is stepping onto while a step is in progress, else the one it stands on.
        // A unit mid-step still counts as on the hex it left (it is handed over on arrival, See HexStep), though it is drawn
        // almost on the next one - so whoever measures the way to it should measure to where it is arriving.
        // e.g.     a Minotaur stepping up next to Reaper is 1 step away, not 4 from the hex it is leaving
        public static Hex ArrivingHex(this ICombatant unit)
        {
            Hex current = unit.CurrentHex();
            if (current == null || unit is not IHexPlaceable walker) return current;

            // only a step in progress. A stunned unit keeps its reservation but is not coming.
            bool isStepping = unit.StateType == HeroStateEnum.Walk || unit.StateType == HeroStateEnum.Cast;
            Hex arriving = walker.ReservedHex;

            return isStepping && arriving != null ? arriving : current;
        }
    }
}
