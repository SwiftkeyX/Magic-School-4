using System.Collections.Generic;
using MagicSchool.Contracts;
using static MagicSchool.Skills.SkillFactory;

namespace MagicSchool.Skills
{
    internal class TrollSkill : SkillDefinition
    {
        // slam
        private static float DamageRatio => Tuned(150f);
        private static float ShredDF => Tuned(20f);
        private static float ShredDuration => Tuned(4f);

        // shield on cast - a flat part, and a share of his total max HP, so the bonus health he has fed on counts
        private static float ShieldFlat => Tuned(50f);
        private static float ShieldRatio => Tuned(10f);
        private static float ShieldDuration => Tuned(4f);
        private readonly ICustomModifier _shield = BundleRefresh(ShieldDuration, Shield(ShieldFlat, (StatEnum.MaxHP, ShieldRatio)));

        // area - the centre hex plus that many rings around it (hexes are ~1 apart)
        private static int AreaHexes => Tuned(1);
        private static float AreaDiameter => AreaHexes * 2f + 0.5f;

        // passive
        private static float BonusHPPerDeath => Tuned(300f);
        private static float RestOfFight => Tuned(-1f);

        // only the kills of the allies standing on this mark feed him (See AlignMark)
        private const string FeedMark = "Feed";
        public override IReadOnlyList<string> SpecialMarks => new[] { FeedMark };

        public static SkillDefinition Build(TemplateActionRegistrySO registry) => new TrollSkill(registry);

        private TrollSkill(TemplateActionRegistrySO registry) : base(registry) { }

        public override string SkillName => "Rotting Slam";

        public override string Description
            => $"Slams the ground for {DamageRatio}% AD to enemies around him and shreds {ShredDF} of their DF for "
             + $"{ShredDuration} seconds, then shields himself for {ShieldFlat} + {ShieldRatio}% of his max HP for {ShieldDuration} seconds.";

        public override string PassiveDescription
            => $"Whenever an ally on his {FeedMark} mark kills an enemy, he gains {BonusHPPerDeath} bonus health for the rest of the fight "
             + "and heals for as much. Summoned units do not count.";

        // ============================== passive ==============================
        public override bool HasAutoAttackPassive => true;

        // an enemy was killed by an ally on his Feed mark - gain bonus health, and the HP to fill it.
        // A fresh bundle every time, so the kills stack
        public override void OnHeroDied(ICombatant dead)
        {
            if (dead.Team == Caster.Team) return;
            if (!(dead is IFallen fallen) || fallen.IsSummon) return;
            if (!IsKilledFromFeedMark(fallen)) return;

            Caster.AddModifier(
                BundleStack(RestOfFight, Buff(ModifierEnum.BonusHP, BonusHPPerDeath)),
                Caster as IHeroStats);

            Caster.Heal(BonusHPPerDeath, Caster);
        }

        // was the killer one of the allies on his Feed mark? He himself is not on it, so his own kills do not count
        private bool IsKilledFromFeedMark(IFallen fallen)
        {
            if (fallen.KilledBy == null || !(Caster is IAligned aligned)) return false;

            foreach (ICombatant ally in aligned.AlliesOnSpecialMark(FeedMark))
            {
                if (ReferenceEquals(ally, fallen.KilledBy)) return true;
            }

            return false;
        }

        // ============================== active ==============================
        // on cast, slam and take the shield. Refreshed, not stacked, if he casts again before it runs out
        public override bool OnCast()
        {
            if (!base.OnCast()) return false;

            Caster.AddModifier(_shield, Caster as IHeroStats);

            return true;
        }

        protected override SkillFlow BuildActiveFlow(TemplateActionRegistrySO registry)
        {
            ICustomModifier shred = BundleRefresh(ShredDuration, Debuff(ModifierEnum.DefendShred, ShredDF));

            // a circle around himself
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
