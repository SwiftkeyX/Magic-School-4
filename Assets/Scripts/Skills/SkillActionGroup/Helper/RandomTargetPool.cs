using System.Collections.Generic;
using MagicSchool.Contracts;
using UnityEngine;

namespace MagicSchool.Skills
{

    /// Now was use in 1 situation:
    /// 1) Attack random enemies in 2 hex of current target e.g. Sparks spraying the units around her current target.
    /// 2) (There is other case but not implement yet) ...
    /// 
    /// README:
    /// Pseudo-random rather than random: everyone in the bag is dealt once before anyone is dealt
    /// twice, so a 4 shot volley over 4 enemies always covers all 4 - which plain Random.Range does
    /// not. The bag re-shuffles each time it empties, so the order still differs every pass.
    internal class RandomTargetPool
    {
        private readonly ITargeter _owner;
        private readonly int _radius;    // how many hexes from the current target the bag reaches

        private List<ICombatant> _pool;
        private int _index;

        public RandomTargetPool(ITargeter owner, int radius)
        {
            _owner = owner;
            _radius = radius;
        }

        // the next unit to shoot, or null when there is nobody to shoot at
        public ICombatant Next()
        {
            if (_pool == null) Fill();

            if (_pool.Count == 0) return null;

            // the bag is empty - refill it by re-shuffling what it already holds
            if (_index >= _pool.Count)
            {
                Shuffle(_pool);
                _index = 0;
            }

            return _pool[_index++];
        }

        // filled the random pool with:
        // 1) enemy who is near the current target
        // FLAGGING: this is hardcode for Sparks, let leave it for now
        private void Fill()
        {
            ICombatant currentTarget = _owner.FindCurrentTarget();

            _pool = new List<ICombatant>(_owner.FindEnemiesNear(currentTarget, _radius));
            _index = _pool.Count;   // starts empty, so the first Next() shuffles before it deals
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
