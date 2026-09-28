using System;
using System.Collections.Generic;
using MagicSchool.Contracts;

namespace MagicSchool.Modifiers
{
    // 1) ModifierResolver are used to calculate final stat from modifier:
    // 1.1) stat modifier: they increase/decrease stat e.g. atk, def, as, mana, hp, etc...
    // 1.2) status modifier: they don't relate to stat e.g. stun, wound, untargetable, disarm, etc...
    // 2) ModifierResolver track which modifier will expired
    public class ModifierResolver
    {
        // all current active modifier on this hero 
        private readonly List<ActiveCustomModifier> _activeModifiers = new List<ActiveCustomModifier>();

        // the amount of bonus stat give by all modifier
        private readonly float[] _bonus = new float[StatSlots];

        // FLAGGING: hardcode slot number, this is adjust as the maximum number in StatEnum.cs
        private const int StatSlots = 10;

        // tracking if the bonus stat from modifier is stale 
        private bool _isBonusStale = true;

        // pair of modifier & stat - tell which stat is increase by this modifier
        private static readonly Dictionary<ModifierEnum, StatEnum> _lookup = new Dictionary<ModifierEnum, StatEnum>
        {
            // buff
            { ModifierEnum.BonusHP        , StatEnum.MaxHP }          ,
            { ModifierEnum.ATK            , StatEnum.ATK }            ,
            { ModifierEnum.AS             , StatEnum.AS}              ,
            { ModifierEnum.DF             , StatEnum.DF }             ,
            { ModifierEnum.DamageReduction, StatEnum.DamageReduction },
            { ModifierEnum.AP             , StatEnum.AP }             ,
            { ModifierEnum.Range          , StatEnum.Range }          ,
            { ModifierEnum.StartMana      , StatEnum.StartMana }      ,
            // ...

            // debuff
            { ModifierEnum.DefendShred    , StatEnum.DF }             ,
            // ...
        };

        // =================================== life cycle ===================================
        // update all current modifier duration, removing any that just expired
        public void Tick(float deltaTime)
        {
            // backwards, so removing one of the modifier does not skip over the next
            for (int i = _activeModifiers.Count - 1; i >= 0; i--)
            {
                // if permanent, skip timer
                if (float.IsPositiveInfinity(_activeModifiers[i].Remaining)) continue;

                // update timer
                _activeModifiers[i].Tick(deltaTime);

                // if the group expired, every modifier in it goes at once
                if (_activeModifiers[i].Remaining > 0f) continue;

                // the modifier expire, remove the modifier, and mark bonus as stale
                _activeModifiers.RemoveAt(i);
                _isBonusStale = true;
            }
        }

        // =================================== setter ===================================
        // add new modifier
        public void AddModifier(ICustomModifier modifier, float amplifier, IHeroStats casterStats, IHeroStats recipientStats)
        {
            // FLAGGING: we shouldn't have to loop all the modifiers, but RefernceEqual is still need, 
            // we can use dict to help this.
            // if the same modifier is added again, refresh the modifier
            for (int i = 0; i < _activeModifiers.Count; i++)
            {
                // is new added modifier the same to current active one?
                if (!ReferenceEquals(_activeModifiers[i].CustomModifier, modifier)) continue;

                // FLAGGING: Depend on skill, some skill can be stacked, while some skill refresh cleanly.
                // Now only refresh exist. We'll do this later when pattern is more clear.
                // refresh modifier
                _activeModifiers.RemoveAt(i);
                break;
            }

            // add modifier, and mark bonus as stale
            _activeModifiers.Add(new ActiveCustomModifier(modifier, amplifier, casterStats, recipientStats));
            _isBonusStale = true;
        }

        // force remove modifier before it expired
        public bool RemoveModifier(ICustomModifier modifier)
        {
            if (modifier == null) return false;

            // FLAGGING: we shouldn't have to loop all the modifiers.
            for (int i = 0; i < _activeModifiers.Count; i++)
            {
                if (!ReferenceEquals(_activeModifiers[i].CustomModifier, modifier)) continue;

                _activeModifiers.RemoveAt(i);
                _isBonusStale = true;
                return true;
            }

            return false;
        }

        // =================================== getter ===================================
        // Compute final stat after calculating modifier.
        // Consume base stat. Spit the final stat out.
        public float GetStatModifier(StatEnum type, float baseStat)
        {
            // if it was marked as stale, rebuild the bonus stat.
            // if not stale, return the cache one.
            if (_isBonusStale) RebuildBonus();

            // a stat's own enum value IS its slot: StatEnum.MaxHP is 0, so its bonus is _bonus[0].
            // StatEnum.None is -1 though, and would throw on the way in - it, and anything sitting
            // past the table, answer with the plain base stat: nothing has added to them.
            int slot = (int)type;
            if (slot < 0 || slot >= _bonus.Length) return baseStat;

            // StatModifier is always additive, no multiplicative
            return baseStat + _bonus[slot];
        }

        // Return available status modifier
        public bool GetStatusModifier(ModifierEnum type)
        {
            foreach (ActiveCustomModifier tracker in _activeModifiers)
                foreach (IModifier modifier in tracker.CustomModifier.GetModifiers())
                    if (modifier.GetModifierEnum() == type) return true;

            return false;
        }

        // return the count of active modifier 
        public int ActiveCount => _activeModifiers.Count;

        // get remaining duration of the active modifier
        public float GetRemainingDuration(int index)
        {
            if (index < 0 || index >= _activeModifiers.Count) return 0f;

            ActiveCustomModifier tracker = _activeModifiers[index];

            if (float.IsPositiveInfinity(tracker.Remaining)) return 0f;

            float duration = tracker.CustomModifier.GetDuration();
            if (duration <= 0f) return 0f;

            return tracker.Remaining / duration;
        }

        // =================================== private ===================================
        // Add every active modifier up, once, into the slot of the stat it feeds.
        private void RebuildBonus()
        {
            // clear bonus list
            Array.Clear(_bonus, 0, _bonus.Length);

            // build the bonus list using enum value, to identify which bonus this belong to.
            // e.g. according to StatEnum, _bonus[0] is the bonus for MaxHP
            foreach (ActiveCustomModifier active in _activeModifiers)
            {
                IReadOnlyList<IModifier> modifiers = active.CustomModifier.GetModifiers();

                for (int i = 0; i < modifiers.Count; i++)
                {
                    // lookup modifier table - what stat is increase?
                    if (!_lookup.TryGetValue(modifiers[i].GetModifierEnum(), out StatEnum target)) continue;

                    _bonus[(int)target] += active.BonusStat[i];
                }
            }

            // no longer stale
            _isBonusStale = false;
        }
    }
}