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
        private static float MaxHPPerSecond => Tuned(6f);          // the stacks stop here: 3 casts at 2%
        private static int MaxRegenStacks => Mathf.FloorToInt(MaxHPPerSecond / HPPerSecondPerCast);
        private static float RestOfFight => Tuned(-1f);

        // every cast she forges one item; every MarksPerTier allies on her Anvil mark make a tank's plate a tier better (See Forge)
        private const string AnvilMark = "Anvil";
        public override IReadOnlyList<string> SpecialMarks => new[] { AnvilMark };
        private static int MarksPerTier => Tuned(2);
        private static int MaxWornOnSelf => Tuned(3);      // every item she wears counts, yours too

        public static SkillDefinition Build(TemplateActionRegistrySO registry) => new BlacksmithSkill(registry);

        private int _regenStacks;       // casts that gave regen this fight

        private BlacksmithSkill(TemplateActionRegistrySO registry) : base(registry) { }

        public override string SkillName => "Forge";

        public override string Description
            => $"Every cast, she gains {MaxHP(HPPerSecondPerCast)} HP a second for the rest of the fight (stacks, up to {MaxHP(MaxHPPerSecond)}), "
             + "and forges an item for the nearest ally wearing nothing: a Whetstone for a fighter, marksman or assassin, "
             + "an Archmage Tome for a mage, an Iron Plate for a tank - "
             + $"+20 DF, +50 DF with {MarksPerTier} allies on her {AnvilMark} mark, +100 DF with {MarksPerTier * 2}. "
             + $"When every ally wears something, she wears the plate herself, while she wears fewer than {MaxWornOnSelf} items. "
             + "Forged items last the fight.";

        // ============================== on cast ==============================
        // e.g. 1400 max HP, 3 casts => 84 HP a second (See HPRegen); a 4th cast forges but adds no regen
        public override bool OnCast()
        {
            if (!base.OnCast()) return false;

            if (_regenStacks < MaxRegenStacks)
            {
                _regenStacks++;
                Caster.AddModifier(BundleStack(RestOfFight,
                                       Buff(ModifierEnum.HPPerSecond, (StatEnum.MaxHP, HPPerSecondPerCast, ScaleFromEnum.Total))),
                                   Caster as IHeroStats);
            }
            Forge();
            return true;
        }

        // the regen is for the fight, so is its count
        public override bool OnCombatStart()
        {
            _regenStacks = 0;
            return base.OnCombatStart();
        }

        // One item: for the nearest ally wearing nothing, by role - anywhere, not only on the mark. Nobody left: a plate on
        // herself, unless she already wears MaxWornOnSelf items of any kind.
        // e.g. 2 allies on Anvil, a bare tank => a +50 DF plate on him
        private void Forge()
        {
            if (!(Caster is ICombatant me)) return;

            int tier = Caster is IAligned aligned ? aligned.AlliesOnSpecialMark(AnvilMark).Count / Mathf.Max(1, MarksPerTier) : 0;

            Vector3 at = me.transform.position;
            IItemWearer bare = (Caster is ITargeter targeter ? targeter.FindAllAllies() : new List<ICombatant>())
                .Where(ally => ally != null && ally.IsAlive && ally is IItemWearer wearer && wearer.WornItemCount == 0
                               && ForgeFor(wearer.Role, tier).HasValue)
                .OrderBy(ally => (ally.transform.position - at).sqrMagnitude)
                .Cast<IItemWearer>()
                .FirstOrDefault();

            if (bare != null) bare.ReceiveForgedItem(ForgeFor(bare.Role, tier).Value, me);
            else if (Caster is IItemWearer self && self.WornItemCount < MaxWornOnSelf) self.ReceiveForgedItem(PlateOf(tier), me);
        }

        // what an ally of this role gets; none for a unit with no role (a dummy, a minion)
        private static ForgedItemEnum? ForgeFor(HeroRoleEnum role, int tier)
        {
            switch (role)
            {
                case HeroRoleEnum.Tank: return PlateOf(tier);
                case HeroRoleEnum.Mage: return ForgedItemEnum.ArchmageTome;
                case HeroRoleEnum.Fighter:
                case HeroRoleEnum.Marksman:
                case HeroRoleEnum.Assassin: return ForgedItemEnum.Whetstone;
                default: return null;
            }
        }

        // 0-1 allies on Anvil: +20 DF, 2-3: +50 DF, 4+: +100 DF
        private static ForgedItemEnum PlateOf(int tier)
            => tier <= 0 ? ForgedItemEnum.IronPlate : tier == 1 ? ForgedItemEnum.ForgedIronPlate : ForgedItemEnum.MasterworkIronPlate;

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
