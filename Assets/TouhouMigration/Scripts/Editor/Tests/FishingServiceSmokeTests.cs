using System;
using TouhouMigration.Runtime.Fishing;
using TouhouMigration.Runtime.Inventory;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    public static class FishingServiceSmokeTests
    {
        private const string ItemDataPath = "Assets/TouhouMigration/Data/Items/items.json";
        private const string FishItemId = "health_potion_small";

        [MenuItem("Touhou Migration/Tests/Run Fishing Service Smoke Tests")]
        public static void RunAll()
        {
            TestRarityWeightsMatchGodot();
            TestWeightedSelectionByRoll();
            TestCatchAddsFishToInventory();
            TestNoFishRegisteredFails();
            Debug.Log("Fishing service smoke tests passed.");
        }

        private static void TestRarityWeightsMatchGodot()
        {
            AssertEqual(50, MigrationFishingService.RarityWeight(MigrationFishRarity.Common), "Common weight is 50.");
            AssertEqual(30, MigrationFishingService.RarityWeight(MigrationFishRarity.Uncommon), "Uncommon weight is 30.");
            AssertEqual(15, MigrationFishingService.RarityWeight(MigrationFishRarity.Rare), "Rare weight is 15.");
            AssertEqual(5, MigrationFishingService.RarityWeight(MigrationFishRarity.Legendary), "Legendary weight is 5.");
        }

        private static void TestWeightedSelectionByRoll()
        {
            MigrationFishingService fishing = new MigrationFishingService(null);
            fishing.RegisterFish(new MigrationFishDefinition("carp", MigrationFishRarity.Common, string.Empty)); // weight 50
            fishing.RegisterFish(new MigrationFishDefinition("koi", MigrationFishRarity.Rare, string.Empty));     // weight 15
            AssertEqual(65, fishing.TotalWeight(), "Total weight is 50 + 15.");

            AssertEqual("carp", fishing.Catch(max => 0).FishId, "Roll 0 lands in the first (common) fish's band [0,50).");
            AssertEqual("carp", fishing.Catch(max => 49).FishId, "Roll 49 is still in the common band.");
            AssertEqual("koi", fishing.Catch(max => 50).FishId, "Roll 50 crosses into the rare band [50,65).");
            AssertEqual("koi", fishing.Catch(max => 64).FishId, "Roll 64 is the last of the rare band.");
        }

        private static void TestCatchAddsFishToInventory()
        {
            ItemDatabase items = new ItemDatabase();
            AssertEqual(true, items.LoadFromPath(ItemDataPath), "Item database should load items.json.");
            InventoryService inventory = new InventoryService(items);
            MigrationFishingService fishing = new MigrationFishingService(inventory);
            fishing.RegisterFish(new MigrationFishDefinition("carp", MigrationFishRarity.Common, FishItemId));

            MigrationFishCatchResult result = fishing.Catch(max => 0);
            AssertEqual(true, result.Success, "Catching with a registered fish should succeed.");
            AssertEqual("carp", result.FishId, "Catch result carries the fish id.");
            AssertEqual(MigrationFishRarity.Common, result.Rarity, "Catch result carries the fish rarity.");
            AssertEqual(1, inventory.GetItemCount(FishItemId), "Catch should add one fish item to the inventory.");
        }

        private static void TestNoFishRegisteredFails()
        {
            MigrationFishingService fishing = new MigrationFishingService(null);
            MigrationFishCatchResult result = fishing.Catch(max => 0);
            AssertEqual(false, result.Success, "Catching with no registered fish should fail.");
            AssertEqual("no_fish", result.FailureReason, "Failure reason should be no_fish.");
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
