using System;
using System.Collections.Generic;

namespace TouhouMigration.Runtime.Farming
{
    // A single farm plot's crop growth (Godot FarmPlot): plant a crop, water/fertilize it, advance the
    // calendar day to grow it (water and fertilizer decay daily), and harvest once fully grown. Tracks the
    // crop quality tier from the water/fertilizer condition and computes the scaled harvest yield. Free of
    // UnityEngine so the rules are unit-tested. Soil-memory, spirit-crystal, mutant-seed and full-moon
    // quality bonuses (Masterwork+), multi-harvest regrow, and the water-level growth-speed coupling are
    // deferred to later farming slices.
    public sealed class MigrationFarmPlot
    {
        public bool HasCrop { get; private set; }
        public string CropId { get; private set; } = string.Empty;
        public int TotalGrowthDays { get; private set; }
        public int DaysGrown { get; private set; }
        public bool NeedsWaterDaily { get; private set; }
        public bool IsWateredToday { get; private set; }

        public double WaterLevel { get; private set; }
        public double FertilizerLevel { get; private set; }
        public CropQuality QualityTier { get; private set; } = CropQuality.Normal;

        private const double MaxWater = 100.0;
        private const double MaxFertilizer = 100.0;
        private const double WaterDecayPerDay = 5.0;
        private const double FertilizerDecayPerDay = 1.5;

        private static readonly Dictionary<CropQuality, double> QualityMultipliers = new Dictionary<CropQuality, double>
        {
            { CropQuality.Normal, 1.0 },
            { CropQuality.Good, 1.25 },
            { CropQuality.Excellent, 1.5 },
            { CropQuality.Masterwork, 2.0 },
            { CropQuality.Legendary, 3.0 },
        };

        // Add fertilizer (Godot fertilize(): +max(power, 1), clamped to MAX_FERTILIZER).
        public void Fertilize(double power = 30.0)
        {
            FertilizerLevel = Math.Min(FertilizerLevel + Math.Max(power, 1.0), MaxFertilizer);
        }

        public double GetQualityMultiplier()
        {
            return QualityMultipliers[QualityTier];
        }

        // Harvest yield (Godot preview_harvest_yield / _calculate_harvest_yield): base 4 scaled by the water and
        // fertilizer condition and the quality multiplier, floored at 1. Returns 0 when the crop is not ready.
        public int CalculateHarvestYield()
        {
            if (!IsReadyToHarvest)
            {
                return 0;
            }

            UpdateQualityTier();
            double yieldMultiplier = (WaterLevel / 100.0) * (0.5 + (FertilizerLevel / 100.0) * 0.5);
            return Math.Max(1, (int)(4 * yieldMultiplier * GetQualityMultiplier()));
        }

        // Recompute the quality tier from current water/fertilizer (Godot _update_quality_tier). The soil-memory,
        // spirit-crystal, mutant-seed, and full-moon bonuses (which only raise Masterwork+) are deferred to a
        // later slice: they need extra plot state and the WeatherSystem.
        private void UpdateQualityTier()
        {
            if (!HasCrop)
            {
                QualityTier = CropQuality.Normal;
                return;
            }

            CropQuality quality = CropQuality.Normal;
            double baseScore = 0.0;
            if (WaterLevel >= 80.0 && FertilizerLevel >= 30.0)
            {
                baseScore = 1.0;
                quality = CropQuality.Good;
            }

            if (quality >= CropQuality.Good && FertilizerLevel >= 60.0)
            {
                baseScore = 2.0;
                quality = CropQuality.Excellent;
            }

            if (baseScore >= 2.0)
            {
                quality = CropQuality.Excellent;
            }
            else if (baseScore >= 1.0)
            {
                quality = CropQuality.Good;
            }

            QualityTier = quality;
        }

        public bool IsReadyToHarvest => HasCrop && DaysGrown >= TotalGrowthDays;

        public float GrowthProgress
        {
            get
            {
                if (!HasCrop)
                {
                    return 0f;
                }

                if (TotalGrowthDays <= 0)
                {
                    return 1f;
                }

                return Math.Min(1f, DaysGrown / (float)TotalGrowthDays);
            }
        }

        public bool Plant(string cropId, int growthDays, bool needsWaterDaily)
        {
            if (HasCrop || string.IsNullOrWhiteSpace(cropId) || growthDays < 0)
            {
                return false;
            }

            HasCrop = true;
            CropId = cropId;
            TotalGrowthDays = growthDays;
            DaysGrown = 0;
            NeedsWaterDaily = needsWaterDaily;
            IsWateredToday = false;
            QualityTier = CropQuality.Normal;
            return true;
        }

        // Water the plot (Godot water(): +50 water level, clamped to MAX_WATER, and flags watered today).
        public void Water()
        {
            WaterLevel = Math.Min(WaterLevel + 50.0, MaxWater);
            if (HasCrop)
            {
                IsWateredToday = true;
            }
        }

        // Advance one calendar day. A crop grows one day unless it needs water and was not watered
        // today; the watered flag clears at the end of the day (Godot is_watered_today resets daily).
        public void AdvanceDay()
        {
            if (!HasCrop)
            {
                return;
            }

            // Daily evaporation (Godot update_day): water decays faster than fertilizer.
            WaterLevel = Math.Max(WaterLevel - WaterDecayPerDay, 0.0);
            FertilizerLevel = Math.Max(FertilizerLevel - FertilizerDecayPerDay, 0.0);

            bool canGrow = !NeedsWaterDaily || IsWateredToday;
            if (canGrow && DaysGrown < TotalGrowthDays)
            {
                DaysGrown++;
            }

            IsWateredToday = false;
            UpdateQualityTier();
        }

        public bool Harvest()
        {
            if (!IsReadyToHarvest)
            {
                return false;
            }

            Clear();
            return true;
        }

        public void Clear()
        {
            HasCrop = false;
            CropId = string.Empty;
            TotalGrowthDays = 0;
            DaysGrown = 0;
            NeedsWaterDaily = false;
            IsWateredToday = false;
            WaterLevel = 0.0;
            FertilizerLevel = 0.0;
            QualityTier = CropQuality.Normal;
        }
    }

    // Crop quality tiers (Godot FarmPlot QUALITY_* constants). Higher tiers multiply harvest yield.
    public enum CropQuality
    {
        Normal = 0,
        Good = 1,
        Excellent = 2,
        Masterwork = 3,
        Legendary = 4,
    }
}
