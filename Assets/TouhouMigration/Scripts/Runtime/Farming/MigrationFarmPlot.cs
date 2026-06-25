using System;

namespace TouhouMigration.Runtime.Farming
{
    // A single farm plot's crop growth (Godot FarmPlot/CropData intent): plant a crop, water it daily
    // when it needs water, advance the calendar day to grow it, and harvest once fully grown. Free of
    // UnityEngine so the growth/watering rules are unit-tested. Yield/quality/multi-harvest/soil-memory
    // and inventory payout are deferred to later farming slices.
    public sealed class MigrationFarmPlot
    {
        public bool HasCrop { get; private set; }
        public string CropId { get; private set; } = string.Empty;
        public int TotalGrowthDays { get; private set; }
        public int DaysGrown { get; private set; }
        public bool NeedsWaterDaily { get; private set; }
        public bool IsWateredToday { get; private set; }

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
            return true;
        }

        public void Water()
        {
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

            bool canGrow = !NeedsWaterDaily || IsWateredToday;
            if (canGrow && DaysGrown < TotalGrowthDays)
            {
                DaysGrown++;
            }

            IsWateredToday = false;
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
        }
    }
}
