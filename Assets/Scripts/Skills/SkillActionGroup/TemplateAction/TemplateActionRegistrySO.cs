using System;
using System.Collections.Generic;
using UnityEngine;

namespace MagicSchool.Skills
{
    /// <summary>
    /// The one place a TemplateAction prefab is referenced from.
    ///
    /// Skills are written in C#, which cannot hold a prefab reference, meaning it can't ref to TemplateAction.
    /// So this class pair enum & TemplateAction. So you could reference the TemplateAction via this class.
    /// </summary>
    [CreateAssetMenu(fileName = "TemplateActionRegistry", menuName = "Magic School 3/Template Action Registry")]
    public class TemplateActionRegistrySO : ScriptableObject
    {
        [Serializable]
        private struct Entry
        {
            public TemplateActionEnum Action;
            public TemplateAction Prefab;
        }

        [SerializeField] private List<Entry> _entries = new List<Entry>();

        // built on first use rather than in OnEnable - a ScriptableObject's OnEnable runs at a point
        // in load order we do not control, and a half-built lookup is worse than a late one
        private Dictionary<TemplateActionEnum, TemplateAction> _byAction;

        /// <summary>
        /// The prefab for this action, or null if it has no usable entry. Null is always reported -
        /// a skill silently doing nothing is the failure this whole registry exists to make loud.
        /// </summary>
        public TemplateAction Get(TemplateActionEnum action)
        {
            if (action == TemplateActionEnum.None)
            {
                Debug.LogError($"[{name}] asked for {nameof(TemplateActionEnum.None)} - an action group left unset?", this);
                return null;
            }

            Build();

            if (!_byAction.TryGetValue(action, out TemplateAction prefab))
            {
                Debug.LogError($"[{name}] has no entry for {action}. Add one to the registry asset.", this);
                return null;
            }

            if (prefab == null)
            {
                Debug.LogError($"[{name}] has an entry for {action} but its prefab is missing.", this);
                return null;
            }

            return prefab;
        }

        private void Build()
        {
            if (_byAction != null) return;

            _byAction = new Dictionary<TemplateActionEnum, TemplateAction>();

            foreach (Entry entry in _entries)
            {
                if (entry.Action == TemplateActionEnum.None) continue;

                // first one wins, and the duplicate is reported - silently keeping the last would
                // make the list order matter without anything saying so
                if (_byAction.ContainsKey(entry.Action))
                {
                    Debug.LogError($"[{name}] lists {entry.Action} more than once - ignoring the later one.", this);
                    continue;
                }

                _byAction.Add(entry.Action, entry.Prefab);
            }
        }

        // the list is edited by hand, so drop the cache whenever it might have changed
        private void OnValidate() => _byAction = null;
    }
}
