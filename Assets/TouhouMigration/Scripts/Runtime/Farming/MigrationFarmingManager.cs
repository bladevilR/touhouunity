using System;
using System.Collections.Generic;
using TouhouMigration.Runtime.Inventory;

namespace TouhouMigration.Runtime.Farming
{
    // Owns a set of farm plots + the crop catalog and runs the farming loop (Godot FarmingManager
    // intent): plant a registered crop, water it, advance the calendar day to grow all plots, and
    // harvest produce (yield range -> inventory). Free of UnityEngine so it is unit-testable; harvest
    // randomness is injected. Water/fertilizer/quality yield scaling (Godot _calculate_harvest_yield)
    // is a later slice.
    public sealed class MigrationFarmingManager
    {
        private readonly InventoryService inventory;
        private readonly Dictionary<string, MigrationCropDefinition> crops = new Dictionary<string, MigrationCropDefinition>();
        private readonly MigrationFarmPlot[] plots;

        public MigrationFarmingManager(InventoryService inventory, int plotCount)
        {
            this.inventory = inventory;
            int count = Math.Max(0, plotCount);
            plots = new MigrationFarmPlot[count];
            for (int index = 0; index < count; index++)
            {
                plots[index] = new MigrationFarmPlot();
            }
        }

        public int PlotCount => plots.Length;

        public void RegisterCrop(MigrationCropDefinition crop)
        {
            if (crop != null && !string.IsNullOrWhiteSpace(crop.CropId))
            {
                crops[crop.CropId] = crop;
            }
        }

        // Register every crop from a loaded MigrationCropDatabase so crops can be planted by id.
        public void RegisterCropsFrom(MigrationCropDatabase database)
        {
            if (database == null)
            {
                return;
            }

            foreach (KeyValuePair<string, MigrationCropDefinition> pair in database.GetAllCrops())
            {
                RegisterCrop(pair.Value);
            }
        }

        public MigrationFarmPlot GetPlot(int plotIndex)
        {
            return plotIndex >= 0 && plotIndex < plots.Length ? plots[plotIndex] : null;
        }

        public bool Plant(int plotIndex, string cropId)
        {
            MigrationFarmPlot plot = GetPlot(plotIndex);
            if (plot == null || !crops.TryGetValue(cropId ?? string.Empty, out MigrationCropDefinition crop))
            {
                return false;
            }

            return plot.Plant(crop.CropId, crop.GrowthDays, crop.NeedsWaterDaily);
        }

        public void Water(int plotIndex)
        {
            GetPlot(plotIndex)?.Water();
        }

        public void AdvanceDay()
        {
            foreach (MigrationFarmPlot plot in plots)
            {
                plot.AdvanceDay();
            }
        }

        public MigrationHarvestResult Harvest(int plotIndex, Func<int, int, int> randomRange)
        {
            MigrationFarmPlot plot = GetPlot(plotIndex);
            if (plot == null)
            {
                return MigrationHarvestResult.Fail("no_plot");
            }

            if (!plot.IsReadyToHarvest)
            {
                return MigrationHarvestResult.Fail("not_ready");
            }

            if (!crops.TryGetValue(plot.CropId, out MigrationCropDefinition crop))
            {
                return MigrationHarvestResult.Fail("unknown_crop");
            }

            int amount = RollYield(crop, randomRange);
            string itemId = crop.HarvestItemId;
            plot.Harvest();

            if (amount > 0 && !string.IsNullOrWhiteSpace(itemId))
            {
                inventory?.AddItem(itemId, amount);
            }

            return MigrationHarvestResult.Ok(itemId, amount);
        }

        private static int RollYield(MigrationCropDefinition crop, Func<int, int, int> randomRange)
        {
            if (crop.MinYield >= crop.MaxYield || randomRange == null)
            {
                return crop.MinYield;
            }

            int rolled = randomRange(crop.MinYield, crop.MaxYield + 1);
            return Math.Clamp(rolled, crop.MinYield, crop.MaxYield);
        }
    }
}
