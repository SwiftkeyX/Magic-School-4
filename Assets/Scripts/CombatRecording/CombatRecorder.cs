using System.Collections.Generic;
using MagicSchool.Contracts;

namespace MagicSchool.CombatRecording
{
    // CombatRecorder tell damage dealt, damage taken, heal, etc... for each hero.
    // This data will be shown in a UI panel.
    // BeginRoundFor starts a round for some units alone, on a clock of their own, leaving everyone else's record
    // alone: TeamCompBenchmark's lanes start and end their fights at different times side by side.
    public class CombatRecorder
    {
        private readonly Dictionary<IEffectable, CombatRecord> _round = new Dictionary<IEffectable, CombatRecord>();
        private RoundClock _clock = new RoundClock();       

        public CombatRecord RoundOf(IEffectable unit) => RecordFor(unit);

        public void BeginRound()
        {
            _round.Clear();
            _clock = new RoundClock();
        }

        public void EndRound() => _clock.Stop();

        public void BeginRoundFor(IEnumerable<IEffectable> units)
        {
            var clock = new RoundClock();
            foreach (IEffectable unit in units)
            {
                if (unit != null) _round[unit] = new CombatRecord(clock);
            }
        }

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
