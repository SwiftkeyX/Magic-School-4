using System.Collections.Generic;
using MagicSchool.Contracts;
using MagicSchool.Combat.Placements;

namespace MagicSchool.Combat.Heroes
{
    // Hero: finding who to hit (ITargeter). All of it is FindTarget's; the hero only hands it out.
    // PathLean: which way I go when two routes are equally short and equally near the target, +1 over the top, -1
    // under; rolled when built and at every fight start, so no side is favoured. ExpectedTarget is null while my
    // nearest is a tie left automatic - that is settled only in the fight. RouteToExpectedTarget is the walk's own
    // one-step search repeated, as if nobody else moved: empty when already in range, stopping short when the way is
    // shut.
    public partial class Hero
    {
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
        internal void ForgetScanCaches() => _findEnemy?.ForgetScanCaches();

        internal int PathLean { get; private set; } = 1;
        private void RollPathLean() => PathLean = UnityEngine.Random.value < 0.5f ? 1 : -1;

        public IReadOnlyList<ICombatant> TiedNearestEnemies() => _findEnemy.TiedNearestEnemies();
        public ICombatant ChosenTarget => _findEnemy.ShownChosenTarget();
        public bool SwitchChosenTarget() => _findEnemy.SwitchChosenTarget();

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

        public IReadOnlyList<Hex> RouteToExpectedTarget() => RouteTo(ExpectedTarget);

        public ICombatant TargetInFight => _findEnemy.TargetInFight;

        public IReadOnlyList<Hex> RouteTo(ICombatant enemy)
        {
            const int MaxSteps = 40;

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
