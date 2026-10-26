using MagicSchool.Contracts;

namespace MagicSchool.CombatRecording
{
    public class CombatRecord : ICombatRecord
    {
        private readonly RoundClock _clock;
        private float? _diedAt;                            

        internal CombatRecord(RoundClock clock)
        {
            _clock = clock;
        }

        public float FightSeconds => _clock != null ? _clock.Elapsed : 0f;
        public float SecondsAlive => _diedAt ?? FightSeconds;

        public int DamageDealt { get; private set; }
        public int AutoAttackDamage { get; private set; }
        public int SkillDamage { get; private set; }
        public int Overkill { get; private set; }
        public int HealingDone { get; private set; }
        public int Overhealing { get; private set; }

        public int DamageTaken { get; private set; }
        public int DamageMitigated { get; private set; }
        public int HealingReceived { get; private set; }
        public int HealingLostToWound { get; private set; }

        internal void AddDealt(DamageKindEnum kind, int landed, int overkill)
        {
            DamageDealt += landed;
            Overkill += overkill;

            if (kind == DamageKindEnum.AutoAttack) AutoAttackDamage += landed;
            else if (kind == DamageKindEnum.Skill || kind == DamageKindEnum.Burn) SkillDamage += landed;
        }

        internal void AddTaken(int landed, int mitigated)
        {
            DamageTaken += landed;
            DamageMitigated += mitigated;
        }

        internal void MarkDied()
        {
            if (_diedAt == null) _diedAt = FightSeconds;
        }

        internal void AddHealingDone(int healed, int overhealed)
        {
            HealingDone += healed;
            Overhealing += overhealed;
        }

        internal void AddHealingReceived(int healed, int lostToWound)
        {
            HealingReceived += healed;
            HealingLostToWound += lostToWound;
        }
    }
}
