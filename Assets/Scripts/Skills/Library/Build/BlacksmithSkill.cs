using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MagicSchool.Contracts;
using static MagicSchool.Skills.SkillFactory;

namespace MagicSchool.Skills
{
    internal class BlacksmithSkill : SkillDefinition
    {
        private static float HPPerSecondPerCast => Tuned(2f);      // % of her max HP, healed every second
        private static float HPPerSecondPerAnvilAlly => Tuned(0.3f);   // % of her max HP more per cast, for every ally on Anvil
        private static int MaxRegenStacks => Tuned(3);             // casts that give regen; later ones only forge
        private static float RestOfFight => Tuned(-1f);

        // every cast she forges one item, picked by role from her forge table (HeroDataSO); every MarksPerTier allies on her
        // Anvil mark make it a tier better (See Forge)
        private const string AnvilMark = "Anvil";
        public override IReadOnlyList<string> SpecialMarks => new[] { AnvilMark };
        private static int MarksPerTier => Tuned(1);
        private static int MaxWornOnSelf => Tuned(3);      // every item she wears counts, yours too

        public static SkillDefinition Build(TemplateActionRegistrySO registry) => new BlacksmithSkill(registry);

        private int _regenStacks;       // casts that gave regen this fight

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

        // ============================== on cast ==============================
        // e.g. 1400 max HP, 2 allies on Anvil => 2.6% = 36.4 HP a second a cast, 3 casts => 109.2 (See HPRegen);
        //      a 4th cast forges but adds no regen. The allies are counted at each cast.
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

        // 2% of her max HP, plus 0.3% for every ally on her Anvil mark
        private float RegenThisCast()
        {
            int onAnvil = Caster is IAligned aligned ? aligned.AlliesOnSpecialMark(AnvilMark).Count : 0;
            return HPPerSecondPerCast + HPPerSecondPerAnvilAlly * onAnvil;
        }

        // the regen is for the fight, so is its count
        public override bool OnCombatStart()
        {
            _regenStacks = 0;
            return base.OnCombatStart();
        }

        // One item: for the nearest ally wearing nothing whose role her forge table has a row for - anywhere, not only on
        // the mark. Nobody left: her table's tank item on herself, unless she already wears MaxWornOnSelf items of any kind.
        // e.g. 2 allies on Anvil, a bare tank => the tank row's tier 2 item (+40 DF by default) on him
        private void Forge()
        {
            if (!(Caster is ICombatant me) || !(Caster is IForger forger)) return;

            int tier = Caster is IAligned aligned ? aligned.AlliesOnSpecialMark(AnvilMark).Count / Mathf.Max(1, MarksPerTier) : 0;

            Vector3 at = me.transform.position;
            IItemWearer bare = (Caster is ITargeter targeter ? targeter.FindAllAllies() : new List<ICombatant>())
                .Where(ally => ally != null && ally.IsAlive && ally is IItemWearer wearer && wearer.WornItemCount == 0
                               && forger.CanForgeFor(wearer.Role))
                .OrderBy(ally => (ally.transform.position - at).sqrMagnitude)
                .Cast<IItemWearer>()
                .FirstOrDefault();

            if (bare != null) bare.ReceiveForgedItem(bare.Role, tier, me);
            else if (Caster is IItemWearer self && self.WornItemCount < MaxWornOnSelf && forger.CanForgeFor(HeroRoleEnum.Tank))
                self.ReceiveForgedItem(HeroRoleEnum.Tank, tier, me);
        }

        // ============================== active ==============================
        // no hammer any more: the cast itself is the whole active - she stands and forges (See OnCast)
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
