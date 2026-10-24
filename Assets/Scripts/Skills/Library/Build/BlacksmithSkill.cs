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
        private static float RestOfFight => Tuned(-1f);

        // every cast she forges one item for every ally standing on her Anvil mark (See Forge)
        private const string AnvilMark = "Anvil";
        public override IReadOnlyList<string> SpecialMarks => new[] { AnvilMark };

        public static SkillDefinition Build(TemplateActionRegistrySO registry) => new BlacksmithSkill(registry);

        private BlacksmithSkill(TemplateActionRegistrySO registry) : base(registry) { }

        public override string SkillName => "Forge";

        public override string Description
            => $"Every cast, she gains {MaxHP(HPPerSecondPerCast)} HP a second for the rest of the fight (stacks), "
             + $"and forges an item for every ally on her {AnvilMark} mark, each for the nearest ally wearing nothing: "
             + "a Whetstone for a fighter, marksman or assassin, an Archmage Tome for a mage, a Forged Iron Plate (+50 DF) for a tank. "
             + "What nobody takes she wears herself, as Forged Iron Plates. Forged items last the fight.";

        // ============================== on cast ==============================
        // e.g. 1400 max HP, 3 casts => 84 HP a second (See HPRegen)
        public override bool OnCast()
        {
            if (!base.OnCast()) return false;

            Caster.AddModifier(BundleStack(RestOfFight,
                                   Buff(ModifierEnum.HPPerSecond, (StatEnum.MaxHP, HPPerSecondPerCast, ScaleFromEnum.Total))),
                               Caster as IHeroStats);
            Forge();
            return true;
        }

        // One item for every ally on her Anvil mark, handed to the allies wearing nothing - anywhere, not only on the mark -
        // nearest first, by role. The rest go on her.
        // e.g. 3 allies on Anvil, a mage and a tank with nothing on => a Tome, a Plate, and a Plate on herself
        private void Forge()
        {
            if (!(Caster is ICombatant me)) return;

            int items = Caster is IAligned aligned ? aligned.AlliesOnSpecialMark(AnvilMark).Count : 0;
            if (items <= 0) return;

            Vector3 at = me.transform.position;
            List<IItemWearer> bare = (Caster is ITargeter targeter ? targeter.FindAllAllies() : new List<ICombatant>())
                .Where(ally => ally != null && ally.IsAlive && ally is IItemWearer wearer && wearer.WornItemCount == 0
                               && ForgeFor(wearer.Role).HasValue)
                .OrderBy(ally => (ally.transform.position - at).sqrMagnitude)
                .Cast<IItemWearer>()
                .ToList();

            for (int i = 0; i < items; i++)
            {
                if (i < bare.Count) bare[i].ReceiveForgedItem(ForgeFor(bare[i].Role).Value, me);
                else if (Caster is IItemWearer self) self.ReceiveForgedItem(ForgedItemEnum.ForgedIronPlate, me);
            }
        }

        // what an ally of this role gets; none for a unit with no role (a dummy, a minion)
        private static ForgedItemEnum? ForgeFor(HeroRoleEnum role)
        {
            switch (role)
            {
                case HeroRoleEnum.Tank: return ForgedItemEnum.ForgedIronPlate;
                case HeroRoleEnum.Mage: return ForgedItemEnum.ArchmageTome;
                case HeroRoleEnum.Fighter:
                case HeroRoleEnum.Marksman:
                case HeroRoleEnum.Assassin: return ForgedItemEnum.Whetstone;
                default: return null;
            }
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
