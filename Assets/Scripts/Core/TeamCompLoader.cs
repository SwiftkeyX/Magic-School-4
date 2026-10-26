using System;
using System.Collections.Generic;
using UnityEngine;
using MagicSchool.Contracts;
using MagicSchool.Combat.Heroes;
using MagicSchool.Combat.Placements;
using MagicSchool.Items;

namespace MagicSchool.Core
{
    // Puts a saved team comp's heroes on the board as they were saved - hex, level, star, align pattern, worn item - and
    // then the items lying on hexes. The one copy the Stage Editor and the Team Comp Benchmark both use. The board must be
    // the comp's layout: a hex number only means something on its own. A hero whose hex is missing or taken is skipped;
    // an item that cannot be worn or laid where it was saved is handed to onLeftOver (the Stage Editor drops it on the
    // loose row, the benchmark destroys it).
    public static class TeamCompLoader
    {
        public static List<Hero> Load(BattlePlacementSO comp, BattleBoard board, Func<HeroDataSO, IPlacement, Hero> spawn, Action<Item> onLeftOver)
        {
            var placed = new List<Hero>();

            foreach (HeroPlacement placement in comp.HeroesPlacement)
            {
                if (placement.team != TeamEnum.Blue || placement.dataSO == null) continue;

                if (!board.Hexs.TryGetValue(placement.hexPlacement, out Hex hex) || board.WhoReservedThisHex(hex) != null)
                {
                    Debug.LogWarning($"[TeamCompLoader] {comp.name}: skipped {placement.dataSO.name}, hex {placement.hexPlacement.column},{placement.hexPlacement.row} is missing or taken.");
                    continue;
                }

                Hero hero = spawn(placement.dataSO, hex);
                placed.Add(hero);

                if (placement.Level > HeroLevel.Min) hero.SetLevel(placement.Level);
                if (placement.Star > HeroStar.Min) hero.UpgradeTo(placement.Star);
                hero.SetAlignMarkPattern(placement.alignPattern);

                if (placement.item is ItemDataSO itemData)
                {
                    Item worn = Item.Spawn(itemData, hex.transform.position);
                    if (worn != null && !hero.TryWear(worn)) onLeftOver(worn);
                }
            }

            foreach (ItemPlacement lying in comp.ItemsOnHexes)
            {
                if (!(lying.item is ItemDataSO itemData)) continue;

                Item item = Item.Spawn(itemData, Vector3.zero);
                if (item == null) continue;

                bool isLaid = board.Hexs.TryGetValue(lying.hexPlacement, out Hex hex)
                              && board.WhoReservedThisHex(hex) == null
                              && board.Items.Place(item, hex);
                if (isLaid) continue;

                Debug.LogWarning($"[TeamCompLoader] {comp.name}: {itemData.name} could not lie on hex {lying.hexPlacement.column},{lying.hexPlacement.row}.");
                onLeftOver(item);
            }

            return placed;
        }
    }
}
