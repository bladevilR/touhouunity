using System;
using TouhouMigration.Runtime.Farming;
using TouhouMigration.Runtime.Inventory;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    public static class FarmingManagerSmokeTests
    {
        private const string ItemDataPath = "Assets/TouhouMigration/Data/Items/items.json";
        // A known stackable item (used by the dialogue inventory smoke tests) to stand in as produce.
        private const string ProduceItemId = "health_potion_small";

        [MenuItem("Touhou Migration/Tests/Run Farming Manager Smoke Tests")]
        public static void RunAll()
        {
            TestPlantGrowHarvestAddsProduceToInventory();
            TestHarvestBeforeReadyFails();
            TestPlantUnknownCropFails();
            TestYieldRollHonorsRange();
            Debug.Log("Farming manager smoke tests passed.");
        }

        private static InventoryService BuildInventory()
        {
            ItemDatabase items = new ItemDatabase();
            AssertEqual(true, items.LoadFromPath(ItemDataPath), "Item database should load items.json.");
            return new InventoryService(items);
        }

        private static void TestPlantGrowHarvestAddsProduceToInventory()
        {
            InventoryService inventory = BuildInventory();
            MigrationFarmingManager manager = new MigrationFarmingManager(inventory, 4);
            manager.RegisterCrop(new MigrationCropDefinition("crop_turnip", 2, false, ProduceItemId, 3, 3));

            AssertEqual(true, manager.Plant(0, "crop_turnip"), "Planting a registered crop should succeed.");
            manager.AdvanceDay();
            manager.AdvanceDay();

            MigrationHarvestResult result = manager.Harvest(0, (lo, hi) => lo);
            AssertEqual(true, result.Success, "Harvesting a ready crop should succeed.");
            AssertEqual(ProduceItemId, result.ItemId, "Harvest result should carry the crop's produce id.");
            AssertEqual(3, result.Amount, "Harvest should yield the crop's fixed amount.");
            AssertEqual(3, inventory.GetItemCount(ProduceItemId), "Harvest should add the produce to the inventory.");
            AssertEqual(false, manager.GetPlot(0).HasCrop, "Harvesting should clear the plot.");
        }

        private static void TestHarvestBeforeReadyFails()
        {
            InventoryService inventory = BuildInventory();
            MigrationFarmingManager manager = new MigrationFarmingManager(inventory, 2);
            manager.RegisterCrop(new MigrationCropDefinition("crop_turnip", 3, false, ProduceItemId, 1, 1));
            manager.Plant(0, "crop_turnip");

            MigrationHarvestResult result = manager.Harvest(0, (lo, hi) => lo);
            AssertEqual(false, result.Success, "Harvesting an unready crop should fail.");
            AssertEqual(0, inventory.GetItemCount(ProduceItemId), "A failed harvest should not add produce.");
        }

        private static void TestPlantUnknownCropFails()
        {
            InventoryService inventory = BuildInventory();
            MigrationFarmingManager manager = new MigrationFarmingManager(inventory, 1);
            AssertEqual(false, manager.Plant(0, "nonexistent_crop"), "Planting an unregistered crop should fail.");
        }

        private static void TestYieldRollHonorsRange()
        {
            InventoryService inventory = BuildInventory();
            MigrationFarmingManager manager = new MigrationFarmingManager(inventory, 2);
            manager.RegisterCrop(new MigrationCropDefinition("crop_big", 1, false, ProduceItemId, 2, 5));

            manager.Plant(0, "crop_big");
            manager.AdvanceDay();
            // randomRange(min, max+1): returning the low bound yields min; returning (hi-1) yields max.
            MigrationHarvestResult low = manager.Harvest(0, (lo, hi) => lo);
            AssertEqual(2, low.Amount, "Yield uses the low bound of the range.");

            manager.Plant(1, "crop_big");
            manager.AdvanceDay();
            MigrationHarvestResult high = manager.Harvest(1, (lo, hi) => hi - 1);
            AssertEqual(5, high.Amount, "Yield uses the high (inclusive) bound of the range.");
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
