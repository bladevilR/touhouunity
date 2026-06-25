using System;
using System.Collections.Generic;
using System.IO;
using TouhouMigration.Runtime.Serialization;

namespace TouhouMigration.Runtime.Fishing
{
    // Loads the fish catalog (Data/Fishing/fish.json, ported from Godot FishDatabase): each fish's
    // rarity (the catch-weight driver). The inventory item id defaults to the fish id. Mirrors the
    // ItemDatabase loader (reuses MigrationJson).
    public sealed class MigrationFishDatabase
    {
        private readonly Dictionary<string, MigrationFishDefinition> fish = new Dictionary<string, MigrationFishDefinition>();
        private readonly List<string> errors = new List<string>();

        public IReadOnlyList<string> Errors => errors;
        public int FishCount => fish.Count;

        public bool LoadFromPath(string assetPath)
        {
            fish.Clear();
            errors.Clear();

            string path = ResolvePath(assetPath);
            if (!File.Exists(path))
            {
                errors.Add($"missing fish data file: {assetPath}");
                return false;
            }

            object parsed;
            try
            {
                parsed = MigrationJson.Parse(File.ReadAllText(path));
            }
            catch (Exception exception)
            {
                errors.Add($"invalid fish json: {exception.Message}");
                return false;
            }

            if (parsed is not Dictionary<string, object> root)
            {
                errors.Add("fish json root must be an object");
                return false;
            }

            if (!root.TryGetValue("fish", out object fishObject) || fishObject is not Dictionary<string, object> fishMap)
            {
                errors.Add("fish json must contain a 'fish' object");
                return false;
            }

            foreach (KeyValuePair<string, object> pair in fishMap)
            {
                if (pair.Value is not Dictionary<string, object> data)
                {
                    errors.Add($"fish {pair.Key} must be an object");
                    continue;
                }

                string fishId = pair.Key;
                MigrationFishRarity rarity = ParseRarity(GetString(data, "rarity"));
                fish[fishId] = new MigrationFishDefinition(fishId, rarity, fishId);
            }

            return fish.Count > 0 && errors.Count == 0;
        }

        public MigrationFishDefinition GetFish(string fishId)
        {
            return fishId != null && fish.TryGetValue(fishId, out MigrationFishDefinition definition) ? definition : null;
        }

        public IReadOnlyDictionary<string, MigrationFishDefinition> GetAllFish()
        {
            return fish;
        }

        private static MigrationFishRarity ParseRarity(string value)
        {
            return Enum.TryParse(value, true, out MigrationFishRarity rarity) ? rarity : MigrationFishRarity.Common;
        }

        private static string GetString(Dictionary<string, object> data, string key)
        {
            return data.TryGetValue(key, out object value) && value != null ? value.ToString() : string.Empty;
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
