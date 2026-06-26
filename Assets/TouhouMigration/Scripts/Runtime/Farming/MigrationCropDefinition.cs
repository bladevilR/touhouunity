using System;

namespace TouhouMigration.Runtime.Farming
{
    // Crop rarity tiers (Godot CropDatabase.CropRarity).
    public enum MigrationCropRarity
    {
        Common,
        Uncommon,
        Rare,
        Legendary
    }

    // Crop planting seasons (Godot CropDatabase.Season). All = any season; SpringSummerAutumn = every
    // season except winter.
    public enum MigrationCropSeason
    {
        Spring,
        Summer,
        Autumn,
        Winter,
        All,
        SpringSummerAutumn
    }

    // A crop's definition (Godot CropData): growth days, daily-water need, the produce item + yield range
    // granted on harvest, and its rarity/season (drives seed-bag gacha + season planting). Immutable;
    // consumed by MigrationFarmingManager.
    public sealed class MigrationCropDefinition
    {
        public string CropId { get; }
        public int GrowthDays { get; }
        public bool NeedsWaterDaily { get; }
        public string HarvestItemId { get; }
        public int MinYield { get; }
        public int MaxYield { get; }
        public MigrationCropRarity Rarity { get; }
        public MigrationCropSeason Season { get; }

        public MigrationCropDefinition(
            string cropId,
            int growthDays,
            bool needsWaterDaily,
            string harvestItemId,
            int minYield,
            int maxYield,
            MigrationCropRarity rarity = MigrationCropRarity.Common,
            MigrationCropSeason season = MigrationCropSeason.Spring)
        {
            CropId = cropId ?? string.Empty;
            GrowthDays = Math.Max(0, growthDays);
            NeedsWaterDaily = needsWaterDaily;
            HarvestItemId = harvestItemId ?? string.Empty;
            MinYield = Math.Max(0, minYield);
            MaxYield = Math.Max(MinYield, maxYield);
            Rarity = rarity;
            Season = season;
        }

        // Whether this crop can be planted in the given season (Godot can_plant_in_season).
        public bool CanPlantIn(MigrationCropSeason currentSeason)
        {
            if (Season == MigrationCropSeason.All)
            {
                return true;
            }

            if (Season == MigrationCropSeason.SpringSummerAutumn)
            {
                return currentSeason == MigrationCropSeason.Spring
                    || currentSeason == MigrationCropSeason.Summer
                    || currentSeason == MigrationCropSeason.Autumn;
            }

            return Season == currentSeason;
        }
    }
}
