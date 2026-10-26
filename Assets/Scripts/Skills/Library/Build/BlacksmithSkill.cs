using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MagicSchool.Contracts;
using static MagicSchool.Skills.SkillFactory;

namespace MagicSchool.Skills
{
    // Every cast she gains HP regen of HPPerSecondPerCast% of her max HP, plus HPPerSecondPerAnvilAlly% per ally on
    // her Anvil mark (counted at each cast), stacking for MaxRegenStacks casts: 1400 max HP, 2 allies => 36.4 HP/s a
    // cast, 109.2 after 3. Every cast also forges one item for the nearest ally wearing nothing whose role her forge
    // table has a row for (anywhere, not only on the mark), a tier better per MarksPerTier allies on Anvil; nobody
    // left, she takes the tank row's item herself while she wears fewer than MaxWornOnSelf items of any kind. The cast
    // is the whole active.
    internal class BlacksmithSkill : SkillDefinition
    {
        private static float HPPerSecondPerCast => Tuned(2f);
        private static float HPPerSecondPerAnvilAlly => Tuned(0.3f);
        private static int MaxRegenStacks => Tuned(3);
        private static float RestOfFight => Tuned(-1f);

        private const string AnvilMark = "Anvil";
        public override IReadOnlyList<string> SpecialMarks => new[] { AnvilMark };
        private static int MarksPerTier => Tuned(1);
        private static int MaxWornOnSelf => Tuned(3);

        public static SkillDefinition Build(TemplateActionRegistrySO registry) => new BlacksmithSkill(registry);

        private int _regenStacks;

        private BlacksmithSkill(TemplateActionRegistrySO registry) : base(registry) { }

        public override string SkillName => "Forge";

        public override string Description
            => $"Every cast, she gains {MaxHP(HPPerSecondPerCast)} HP a second, plus {MaxHP(HPPerSecondPerAnvilAlly)} for every ally on her "
             + $"{AnvilMark} mark, for the rest of the fight (stacks {MaxRegenStacks} times), "
             + "and forges an item for the nearest ally wearing nothing: a Whetstone for a fighter, marksman or assassin, "
             + "an Archmage Tome for a mage, an Iron Plate for a tank - "
             + $"a better plate for every {(MarksPerTier == 1 ? "ally" : $"{MarksPerTier} allies")} on her {AnvilMark} mark. "
             + $"When every ally wears something, she wears the plate herself, while she wears fewer than {MaxWornOnSelf} items. "
             + "Forged items last the fight.";

        public override bool OnCast()
        {
            if (!base.OnCast()) return false;

            if (_regenStacks < MaxRegenStacks)
            {
                _regenStacks++;
                Caster.AddModifier(BundleStack(RestOfFight,
                                       Buff(ModifierEnum.HPPerSecond, (StatEnum.MaxHP, RegenThisCast(), ScaleFromEnum.Total))),
                                   Caster as IHeroStats);
            }
            Forge();
            return true;
        }

        private float RegenThisCast()
        {
            int onAnvil = Caster?.AlliesOnSpecialMark(AnvilMark).Count ?? 0;
            return HPPerSecondPerCast + HPPerSecondPerAnvilAlly * onAnvil;
        }

        public override bool OnCombatStart()
        {
            _regenStacks = 0;
            return base.OnCombatStart();
        }

        private void Forge()
        {
            if (Caster == null) return;

            int tier = Caster.AlliesOnSpecialMark(AnvilMark).Count / Mathf.Max(1, MarksPerTier);

            Vector3 at = Caster.transform.position;
            ISkillCaster bare = Caster.FindAllAllies()
                .Where(ally => ally != null && ally.IsAlive && ally is ISkillCaster wearer && wearer.WornItemCount == 0
                               && Caster.CanForgeFor(wearer.Role))
                .OrderBy(ally => (ally.transform.position - at).sqrMagnitude)
                .Cast<ISkillCaster>()
                .FirstOrDefault();

            if (bare != null) bare.ReceiveForgedItem(bare.Role, tier, Caster);
            else if (Caster.WornItemCount < MaxWornOnSelf && Caster.CanForgeFor(HeroRoleEnum.Tank))
                Caster.ReceiveForgedItem(HeroRoleEnum.Tank, tier, Caster);
        }

        protected override SkillFlow BuildActiveFlow(TemplateActionRegistrySO registry)
        {
            SkillPart forge = Part(
                registry: registry,
                source: ActionSourceEnum.Self,
                action: TemplateActionEnum.Cast,
                target: AimTargetEnum.Self);

            return Flow(onStart: forge);
        }
    }
}
