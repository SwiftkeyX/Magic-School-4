using System.Collections.Generic;
using MagicSchool.Contracts;
using static MagicSchool.Skills.SkillFactory;

namespace MagicSchool.Skills
{
    internal class HarpySkill : SkillDefinition
    {
        private static float DamageRatio => Tuned(200f);

        // stance 
        private enum STANCE { melee, range, both }
        private STANCE _stance;
        private static int MeleeLines => Tuned(2);
        private static int MeleeRange => Tuned(1);

        // scaling
        private static float AttackSpeedPerAttack => Tuned(2f);  // ranged: +2% base AS every auto attack
        private static float AttackPerCast => Tuned(20f);        // melee: +20% base AD every cast

        // condition
        private static float BothBonusesAttack => Tuned(100f);
        private static float BothBonusesAttackSpeed => Tuned(1.5f);

        // other
        private static float RestOfFight => Tuned(-1f);

        public static SkillDefinition Build(TemplateActionRegistrySO registry) => new HarpySkill(registry);

        private HarpySkill(TemplateActionRegistrySO registry) : base(registry) { }

        public override string SkillName => "Screech";

        public override string Description
            => $"Screeches at the current target for {AD(DamageRatio)}.";

        // COMMENTED OUT with the melee stance, see OnCombatStart
        // public override string PassiveDescription
        //     => $"Her stance is locked in when combat starts. Placed in the first {MeleeLines} column "
        //      + $"she become melee and gains {AttackPerCast}% AD every cast; anywhere else she fights at range "
        //      + $"and gains {AttackSpeedPerAttack}% attack speed every auto attack. With more than "
        //      + $"{BothBonusesAttack} AD and {BothBonusesAttackSpeed} attack speed she gets both.";
        public override string PassiveDescription
            => $"She fights at range and gains {AttackSpeedPerAttack}% attack speed every auto attack. With more than "
             + $"{BothBonusesAttack} AD and {BothBonusesAttackSpeed} attack speed she also gains {AttackPerCast}% AD every cast.";

        // ============================== passive ==============================
        public override bool HasAutoAttackPassive => true;

        // at combat start, choose the stance
        public override bool OnCombatStart()
        {
            if (Caster is not IHeroStats stats) return false;

            // if harpy achieve the condition, gain both stance
            if (stats.GetStat(StatEnum.ATK) > BothBonusesAttack && stats.GetStat(StatEnum.AS) > BothBonusesAttackSpeed)
            {
                _stance = STANCE.both;
            }

            // COMMENTED OUT: the melee stance read which column she stood in, counted from the front line.
            // The board is losing its red and blue sides, so there is no front line left to count from.
            // int line = Caster.LinesFromFront;
            //
            // // if harpy was place in the first MeleeLines, cut her attack range data to 1.
            // else if (line >= 0 && line < MeleeLines)
            // {
            //     _stance = STANCE.melee;
            //     float cut = stats.GetBaseStat(StatEnum.Range) - MeleeRange;
            //     Grow(Debuff(ModifierEnum.Range, cut));
            // }

            // else harpy was range.
            else
            {
                _stance = STANCE.range;
            }

            return base.OnCombatStart();
        }


        // on attack, if harpy was range or both, gain AS buff
        public override bool OnAutoAttack(ICombatant target)
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
            => Caster.AddModifier(BundleStack(RestOfFight, modifier), Caster as IHeroStats);
    }
}
