using System.Collections.Generic;

namespace TouhouMigration.Runtime.Economy
{
    // The persistent runtime shop-stock ledger (Godot ShopManager.runtime_stock): shop_id -> item_id ->
    // remaining stock. Stock decrements on buy, refunds on sell-back (only for items the shop carries),
    // refreshes per day via ResetShop, and round-trips through a snapshot for saves. UnityEngine-free so
    // it stays unit-testable; lives across shop open/close (one ledger, many MigrationShop instances).
    public sealed class MigrationShopStock
    {
        private readonly Dictionary<string, Dictionary<string, int>> stock =
            new Dictionary<string, Dictionary<string, int>>();

        // Seed (or daily-refresh) every shop's stock from the catalog.
        public void InitializeFrom(MigrationShopDatabase database)
        {
            if (database == null)
            {
                return;
            }

            foreach (KeyValuePair<string, MigrationShopDefinition> pair in database.GetAllShops())
            {
                ResetShop(pair.Key, pair.Value?.Items);
            }
        }

        // Reset one shop's stock to its catalog levels (Godot _reset_shop_stock). Used on day start.
        public void ResetShop(string shopId, IReadOnlyList<MigrationShopItem> items)
        {
            if (string.IsNullOrEmpty(shopId))
            {
                return;
            }

            Dictionary<string, int> shopStock = new Dictionary<string, int>();
            if (items != null)
            {
                foreach (MigrationShopItem item in items)
                {
                    if (item != null && !string.IsNullOrEmpty(item.ItemId))
                    {
                        shopStock[item.ItemId] = item.Stock;
                    }
                }
            }

            stock[shopId] = shopStock;
        }

        public int GetStock(string shopId, string itemId)
        {
            return shopId != null && itemId != null
                && stock.TryGetValue(shopId, out Dictionary<string, int> shopStock)
                && shopStock.TryGetValue(itemId, out int remaining)
                ? remaining
                : 0;
        }

        public bool HasItem(string shopId, string itemId)
        {
            return shopId != null && itemId != null
                && stock.TryGetValue(shopId, out Dictionary<string, int> shopStock)
                && shopStock.ContainsKey(itemId);
        }

        // Decrement stock for a buy. Fails (returns false, no change) if the shop lacks the item or stock.
        public bool TryConsume(string shopId, string itemId, int amount)
        {
            if (amount <= 0 || shopId == null || itemId == null
                || !stock.TryGetValue(shopId, out Dictionary<string, int> shopStock)
                || !shopStock.TryGetValue(itemId, out int remaining)
                || remaining < amount)
            {
                return false;
            }

            shopStock[itemId] = remaining - amount;
            return true;
        }

        // Refund stock on a sell-back, but only for items the shop already carries (Godot fidelity:
        // sell_item only bumps runtime_stock when the shop has that item id).
        public void Restock(string shopId, string itemId, int amount)
        {
            if (amount <= 0 || shopId == null || itemId == null
                || !stock.TryGetValue(shopId, out Dictionary<string, int> shopStock)
                || !shopStock.TryGetValue(itemId, out int remaining))
            {
                return;
            }

            shopStock[itemId] = remaining + amount;
        }

        // Deep-copy the ledger for a save (Godot get_save_data duplicates runtime_stock).
        public Dictionary<string, Dictionary<string, int>> CaptureSnapshot()
        {
            Dictionary<string, Dictionary<string, int>> snapshot =
                new Dictionary<string, Dictionary<string, int>>();
            foreach (KeyValuePair<string, Dictionary<string, int>> shopPair in stock)
            {
                snapshot[shopPair.Key] = new Dictionary<string, int>(shopPair.Value);
            }

            return snapshot;
        }

        public void RestoreSnapshot(Dictionary<string, Dictionary<string, int>> snapshot)
        {
            stock.Clear();
            if (snapshot == null)
            {
                return;
            }

            foreach (KeyValuePair<string, Dictionary<string, int>> shopPair in snapshot)
            {
                stock[shopPair.Key] = new Dictionary<string, int>(shopPair.Value ?? new Dictionary<string, int>());
            }
        }
    }
}
