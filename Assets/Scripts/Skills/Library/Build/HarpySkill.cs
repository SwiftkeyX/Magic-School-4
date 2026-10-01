using System.Collections.Generic;
using MagicSchool.Contracts;
using static MagicSchool.Skills.SkillFactory;

namespace MagicSchool.Skills
{
    internal class HarpySkill : SkillDefinition
    {
        private const float DamageRatio = 200f;

        // stance 
        private enum STANCE { melee, range, both }
        private STANCE _stance;
        private const int MeleeLines = 2;
        private const int MeleeRange = 1;

        // scaling
        private const float AttackSpeedPerAttack = 2f;  // ranged: +2% base AS every auto attack
        private const float AttackPerCast = 20f;        // melee: +20% base AD every cast

        // condition
        private const float BothBonusesAttack = 100f;
        private const float BothBonusesAttackSpeed = 1.5f;

        // other
        private const float RestOfFight = -1f;

        public static SkillDefinition Build(TemplateActionRegistrySO registry) => new HarpySkill(registry);

        private HarpySkill(TemplateActionRegistrySO registry) : base(registry) { }

        public override string SkillName => "Screech";

        public override string Description
            => $"Screeches at the current target for {DamageRatio}% AD.";

        public override string PassiveDescription
            => $"Her stance is locked in when combat starts. Placed in the first {MeleeLines} column "
             + $"she become melee and gains {AttackPerCast}% AD every cast; anywhere else she fights at range "
             + $"and gains {AttackSpeedPerAttack}% attack speed every auto attack. With more than "
             + $"{BothBonusesAttack} AD and {BothBonusesAttackSpeed} attack speed she gets both.";

        // ============================== passive ==============================
        public override bool HasAttackPassive => true;

        // at combat start, choose the stance
        public override bool OnCombatStart()
        {
            if (Caster is not IHeroStats stats) return false;
            int line = Caster.LinesFromFront;

            // if harpy achieve the condition, gain both stance
            if (stats.GetStat(StatEnum.ATK) > BothBonusesAttack && stats.GetStat(StatEnum.AS) > BothBonusesAttackSpeed)
            {
                _stance = STANCE.both;
            }

            // if harpy was place in the first MeleeLines, cut her attack range data to 1.
            else if (line >= 0 && line < MeleeLines)
            {
                _stance = STANCE.melee;
                float cut = stats.GetBaseStat(StatEnum.Range) - MeleeRange;
                Grow(Debuff(ModifierEnum.Range, cut));
            }

            // else harpy was range.
            else
            {
                _stance = STANCE.range;
            }

            return base.OnCombatStart();
        }


        // on attack, if harpy was range or both, gain AS buff
        public override bool OnAttack(ICombatant target)
        {
            if (_stance == STANCE.melee) return false;

            Grow(Buff(ModifierEnum.AS, (StatEnum.AS, AttackSpeedPerAttack, ScaleFromEnum.Base)));
            return true;
        }

        // ============================== active ==============================
        // on cast, if harpy was melee or both, gain ATK buff
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

        // ============================== helper ==============================
        private void Grow(IModifier modifier)
            => Caster.AddModifier(Bundle(RestOfFight, modifier), Caster as IHeroStats);
    }
}
