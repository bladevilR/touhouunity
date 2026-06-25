using System;
using TouhouMigration.Runtime.Farming;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    public static class CropDatabaseSmokeTests
    {
        private const string CropDataPath = "Assets/TouhouMigration/Data/Farming/crops.json";

        [MenuItem("Touhou Migration/Tests/Run Crop Database Smoke Tests")]
        public static void RunAll()
        {
            TestLoadsCropsFromJson();
            TestHarvestItemIdDerivation();
            Debug.Log("Crop database smoke tests passed.");
        }

        private static MigrationCropDatabase LoadDatabase()
        {
            MigrationCropDatabase database = new MigrationCropDatabase();
            bool loaded = database.LoadFromPath(CropDataPath);
            AssertEqual(true, loaded, "crops.json should load. Errors: " + string.Join("; ", database.Errors));
            return database;
        }

        private static void TestLoadsCropsFromJson()
        {
            MigrationCropDatabase database = LoadDatabase();
            AssertEqual(true, database.CropCount >= 60, "crops.json should provide the full crop catalog (60+).");

            MigrationCropDefinition turnip = database.GetCrop("crop_turnip");
            AssertEqual(true, turnip != null, "crop_turnip should be present.");
            AssertEqual("crop_turnip", turnip.CropId, "Crop id is preserved.");
            AssertEqual(3, turnip.GrowthDays, "crop_turnip grows in 3 days (Godot crops.json).");
            AssertEqual(true, turnip.NeedsWaterDaily, "Crops need daily water by default.");
            AssertEqual(true, database.GetCrop("crop_nonexistent") == null, "An unknown crop id returns null.");
        }

        private static void TestHarvestItemIdDerivation()
        {
            MigrationCropDatabase database = LoadDatabase();
            AssertEqual("turnip", database.GetCrop("crop_turnip").HarvestItemId, "Harvest item id strips the crop_ prefix.");
            AssertEqual("chili", database.GetCrop("crop_pepper").HarvestItemId, "crop_pepper maps to chili via the override.");
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
