using System;

namespace TouhouMigration.Runtime.Economy
{
    // One item a shop sells (Godot shops.json item entry): the item id, its price, and stock.
    public sealed class MigrationShopItem
    {
        public string ItemId { get; }
        public int Price { get; }
        public int Stock { get; }

        public MigrationShopItem(string itemId, int price, int stock)
        {
            ItemId = itemId ?? string.Empty;
            Price = Math.Max(0, price);
            Stock = Math.Max(0, stock);
        }
    }
}
