using System;
using System.Collections.Generic;
using TouhouMigration.Runtime.Economy;
using TouhouMigration.Runtime.Inventory;
using TouhouMigration.Runtime.Player;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    public static class MigrationShopSmokeTests
    {
        private const string ItemDataPath = "Assets/TouhouMigration/Data/Items/items.json";
        private const string StockItemId = "health_potion_small";

        [MenuItem("Touhou Migration/Tests/Run Migration Shop Smoke Tests")]
        public static void RunAll()
        {
            TestBuyUsesShopPriceWhenOpen();
            TestBuyFailsWhenClosedOrNotStocked();
            TestSellUsesShopBuyRate();
            Debug.Log("Migration shop smoke tests passed.");
        }

        private static (MigrationShop, InventoryService, MigrationPlayerProgressService) BuildShop()
        {
            ItemDatabase items = new ItemDatabase();
            AssertEqual(true, items.LoadFromPath(ItemDataPath), "Item database should load items.json.");
            InventoryService inventory = new InventoryService(items);
            MigrationPlayerProgressService progress = new MigrationPlayerProgressService();
            MigrationShopService service = new MigrationShopService(inventory, items, progress);

            MigrationShopDefinition definition = new MigrationShopDefinition(
                "test_shop", "owner", 0.5f, 6, 20,
                new List<MigrationShopItem> { new MigrationShopItem(StockItemId, 50, 99) });
            return (new MigrationShop(definition, service), inventory, progress);
        }

        private static void TestBuyUsesShopPriceWhenOpen()
        {
            (MigrationShop shop, InventoryService inventory, MigrationPlayerProgressService progress) = BuildShop();
            progress.AddCoins(100);

            ShopTransactionResult result = shop.Buy(StockItemId, 1, 10);
            AssertEqual(true, result.Success, "Buying a stocked item while open should succeed.");
            AssertEqual(-50, result.CoinDelta, "Buy should charge the shop's catalog price (50).");
            AssertEqual(50, progress.Coins, "Coins should drop by the shop price.");
            AssertEqual(1, inventory.GetItemCount(StockItemId), "The bought item should land in the inventory.");
        }

        private static void TestBuyFailsWhenClosedOrNotStocked()
        {
            (MigrationShop shop, InventoryService inventory, MigrationPlayerProgressService progress) = BuildShop();
            progress.AddCoins(100);

            AssertEqual("shop_closed", shop.Buy(StockItemId, 1, 22).FailureReason, "Buying after hours should report shop_closed.");
            AssertEqual("not_for_sale", shop.Buy("unstocked_item", 1, 10).FailureReason, "Buying an unstocked item should report not_for_sale.");
            AssertEqual(100, progress.Coins, "A failed buy should not spend coins.");
        }

        private static void TestSellUsesShopBuyRate()
        {
            (MigrationShop shop, InventoryService inventory, MigrationPlayerProgressService progress) = BuildShop();
            inventory.AddItem(StockItemId, 2);

            ShopTransactionResult open = shop.Sell(StockItemId, 1, 10);
            AssertEqual(true, open.Success, "Selling while open should succeed (payout via the shop's buy_rate).");
            AssertEqual(1, inventory.GetItemCount(StockItemId), "Selling should remove the sold item.");

            ShopTransactionResult closed = shop.Sell(StockItemId, 1, 22);
            AssertEqual("shop_closed", closed.FailureReason, "Selling after hours should report shop_closed.");
            AssertEqual(1, inventory.GetItemCount(StockItemId), "A closed-shop sell should not remove items.");
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
