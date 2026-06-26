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
            TestRarityAndSeasonParse();
            TestGetCropsByRarity();
            TestCanPlantInSeason();
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

        private static void TestRarityAndSeasonParse()
        {
            MigrationCropDatabase database = LoadDatabase();
            AssertEqual(MigrationCropRarity.Common, database.GetCrop("crop_turnip").Rarity, "crop_turnip is COMMON.");
            AssertEqual(MigrationCropRarity.Uncommon, database.GetCrop("crop_bamboo_shoot").Rarity, "crop_bamboo_shoot is UNCOMMON.");
            AssertEqual(MigrationCropRarity.Legendary, database.GetCrop("crop_saigyou_seed").Rarity, "crop_saigyou_seed is LEGENDARY.");
            AssertEqual(MigrationCropSeason.Spring, database.GetCrop("crop_turnip").Season, "crop_turnip is a SPRING crop.");
            AssertEqual(MigrationCropSeason.Winter, database.GetCrop("crop_winter_radish").Season, "crop_winter_radish is a WINTER crop.");
            AssertEqual(MigrationCropSeason.All, database.GetCrop("crop_four_season_flower").Season, "crop_four_season_flower is ALL-season.");
            AssertEqual(MigrationCropSeason.SpringSummerAutumn, database.GetCrop("crop_youkai_herb").Season, "crop_youkai_herb is SPRING_SUMMER_AUTUMN.");
        }

        private static void TestGetCropsByRarity()
        {
            MigrationCropDatabase database = LoadDatabase();
            var legendary = database.GetCropsByRarity(MigrationCropRarity.Legendary);
            AssertEqual(true, legendary.Count > 0, "There is at least one LEGENDARY crop.");
            AssertEqual(true, Contains(legendary, "crop_saigyou_seed"), "Legendary crops include crop_saigyou_seed.");
            AssertEqual(false, Contains(legendary, "crop_turnip"), "A COMMON crop is not in the LEGENDARY bucket.");
        }

        private static void TestCanPlantInSeason()
        {
            MigrationCropDatabase database = LoadDatabase();
            AssertEqual(true, database.CanPlantInSeason("crop_turnip", MigrationCropSeason.Spring), "A spring crop plants in spring.");
            AssertEqual(false, database.CanPlantInSeason("crop_turnip", MigrationCropSeason.Winter), "A spring crop does not plant in winter.");
            AssertEqual(true, database.CanPlantInSeason("crop_four_season_flower", MigrationCropSeason.Winter), "An ALL-season crop plants in winter.");
            AssertEqual(true, database.CanPlantInSeason("crop_youkai_herb", MigrationCropSeason.Autumn), "A spring/summer/autumn crop plants in autumn.");
            AssertEqual(false, database.CanPlantInSeason("crop_youkai_herb", MigrationCropSeason.Winter), "A spring/summer/autumn crop does not plant in winter.");
            AssertEqual(false, database.CanPlantInSeason("crop_nonexistent", MigrationCropSeason.Spring), "An unknown crop cannot be planted.");
        }

        private static bool Contains(System.Collections.Generic.IReadOnlyList<string> list, string value)
        {
            foreach (string entry in list)
            {
                if (entry == value)
                {
                    return true;
                }
            }

            return false;
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
