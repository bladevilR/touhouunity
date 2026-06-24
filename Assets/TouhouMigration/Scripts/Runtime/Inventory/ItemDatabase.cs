using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using TouhouMigration.Runtime.Serialization;

namespace TouhouMigration.Runtime.Inventory
{
    public sealed class ItemDatabase
    {
        private static readonly Dictionary<string, string> TypeMap = new Dictionary<string, string>
        {
            { "consumables", "consumable" },
            { "equipment", "equipment" },
            { "materials", "material" },
            { "fish", "fish" },
            { "ingredients", "ingredient" },
            { "crops", "crop" },
            { "seeds", "seed" },
            { "fertilizers", "fertilizer" },
            { "dishes", "dish" },
            { "drinks", "drink" },
            { "currency", "currency" },
            { "combat_materials", "combat_material" },
            { "lore_items", "lore_item" }
        };

        private readonly Dictionary<string, ItemDefinition> items = new Dictionary<string, ItemDefinition>();
        private readonly HashSet<string> categories = new HashSet<string>();
        private readonly List<string> errors = new List<string>();

        public int CategoryCount => categories.Count;
        public int ItemCount => items.Count;
        public IReadOnlyList<string> Errors => errors;

        public bool LoadFromPath(string assetPath)
        {
            items.Clear();
            categories.Clear();
            errors.Clear();

            string path = ResolvePath(assetPath);
            if (!File.Exists(path))
            {
                errors.Add($"missing items data file: {assetPath}");
                return false;
            }

            object parsed;
            try
            {
                parsed = MigrationJson.Parse(File.ReadAllText(path));
            }
            catch (Exception exception)
            {
                errors.Add($"invalid items json: {exception.Message}");
                return false;
            }

            if (parsed is not Dictionary<string, object> root)
            {
                errors.Add("items json root must be an object");
                return false;
            }

            foreach (KeyValuePair<string, object> categoryPair in root)
            {
                if (categoryPair.Value is not Dictionary<string, object> categoryItems)
                {
                    errors.Add($"items category {categoryPair.Key} must be an object");
                    continue;
                }

                categories.Add(categoryPair.Key);
                string itemType = TypeMap.TryGetValue(categoryPair.Key, out string mappedType)
                    ? mappedType
                    : categoryPair.Key;

                foreach (KeyValuePair<string, object> itemPair in categoryItems)
                {
                    if (itemPair.Value is not Dictionary<string, object> itemData)
                    {
                        errors.Add($"item {itemPair.Key} must be an object");
                        continue;
                    }

                    int maxStack = GetInt(itemData, "max_stack", itemType == "equipment" ? 1 : 99);
                    ItemDefinition definition = new ItemDefinition(
                        itemPair.Key,
                        categoryPair.Key,
                        itemType,
                        GetString(itemData, "name"),
                        GetString(itemData, "description"),
                        GetInt(itemData, "price", 0),
                        maxStack,
                        GetString(itemData, "slot"),
                        GetString(itemData, "rarity"),
                        GetString(itemData, "crop_id"),
                        GetString(itemData, "source"),
                        GetString(itemData, "element"),
                        GetEffects(itemData),
                        GetStats(itemData));

                    items[itemPair.Key] = definition;
                }
            }

            return errors.Count == 0;
        }

        public bool HasItem(string itemId)
        {
            return items.ContainsKey(itemId);
        }

        public ItemDefinition GetItem(string itemId)
        {
            return items.TryGetValue(itemId, out ItemDefinition item) ? item : null;
        }

        public IReadOnlyDictionary<string, ItemDefinition> GetAllItems()
        {
            return items;
        }

        private static string ResolvePath(string assetPath)
        {
            if (Path.IsPathRooted(assetPath))
            {
                return assetPath;
            }

            return Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), assetPath));
        }

        private static string GetString(Dictionary<string, object> data, string key)
        {
            return data.TryGetValue(key, out object value) && value != null ? value.ToString() : string.Empty;
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

        private static Dictionary<string, string> GetEffects(Dictionary<string, object> data)
        {
            Dictionary<string, string> effects = new Dictionary<string, string>(StringComparer.Ordinal);
            if (data.TryGetValue("effects", out object rawEffects) &&
                rawEffects is Dictionary<string, object> effectMap)
            {
                foreach (KeyValuePair<string, object> pair in effectMap)
                {
                    string key = NormalizeKey(pair.Key);
                    if (!string.IsNullOrEmpty(key))
                    {
                        effects[key] = Convert.ToString(pair.Value, CultureInfo.InvariantCulture) ?? string.Empty;
                    }
                }
            }

            CopyTopLevelEffect(data, effects, "heal_hp");
            CopyTopLevelEffect(data, effects, "restore_mp");
            CopyTopLevelEffect(data, effects, "buff");
            CopyTopLevelEffect(data, effects, "combat_item");
            return effects;
        }

        private static Dictionary<string, int> GetStats(Dictionary<string, object> data)
        {
            Dictionary<string, int> stats = new Dictionary<string, int>(StringComparer.Ordinal);
            if (!data.TryGetValue("stats", out object rawStats) ||
                rawStats is not Dictionary<string, object> statMap)
            {
                return stats;
            }

            foreach (KeyValuePair<string, object> pair in statMap)
            {
                string key = NormalizeKey(pair.Key);
                if (!string.IsNullOrEmpty(key))
                {
                    stats[key] = ToInt(pair.Value, 0);
                }
            }

            return stats;
        }

        private static void CopyTopLevelEffect(
            Dictionary<string, object> data,
            Dictionary<string, string> effects,
            string key)
        {
            if (data.TryGetValue(key, out object value) && value != null)
            {
                effects[NormalizeKey(key)] = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
            }
        }

        private static int ToInt(object value, int fallback)
        {
            return value switch
            {
                long longValue => (int)longValue,
                double doubleValue => (int)doubleValue,
                int intValue => intValue,
                _ => int.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), out int parsed) ? parsed : fallback
            };
        }

        private static string NormalizeKey(string key)
        {
            return string.IsNullOrWhiteSpace(key) ? string.Empty : key.Trim().ToLowerInvariant();
        }
    }
}
