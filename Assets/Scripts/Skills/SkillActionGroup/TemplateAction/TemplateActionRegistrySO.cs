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
    // The lookup is built on first use, not in OnEnable: a ScriptableObject's OnEnable runs at a point in load order
    // we do not control, and a half-built lookup is worse than a late one. A missing action is always reported, since
    // a skill silently doing nothing is what this registry exists to make loud; a duplicate keeps the first and is
    // reported, so list order never matters unsaid. The cache is dropped on OnValidate because the list is edited by
    // hand.
    [CreateAssetMenu(fileName = "TemplateActionRegistry", menuName = "Magic School 4/Template Action Registry")]
    public class TemplateActionRegistrySO : ScriptableObject
    {
        [Serializable]
        private struct Entry
        {
            public TemplateActionEnum Action;
            public TemplateAction Prefab;
        }

        [SerializeField] private List<Entry> _entries = new List<Entry>();

        private Dictionary<TemplateActionEnum, TemplateAction> _byAction;

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

                if (_byAction.ContainsKey(entry.Action))
                {
                    Debug.LogError($"[{name}] lists {entry.Action} more than once - ignoring the later one.", this);
                    continue;
                }

                _byAction.Add(entry.Action, entry.Prefab);
            }
        }

        private void OnValidate() => _byAction = null;
    }
}
