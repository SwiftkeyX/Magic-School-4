using System.Collections.Generic;
using UnityEngine;
using MagicSchool.Contracts;
using static MagicSchool.Skills.SkillFactory;

namespace MagicSchool.Skills
{
    internal class GoblinArcherSkill : SkillDefinition
    {
        private const float VenomDuration = 5f;
        private const float PoisonTotal = 240f;
        private const float PoisonDuration = 2f;
        private const float PoisonInterval = 0.5f;

        private SkillPart _poison;
        private float _venomEndsAt;

        // the enemies he hasn't shot yet. He want to shot everyone before hitting the same target again.
        private readonly List<ICombatant> _notShotYet = new List<ICombatant>();

        public static SkillDefinition Build(TemplateActionRegistrySO registry) => new GoblinArcherSkill(registry);

        private GoblinArcherSkill(TemplateActionRegistrySO registry) : base(registry) { }

        public override string SkillName => "Venom Arrows";

        public override string Description
            => $"For {VenomDuration} seconds, every auto attack also poisons its target for "
             + $"{PoisonTotal}% AD over {PoisonDuration} seconds. The poison stacks.";

        public override string PassiveDescription
            => "Infinite range. Every auto attack picks a new random enemy, "
             + "with no repeats until every enemy has been hit.";

        // ============================== init ==============================
        public override void Init(ICombatant caster)
        {
            base.Init(caster);

            // the poison is played on, not inside the flow, so it is initialized here
            _poison.Init(caster);
        }

        // ============================== passive ==============================
        public override bool HasAttackPassive => true;

        // at combat start, nobody has been shot and his arrows are clean
        public override bool OnCombatStart()
        {
            _notShotYet.Clear();
            _venomEndsAt = 0f;

            return base.OnCombatStart();
        }

        // every arrow goes to a random enemy he has not shot yet
        public override ICombatant OnPickAttackTarget(ICombatant target)
        {
            _notShotYet.RemoveAll(enemy => !enemy.IsAlive);

            // everyone has been shot once - start over with whoever is still standing
            if (_notShotYet.Count == 0) _notShotYet.AddRange(Caster.FindAllEnemies());

            if (_notShotYet.Count == 0) return target;

            int pick = Random.Range(0, _notShotYet.Count);
            ICombatant picked = _notShotYet[pick];
            _notShotYet.RemoveAt(pick);

            return picked;
        }

        // while the venom lasts, the arrow poisons whoever it landed on
        public override bool OnAttack(ICombatant target)
        {
            if (Time.time >= _venomEndsAt) return false;

            return PlayOnePart(_poison, assignedTarget: target);
        }

        // ============================== active ==============================
        public override bool OnCast()
        {
            if (!base.OnCast()) return false;

            _venomEndsAt = Time.time + VenomDuration;

            return true;
        }

        protected override SkillFlow BuildActiveFlow(TemplateActionRegistrySO registry)
        {
            // the cast itself only plays the animation
            SkillPart cast = Part(
                registry: registry,
                source: ActionSourceEnum.Self,
                action: TemplateActionEnum.Cast,
                target: AimTargetEnum.Self);

            // the poison here do the actual work
            _poison = Part(
                registry: registry,
                source: ActionSourceEnum.Self,
                action: TemplateActionEnum.Cast,
                target: AimTargetEnum.Assigned,

                DamageOverTime(
                    recipient: EffectRecipientEnum.SameToAimTarget,
                    interval: PoisonInterval,
                    duration: PoisonDuration,
                    ratios: (StatEnum.ATK, PoisonTotal))
            );

            return Flow(onStart: cast);
        }
    }
}
