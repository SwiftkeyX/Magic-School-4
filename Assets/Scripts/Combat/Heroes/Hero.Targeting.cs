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
    }
}
