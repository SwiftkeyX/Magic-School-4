using MagicSchool.Contracts;
using static MagicSchool.Skills.SkillFactory;

namespace MagicSchool.Skills
{
    // "X% attack speed per 100 AP" is base AS x AP, but a ratio only adds; base AS never changes for a hero (stars and
    // levels leave it), so it is folded into the AP ratio: 100 AP on 0.75 AS gives +0.225 AS (+30%).
    internal class ElfSkill : SkillDefinition
    {
        private static float BuffDuration => Tuned(6f);
        private static float AttackFromAP => Tuned(15f);
        private static float AttackSpeedFromAP => Tuned(30f);

        public static SkillDefinition Build(TemplateActionRegistrySO registry) => new ElfSkill(registry);

        private ElfSkill(TemplateActionRegistrySO registry) : base(registry) { }

        public override string SkillName => "Hunter's Focus";

        public override string Description
            => $"Focuses for {BuffDuration} seconds, gaining {AP(AttackFromAP)} AD and {AP(AttackSpeedFromAP)}% attack speed.";

        protected override SkillFlow BuildActiveFlow(TemplateActionRegistrySO registry)
        {
            float baseAS = Caster is IHeroStats stats ? stats.GetBaseStat(StatEnum.AS) : 0f;

            SkillPart focus = Part(
                registry: registry,
                source: ActionSourceEnum.Self,
                action: TemplateActionEnum.Cast,
                target: AimTargetEnum.Self,

                Apply(
                    recipient: EffectRecipientEnum.Self,
                    modifier: BundleRefresh(
                        duration: BuffDuration,
                        modifiers: new[]
                        {
                            Buff(ModifierEnum.ATK, (StatEnum.AP, AttackFromAP, ScaleFromEnum.Total)),
                            Buff(ModifierEnum.AS, (StatEnum.AP, AttackSpeedFromAP * baseAS / 100f, ScaleFromEnum.Total)),
                        }))
            );

            return Flow(onStart: focus);
        }
    }
}
