using System.Collections;
using UnityEngine;
using MagicSchool.Contracts;

namespace MagicSchool.Skills
{
    /// <summary>
    /// Companion follow its owner, and on every interval it plays its act.
    /// e.g. Priest's fairy.
    ///
    /// FLAGGING: the companion still doesn't stable. it would be more stable if we have more type of companion.
    /// </summary>
    internal class Companion : MonoBehaviour
    {
        private static readonly Vector3 Offset = new Vector3(0.35f, 0.45f, 0f);

        private ICombatant _owner;
        private SkillPart _act;

        // called by Summon, once, right after it spawned this companion
        public void Begin(ICombatant owner, SkillPart act, float duration, float interval)
        {
            _owner = owner;
            _act = act;

            transform.position = _owner.transform.position + Offset;

            StartCoroutine(ActEvery(interval, duration));
        }

        // ======================================= private ========================================
        private void Update()
        {
            // companion dies with its owner
            if (!IsOwnerAlive()) { Destroy(gameObject); return; }

            // companion follow owner
            transform.position = _owner.transform.position + Offset;
        }

        private bool IsOwnerAlive()
        {
            if (_owner is Object hero && hero == null) return false;

            return _owner != null && _owner.IsAlive;
        }

        // companion plays its act every interval
        private IEnumerator ActEvery(float interval, float duration)
        {
            WaitForSeconds wait = new WaitForSeconds(interval);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                yield return wait;
                elapsed += interval;

                // if owner die, break
                if (!IsOwnerAlive()) break;

                // played as the owner, read the summary of this class
                TemplateAction.TryPlay(
                    _act,
                    _owner,
                    new TemplateActionCallbacks { OnSkillHit = _act.OnSkillHit }
                );
            }

            // it leaves after its last act
            Destroy(gameObject);
        }
    }
}
