using System;
using System.Collections.Generic;
using UnityEngine;
using MagicSchool.Contracts;

namespace MagicSchool.Items
{
    // this is where the item was registered in the game.
    public static class ItemLibrary
    {
        // a pair of ItemIdEnum & Modifiers
        private static readonly Dictionary<ItemIdEnum, Func<ICustomModifier>> Builders =
            new Dictionary<ItemIdEnum, Func<ICustomModifier>>
            {
                // attack
                { ItemIdEnum.Whetstone,       Whetstone.Build },
                { ItemIdEnum.DuelistGauntlet, DuelistGauntlet.Build },
                { ItemIdEnum.HuntersCord,     HuntersCord.Build },
                { ItemIdEnum.ReaversEdge,     ReaversEdge.Build },
                { ItemIdEnum.RunedEdge,       RunedEdge.Build },

                // magic
                { ItemIdEnum.ApprenticeWand,  ApprenticeWand.Build },
                { ItemIdEnum.ArchmageTome,    ArchmageTome.Build },
                { ItemIdEnum.SagesFocus,      SagesFocus.Build },
                { ItemIdEnum.Stormglass,      Stormglass.Build },
                { ItemIdEnum.LeyBattery,      LeyBattery.Build },

                // defense
                { ItemIdEnum.IronPlate,       IronPlate.Build },
                { ItemIdEnum.OakenCharm,      OakenCharm.Build },
                { ItemIdEnum.BulwarkCrest,    BulwarkCrest.Build },
                { ItemIdEnum.AegisShard,      AegisShard.Build },
                { ItemIdEnum.WardensVow,      WardensVow.Build },

                // utility
                { ItemIdEnum.ChaliceOfDawn,   ChaliceOfDawn.Build },
                { ItemIdEnum.ScholarsSash,    ScholarsSash.Build },
                { ItemIdEnum.FarsightLens,    FarsightLens.Build },
                { ItemIdEnum.LongshotQuiver,  LongshotQuiver.Build },
                { ItemIdEnum.VitalisWeave,    VitalisWeave.Build },
            };

        /// Return a modifier that match itemID.
        public static ICustomModifier Resolve(ItemIdEnum itemId)
        {
            if (itemId == ItemIdEnum.None) return null;

            if (!Builders.TryGetValue(itemId, out var build))
            {
                Debug.LogError($"[ItemLibrary] no builder for {itemId}. The item will grant nothing.");
                return null;
            }

            return build();
        }
    }
}
