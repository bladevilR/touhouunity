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
            TestSeasonalItemsLoadPerSeason();
            TestFestivalItemsLoadPerFestival();
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

        private static void TestSeasonalItemsLoadPerSeason()
        {
            MigrationShopDatabase database = LoadDatabase();

            var spring = database.GetSeasonalItems("spring");
            AssertEqual(2, spring.Count, "Spring has 2 seasonal items.");
            AssertEqual("seed_cherry", spring[0].ItemId, "Spring's first seasonal item is seed_cherry.");
            AssertEqual(200, spring[0].Price, "seed_cherry costs 200 in spring.");

            AssertEqual("warm_coat", database.GetSeasonalItems("winter")[1].ItemId, "Winter stocks warm_coat.");
            AssertEqual(0, database.GetSeasonalItems("unknown_season").Count, "An unmapped season has no seasonal items.");
        }

        private static void TestFestivalItemsLoadPerFestival()
        {
            MigrationShopDatabase database = LoadDatabase();

            var flower = database.GetFestivalItems("flower_festival");
            AssertEqual(1, flower.Count, "The flower festival stocks 1 item.");
            AssertEqual("sakura_branch", flower[0].ItemId, "The flower festival sells sakura_branch.");
            AssertEqual(300, flower[0].Price, "sakura_branch costs 300.");

            AssertEqual("moon_cake", database.GetFestivalItems("moon_festival")[0].ItemId,
                "The moon festival stocks moon_cake.");
            AssertEqual(0, database.GetFestivalItems("unknown_festival").Count,
                "An unmapped festival has no items.");
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
