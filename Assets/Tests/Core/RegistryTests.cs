using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using MagicSchool.Contracts;
using MagicSchool.Combat.Heroes;
using MagicSchool.Combat.Placements;
using MagicSchool.Items;
using MagicSchool.Modifiers;
using MagicSchool.Skills;

namespace MagicSchool.Core.Tests
{
    /// <summary>
    /// Everything that has to be registered by hand in more than one place, checked without Play mode.
    /// A miss here used to show only as a runtime log, or as a bonus that silently stayed 0.
    /// </summary>
    public class RegistryTests
    {
        private const string BaseHeroPath = "Assets/Prefabs/Hero/BaseHero.prefab";

        private static IEnumerable<T> AssetsIn<T>(params string[] folders) where T : UnityEngine.Object
            => AssetDatabase.FindAssets($"t:{typeof(T).Name}", folders)
                .Select(guid => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid)));

        private static TemplateActionRegistrySO Registry()
        {
            var registry = AssetsIn<TemplateActionRegistrySO>("Assets/Data").FirstOrDefault();
            Assert.IsNotNull(registry, "no TemplateActionRegistrySO in Assets/Data");
            return registry;
        }

        private static IEnumerable<T> EnumValues<T>() where T : Enum => Enum.GetValues(typeof(T)).Cast<T>();

        [Test]
        public void EverySkillIdBuildsASkill()
        {
            TemplateActionRegistrySO registry = Registry();
            foreach (SkillIdEnum id in EnumValues<SkillIdEnum>().Where(id => id != SkillIdEnum.None))
                Assert.IsNotNull(SkillLibrary.Resolve(id, registry), $"{id} has no skill builder in SkillLibrary");
        }

        [Test]
        public void EveryItemIdBuildsAModifier()
        {
            foreach (ItemIdEnum id in EnumValues<ItemIdEnum>().Where(id => id != ItemIdEnum.None))
                Assert.IsNotNull(ItemLibrary.Resolve(id), $"{id} has no builder in ItemLibrary");
        }

        [Test]
        public void EveryStatCanBeModified()
        {
            var fed = new HashSet<StatEnum>(ActiveCustomModifier.StatFedBy.Values);
            foreach (StatEnum stat in EnumValues<StatEnum>().Where(s => s != StatEnum.None))
                Assert.IsTrue(fed.Contains(stat), $"no ModifierEnum feeds {stat} (ActiveCustomModifier._lookup)");

            Assert.Greater(ModifierResolver.StatSlots, EnumValues<StatEnum>().Max(s => (int)s));
        }

        [Test]
        public void EveryHeroHasABaseHeroPrefabAndAKnownSkill()
        {
            var baseHero = AssetDatabase.LoadAssetAtPath<GameObject>(BaseHeroPath);
            Assert.IsNotNull(baseHero, $"no {BaseHeroPath}");
            TemplateActionRegistrySO registry = Registry();

            var heroes = AssetsIn<HeroDataSO>("Assets/Data/Heroes/Stats").ToList();
            Assert.IsNotEmpty(heroes);

            foreach (HeroDataSO hero in heroes)
            {
                Assert.IsNotNull(hero.Prefab, $"{hero.name} has no prefab");
                GameObject root = PrefabUtility.GetCorrespondingObjectFromOriginalSource(hero.Prefab);
                Assert.AreEqual(baseHero, root, $"{hero.name}'s prefab is not a BaseHero variant");

                Assert.IsTrue(Enum.IsDefined(typeof(SkillIdEnum), hero.SkillId), $"{hero.name} holds an unknown skill id {(int)hero.SkillId}");
                if (hero.SkillId != SkillIdEnum.None)
                    Assert.IsNotNull(SkillLibrary.Resolve(hero.SkillId, registry), $"{hero.name}'s skill {hero.SkillId} does not build");
            }
        }

        [Test]
        public void EveryItemAssetResolves()
        {
            // BaseItem's own _data is a placeholder every spawn replaces, so it is not an item anyone gets
            var baseItem = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Item/BaseItem.prefab");
            ItemDataSO placeholder = baseItem != null ? baseItem.GetComponentInChildren<Item>(true)?.Data : null;

            var items = AssetsIn<ItemDataSO>("Assets/Data/Items").Where(item => item != placeholder).ToList();
            Assert.IsNotEmpty(items);

            foreach (ItemDataSO item in items)
            {
                Assert.AreNotEqual(ItemIdEnum.None, item.ItemId, $"{item.name} has no item id");
                Assert.IsNotNull(item.Prefab, $"{item.name} has no prefab");
                Assert.IsNotNull(ItemLibrary.Resolve(item.ItemId), $"{item.name}'s {item.ItemId} does not build");
            }
        }

        [Test]
        public void EveryStageHasABoardADeployZoneAndRealEnemies()
        {
            var stages = AssetsIn<BattlePlacementSO>("Assets/Data/BattleSetups/Campaign").ToList();
            Assert.IsNotEmpty(stages);

            foreach (BattlePlacementSO stage in stages)
            {
                Assert.IsNotNull(stage.BoardLayout, $"{stage.name} has no board layout");
                Assert.IsNotEmpty(stage.DeployZone, $"{stage.name} has an empty deploy zone");
                Assert.IsTrue(stage.HeroesPlacement.Any(p => p.team == TeamEnum.Red), $"{stage.name} has no enemy");
                foreach (HeroPlacement placement in stage.HeroesPlacement)
                    Assert.IsNotNull(placement.dataSO, $"{stage.name} has a placement with no hero");
            }
        }

        [Test]
        public void EveryTeamCompHasABoardAndRealHeroes()
        {
            foreach (BattlePlacementSO comp in AssetsIn<BattlePlacementSO>("Assets/Data/BattleSetups/Team Comps"))
            {
                Assert.IsNotNull(comp.BoardLayout, $"{comp.name} has no board layout");
                foreach (HeroPlacement placement in comp.HeroesPlacement)
                {
                    Assert.IsNotNull(placement.dataSO, $"{comp.name} has a placement with no hero");
                    Assert.IsTrue(placement.item == null || placement.item is ItemDataSO, $"{comp.name}: {placement.dataSO.name} wears a non-item");
                }
            }
        }
    }
}
