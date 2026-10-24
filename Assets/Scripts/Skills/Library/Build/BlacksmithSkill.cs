using System.Collections.Generic;
using MagicSchool.Contracts;
using static MagicSchool.Skills.SkillFactory;

namespace MagicSchool.Skills
{
    internal class BlacksmithSkill : SkillDefinition
    {
        private static float DamageRatio => Tuned(180f);
        private static float ShredDuration => Tuned(-1f);
        private static float ShredFromAP => Tuned(25f);
        private static float HPPerSecondPerCast => Tuned(2f);      // % of her max HP, healed every second
        private static float RestOfFight => Tuned(-1f);

        // she hammers once more for every ally on her Anvil mark
        private const string AnvilMark = "Anvil";
        public override IReadOnlyList<string> SpecialMarks => new[] { AnvilMark };

        public static SkillDefinition Build(TemplateActionRegistrySO registry) => new BlacksmithSkill(registry);

        private BlacksmithSkill(TemplateActionRegistrySO registry) : base(registry) { }

        public override string SkillName => "Forge Hammer";

        public override string Description
            => $"Hammers the current target for {AD(DamageRatio)}, "
             + $"permanently removing armour equal to {AP(ShredFromAP)}. "
             + $"Hammers once more for every ally on her {AnvilMark} mark. "
             + $"Every cast, she gains {MaxHP(HPPerSecondPerCast)} HP a second for the rest of the fight (stacks).";

        // ============================== sustain ==============================
        // once per cast, however many hammers it makes
        // e.g. 1400 max HP, 3 casts => 84 HP a second (See HPRegen)
        public override bool OnCast()
        {
            if (!base.OnCast()) return false;

            Caster.AddModifier(BundleStack(RestOfFight,
                                   Buff(ModifierEnum.HPPerSecond, (StatEnum.MaxHP, HPPerSecondPerCast, ScaleFromEnum.Total))),
                               Caster as IHeroStats);
            return true;
        }

        // ============================== active ==============================
        // one hammer is one round; the count is asked again after every hammer
        // e.g. 2 allies on her Anvil mark => 3 hammers
        protected override SkillFlow BuildActiveFlow(TemplateActionRegistrySO registry)
        {
            SkillPart hammer = Part(registry,
                source: ActionSourceEnum.Current,
                action: TemplateActionEnum.CircleAOE,
                target: AimTargetEnum.Current,
                Damage(EffectRecipientEnum.EnemiesInArea, (StatEnum.ATK, DamageRatio)),
                Apply(EffectRecipientEnum.EnemiesInArea,
                      BundleRefresh(ShredDuration,
                             // 25% of the total AP is taken off the target's DF, for the rest of the fight
                             Debuff(ModifierEnum.DefendShred, (StatEnum.AP, ShredFromAP, ScaleFromEnum.Total))))
            );

            return Repeat(Flow(onStart: hammer), () => 1 + AlliesOnAnvil());
        }

        private int AlliesOnAnvil()
            => Caster is IAligned aligned ? aligned.AlliesOnSpecialMark(AnvilMark).Count : 0;
    }
}
