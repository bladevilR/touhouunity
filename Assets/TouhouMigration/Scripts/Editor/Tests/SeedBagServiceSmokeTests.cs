using System;
using TouhouMigration.Runtime.Farming;
using TouhouMigration.Runtime.Inventory;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    // Covers MigrationSeedBagService: opening a shop-bought seed bag consumes the bag item and grants the
    // rolled seeds to the inventory (composes the seed-bag gacha with the inventory).
    public static class SeedBagServiceSmokeTests
    {
        private const string CropDataPath = "Assets/TouhouMigration/Data/Farming/crops.json";
        private const string ItemDataPath = "Assets/TouhouMigration/Data/Items/items.json";

        [MenuItem("Touhou Migration/Tests/Run Seed Bag Service Smoke Tests")]
        public static void RunAll()
        {
            TestOpeningConsumesBagAndGrantsSeeds();
            TestNoBagFails();
            TestUnknownBagFails();
            Debug.Log("Seed bag service smoke tests passed.");
        }

        private static (MigrationSeedBagService, InventoryService) Build()
        {
            MigrationCropDatabase crops = new MigrationCropDatabase();
            AssertEqual(true, crops.LoadFromPath(CropDataPath), "crops.json loads. " + string.Join("; ", crops.Errors));
            ItemDatabase items = new ItemDatabase();
            AssertEqual(true, items.LoadFromPath(ItemDataPath), "items.json loads.");

            InventoryService inventory = new InventoryService(items);
            MigrationSeedBag bag = new MigrationSeedBag(crops, () => 0.0, _ => 0); // deterministic common roll
            return (new MigrationSeedBagService(bag, inventory), inventory);
        }

        private static void TestOpeningConsumesBagAndGrantsSeeds()
        {
            (MigrationSeedBagService service, InventoryService inventory) = Build();
            AssertEqual(true, inventory.AddItem("seed_bag_bamboo", 1), "seed_bag_bamboo exists and is given to the player.");

            SeedBagOpenResult result = service.OpenBag("bamboo", MigrationCropSeason.Spring);
            AssertEqual(true, result.Success, "Opening a held bag succeeds. Reason: " + result.Reason);
            AssertEqual(3, result.Seeds.Count, "A bamboo bag yields three seeds.");
            AssertEqual(0, inventory.GetItemCount("seed_bag_bamboo"), "The bag item is consumed.");
            AssertEqual(true, inventory.GetItemCount(result.Seeds[0]) >= 1, "The rolled seed is granted to the inventory.");
        }

        private static void TestNoBagFails()
        {
            (MigrationSeedBagService service, _) = Build();
            SeedBagOpenResult result = service.OpenBag("bamboo", MigrationCropSeason.Spring);
            AssertEqual(false, result.Success, "Opening with no bag in inventory fails.");
            AssertEqual("no_bag", result.Reason, "It reports no_bag.");
        }

        private static void TestUnknownBagFails()
        {
            (MigrationSeedBagService service, InventoryService inventory) = Build();
            inventory.AddItem("seed_bag_bamboo", 1);
            SeedBagOpenResult result = service.OpenBag("platinum", MigrationCropSeason.Spring);
            AssertEqual(false, result.Success, "An unknown bag type fails.");
            AssertEqual("unknown_bag", result.Reason, "It reports unknown_bag.");
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
