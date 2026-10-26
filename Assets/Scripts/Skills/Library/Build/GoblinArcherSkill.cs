using System.Collections.Generic;
using UnityEngine;
using MagicSchool.Contracts;
using static MagicSchool.Skills.SkillFactory;

namespace MagicSchool.Skills
{
    // Every arrow goes to a random enemy he has not shot yet; once all have been, he starts over with whoever is
    // standing (reset at combat start). The cast only plays the animation; while the venom lasts the arrow poisons
    // whoever it lands on. The poison is played outside the flow, so it is initialized in Init.
    internal class GoblinArcherSkill : SkillDefinition
    {
        private static float VenomDuration => Tuned(5f);
        private static float PoisonTotal => Tuned(240f);
        private static float PoisonDuration => Tuned(2f);
        private static float PoisonInterval => Tuned(0.5f);

        private SkillPart _poison;
        private float _venomEndsAt;

        private readonly List<ICombatant> _notShotYet = new List<ICombatant>();

        public static SkillDefinition Build(TemplateActionRegistrySO registry) => new GoblinArcherSkill(registry);

        private GoblinArcherSkill(TemplateActionRegistrySO registry) : base(registry) { }

        public override string SkillName => "Venom Arrows";

        public override string Description
            => $"For {VenomDuration} seconds, every auto attack also poisons its target for "
             + $"{AD(PoisonTotal)} over {PoisonDuration} seconds. The poison stacks.";

        public override string PassiveDescription
            => "Infinite range. Every auto attack picks a new random enemy, "
             + "with no repeats until every enemy has been hit.";

        public override void Init(ICombatant caster)
        {
            base.Init(caster);

            _poison.Init(caster);
        }

        public override bool HasAutoAttackPassive => true;

        public override bool OnCombatStart()
        {
            _notShotYet.Clear();
            _venomEndsAt = 0f;

            return base.OnCombatStart();
        }

        public override ICombatant OnPickAutoAttackTarget(ICombatant target)
        {
            _notShotYet.RemoveAll(enemy => !enemy.IsAlive);

            if (_notShotYet.Count == 0) _notShotYet.AddRange(Caster.FindAllEnemies());

            if (_notShotYet.Count == 0) return target;

            int pick = Random.Range(0, _notShotYet.Count);
            ICombatant picked = _notShotYet[pick];
            _notShotYet.RemoveAt(pick);

            return picked;
        }

        public override bool OnAutoAttack(ICombatant target)
        {
            if (Time.time >= _venomEndsAt) return false;

            return PlayOnePart(_poison, assignedTarget: target);
        }

        public override bool OnCast()
        {
            if (!base.OnCast()) return false;

            _venomEndsAt = Time.time + VenomDuration;

            return true;
        }

        protected override SkillFlow BuildActiveFlow(TemplateActionRegistrySO registry)
        {
            SkillPart cast = Part(
                registry: registry,
                source: ActionSourceEnum.Self,
                action: TemplateActionEnum.Cast,
                target: AimTargetEnum.Self);

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
