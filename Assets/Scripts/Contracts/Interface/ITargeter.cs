using System.Collections.Generic;

namespace MagicSchool.Contracts
{
    // ITargeter answer: Which enemy is the target base on specify condition.
    // e.g. skill want to aim at the Nearest/furthest enemy
    public interface ITargeter
    {
        ICombatant FindCurrentTarget();
        ICombatant FindNearestEnemy();
        ICombatant FindNearestEnemyTo(ICombatant target);
        ICombatant FindFurthestEnemy(int reachRange);
        IPlacement FindClusteredCircle(int reachRange, float blastRadius, bool isJump);
        ICombatant FindClusteredLaser(int reachRange, float beamHalfWidth);
        IPlacement FindClusteredCharge(int reachRange, float chargeHalfWidth, bool maxRange = false);
        IPlacement FindRandomFreeHex(int reachRange);
        IReadOnlyList<ICombatant> FindEnemiesNear(ICombatant target, int reachRange);
        IReadOnlyList<ICombatant> FindAllEnemies();
        IReadOnlyList<ICombatant> FindAllAllies();
        IReadOnlyList<ICombatant> FindRandomEnemies(int count);
        ICombatant FindNearestAlly();
    }
}
