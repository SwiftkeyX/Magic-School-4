using System.Collections.Generic;
using MagicSchool.Contracts;
using MagicSchool.Combat.Placements;

namespace MagicSchool.Combat.Heroes
{
    // Hero: finding who to hit (ITargeter). All of it is FindTarget's; the hero only hands it out.
    public partial class Hero
    {
        // ======================================== ITargeter ========================================
        public ICombatant FindCurrentTarget() => _findEnemy.FindCurrentTarget();
        public ICombatant FindNearestEnemy() => _findEnemy.FindNearestEnemy();
        public ICombatant FindNearestEnemyTo(ICombatant target) => _findEnemy.FindNearestEnemyTo(target);
        public ICombatant FindFurthestEnemy(int reachRange) => _findEnemy.FindFurthestEnemy(reachRange);
        public IReadOnlyList<ICombatant> FindFurthestEnemies(int reachRange, int count = 1) => _findEnemy.FindFurthestEnemies(reachRange, count);
        public IPlacement FindClusteredCircle(int reachRange, float blastRadius, bool isJump) => _findEnemy.FindClusteredCircle(reachRange, blastRadius, isJump);
        public IPlacement FindClusteredCharge(int reachRange, float chargeHalfWidth, bool maxRange = false) => _findEnemy.FindClusteredCharge(reachRange, chargeHalfWidth, maxRange);
        public IPlacement FindRandomFreeHex(int reachRange) => _findEnemy.FindRandomFreeHex(reachRange);
        public ICombatant FindClusteredLaser(int reachRange, float beamHalfWidth) => _findEnemy.FindClusteredLaser(reachRange, beamHalfWidth);
        public IReadOnlyList<ICombatant> FindEnemiesNear(ICombatant target, int reachRange) => _findEnemy.FindEnemiesNear(target, reachRange);
        public IReadOnlyList<ICombatant> FindAllEnemies() => _findEnemy.FindAllEnemies();
        public IReadOnlyList<ICombatant> FindAllAllies() => _findEnemy.FindAllAllies();
        public IReadOnlyList<ICombatant> FindRandomEnemies(int count) => _findEnemy.FindRandomEnemies(count);
        public ICombatant FindNearestAlly() => _findEnemy.FindNearestAlly();
        public ICombatant FindLowestHealthAlly() => _findEnemy.FindLowestHealthAlly();

        // ======================================== path lean ========================================
        // Which way I go when two routes are equally short and equally near the target: +1 over the top, -1 under the bottom.
        // Rolled when I am built and again at every fight start, so no side is favoured (See HexPathfinder)
        internal int PathLean { get; private set; } = 1;
        private void RollPathLean() => PathLean = UnityEngine.Random.value < 0.5f ? 1 : -1;

        // ======================================== chosen target ========================================
        // the player picks which of my tied nearest enemies I go for (See FindTarget.SwitchChosenTarget)
        public IReadOnlyList<ICombatant> TiedNearestEnemies() => _findEnemy.TiedNearestEnemies();
        public ICombatant ChosenTarget => _findEnemy.ShownChosenTarget();
        public bool SwitchChosenTarget() => _findEnemy.SwitchChosenTarget();

        // Who I will go for when the fight starts: my chosen target, or my only nearest enemy.
        // null while my nearest is a tie left automatic - that one is only settled in the fight.
        public ICombatant ExpectedTarget
        {
            get
            {
                ICombatant chosen = ChosenTarget;
                if (chosen != null) return chosen;

                IReadOnlyList<ICombatant> tied = TiedNearestEnemies();
                return tied.Count == 1 ? tied[0] : null;
            }
        }

        // The hexes I would walk through to get my expected target in range, from where I stand, if nobody else moved -
        // found a step at a time the way I walk in the fight (See Transition.CanWalk). Shown in Preparation, so the player
        // sees a detour round his own heroes is still on the way to the target (See AlignHighlight).
        // Empty while there is no expected target or it is already in range; it stops short when the way is shut.
        public IReadOnlyList<Hex> RouteToExpectedTarget() => RouteTo(ExpectedTarget);

        // Who I am going for in the fight right now (See FindTarget.TargetInFight)
        public ICombatant TargetInFight => _findEnemy.TargetInFight;

        // The hexes I would walk through to get [enemy] in range, the same way as RouteToExpectedTarget
        public IReadOnlyList<Hex> RouteTo(ICombatant enemy)
        {
            const int MaxSteps = 40;    // far more than any board is wide

            var route = new List<Hex>();
            Hex target = enemy?.ArrivingHex();
            Hex at = CurrentHex;
            if (target == null || at == null) return route;

            while (!at.IsWithinRange(target, Range) && route.Count < MaxSteps)
            {
                Hex next = HexPathfinder.FindValidHexToTarget(at, target, IsHexReservedByOther, PathLean);
                if (next == null || route.Contains(next)) break;

                route.Add(next);
                at = next;
            }

            return route;
        }
    }
}
