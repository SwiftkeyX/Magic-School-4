using System.Collections.Generic;
using MagicSchool.Contracts;
using static MagicSchool.Skills.SkillFactory;

namespace MagicSchool.Skills
{
    // Her melee stance (front columns, +AD every cast) is disabled: it read the column counted from the front line,
    // and the board no longer has sides to count from. The code is in git history. She fights at range,
    // +AttackSpeedPerAttack% base AS every auto attack, and gets both bonuses past the BothBonuses thresholds.
    internal class HarpySkill : SkillDefinition
    {
        private static float DamageRatio => Tuned(200f);

        // stance 
        private enum STANCE { melee, range, both }
        private STANCE _stance;
        private static int MeleeLines => Tuned(2);
        private static int MeleeRange => Tuned(1);

        private static float AttackSpeedPerAttack => Tuned(2f);
        private static float AttackPerCast => Tuned(20f);

        private static float BothBonusesAttack => Tuned(100f);
        private static float BothBonusesAttackSpeed => Tuned(1.5f);

        private static float RestOfFight => Tuned(-1f);

        public static SkillDefinition Build(TemplateActionRegistrySO registry) => new HarpySkill(registry);

        private HarpySkill(TemplateActionRegistrySO registry) : base(registry) { }

        public override string SkillName => "Screech";

        public override string Description
            => $"Screeches at the current target for {AD(DamageRatio)}.";

        public override string PassiveDescription
            => $"She fights at range and gains {AttackSpeedPerAttack}% attack speed every auto attack. With more than "
             + $"{BothBonusesAttack} AD and {BothBonusesAttackSpeed} attack speed she also gains {AttackPerCast}% AD every cast.";

        public override bool HasAutoAttackPassive => true;

        public override bool OnCombatStart()
        {
            if (Caster is not IHeroStats stats) return false;

            if (stats.GetStat(StatEnum.ATK) > BothBonusesAttack && stats.GetStat(StatEnum.AS) > BothBonusesAttackSpeed)
            {
                _stance = STANCE.both;
            }

            else
            {
                _stance = STANCE.range;
            }

            return base.OnCombatStart();
        }

        public override bool OnAutoAttack(ICombatant target)
        {
            if (_stance == STANCE.melee) return false;

            Grow(Buff(ModifierEnum.AS, (StatEnum.AS, AttackSpeedPerAttack, ScaleFromEnum.Base)));
            return true;
        }

        public override bool OnCast()
        {
            if (!base.OnCast()) return false;

            if (_stance != STANCE.range)
                Grow(Buff(ModifierEnum.ATK, (StatEnum.ATK, AttackPerCast, ScaleFromEnum.Base)));

            return true;
        }

        protected override SkillFlow BuildActiveFlow(TemplateActionRegistrySO registry)
        {
            SkillPart screech = Part(
                registry: registry,
                source: ActionSourceEnum.Self,
                action: TemplateActionEnum.HomingProjectile,
                target: AimTargetEnum.Current,

                Damage(
                    recipient: EffectRecipientEnum.SameToAimTarget,
                    ratios: (StatEnum.ATK, DamageRatio))
            );

            return Flow(onStart: screech);
        }

        private void Grow(IModifier modifier)
            => Caster.AddModifier(BundleStack(RestOfFight, modifier), Caster as IHeroStats);
    }
}
