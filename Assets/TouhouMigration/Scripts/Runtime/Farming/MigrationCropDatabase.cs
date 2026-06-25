using System;
using System.Collections.Generic;
using System.IO;
using TouhouMigration.Runtime.Serialization;

namespace TouhouMigration.Runtime.Farming
{
    // Loads the crop catalog (Godot data/crops.json) into MigrationCropDefinition entries. growth_days
    // comes straight from the JSON; the harvest item id strips the "crop_" prefix (with a small override
    // map, Godot HARVEST_ITEM_OVERRIDES). Crops need daily water by default and yield 1 for now
    // (water/quality yield scaling is a later slice). Mirrors the ItemDatabase loader.
    public sealed class MigrationCropDatabase
    {
        private static readonly Dictionary<string, string> HarvestItemOverrides = new Dictionary<string, string>
        {
            ["crop_pepper"] = "chili"
        };

        private readonly Dictionary<string, MigrationCropDefinition> crops = new Dictionary<string, MigrationCropDefinition>();
        private readonly List<string> errors = new List<string>();

        public IReadOnlyList<string> Errors => errors;
        public int CropCount => crops.Count;

        public bool LoadFromPath(string assetPath)
        {
            crops.Clear();
            errors.Clear();

            string path = ResolvePath(assetPath);
            if (!File.Exists(path))
            {
                errors.Add($"missing crops data file: {assetPath}");
                return false;
            }

            object parsed;
            try
            {
                parsed = MigrationJson.Parse(File.ReadAllText(path));
            }
            catch (Exception exception)
            {
                errors.Add($"invalid crops json: {exception.Message}");
                return false;
            }

            if (parsed is not Dictionary<string, object> root)
            {
                errors.Add("crops json root must be an object");
                return false;
            }

            if (!root.TryGetValue("crops", out object cropsObject) || cropsObject is not Dictionary<string, object> cropsMap)
            {
                errors.Add("crops json must contain a 'crops' object");
                return false;
            }

            foreach (KeyValuePair<string, object> pair in cropsMap)
            {
                if (pair.Value is not Dictionary<string, object> data)
                {
                    errors.Add($"crop {pair.Key} must be an object");
                    continue;
                }

                string cropId = pair.Key;
                int growthDays = GetInt(data, "growth_days", 5);
                string harvestItemId = ResolveHarvestItemId(cropId);
                crops[cropId] = new MigrationCropDefinition(cropId, growthDays, true, harvestItemId, 1, 1);
            }

            return crops.Count > 0 && errors.Count == 0;
        }

        public MigrationCropDefinition GetCrop(string cropId)
        {
            return cropId != null && crops.TryGetValue(cropId, out MigrationCropDefinition crop) ? crop : null;
        }

        public IReadOnlyDictionary<string, MigrationCropDefinition> GetAllCrops()
        {
            return crops;
        }

        private static string ResolveHarvestItemId(string cropId)
        {
            if (cropId != null && HarvestItemOverrides.TryGetValue(cropId, out string overrideId))
            {
                return overrideId;
            }

            const string prefix = "crop_";
            return cropId != null && cropId.StartsWith(prefix, StringComparison.Ordinal)
                ? cropId.Substring(prefix.Length)
                : cropId ?? string.Empty;
        }

        private static int GetInt(Dictionary<string, object> data, string key, int fallback)
        {
            if (!data.TryGetValue(key, out object value) || value == null)
            {
                return fallback;
            }

            return value switch
            {
                long longValue => (int)longValue,
                double doubleValue => (int)doubleValue,
                int intValue => intValue,
                _ => int.TryParse(value.ToString(), out int parsed) ? parsed : fallback
            };
        }

        private static string ResolvePath(string assetPath)
        {
            if (Path.IsPathRooted(assetPath))
            {
                return assetPath;
            }

            return Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), assetPath));
        }
    }
}
