using System;
using System.Collections.Generic;
using TouhouMigration.Runtime.Economy;
using TouhouMigration.Runtime.Inventory;
using TouhouMigration.Runtime.Player;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    public static class ShopServiceSmokeTests
    {
        private const string ItemDataPath = "Assets/TouhouMigration/Data/Items/items.json";

        [MenuItem("Touhou Migration/Tests/Run Shop Service Smoke Tests")]
        public static void RunAll()
        {
            TestBuyDeductsCoinsAndGrantsItem();
            TestBuyFailsWithoutEnoughCoins();
            TestSellPaysBuyRateAndRemovesItem();
            TestSellFailsWithoutItem();
            Debug.Log("Shop service smoke tests passed.");
        }

        private static void TestBuyDeductsCoinsAndGrantsItem()
        {
            (ItemDatabase items, InventoryService inventory, MigrationPlayerProgressService progress) = BuildShopContext();
            (string itemId, int price) = FirstPricedItem(items);

            progress.AddCoins(price * 3);
            MigrationShopService shop = new MigrationShopService(inventory, items, progress);

            ShopTransactionResult result = shop.Buy(itemId, 2);

            AssertEqual(true, result.Success, "Buy should succeed when the player can afford it.");
            AssertEqual(-price * 2, result.CoinDelta, "Buy should report the spent coins as a negative delta.");
            AssertEqual(2, inventory.GetItemCount(itemId), "Buy should add the purchased items to the inventory.");
            AssertEqual(price, progress.Coins, "Buy should deduct price*quantity from the player's coins.");
        }

        private static void TestBuyFailsWithoutEnoughCoins()
        {
            (ItemDatabase items, InventoryService inventory, MigrationPlayerProgressService progress) = BuildShopContext();
            (string itemId, int price) = FirstPricedItem(items);

            progress.AddCoins(price - 1);
            MigrationShopService shop = new MigrationShopService(inventory, items, progress);

            ShopTransactionResult result = shop.Buy(itemId, 1);

            AssertEqual(false, result.Success, "Buy should fail when the player cannot afford the item.");
            AssertEqual("insufficient_funds", result.FailureReason, "Buy failure should report insufficient funds.");
            AssertEqual(0, inventory.GetItemCount(itemId), "A failed buy should not grant items.");
            AssertEqual(price - 1, progress.Coins, "A failed buy should not spend coins.");
        }

        private static void TestSellPaysBuyRateAndRemovesItem()
        {
            (ItemDatabase items, InventoryService inventory, MigrationPlayerProgressService progress) = BuildShopContext();
            (string itemId, int price) = FirstPricedItem(items);

            inventory.AddItem(itemId, 2);
            MigrationShopService shop = new MigrationShopService(inventory, items, progress);
            int expectedPayout = (int)Math.Floor(price * MigrationShopService.DefaultBuyRate);

            ShopTransactionResult result = shop.Sell(itemId, 1);

            AssertEqual(true, result.Success, "Sell should succeed when the player owns the item.");
            AssertEqual(expectedPayout, result.CoinDelta, "Sell should pay floor(price * buy_rate) per unit.");
            AssertEqual(expectedPayout, progress.Coins, "Sell should credit the payout to the player's coins.");
            AssertEqual(1, inventory.GetItemCount(itemId), "Sell should remove the sold item from the inventory.");
        }

        private static void TestSellFailsWithoutItem()
        {
            (ItemDatabase items, InventoryService inventory, MigrationPlayerProgressService progress) = BuildShopContext();
            (string itemId, int _) = FirstPricedItem(items);

            MigrationShopService shop = new MigrationShopService(inventory, items, progress);

            ShopTransactionResult result = shop.Sell(itemId, 1);

            AssertEqual(false, result.Success, "Sell should fail when the player does not own the item.");
            AssertEqual("insufficient_items", result.FailureReason, "Sell failure should report insufficient items.");
            AssertEqual(0, progress.Coins, "A failed sell should not credit coins.");
        }

        private static (ItemDatabase, InventoryService, MigrationPlayerProgressService) BuildShopContext()
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

            throw new Exception("items.json should contain at least one item with a positive price for shop tests.");
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
