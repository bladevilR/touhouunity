using System;

namespace TouhouMigration.Runtime.Farming
{
    // A crop's definition (Godot CropData): growth days, daily-water need, and the produce item +
    // yield range granted on harvest. Immutable; consumed by MigrationFarmingManager.
    public sealed class MigrationCropDefinition
    {
        public string CropId { get; }
        public int GrowthDays { get; }
        public bool NeedsWaterDaily { get; }
        public string HarvestItemId { get; }
        public int MinYield { get; }
        public int MaxYield { get; }

        public MigrationCropDefinition(string cropId, int growthDays, bool needsWaterDaily, string harvestItemId, int minYield, int maxYield)
        {
            CropId = cropId ?? string.Empty;
            GrowthDays = Math.Max(0, growthDays);
            NeedsWaterDaily = needsWaterDaily;
            HarvestItemId = harvestItemId ?? string.Empty;
            MinYield = Math.Max(0, minYield);
            MaxYield = Math.Max(MinYield, maxYield);
        }
    }
}
