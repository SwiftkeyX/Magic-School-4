using System.Collections.Generic;
using MagicSchool.Contracts;

namespace MagicSchool.CombatRecording
{
    // CombatRecorder tell damage dealt, damage taken, heal, etc... for each hero.
    // This data will be shown in a UI panel.
    public class CombatRecorder
    {
        private readonly Dictionary<IEffectable, CombatRecord> _round = new Dictionary<IEffectable, CombatRecord>();
        private RoundClock _clock = new RoundClock();       

        // ======================================== public ========================================
        // get combat record from the specify unit
        public CombatRecord RoundOf(IEffectable unit) => RecordFor(unit);

        public void BeginRound()
        {
            _round.Clear();
            _clock = new RoundClock();
        }

        public void EndRound() => _clock.Stop();

        // ======================================== listener ========================================
        // Subscribed to a hero's OnDamaged/OnHealed
        public void Record(DamageEvent e)
        {
            if (e.Source != null) RecordFor(e.Source).AddDealt(e.Kind, e.Outcome.Landed, e.Outcome.Overkill);
            if (e.Target != null)
            {
                CombatRecord target = RecordFor(e.Target);
                target.AddTaken(e.Outcome.Landed, e.Outcome.Mitigated);
                if (e.Outcome.NewHP <= 0) target.MarkDied();
            }
        }
        public void Record(HealEvent e)
        {
            if (e.Source != null) RecordFor(e.Source).AddHealingDone(e.Outcome.Healed, e.Outcome.Overhealed);
            if (e.Target != null) RecordFor(e.Target).AddHealingReceived(e.Outcome.Healed, e.Outcome.LostToWound);
        }

        // ======================================== helper ========================================
        // context: combat record keep data for damage dealt, damage taken, heal
        // to read/write the combat record
        private CombatRecord RecordFor(IEffectable unit)
        {
            if (unit == null) return new CombatRecord(_clock);

            if (!_round.TryGetValue(unit, out CombatRecord record))
            {
                record = new CombatRecord(_clock);
                _round[unit] = record;
            }

            return record;
        }
    }
}
