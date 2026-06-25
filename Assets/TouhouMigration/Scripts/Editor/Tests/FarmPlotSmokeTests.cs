using System;
using TouhouMigration.Runtime.Farming;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    public static class FarmPlotSmokeTests
    {
        [MenuItem("Touhou Migration/Tests/Run Farm Plot Smoke Tests")]
        public static void RunAll()
        {
            TestPlantInitializesGrowthState();
            TestDailyWateringGatesGrowth();
            TestReachesHarvestThenHarvestClearsPlot();
            TestCannotPlantOnOccupiedPlotOrHarvestUnready();
            TestWateringAndFertilizingRaiseLevelsAndClamp();
            TestAdvanceDayDecaysWaterAndFertilizer();
            TestQualityTierRisesWithWaterAndFertilizer();
            TestHarvestYieldScalesWithConditions();
            Debug.Log("Farm plot smoke tests passed.");
        }

        private static void TestPlantInitializesGrowthState()
        {
            MigrationFarmPlot plot = new MigrationFarmPlot();
            AssertEqual(false, plot.HasCrop, "A fresh plot holds no crop.");

            bool planted = plot.Plant("crop_turnip", 3, true);
            AssertEqual(true, planted, "Planting on an empty plot should succeed.");
            AssertEqual(true, plot.HasCrop, "Plot should hold a crop after planting.");
            AssertEqual("crop_turnip", plot.CropId, "Plot should record the planted crop id.");
            AssertEqual(3, plot.TotalGrowthDays, "Plot should record the crop's growth days.");
            AssertEqual(0, plot.DaysGrown, "A freshly planted crop has grown zero days.");
            AssertEqual(false, plot.IsReadyToHarvest, "A freshly planted crop is not ready to harvest.");
            AssertEqual(0f, plot.GrowthProgress, "A freshly planted crop has zero growth progress.");
        }

        private static void TestDailyWateringGatesGrowth()
        {
            MigrationFarmPlot plot = new MigrationFarmPlot();
            plot.Plant("crop_turnip", 3, true);

            plot.AdvanceDay();
            AssertEqual(0, plot.DaysGrown, "A water-needy crop should not grow on an unwatered day.");

            plot.Water();
            plot.AdvanceDay();
            AssertEqual(1, plot.DaysGrown, "A watered crop should grow one day.");

            // Watering does not carry over to the next day.
            plot.AdvanceDay();
            AssertEqual(1, plot.DaysGrown, "Watering should not carry to the next day; unwatered day does not grow.");
        }

        private static void TestReachesHarvestThenHarvestClearsPlot()
        {
            MigrationFarmPlot plot = new MigrationFarmPlot();
            plot.Plant("crop_potato", 2, false);

            plot.AdvanceDay();
            plot.AdvanceDay();
            AssertEqual(2, plot.DaysGrown, "A no-water crop grows each advanced day.");
            AssertEqual(true, plot.IsReadyToHarvest, "Crop should be ready after its growth days elapse.");
            AssertEqual(1f, plot.GrowthProgress, "A fully grown crop reports full progress.");

            bool harvested = plot.Harvest();
            AssertEqual(true, harvested, "Harvesting a ready crop should succeed.");
            AssertEqual(false, plot.HasCrop, "Harvesting should clear the plot.");
            AssertEqual(false, plot.IsReadyToHarvest, "A cleared plot is not ready to harvest.");
        }

        private static void TestCannotPlantOnOccupiedPlotOrHarvestUnready()
        {
            MigrationFarmPlot plot = new MigrationFarmPlot();
            AssertEqual(true, plot.Plant("crop_a", 3, false), "First plant should succeed.");
            AssertEqual(false, plot.Plant("crop_b", 3, false), "Planting on an occupied plot should fail.");
            AssertEqual("crop_a", plot.CropId, "A failed plant should not replace the existing crop.");

            AssertEqual(false, plot.Harvest(), "Harvesting an unready crop should fail.");

            MigrationFarmPlot empty = new MigrationFarmPlot();
            AssertEqual(false, empty.Harvest(), "Harvesting an empty plot should fail.");
        }

        private static void TestWateringAndFertilizingRaiseLevelsAndClamp()
        {
            MigrationFarmPlot plot = new MigrationFarmPlot();
            plot.Plant("crop_turnip", 5, false);
            plot.Water();
            AssertEqual(50.0, plot.WaterLevel, "Watering raises the water level by 50 (Godot water()).");
            plot.Water();
            AssertEqual(100.0, plot.WaterLevel, "Water level clamps at 100.");

            plot.Fertilize(30.0);
            AssertEqual(30.0, plot.FertilizerLevel, "Fertilizing raises the fertilizer level by its power.");
            plot.Fertilize(80.0);
            AssertEqual(100.0, plot.FertilizerLevel, "Fertilizer level clamps at 100.");
        }

        private static void TestAdvanceDayDecaysWaterAndFertilizer()
        {
            MigrationFarmPlot plot = new MigrationFarmPlot();
            plot.Plant("crop_turnip", 5, false);
            plot.Water();
            plot.Water(); // 100
            plot.Fertilize(30.0); // 30
            plot.AdvanceDay();
            AssertEqual(95.0, plot.WaterLevel, "Water level decays by 5 each day (Godot WATER_DECAY).");
            AssertEqual(28.5, plot.FertilizerLevel, "Fertilizer decays by 1.5 each day.");
        }

        private static void TestQualityTierRisesWithWaterAndFertilizer()
        {
            MigrationFarmPlot plot = new MigrationFarmPlot();
            plot.Plant("crop_turnip", 5, false);
            plot.Water();
            plot.Water(); // 100
            plot.Fertilize(30.0);
            plot.Fertilize(30.0);
            plot.Fertilize(30.0); // 90
            plot.AdvanceDay(); // water 95, fert 88.5
            AssertEqual(CropQuality.Excellent, plot.QualityTier, "High water (>=80) and fertilizer (>=60) give Excellent quality.");
            AssertEqual(1.5, plot.GetQualityMultiplier(), "Excellent quality yields a 1.5x multiplier.");
        }

        private static void TestHarvestYieldScalesWithConditions()
        {
            MigrationFarmPlot plot = new MigrationFarmPlot();
            plot.Plant("crop_turnip", 1, false);
            plot.Water();
            plot.Water(); // 100
            plot.Fertilize(30.0);
            plot.Fertilize(30.0); // 60
            plot.AdvanceDay(); // water 95, fert 58.5, grows to ready
            AssertEqual(true, plot.IsReadyToHarvest, "Precondition: the crop is ready after one growth day.");
            AssertEqual(CropQuality.Good, plot.QualityTier, "Good water with fertilizer 30-59 gives Good quality.");
            // yield = max(1, (int)(4 * (95/100)*(0.5 + (58.5/100)*0.5) * 1.25)) = (int)3.764 = 3
            AssertEqual(3, plot.CalculateHarvestYield(), "Harvest yield scales with water, fertilizer, and quality.");
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
