using System.Collections.Generic;
using MagicSchool.Contracts;
using static MagicSchool.Skills.SkillFactory;

namespace MagicSchool.Skills
{
    internal class DwarfSkill : SkillDefinition
    {
        private const float ExplodeDmg = 240f;

        // blast radius of the skill 
        private const int StartBlastHexes = 1;
        private const int MaxBlastHexes = 3;
        private int _blastHexes = StartBlastHexes;

        private ProjectileTuning _shotTuning;
        private AOETuning _blastTuning;

        public static SkillDefinition Build(TemplateActionRegistrySO registry) => new DwarfSkill(registry);

        private DwarfSkill(TemplateActionRegistrySO registry) : base(registry) { }

        public override string SkillName => "Blast Charge";

        public override string Description
            => "Fires a homing shot into the densest cluster of enemies. "
             + $"it bursts where it lands, dealing {ExplodeDmg}% AD to everyone inside the blast. "
             + $"The blast starts at {StartBlastHexes} hex and grows by 1 hex every cast, up to {MaxBlastHexes} hexes.";

        // get the blast diameter of the skill
        private static float BlastDiameter(int hexes) => hexes * 2f + 0.5f;

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

            if (_blastHexes < MaxBlastHexes) _blastHexes++;
            return true;
        }

        protected override List<SkillStep> Active(TemplateActionRegistrySO registry)
            => new List<SkillStep> { Shoot(registry), Explode(registry) };

        private SkillStep Shoot(TemplateActionRegistrySO registry)
        {
            _shotTuning = TuneProjectile();

            SkillActionGroup shoot = ActionGroup(
                registry: registry,
                source: ActionSourceEnum.Self,
                action: TemplateActionEnum.HomingProjectile,
                target: AimTargetEnum.ClusteredCircle,
                tuning: _shotTuning
            );

            return Step(trigger: TriggerEnum.OnCast, groups: shoot);
        }

        private SkillStep Explode(TemplateActionRegistrySO registry)
        {
            _blastTuning = TuneAOE();

            SkillActionGroup explode = ActionGroup(
                registry: registry,
                source: ActionSourceEnum.WhereProjectileHit,
                action: TemplateActionEnum.CircleAOE,
                target: AimTargetEnum.WhereProjectileHit,
                tuning: _blastTuning,

                Damage(
                    recipient: EffectRecipientEnum.EnemiesInArea,
                    ratios: (StatEnum.ATK, ExplodeDmg)
                )
            );

            return Step(trigger: TriggerEnum.OnHit, groups: explode);
        }
    }
}