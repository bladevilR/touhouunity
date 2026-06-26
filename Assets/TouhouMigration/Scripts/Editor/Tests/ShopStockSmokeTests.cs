using System;
using System.Collections.Generic;
using TouhouMigration.Runtime.Economy;
using TouhouMigration.Runtime.Inventory;
using TouhouMigration.Runtime.Player;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    // The persistent runtime shop-stock ledger (Godot ShopManager.runtime_stock): per-shop, per-item
    // remaining stock that decrements on buy, refunds on sell-back, refreshes per day, and persists.
    public static class ShopStockSmokeTests
    {
        private const string ItemDataPath = "Assets/TouhouMigration/Data/Items/items.json";
        private const string ShopDataPath = "Assets/TouhouMigration/Data/Shops/shops.json";

        [MenuItem("Touhou Migration/Tests/Run Shop Stock Smoke Tests")]
        public static void RunAll()
        {
            TestInitializeSeedsStockFromCatalog();
            TestBuyDecrementsStockAndPersistsAcrossReopen();
            TestBuyBlockedWhenOutOfStock();
            TestSellRefundsStockWhenShopCarriesItem();
            TestSellLeavesStockAloneForUnstockedItem();
            TestRefreshShopRestoresStock();
            TestSnapshotRoundTripsDecrementedStock();
            TestMergeStockAddsSeasonalItems();
            Debug.Log("Shop stock smoke tests passed.");
        }

        private static void TestInitializeSeedsStockFromCatalog()
        {
            MigrationShopDatabase database = LoadShopDatabase();
            MigrationShopStock stock = new MigrationShopStock();
            stock.InitializeFrom(database);

            AssertEqual(3, stock.GetStock("town_blacksmith", "sword_basic"),
                "Blacksmith starts with 3 basic swords (from shops.json stock).");
            AssertEqual(99, stock.GetStock("town_general", "seed_tomato"),
                "General store starts with 99 tomato seeds.");
            AssertEqual(0, stock.GetStock("town_general", "sword_basic"),
                "A shop reports 0 stock for an item it does not carry.");
            AssertEqual(true, stock.HasItem("town_blacksmith", "sword_basic"),
                "HasItem is true for a carried item.");
            AssertEqual(false, stock.HasItem("town_general", "sword_basic"),
                "HasItem is false for an uncarried item.");
        }

        private static void TestBuyDecrementsStockAndPersistsAcrossReopen()
        {
            (ItemDatabase items, InventoryService inventory, MigrationPlayerProgressService progress) = BuildContext();
            (string itemId, int price) = FirstPricedItem(items);
            progress.AddCoins(price * 10);

            MigrationShopStock stock = new MigrationShopStock();
            MigrationShopDefinition def = SingleItemShop("test_shop", itemId, price, 5);
            stock.ResetShop("test_shop", def.Items);
            MigrationShopService service = new MigrationShopService(inventory, items, progress);

            MigrationShop shop = new MigrationShop(def, service, stock);
            ShopTransactionResult result = shop.Buy(itemId, 2, 12);

            AssertEqual(true, result.Success, "Buying within stock succeeds.");
            AssertEqual(3, stock.GetStock("test_shop", itemId), "Buying 2 of 5 leaves 3 in stock.");

            // A freshly built shop over the SAME ledger still sees the decremented stock (persistence).
            MigrationShop reopened = new MigrationShop(def, service, stock);
            AssertEqual(3, stock.GetStock("test_shop", itemId),
                "Reopening the shop preserves the decremented stock.");
            ShopTransactionResult again = reopened.Buy(itemId, 1, 12);
            AssertEqual(true, again.Success, "The reopened shop can still buy from remaining stock.");
            AssertEqual(2, stock.GetStock("test_shop", itemId), "Stock keeps decrementing across reopen.");
        }

        private static void TestBuyBlockedWhenOutOfStock()
        {
            (ItemDatabase items, InventoryService inventory, MigrationPlayerProgressService progress) = BuildContext();
            (string itemId, int price) = FirstPricedItem(items);
            progress.AddCoins(price * 10);

            MigrationShopStock stock = new MigrationShopStock();
            MigrationShopDefinition def = SingleItemShop("test_shop", itemId, price, 1);
            stock.ResetShop("test_shop", def.Items);
            MigrationShopService service = new MigrationShopService(inventory, items, progress);
            MigrationShop shop = new MigrationShop(def, service, stock);

            AssertEqual(true, shop.Buy(itemId, 1, 12).Success, "The one in-stock unit can be bought.");
            int coinsBefore = progress.Coins;

            ShopTransactionResult soldOut = shop.Buy(itemId, 1, 12);
            AssertEqual(false, soldOut.Success, "Buying past the stock fails.");
            AssertEqual("out_of_stock", soldOut.FailureReason, "An out-of-stock buy reports out_of_stock.");
            AssertEqual(coinsBefore, progress.Coins, "A blocked buy spends no coins.");
            AssertEqual(0, stock.GetStock("test_shop", itemId), "Stock stays at 0 after a blocked buy.");
        }

        private static void TestSellRefundsStockWhenShopCarriesItem()
        {
            (ItemDatabase items, InventoryService inventory, MigrationPlayerProgressService progress) = BuildContext();
            (string itemId, int price) = FirstPricedItem(items);
            inventory.AddItem(itemId, 2);

            MigrationShopStock stock = new MigrationShopStock();
            MigrationShopDefinition def = SingleItemShop("test_shop", itemId, price, 4);
            stock.ResetShop("test_shop", def.Items);
            MigrationShopService service = new MigrationShopService(inventory, items, progress);
            MigrationShop shop = new MigrationShop(def, service, stock);

            ShopTransactionResult result = shop.Sell(itemId, 1, 12);
            AssertEqual(true, result.Success, "Selling an owned item succeeds.");
            AssertEqual(5, stock.GetStock("test_shop", itemId),
                "Selling an item the shop carries adds it back to stock (Godot fidelity).");
        }

        private static void TestSellLeavesStockAloneForUnstockedItem()
        {
            (ItemDatabase items, InventoryService inventory, MigrationPlayerProgressService progress) = BuildContext();
            (string itemId, int price) = FirstPricedItem(items);
            inventory.AddItem(itemId, 1);

            MigrationShopStock stock = new MigrationShopStock();
            // The shop carries nothing — selling should not invent a stock entry.
            MigrationShopDefinition def = new MigrationShopDefinition("empty_shop", string.Empty, 0.5f, 0, 24,
                new List<MigrationShopItem>());
            stock.ResetShop("empty_shop", def.Items);
            MigrationShopService service = new MigrationShopService(inventory, items, progress);
            MigrationShop shop = new MigrationShop(def, service, stock);

            ShopTransactionResult result = shop.Sell(itemId, 1, 12);
            AssertEqual(true, result.Success, "Selling still succeeds even if the shop does not stock the item.");
            AssertEqual(false, stock.HasItem("empty_shop", itemId),
                "Selling an item the shop never carried does not create a stock entry.");
        }

        private static void TestRefreshShopRestoresStock()
        {
            (ItemDatabase items, InventoryService inventory, MigrationPlayerProgressService progress) = BuildContext();
            (string itemId, int price) = FirstPricedItem(items);
            progress.AddCoins(price * 10);

            MigrationShopStock stock = new MigrationShopStock();
            MigrationShopDefinition def = SingleItemShop("test_shop", itemId, price, 3);
            stock.ResetShop("test_shop", def.Items);
            MigrationShopService service = new MigrationShopService(inventory, items, progress);
            MigrationShop shop = new MigrationShop(def, service, stock);

            shop.Buy(itemId, 2, 12);
            AssertEqual(1, stock.GetStock("test_shop", itemId), "Stock is down to 1 after buying 2.");

            stock.ResetShop("test_shop", def.Items);
            AssertEqual(3, stock.GetStock("test_shop", itemId), "A daily refresh restores stock to the catalog level.");
        }

        private static void TestSnapshotRoundTripsDecrementedStock()
        {
            (ItemDatabase items, InventoryService inventory, MigrationPlayerProgressService progress) = BuildContext();
            (string itemId, int price) = FirstPricedItem(items);
            progress.AddCoins(price * 10);

            MigrationShopStock stock = new MigrationShopStock();
            MigrationShopDefinition def = SingleItemShop("test_shop", itemId, price, 5);
            stock.ResetShop("test_shop", def.Items);
            MigrationShopService service = new MigrationShopService(inventory, items, progress);
            new MigrationShop(def, service, stock).Buy(itemId, 3, 12);

            Dictionary<string, Dictionary<string, int>> snapshot = stock.CaptureSnapshot();

            MigrationShopStock restored = new MigrationShopStock();
            restored.RestoreSnapshot(snapshot);
            AssertEqual(2, restored.GetStock("test_shop", itemId),
                "A restored ledger keeps the decremented stock (2 of 5 left).");

            // The snapshot is a deep copy — mutating the source ledger does not change it.
            stock.ResetShop("test_shop", def.Items);
            AssertEqual(2, snapshot["test_shop"][itemId], "The snapshot is an independent deep copy.");
        }

        private static void TestMergeStockAddsSeasonalItems()
        {
            MigrationShopStock stock = new MigrationShopStock();
            List<MigrationShopItem> baseItems = new List<MigrationShopItem>
            {
                new MigrationShopItem("seed_tomato", 50, 99)
            };
            stock.ResetShop("town_general", baseItems);

            List<MigrationShopItem> seasonal = new List<MigrationShopItem>
            {
                new MigrationShopItem("seed_cherry", 200, 10)
            };
            stock.MergeStock("town_general", seasonal);

            AssertEqual(10, stock.GetStock("town_general", "seed_cherry"),
                "Merged seasonal stock becomes buyable.");
            AssertEqual(99, stock.GetStock("town_general", "seed_tomato"),
                "Merging seasonal items leaves the base stock intact.");

            // Merging into a shop with no ledger entry yet creates it.
            stock.MergeStock("pop_up_stall", seasonal);
            AssertEqual(10, stock.GetStock("pop_up_stall", "seed_cherry"),
                "Merging into an unseeded shop creates its stock entry.");
        }

        private static MigrationShopDefinition SingleItemShop(string shopId, string itemId, int price, int stock)
        {
            return new MigrationShopDefinition(shopId, string.Empty, 0.5f, 0, 24,
                new List<MigrationShopItem> { new MigrationShopItem(itemId, price, stock) });
        }

        private static MigrationShopDatabase LoadShopDatabase()
        {
            MigrationShopDatabase database = new MigrationShopDatabase();
            AssertEqual(true, database.LoadFromPath(ShopDataPath),
                "shops.json should load. Errors: " + string.Join("; ", database.Errors));
            return database;
        }

        private static (ItemDatabase, InventoryService, MigrationPlayerProgressService) BuildContext()
        {
            ItemDatabase items = new ItemDatabase();
            AssertEqual(true, items.LoadFromPath(ItemDataPath), "Item database should load items.json.");
            InventoryService inventory = new InventoryService(items);
            MigrationPlayerProgressService progress = new MigrationPlayerProgressService();
            return (items, inventory, progress);
        }

        private static (string, int) FirstPricedItem(ItemDatabase items)
        {
            foreach (KeyValuePair<string, ItemDefinition> pair in items.GetAllItems())
            {
                if (pair.Value != null && pair.Value.Price > 0)
                {
                    return (pair.Key, pair.Value.Price);
                }
            }

            throw new Exception("items.json should contain at least one priced item for shop-stock tests.");
        }

        private static void AssertEqual<T>(T expected, T actual, string message)
        {
            if (!Equals(expected, actual))
            {
                throw new Exception($"{message} Expected: {expected}. Actual: {actual}.");
            }
        }
    }
}
