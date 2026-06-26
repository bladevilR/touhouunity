using System;
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

        // Merge extra items (e.g. the active season's or festival's stock) into a shop's ledger,
        // making them buyable alongside its catalog. Adds to existing stock for an already-present item;
        // creates the shop entry if it has none yet. The owner calls this after ResetShop when in-season.
        public void MergeStock(string shopId, IReadOnlyList<MigrationShopItem> items)
        {
            if (string.IsNullOrEmpty(shopId) || items == null)
            {
                return;
            }

            if (!stock.TryGetValue(shopId, out Dictionary<string, int> shopStock))
            {
                shopStock = new Dictionary<string, int>();
                stock[shopId] = shopStock;
            }

            foreach (MigrationShopItem item in items)
            {
                if (item == null || string.IsNullOrEmpty(item.ItemId))
                {
                    continue;
                }

                shopStock[item.ItemId] = (shopStock.TryGetValue(item.ItemId, out int existing) ? existing : 0) + item.Stock;
            }
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

        // Snapshot the ledger for a save (Godot get_save_data duplicates runtime_stock). JsonUtility-safe
        // parallel-list shape, mirroring the other service snapshots.
        public ShopStockSnapshot CreateSnapshot()
        {
            ShopStockSnapshot snapshot = new ShopStockSnapshot();
            foreach (KeyValuePair<string, Dictionary<string, int>> shopPair in stock)
            {
                ShopStockEntry entry = new ShopStockEntry { shopId = shopPair.Key };
                foreach (KeyValuePair<string, int> itemPair in shopPair.Value)
                {
                    entry.itemIds.Add(itemPair.Key);
                    entry.remaining.Add(itemPair.Value);
                }

                snapshot.shops.Add(entry);
            }

            return snapshot;
        }

        public void LoadSnapshot(ShopStockSnapshot snapshot)
        {
            stock.Clear();
            if (snapshot?.shops == null)
            {
                return;
            }

            foreach (ShopStockEntry entry in snapshot.shops)
            {
                if (entry == null || string.IsNullOrEmpty(entry.shopId) || entry.itemIds == null || entry.remaining == null)
                {
                    continue;
                }

                Dictionary<string, int> shopStock = new Dictionary<string, int>();
                int count = Math.Min(entry.itemIds.Count, entry.remaining.Count);
                for (int i = 0; i < count; i++)
                {
                    if (!string.IsNullOrEmpty(entry.itemIds[i]))
                    {
                        shopStock[entry.itemIds[i]] = entry.remaining[i];
                    }
                }

                stock[entry.shopId] = shopStock;
            }
        }
    }

    // Persisted runtime shop stock (shop -> item -> remaining), JsonUtility-safe parallel lists per shop.
    [Serializable]
    public sealed class ShopStockSnapshot
    {
        public List<ShopStockEntry> shops = new List<ShopStockEntry>();
    }

    [Serializable]
    public sealed class ShopStockEntry
    {
        public string shopId = string.Empty;
        public List<string> itemIds = new List<string>();
        public List<int> remaining = new List<int>();
    }
}
