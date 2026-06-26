using System;
using TouhouMigration.Runtime.Farming;
using TouhouMigration.Runtime.Inventory;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    // Covers MigrationCropDatabase.RegisterProduceInto: deriving missing crop-produce items from the crop's
    // own name + base_price (Godot ItemData "crops" category) so every crop's harvest is a real sellable
    // item — closing the harvest -> inventory -> sell loop for crops absent from items.json.
    public static class CropProduceRegistrarSmokeTests
    {
        private const string CropDataPath = "Assets/TouhouMigration/Data/Farming/crops.json";
        private const string ItemDataPath = "Assets/TouhouMigration/Data/Items/items.json";

        [MenuItem("Touhou Migration/Tests/Run Crop Produce Registrar Smoke Tests")]
        public static void RunAll()
        {
            TestDerivesMissingProduce();
            TestDoesNotOverwriteExistingProduce();
            Debug.Log("Crop produce registrar smoke tests passed.");
        }

        private static (MigrationCropDatabase, ItemDatabase) Build()
        {
            MigrationCropDatabase crops = new MigrationCropDatabase();
            AssertEqual(true, crops.LoadFromPath(CropDataPath), "crops.json loads.");
            ItemDatabase items = new ItemDatabase();
            AssertEqual(true, items.LoadFromPath(ItemDataPath), "items.json loads.");
            return (crops, items);
        }

        private static void TestDerivesMissingProduce()
        {
            (MigrationCropDatabase crops, ItemDatabase items) = Build();
            AssertEqual(false, items.HasItem("corn"), "corn produce is absent from items.json to begin with.");

            int added = crops.RegisterProduceInto(items);
            AssertEqual(true, added > 0, "Some missing produce items were registered.");

            AssertEqual(true, items.HasItem("corn"), "corn produce is now a known item.");
            ItemDefinition corn = items.GetItem("corn");
            AssertEqual("玉米", corn.Name, "The produce name comes from the crop's name.");
            AssertEqual(65, corn.Price, "The produce price comes from the crop's base_price.");
            AssertEqual("crops", corn.Category, "The produce lands in the crops category.");

            // After registration the whole crop set is sellable produce.
            AssertEqual(true, items.HasItem("melon") && items.HasItem("eggplant"),
                "Other previously-missing produce is registered too.");
        }

        private static void TestDoesNotOverwriteExistingProduce()
        {
            (MigrationCropDatabase crops, ItemDatabase items) = Build();
            ItemDefinition turnipBefore = items.GetItem("turnip"); // already defined in items.json
            AssertEqual(true, turnipBefore != null, "turnip produce already exists.");
            string nameBefore = turnipBefore.Name;

            crops.RegisterProduceInto(items);
            AssertEqual(nameBefore, items.GetItem("turnip").Name, "An already-defined produce item is left untouched.");
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
