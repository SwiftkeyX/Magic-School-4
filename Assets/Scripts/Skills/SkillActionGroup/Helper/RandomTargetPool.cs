using System.Collections.Generic;
using MagicSchool.Contracts;
using UnityEngine;

namespace MagicSchool.Skills
{

    /// Now was use in 1 situation:
    /// 1) Attack random enemies in 2 hex of current target e.g. Imp spraying the units around his current target.
    /// 2) (There is other case but not implement yet) ...
    /// 
    /// README:
    /// Pseudo-random rather than random: everyone in the bag is dealt once before anyone is dealt twice
    // A shuffled bag: every enemy near the target is dealt once before any repeats, then the bag is shuffled again.
    internal class RandomTargetPool
    {
        private readonly ITargeter _owner;
        private readonly int _radius;

        private List<ICombatant> _pool;
        private int _index;

        public RandomTargetPool(ITargeter owner, int radius)
        {
            _owner = owner;
            _radius = radius;
        }

        public ICombatant Next()
        {
            if (_pool == null) Fill();

            if (_pool.Count == 0) return null;

            if (_index >= _pool.Count)
            {
                Shuffle(_pool);
                _index = 0;
            }

            return _pool[_index++];
        }

        private void Fill()
        {
            ICombatant currentTarget = _owner.FindCurrentTarget();

            _pool = new List<ICombatant>(_owner.FindEnemiesNear(currentTarget, _radius));
            _index = _pool.Count;
        }

        private static void Shuffle(List<ICombatant> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
