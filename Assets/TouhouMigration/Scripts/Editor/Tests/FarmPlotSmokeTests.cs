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

        private static void AssertEqual<T>(T expected, T actual, string message)
        {
            if (!Equals(expected, actual))
            {
                throw new Exception($"{message} Expected: {expected}. Actual: {actual}.");
            }
        }
    }
}
