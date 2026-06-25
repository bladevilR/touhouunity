using System;
using TouhouMigration.Runtime.Fishing;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    public static class FishDatabaseSmokeTests
    {
        private const string FishDataPath = "Assets/TouhouMigration/Data/Fishing/fish.json";

        [MenuItem("Touhou Migration/Tests/Run Fish Database Smoke Tests")]
        public static void RunAll()
        {
            TestLoadsFishFromJson();
            TestRegisterIntoFishingService();
            Debug.Log("Fish database smoke tests passed.");
        }

        private static MigrationFishDatabase LoadDatabase()
        {
            MigrationFishDatabase database = new MigrationFishDatabase();
            bool loaded = database.LoadFromPath(FishDataPath);
            AssertEqual(true, loaded, "fish.json should load. Errors: " + string.Join("; ", database.Errors));
            return database;
        }

        private static void TestLoadsFishFromJson()
        {
            MigrationFishDatabase database = LoadDatabase();
            AssertEqual(15, database.FishCount, "fish.json defines 15 fish.");

            MigrationFishDefinition carp = database.GetFish("crucian_carp");
            AssertEqual(true, carp != null, "crucian_carp should be present.");
            AssertEqual(MigrationFishRarity.Common, carp.Rarity, "crucian_carp is common.");
            AssertEqual("crucian_carp", carp.ItemId, "Fish item id defaults to the fish id.");
            AssertEqual(MigrationFishRarity.Legendary, database.GetFish("phantom_fish").Rarity, "phantom_fish is legendary.");
            AssertEqual(true, database.GetFish("nonexistent") == null, "An unknown fish returns null.");
        }

        private static void TestRegisterIntoFishingService()
        {
            MigrationFishDatabase database = LoadDatabase();
            MigrationFishingService fishing = new MigrationFishingService(null);
            fishing.RegisterFrom(database);

            AssertEqual(true, fishing.TotalWeight() > 0, "Registering the catalog gives the fishing service catchable fish.");
            MigrationFishCatchResult result = fishing.Catch(max => 0);
            AssertEqual(true, result.Success, "A catch from the registered catalog should succeed.");
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
