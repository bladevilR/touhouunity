using System.Collections.Generic;
using TouhouMigration.Runtime.Foundation;

namespace TouhouMigration.Runtime.Economy
{
    // A shop (Godot shops.json entry): its owner NPC, sell-back buy_rate, open hours, and the items it
    // stocks. Open-hour checks reuse the shared MigrationHourRange.
    public sealed class MigrationShopDefinition
    {
        public string ShopId { get; }
        public string OwnerNpcId { get; }
        public float BuyRate { get; }
        public int OpenHourStart { get; }
        public int OpenHourEnd { get; }

        private readonly List<MigrationShopItem> items;
        public IReadOnlyList<MigrationShopItem> Items => items;

        public MigrationShopDefinition(string shopId, string ownerNpcId, float buyRate, int openHourStart, int openHourEnd, List<MigrationShopItem> items)
        {
            ShopId = shopId ?? string.Empty;
            OwnerNpcId = ownerNpcId ?? string.Empty;
            BuyRate = buyRate;
            OpenHourStart = openHourStart;
            OpenHourEnd = openHourEnd;
            this.items = items ?? new List<MigrationShopItem>();
        }

        public bool IsOpen(int hour)
        {
            return MigrationHourRange.Contains(OpenHourStart, OpenHourEnd, hour);
        }

        public bool SellsItem(string itemId)
        {
            foreach (MigrationShopItem item in items)
            {
                if (item.ItemId == itemId)
                {
                    return true;
                }
            }

            return false;
        }

        public int GetItemPrice(string itemId)
        {
            foreach (MigrationShopItem item in items)
            {
                if (item.ItemId == itemId)
                {
                    return item.Price;
                }
            }

            return 0;
        }
    }
}
