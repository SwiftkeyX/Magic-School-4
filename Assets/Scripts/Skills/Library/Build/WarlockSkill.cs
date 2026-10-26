using System.Collections.Generic;
using UnityEngine;
using MagicSchool.Contracts;
using static MagicSchool.Skills.SkillFactory;

namespace MagicSchool.Skills
{
    // CurseTotal is over the whole curse, not per tick. The bolt only carries the curse; it is played on whoever the
    // bolt hit, and when the cursed one dies with it still on, it jumps to the enemy closest to where they fell.
    // Nobody is cursed at combat start. The curse is played outside the flow, so it is initialized in Init.
    internal class WarlockSkill : SkillDefinition
    {
        private static float CurseTotal => Tuned(450f);
        private static float CurseDuration => Tuned(4f);
        private static float CurseInterval => Tuned(1f);

        private SkillPart _curse;

        private ICombatant _cursed;
        private float _curseEndsAt;

        public static SkillDefinition Build(TemplateActionRegistrySO registry) => new WarlockSkill(registry);

        private WarlockSkill(TemplateActionRegistrySO registry) : base(registry) { }

        public override string SkillName => "Spreading Hex";

        public override string Description
            => $"Curses the current target with a homing bolt: {AP(CurseTotal)} over {CurseDuration} seconds. "
             + "If the target dies while cursed, the curse jumps to the next closest enemy.";

        public override void Init(ISkillCaster caster)
        {
            base.Init(caster);

            _curse.Init(caster);
        }

        public override bool OnCombatStart()
        {
            _cursed = null;

            return base.OnCombatStart();
        }

        public override void OnHeroDied(ICombatant dead)
        {
            if (!ReferenceEquals(dead, _cursed) || Time.time >= _curseEndsAt) return;

            _cursed = null;

            ICombatant next = Caster.FindNearestEnemyTo(dead);
            if (next != null) Curse(next);
        }

        protected override SkillFlow BuildActiveFlow(TemplateActionRegistrySO registry)
        {
            SkillPart bolt = Part(
                registry: registry,
                source: ActionSourceEnum.Self,
                action: TemplateActionEnum.HomingProjectile,
                target: AimTargetEnum.Current);

            bolt.OnSkillHit = Curse;

            _curse = Part(
                registry: registry,
                source: ActionSourceEnum.Self,
                action: TemplateActionEnum.Cast,
                target: AimTargetEnum.Assigned,

                DamageOverTime(
                    recipient: EffectRecipientEnum.SameToAimTarget,
                    interval: CurseInterval,
                    duration: CurseDuration,
                    ratios: (StatEnum.AP, CurseTotal))
            );

            return Flow(onStart: bolt);
        }

        private void Curse(ICombatant enemy)
        {
            if (!PlayOnePart(_curse, assignedTarget: enemy)) return;

            _cursed = enemy;
            _curseEndsAt = Time.time + CurseDuration;
        }
    }
}
