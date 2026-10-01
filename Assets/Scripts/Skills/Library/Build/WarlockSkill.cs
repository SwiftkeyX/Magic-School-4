using System.Collections.Generic;
using UnityEngine;
using MagicSchool.Contracts;
using static MagicSchool.Skills.SkillFactory;

namespace MagicSchool.Skills
{
    internal class WarlockSkill : SkillDefinition
    {
        private const float CurseTotal = 300f;      // over the whole curse, not per tick
        private const float CurseDuration = 4f;
        private const float CurseInterval = 1f;

        // the curse itself - played on whoever the bolt hit, and again on whoever it jumps to
        private SkillPart _curse;

        // who carries the curse right now, and until when
        private ICombatant _cursed;
        private float _curseEndsAt;

        public static SkillDefinition Build(TemplateActionRegistrySO registry) => new WarlockSkill(registry);

        private WarlockSkill(TemplateActionRegistrySO registry) : base(registry) { }

        public override string SkillName => "Spreading Hex";

        public override string Description
            => $"Curses the current target with a homing bolt: {CurseTotal}% AP over {CurseDuration} seconds. "
             + "If the target dies while cursed, the curse jumps to the next closest enemy.";

        // ============================== init ==============================
        // the curse is played on its own rather than as a step, so it is initialized here
        public override void Init(ICombatant caster)
        {
            base.Init(caster);

            _curse.Init(caster);
        }

        // ============================== passive ==============================
        // at combat start, nobody is cursed
        public override bool OnCombatStart()
        {
            _cursed = null;

            return base.OnCombatStart();
        }

        // the cursed one died with the curse still on - it jumps to the enemy closest to where they fell
        public override void OnHeroDied(ICombatant dead)
        {
            // if a dead heroes not carry curse, return 
            if (!ReferenceEquals(dead, _cursed) || Time.time >= _curseEndsAt) return;

            // reset
            _cursed = null;

            // cursed the closest enemy
            ICombatant next = Caster.FindNearestEnemyTo(dead);
            if (next != null) Curse(next);
        }

        // ============================== active ==============================
        protected override List<SkillFlow> Active(TemplateActionRegistrySO registry)
        {
            // the bolt does nothing itself - it only carries the curse to its target
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

            return new List<SkillFlow> { Flow(trigger: TriggerEnum.OnCast, groups: bolt) };
        }

        // ============================== helper ==============================
        // put a curse on this enemy   
        private void Curse(ICombatant enemy)
        {
            if (!PlayGroup(_curse, assignedTarget: enemy)) return;

            _cursed = enemy;
            _curseEndsAt = Time.time + CurseDuration;
        }
    }
}
