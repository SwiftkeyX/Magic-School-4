using System.Collections.Generic;
using MagicSchool.Contracts;
using MagicSchool.StatScaling;
using static MagicSchool.Skills.SkillFactory;

namespace MagicSchool.Skills
{
    internal class ReaperSkill : SkillDefinition
    {
        private static float MGDamagePerSnip => Tuned(100f);   // sheet: 100/150/400% AP
        private static int SnipCount => Tuned(3);
        private static float IntervalBetweenSnip => Tuned(0.2f);
        private static float TotalCastTime => IntervalBetweenSnip * (SnipCount - 1);

        // passive - every [this] kills of hers, the allies on her Reap mark cast their skill once for free
        private static int KillsPerFreeCast => Tuned(3);
        private int _kills;

        // passive - once a fight, she gets back up [ReviveDelay] seconds after she dies, with [ReviveHPPercent]% of her max HP
        private static float ReviveDelay => Tuned(3f);
        private static float ReviveHPPercent => Tuned(30f);
        private bool _hasRevived;

        private const string ReapMark = "Reap";
        public override IReadOnlyList<string> SpecialMarks => new[] { ReapMark };

        public static SkillDefinition Build(TemplateActionRegistrySO registry) => new ReaperSkill(registry);

        private ReaperSkill(TemplateActionRegistrySO registry) : base(registry) { }

        public override string SkillName => "Harvest";

        public override string Description
            => $"Snips {SnipCount} times in quick succession, each cut carving a wedge in front of her "
             + $"that deals {AP(MGDamagePerSnip)} to every enemy standing inside it.";

        public override string PassiveDescription
            => $"Every {KillsPerFreeCast} enemies she kills, each ally on her {ReapMark} mark casts its skill at once, "
             + "without using mana. Summoned units do not count. "
             + $"Once a fight, when she dies she gets back up {ReviveDelay} seconds later with {ReviveHPPercent}% of her max HP.";

        // ============================== passive ==============================
        public override bool HasAutoAttackPassive => true;   // so the inspector shows the passive

        public override bool OnCombatStart()
        {
            _kills = 0;
            _hasRevived = false;

            return base.OnCombatStart();
        }

        // an enemy she killed herself - every KillsPerFreeCast of them, the allies on her Reap mark cast for free
        public override void OnHeroDied(ICombatant dead)
        {
            if (dead.Team == Caster.Team) return;
            if (!(dead is IFallen fallen) || fallen.IsSummon) return;
            if (!ReferenceEquals(fallen.KilledBy, Caster)) return;

            _kills++;
            if (_kills % System.Math.Max(1, KillsPerFreeCast) != 0) return;

            if (!(Caster is IAligned aligned)) return;

            foreach (ICombatant ally in aligned.AlliesOnSpecialMark(ReapMark))
            {
                if (ally.IsAlive && ally is IFreeCaster caster) caster.GrantFreeCast();
            }
        }

        // she died - the first time this fight, she gets back up
        public override void OnDeath()
        {
            if (_hasRevived || !(Caster is IRevivable revivable)) return;

            _hasRevived = true;
            revivable.ReviveAfter(ReviveDelay, ReviveHPPercent / 100f);
        }

        // ============================== active ==============================
        protected override SkillFlow BuildActiveFlow(TemplateActionRegistrySO registry)
        {
            return Snip(registry);
        }

        private static SkillFlow Snip(TemplateActionRegistrySO registry)
        {
            AOETuning tune = TuneAOE(offset: AOEOffsetEnum.Tip);

            SkillPart snip = Part(
                registry: registry,
                source: ActionSourceEnum.Self,
                action: TemplateActionEnum.FireTimingRunnerTriangleAOE,
                target: AimTargetEnum.Current,
                tuning: TuneFireTimingRunner(
                    SnipCount, FireTimingModeEnum.Sequence,
                    IntervalBetweenSnip,
                    tune,
                    castTime: TotalCastTime),

                Damage(
                    recipient: EffectRecipientEnum.EnemiesInArea,
                    (StatEnum.AP, MGDamagePerSnip)
                )
            );

            return Flow(onStart: snip);
        }
    }
}
