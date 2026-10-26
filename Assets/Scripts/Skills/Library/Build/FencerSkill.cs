using UnityEngine;
using MagicSchool.Contracts;
using static MagicSchool.Skills.SkillFactory;

namespace MagicSchool.Skills
{
    // 3 strikes plus 1 for every 10% crit chance he has right now (25% => 5); one strike is one round. He is gone from
    // the board while the strikes last, and gains no mana from them.
    internal class FencerSkill : SkillDefinition
    {
        private static int BaseStrikes => Tuned(3);
        private static float CritChancePerStrike => Tuned(10f);
        private static float StrikeDamage => Tuned(100f);
        private static float StrikeInterval => Tuned(0.25f);

        private ICustomModifier _vanish;

        public static SkillDefinition Build(TemplateActionRegistrySO registry) => new FencerSkill(registry);

        private FencerSkill(TemplateActionRegistrySO registry) : base(registry) { }

        public override string SkillName => "Vanishing Flurry";

        public override string Description
            => $"Vanishes, becoming untargetable, and strikes the closest enemy {BaseStrikes} times in quick "
             + $"succession for {AD(StrikeDamage)} each. The strikes can crit, and every {CritChancePerStrike}% "
             + "crit chance adds 1 more strike.";

        public override bool OnCast()
        {
            if (Caster.FindCurrentTarget() == null) return false;

            if (!base.OnCast()) return false;

            Caster.AddModifier(_vanish, Caster as IHeroStats);

            return true;
        }

        protected override SkillFlow BuildActiveFlow(TemplateActionRegistrySO registry)
        {
            _vanish = BundleRefresh(
                duration: () => StrikeCount() * StrikeInterval,
                modifiers: Status(ModifierEnum.Untargetable));

            SkillPart tempo = Part(registry,
                source: ActionSourceEnum.Self,
                action: TemplateActionEnum.Cast,
                target: AimTargetEnum.Self,
                
                Apply(
                    EffectRecipientEnum.Self, 
                    BundleRefresh(
                        StrikeInterval, 
                        Status(ModifierEnum.ManaBlocked)
                ))
            );

            SkillPart strike = Part(registry,
                source: ActionSourceEnum.Self,
                action: TemplateActionEnum.Cast,
                target: AimTargetEnum.Current,
                DamageCanCrit(EffectRecipientEnum.SameToAimTarget, (StatEnum.ATK, StrikeDamage))
            );

            return Repeat(Flow(onStart: Together(tempo, strike)), times: StrikeCount);
        }

        private int StrikeCount()
        {
            float critChance = Caster is IHeroStats stats ? stats.GetStat(StatEnum.CritChance) : 0f;

            return BaseStrikes + Mathf.FloorToInt(critChance / CritChancePerStrike);
        }
    }
}
