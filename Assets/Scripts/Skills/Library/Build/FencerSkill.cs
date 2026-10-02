using UnityEngine;
using MagicSchool.Contracts;
using static MagicSchool.Skills.SkillFactory;

namespace MagicSchool.Skills
{
    internal class FencerSkill : SkillDefinition
    {
        private const int BaseStrikes = 3;
        private const float CritChancePerStrike = 10f;      // every 10% crit chance adds 1 more strike
        private const float StrikeDamage = 100f;
        private const float StrikeInterval = 0.25f;         // time between two strikes

        // he is gone from the board for as long as the strikes take
        private ICustomModifier _vanish;

        public static SkillDefinition Build(TemplateActionRegistrySO registry) => new FencerSkill(registry);

        private FencerSkill(TemplateActionRegistrySO registry) : base(registry) { }

        public override string SkillName => "Vanishing Flurry";

        public override string Description
            => $"Vanishes, becoming untargetable, and strikes the closest enemy {BaseStrikes} times in quick "
             + $"succession for {StrikeDamage}% AD each. The strikes can crit, and every {CritChancePerStrike}% "
             + "crit chance adds 1 more strike.";

        // ============================== active ==============================
        public override bool OnCast()
        {
            // nobody to strike, return
            if (Caster.FindCurrentTarget() == null) return false;

            if (!base.OnCast()) return false;

            Caster.AddModifier(_vanish, Caster as IHeroStats);

            return true;
        }

        // one strike is one round, played as many times as he has strikes.
        protected override SkillFlow BuildActiveFlow(TemplateActionRegistrySO registry)
        {
            _vanish = BundleRefresh(
                duration: () => StrikeCount() * StrikeInterval,
                modifiers: Status(ModifierEnum.Untargetable));

            // the tempo. prevent the strike to gain mana.
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

            // the strike. 
            SkillPart strike = Part(registry,
                source: ActionSourceEnum.Self,
                action: TemplateActionEnum.Cast,
                target: AimTargetEnum.Current,
                DamageCanCrit(EffectRecipientEnum.SameToAimTarget, (StatEnum.ATK, StrikeDamage))
            );

            return Repeat(Flow(onStart: Together(tempo, strike)), times: StrikeCount);
        }

        // ============================== helper ==============================
        // 3 strikes, and 1 more for every 10% crit chance he has right now. 25% crit chance => 5 strikes
        private int StrikeCount()
        {
            float critChance = Caster is IHeroStats stats ? stats.GetStat(StatEnum.CritChance) : 0f;

            return BaseStrikes + Mathf.FloorToInt(critChance / CritChancePerStrike);
        }
    }
}
