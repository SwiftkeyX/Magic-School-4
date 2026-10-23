using System.Collections.Generic;
using MagicSchool.Contracts;
using static MagicSchool.Skills.SkillFactory;

namespace MagicSchool.Skills
{
    internal class DwarfSkill : SkillDefinition
    {
        // the blast's damage by its size, % AD
        private static float SmallBlastDmg => Tuned(250f);     // 1 hex
        private static float MediumBlastDmg => Tuned(250f);    // 2 hexes
        private static float BigBlastDmg => Tuned(500f);       // 3 hexes

        // blast radius of the skill: 1 -> 2 -> 3 -> 1 -> ... one step every cast
        private static int StartBlastHexes => Tuned(1);
        private static int MaxBlastHexes => Tuned(3);
        private int _blastHexes = StartBlastHexes;
        private int _firedHexes = StartBlastHexes;     // the size of the shot in flight - the blast lands after the next size is set

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

        // get the blast diameter of the skill
        private static float BlastDiameter(int hexes) => hexes * 2f + 0.5f;

        // % AD by the size it was fired at; past 3 counts as the biggest
        private static float DamageFor(int hexes) => hexes <= 1 ? SmallBlastDmg : hexes == 2 ? MediumBlastDmg : BigBlastDmg;

        // the damage effect is built at the small blast's number; this scales it to the size in flight
        private float BlastAmplifier(IEffectable _) => SmallBlastDmg > 0f ? DamageFor(_firedHexes) / SmallBlastDmg - 1f : 0f;

        // ============================== passive ==============================
        // at combat start, the blast is small again
        public override bool OnCombatStart()
        {
            _blastHexes = StartBlastHexes;

            return base.OnCombatStart();
        }

        // ============================== active ==============================
        // on cast, fire with the blast at its current size, 
        // then grow it for the next one
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