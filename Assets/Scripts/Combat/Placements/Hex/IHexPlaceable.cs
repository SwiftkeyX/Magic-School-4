using MagicSchool.Contracts;

namespace MagicSchool.Combat.Placements
{
    // Anything that can stand on a board hex
    // A hero's placement is a Hex only while it is on the battlefield; null on the bench. ArrivingHex: a unit mid-step
    // still counts as on the hex it left (it is handed over on arrival, See HexStep), though it is drawn almost on the
    // next one, so whoever measures the way to it measures to where it is arriving - a Minotaur stepping up next to
    // Reaper is 1 step away, not 4. Only a step in progress counts: a stunned unit keeps its booking but is not
    // coming.
    internal interface IHexPlaceable : IPlaceable
    {
        Hex ReservedHex { get; }

        void SetReservedHex(Hex hex);
    }

    internal static class PlaceableHexExtensions
    {
        public static Hex CurrentHex(this IPlaceable placeable) => placeable?.CurrentPlacement as Hex;

        public static Hex ArrivingHex(this ICombatant unit)
        {
            Hex current = unit.CurrentHex();
            if (current == null || unit is not IHexPlaceable walker) return current;

            bool isStepping = unit.StateType == HeroStateEnum.Walk || unit.StateType == HeroStateEnum.Cast;
            Hex arriving = walker.ReservedHex;

            return isStepping && arriving != null ? arriving : current;
        }
    }
}
