using System;
using System.Collections.Generic;
using System.Linq;
using MagicSchool.Contracts;

namespace MagicSchool.Modifiers
{
    // 1) ModifierResolver are used to calculate final stat from modifier:
    // 1.1) stat modifier: they increase/decrease stat e.g. atk, def, as, mana, hp, etc...
    // 1.2) status modifier: they don't relate to stat e.g. stun, wound, untargetable, disarm, etc...
    // 2) ModifierResolver track which modifier will expired
    // StatModifiers are always additive, never multiplied. StatSlots is worked out from StatEnum: a hand-kept count
    // once fell one short and Omnivamp modifiers silently did nothing.
    public class ModifierResolver
    {
        private readonly List<ActiveCustomModifier> _activeModifiers = new List<ActiveCustomModifier>();

        private readonly float[] _bonus = new float[StatSlots];

        internal static readonly int StatSlots = Enum.GetValues(typeof(StatEnum)).Cast<int>().Max() + 1;

        private bool _isBonusStale = true;

        public void Tick(float deltaTime, Action<ICustomModifier> expired = null)
        {
            for (int i = _activeModifiers.Count - 1; i >= 0; i--)
            {
                if (float.IsPositiveInfinity(_activeModifiers[i].Remaining)) continue;

                _activeModifiers[i].Tick(deltaTime);

                if (_activeModifiers[i].Remaining > 0f) continue;

                ICustomModifier gone = _activeModifiers[i].CustomModifier;
                _activeModifiers.RemoveAt(i);
                _isBonusStale = true;
                expired?.Invoke(gone);
            }
        }

        public void AddModifier(ICustomModifier modifier, float amplifier, IHeroStats casterStats, IHeroStats recipientStats)
        {
            if (!modifier.IsStack())
            {
                for (int i = 0; i < _activeModifiers.Count; i++)
                {
                    if (!ReferenceEquals(_activeModifiers[i].CustomModifier, modifier)) continue;

                    _activeModifiers.RemoveAt(i);
                    break;
                }
            }

            _activeModifiers.Add(new ActiveCustomModifier(modifier, amplifier, casterStats, recipientStats));
            _isBonusStale = true;
        }

        public bool RemoveModifier(ICustomModifier modifier)
        {
            if (modifier == null) return false;

            for (int i = 0; i < _activeModifiers.Count; i++)
            {
                if (!ReferenceEquals(_activeModifiers[i].CustomModifier, modifier)) continue;

                _activeModifiers.RemoveAt(i);
                _isBonusStale = true;
                return true;
            }

            return false;
        }

        public float GetStatModifier(StatEnum type, float baseStat)
        {
            if (_isBonusStale) RebuildBonus();

            int slot = (int)type;
            if (slot < 0 || slot >= _bonus.Length) return baseStat;

            return baseStat + _bonus[slot];
        }

        public bool GetStatusModifier(ModifierEnum type)
        {
            foreach (ActiveCustomModifier tracker in _activeModifiers)
                foreach (IModifier modifier in tracker.CustomModifier.GetModifiers())
                    if (modifier.GetModifierEnum() == type) return true;

            return false;
        }

        public int ActiveCount => _activeModifiers.Count;

        public float GetRemainingDuration(int index)
        {
            if (index < 0 || index >= _activeModifiers.Count) return 0f;

            ActiveCustomModifier tracker = _activeModifiers[index];

            if (float.IsPositiveInfinity(tracker.Remaining)) return 0f;

            float duration = tracker.CustomModifier.GetDuration();
            if (duration <= 0f) return 0f;

            return tracker.Remaining / duration;
        }

        public float ShieldTotal => Shield.Total(_activeModifiers);
        public float AbsorbDamage(float damage) => Shield.Absorb(_activeModifiers, damage);

        private void RebuildBonus()
        {
            Array.Clear(_bonus, 0, _bonus.Length);

            for (int slot = 0; slot < _bonus.Length; slot++)
                foreach (ActiveCustomModifier active in _activeModifiers)
                    _bonus[slot] += active.GetStat((StatEnum)slot);

            _isBonusStale = false;
        }
    }
}