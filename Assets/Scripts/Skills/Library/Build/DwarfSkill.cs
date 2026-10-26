using System.Collections.Generic;
using MagicSchool.Contracts;
using static MagicSchool.Skills.SkillFactory;

namespace MagicSchool.Skills
{
    // The blast grows 1 -> 2 -> 3 hexes and back to 1, one step each cast, and resets at combat start. Damage is by
    // the size it was fired at (past 3 counts as the biggest): the effect is built at the small blast's number and
    // scaled to the size in flight, since the next size is set before the shot lands.
    internal class DwarfSkill : SkillDefinition
    {
        private static float SmallBlastDmg => Tuned(250f);
        private static float MediumBlastDmg => Tuned(250f);
        private static float BigBlastDmg => Tuned(500f);

        private static int StartBlastHexes => Tuned(1);
        private static int MaxBlastHexes => Tuned(3);
        private int _blastHexes = StartBlastHexes;
        private int _firedHexes = StartBlastHexes;

        private ProjectileTuning _shotTuning;
        private AOETuning _blastTuning;

        public static SkillDefinition Build(TemplateActionRegistrySO registry) => new DwarfSkill(registry);

        private DwarfSkill(TemplateActionRegistrySO registry) : base(registry) { }

        public override string SkillName => "Blast Charge";

        public override string Description
            => "Fires a homing shot into the densest cluster of enemies. "
             + $"It bursts where it lands, damaging everyone inside the blast. "
             + $"The blast starts at {StartBlastHexes} hex and grows by 1 hex every cast up to {MaxBlastHexes}, then starts small again. "
             + $"It deals {AD(SmallBlastDmg)} at 1 hex, {AD(MediumBlastDmg)} at 2 and {AD(BigBlastDmg)} at 3.";

        private static float BlastDiameter(int hexes) => hexes * 2f + 0.5f;

        private static float DamageFor(int hexes) => hexes <= 1 ? SmallBlastDmg : hexes == 2 ? MediumBlastDmg : BigBlastDmg;

        private float BlastAmplifier(IEffectable _) => SmallBlastDmg > 0f ? DamageFor(_firedHexes) / SmallBlastDmg - 1f : 0f;

        public override bool OnCombatStart()
        {
            _blastHexes = StartBlastHexes;

            return base.OnCombatStart();
        }

        public override bool OnCast()
        {
            float diameter = BlastDiameter(_blastHexes);
            _shotTuning.Spread = diameter / 2f;
            _blastTuning.Length = diameter;
            _blastTuning.Width = diameter;

            if (!base.OnCast()) return false;

            _firedHexes = _blastHexes;
            _blastHexes = _blastHexes >= MaxBlastHexes ? StartBlastHexes : _blastHexes + 1;
            return true;
        }

        protected override SkillFlow BuildActiveFlow(TemplateActionRegistrySO registry)
            => Flow(
                    onStart: Shoot(registry),
                    onHit: Explode(registry)
                );

        private SkillPart Shoot(TemplateActionRegistrySO registry)
        {
            _shotTuning = TuneProjectile();

            SkillPart shoot = Part(
                registry: registry,
                source: ActionSourceEnum.Self,
                action: TemplateActionEnum.HomingProjectile,
                target: AimTargetEnum.ClusteredCircle,
                tuning: _shotTuning
            );

            return shoot;
        }

        private SkillPart Explode(TemplateActionRegistrySO registry)
        {
            _blastTuning = TuneAOE();

            SkillPart explode = Part(
                registry: registry,
                source: ActionSourceEnum.WhereProjectileHit,
                action: TemplateActionEnum.CircleAOE,
                target: AimTargetEnum.WhereProjectileHit,
                tuning: _blastTuning,

                Damage(
                    recipient: EffectRecipientEnum.EnemiesInArea,
                    ratios: (StatEnum.ATK, SmallBlastDmg)
                ).AmplifiedBy(BlastAmplifier)
            );

            return explode;
        }
    }
}