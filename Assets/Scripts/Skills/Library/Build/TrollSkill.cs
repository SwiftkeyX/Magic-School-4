using System.Collections.Generic;
using MagicSchool.Contracts;
using static MagicSchool.Skills.SkillFactory;

namespace MagicSchool.Skills
{
    // Grows only from kills by the allies on his Feed mark: bonus health and the HP to fill it, a fresh bundle each so
    // kills stack. He is not on his own mark, so his own kills do not count. His cast shield is a flat part plus a
    // share of his total max HP, so what he fed on counts; it refreshes, not stacks. The slam's area is the centre hex
    // plus that many rings (hexes are ~1 apart).
    internal class TrollSkill : SkillDefinition
    {
        private static float DamageRatio => Tuned(150f);
        private static float ShredDF => Tuned(20f);
        private static float ShredDuration => Tuned(4f);

        private static float ShieldFlat => Tuned(50f);
        private static float ShieldRatio => Tuned(10f);
        private static float ShieldDuration => Tuned(4f);
        private readonly ICustomModifier _shield = BundleRefresh(ShieldDuration, Shield(ShieldFlat, (StatEnum.MaxHP, ShieldRatio)));

        private static int AreaHexes => Tuned(1);
        private static float AreaDiameter => AreaHexes * 2f + 0.5f;

        private static float BonusHPPerDeath => Tuned(150f);
        private static float RestOfFight => Tuned(-1f);

        private const string FeedMark = "Feed";
        public override IReadOnlyList<string> SpecialMarks => new[] { FeedMark };

        public static SkillDefinition Build(TemplateActionRegistrySO registry) => new TrollSkill(registry);

        private TrollSkill(TemplateActionRegistrySO registry) : base(registry) { }

        public override string SkillName => "Rotting Slam";

        public override string Description
            => $"Slams the ground for {AD(DamageRatio)} to enemies around him and shreds {ShredDF} of their DF for "
             + $"{ShredDuration} seconds, then shields himself for {ShieldFlat} + {MaxHP(ShieldRatio)} for {ShieldDuration} seconds.";

        public override string PassiveDescription
            => $"Whenever an ally on his {FeedMark} mark kills an enemy, he gains {BonusHPPerDeath} bonus health for the rest of the fight "
             + "and heals for as much. Summoned units do not count.";

        public override bool HasAutoAttackPassive => true;

        public override void OnHeroDied(ICombatant dead)
        {
            if (dead.Team == Caster.Team) return;
            if (!(dead is ISkillCaster fallen) || fallen.IsSummon) return;
            if (!IsKilledFromFeedMark(fallen)) return;

            Caster.AddModifier(
                BundleStack(RestOfFight, Buff(ModifierEnum.BonusHP, BonusHPPerDeath)),
                Caster as IHeroStats);

            Caster.Heal(BonusHPPerDeath, Caster);
        }

        private bool IsKilledFromFeedMark(ISkillCaster fallen)
        {
            if (fallen.KilledBy == null || Caster == null) return false;

            foreach (ICombatant ally in Caster.AlliesOnSpecialMark(FeedMark))
            {
                if (ReferenceEquals(ally, fallen.KilledBy)) return true;
            }

            return false;
        }

        public override bool OnCast()
        {
            if (!base.OnCast()) return false;

            Caster.AddModifier(_shield, Caster as IHeroStats);

            return true;
        }

        protected override SkillFlow BuildActiveFlow(TemplateActionRegistrySO registry)
        {
            ICustomModifier shred = BundleRefresh(ShredDuration, Debuff(ModifierEnum.DefendShred, ShredDF));

            SkillPart slam = Part(
                registry: registry,
                source: ActionSourceEnum.Current,
                action: TemplateActionEnum.CircleAOE,
                target: AimTargetEnum.Self,
                tuning: TuneAOE(length: AreaDiameter, width: AreaDiameter),

                Damage(EffectRecipientEnum.EnemiesInArea, (StatEnum.ATK, DamageRatio)),
                Apply(EffectRecipientEnum.EnemiesInArea, shred)
            );

            return Flow(onStart: slam);
        }
    }
}
