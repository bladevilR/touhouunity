using System;
using TouhouMigration.Runtime.Economy;
using TouhouMigration.Runtime.Inventory;
using TouhouMigration.Runtime.Player;
using TouhouMigration.Runtime.UI;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    // The scoped shop modal controller: opening it for a shop id, listing that shop's catalog, and
    // buying/selling through it (open-hours/price/buy_rate gated via the live services). The underlying
    // buy/sell economy is covered by MigrationShopSmokeTests; this exercises the controller surface that
    // the shopkeeper interactor drives.
    public static class ShopControllerSmokeTests
    {
        private const string ItemDataPath = "Assets/TouhouMigration/Data/Items/items.json";
        private const string ShopDataPath = "Assets/TouhouMigration/Data/Shops/shops.json";
        private const string OpenShopId = "town_general";
        private const string StockItemId = "seed_tomato"; // town_general stocks this at price 50.

        [MenuItem("Touhou Migration/Tests/Run Shop Controller Smoke Tests")]
        public static void RunAll()
        {
            TestOpenForKnownShopListsCatalog();
            TestOpenForUnknownShopFails();
            TestBuyThroughControllerSpendsCoinsWhenOpen();
            TestBuyThroughControllerFailsWhenClosed();
            TestSellThroughControllerPaysOut();
            Debug.Log("Shop controller smoke tests passed.");
        }

        private static (MigrationShopController, InventoryService, MigrationPlayerProgressService, int[]) Build()
        {
            ItemDatabase items = new ItemDatabase();
            AssertEqual(true, items.LoadFromPath(ItemDataPath), "Item database should load items.json.");
            MigrationShopDatabase shops = new MigrationShopDatabase();
            AssertEqual(true, shops.LoadFromPath(ShopDataPath), "Shop database should load shops.json.");

            InventoryService inventory = new InventoryService(items);
            MigrationPlayerProgressService progress = new MigrationPlayerProgressService();
            MigrationShopService service = new MigrationShopService(inventory, items, progress);

            int[] hour = { 10 }; // town_general is open 6-20.
            MigrationShopController controller = new GameObject("ShopControllerTest").AddComponent<MigrationShopController>();
            controller.Bind(shops, service, inventory, items, progress, () => hour[0]);
            return (controller, inventory, progress, hour);
        }

        private static void TestOpenForKnownShopListsCatalog()
        {
            (MigrationShopController controller, _, _, _) = Build();
            try
            {
                AssertEqual(true, controller.OpenForShop(OpenShopId), "Opening a known shop should succeed.");
                AssertEqual(true, controller.IsOpen, "The shop modal should be open after OpenForShop.");
                AssertEqual(OpenShopId, controller.ShopId, "The controller should record the opened shop id.");
                AssertEqual(true, controller.ItemCount > 0, "A known shop should list at least one item.");
                AssertEqual(true, controller.IsShopOpenNow, "town_general is open at hour 10.");
                controller.Close();
                AssertEqual(false, controller.IsOpen, "Close should dismiss the modal.");
            }
            finally
            {
                Cleanup(controller);
            }
        }

        private static void TestOpenForUnknownShopFails()
        {
            (MigrationShopController controller, _, _, _) = Build();
            try
            {
                AssertEqual(false, controller.OpenForShop("no_such_shop"), "Opening an unknown shop should fail.");
                AssertEqual(false, controller.IsOpen, "An unknown shop should not open the modal.");
            }
            finally
            {
                Cleanup(controller);
            }
        }

        private static void TestBuyThroughControllerSpendsCoinsWhenOpen()
        {
            (MigrationShopController controller, InventoryService inventory, MigrationPlayerProgressService progress, _) = Build();
            try
            {
                progress.AddCoins(100);
                controller.OpenForShop(OpenShopId);

                ShopTransactionResult result = controller.Buy(StockItemId);
                AssertEqual(true, result.Success, "Buying a stocked item while open should succeed.");
                AssertEqual(-50, result.CoinDelta, "Buy should charge the shop's catalog price (50).");
                AssertEqual(50, progress.Coins, "Coins should drop by the shop price.");
                AssertEqual(1, inventory.GetItemCount(StockItemId), "The bought item should land in the inventory.");
            }
            finally
            {
                Cleanup(controller);
            }
        }

        private static void TestBuyThroughControllerFailsWhenClosed()
        {
            (MigrationShopController controller, _, MigrationPlayerProgressService progress, int[] hour) = Build();
            try
            {
                progress.AddCoins(100);
                controller.OpenForShop(OpenShopId);
                hour[0] = 22; // after close (6-20).

                ShopTransactionResult result = controller.Buy(StockItemId);
                AssertEqual("shop_closed", result.FailureReason, "Buying after hours should report shop_closed.");
                AssertEqual(100, progress.Coins, "A closed-shop buy should not spend coins.");
                AssertEqual(false, controller.IsShopOpenNow, "The shop reports closed at hour 22.");
            }
            finally
            {
                Cleanup(controller);
            }
        }

        private static void TestSellThroughControllerPaysOut()
        {
            (MigrationShopController controller, InventoryService inventory, MigrationPlayerProgressService progress, _) = Build();
            try
            {
                inventory.AddItem(StockItemId, 2);
                controller.OpenForShop(OpenShopId);

                ShopTransactionResult result = controller.Sell(StockItemId);
                AssertEqual(true, result.Success, "Selling while open should succeed.");
                AssertEqual(true, result.CoinDelta > 0, "Selling should pay out coins (price * buy_rate).");
                AssertEqual(1, inventory.GetItemCount(StockItemId), "Selling should remove the sold item.");
            }
            finally
            {
                Cleanup(controller);
            }
        }

        private static void Cleanup(MigrationShopController controller)
        {
            if (controller != null)
            {
                UnityEngine.Object.DestroyImmediate(controller.gameObject);
            }
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
