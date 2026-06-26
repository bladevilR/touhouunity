using System;
using TouhouMigration.Runtime.Farming;
using TouhouMigration.Runtime.Inventory;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    // Covers MigrationFarmingManager.PlantFromInventory: planting consumes a seed_<x> item from the
    // inventory (Godot FarmingUI plant branch: has_item(seed_id) -> remove_item -> plant crop_<x>).
    public static class PlantFromInventorySmokeTests
    {
        private const string CropDataPath = "Assets/TouhouMigration/Data/Farming/crops.json";
        private const string ItemDataPath = "Assets/TouhouMigration/Data/Items/items.json";

        [MenuItem("Touhou Migration/Tests/Run Plant From Inventory Smoke Tests")]
        public static void RunAll()
        {
            TestPlantingConsumesTheSeed();
            TestPlantingWithoutSeedFails();
            Debug.Log("Plant from inventory smoke tests passed.");
        }

        private static (MigrationFarmingManager, InventoryService) Build()
        {
            MigrationCropDatabase crops = new MigrationCropDatabase();
            AssertEqual(true, crops.LoadFromPath(CropDataPath), "crops.json loads.");
            ItemDatabase items = new ItemDatabase();
            AssertEqual(true, items.LoadFromPath(ItemDataPath), "items.json loads.");

            InventoryService inventory = new InventoryService(items);
            MigrationFarmingManager manager = new MigrationFarmingManager(inventory, 4);
            manager.RegisterCropsFrom(crops);
            return (manager, inventory);
        }

        private static void TestPlantingConsumesTheSeed()
        {
            (MigrationFarmingManager manager, InventoryService inventory) = Build();
            AssertEqual(true, inventory.AddItem("seed_turnip", 2), "seed_turnip exists and is given to the player.");

            AssertEqual(true, manager.PlantFromInventory(0, "crop_turnip"), "Planting with the seed in hand succeeds.");
            AssertEqual(1, inventory.GetItemCount("seed_turnip"), "One seed_turnip was consumed by planting.");

            // The plot is now occupied, so re-planting it fails but must NOT consume another seed.
            AssertEqual(false, manager.PlantFromInventory(0, "crop_turnip"), "Re-planting an occupied plot fails.");
            AssertEqual(1, inventory.GetItemCount("seed_turnip"), "A failed plant consumes no seed.");
        }

        private static void TestPlantingWithoutSeedFails()
        {
            (MigrationFarmingManager manager, InventoryService inventory) = Build();
            AssertEqual(false, manager.PlantFromInventory(0, "crop_turnip"), "Planting with no seed fails.");
            AssertEqual(0, inventory.GetItemCount("seed_turnip"), "No seed is consumed when none is held.");
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
