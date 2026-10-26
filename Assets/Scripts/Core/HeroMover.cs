using MagicSchool.Contracts;

namespace MagicSchool.Core
{
    /// <summary>
    /// This is the system that move hero during preparation state. (not in combat state) 
    /// e.g. seeding hero on the board, player move hero by dragging
    /// </summary>
    public class HeroMover
    {
        public void MoveThisHeroTo(ICombatant hero, IPlacement placement)
        {
            IPlacement oldPlacement = hero.CurrentPlacement;
            if (oldPlacement != null) oldPlacement.OnUnitUnplaced(hero);

            placement.OnUnitPlaced(hero);

            if (hero.IsInCombat) hero.TrackOnBoard();
            else hero.UntrackFromBoard();
        }
    }
}

