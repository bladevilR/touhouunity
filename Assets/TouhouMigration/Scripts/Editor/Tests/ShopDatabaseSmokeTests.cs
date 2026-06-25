using System;
using TouhouMigration.Runtime.Economy;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    public static class ShopDatabaseSmokeTests
    {
        private const string ShopDataPath = "Assets/TouhouMigration/Data/Shops/shops.json";

        [MenuItem("Touhou Migration/Tests/Run Shop Database Smoke Tests")]
        public static void RunAll()
        {
            TestLoadsShopsFromJson();
            TestShopQueriesAndHours();
            Debug.Log("Shop database smoke tests passed.");
        }

        private static MigrationShopDatabase LoadDatabase()
        {
            MigrationShopDatabase database = new MigrationShopDatabase();
            bool loaded = database.LoadFromPath(ShopDataPath);
            AssertEqual(true, loaded, "shops.json should load. Errors: " + string.Join("; ", database.Errors));
            return database;
        }

        private static void TestLoadsShopsFromJson()
        {
            MigrationShopDatabase database = LoadDatabase();
            AssertEqual(7, database.ShopCount, "shops.json defines 7 shops.");
            AssertEqual(true, database.GetShop("town_general") != null, "town_general should be present.");
            AssertEqual(true, database.GetShop("nonexistent") == null, "An unknown shop returns null.");
            AssertEqual("nitori", database.GetShop("nitori_combat").OwnerNpcId, "Shop owner npc id loads from shop_owner_npc_ids.");
        }

        private static void TestShopQueriesAndHours()
        {
            MigrationShopDefinition general = LoadDatabase().GetShop("town_general");
            AssertEqual(0.5f, general.BuyRate, "town_general buy_rate is 0.5.");
            AssertEqual(50, general.GetItemPrice("seed_tomato"), "town_general sells seed_tomato for 50.");
            AssertEqual(true, general.SellsItem("seed_tomato"), "town_general stocks seed_tomato.");
            AssertEqual(0, general.GetItemPrice("sword_steel"), "town_general does not sell sword_steel (price 0).");
            AssertEqual(true, general.IsOpen(10), "town_general is open at 10:00 (open_hours 6-20).");
            AssertEqual(false, general.IsOpen(22), "town_general is closed at 22:00.");
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
