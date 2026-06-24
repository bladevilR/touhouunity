using System;
using System.Collections.Generic;
using System.Globalization;

namespace TouhouMigration.Runtime.Inventory
{
    [Serializable]
    public sealed class ItemDefinition
    {
        public ItemDefinition(
            string id,
            string category,
            string itemType,
            string name,
            string description,
            int price,
            int maxStack,
            string slot,
            string rarity,
            string cropId,
            string source,
            string element,
            Dictionary<string, string> effects = null,
            Dictionary<string, int> stats = null)
        {
            Id = id;
            Category = category;
            ItemType = itemType;
            Name = name;
            Description = description;
            Price = price;
            MaxStack = maxStack;
            Slot = slot;
            Rarity = rarity;
            CropId = cropId;
            Source = source;
            Element = element;
            Effects = effects != null
                ? new Dictionary<string, string>(effects, StringComparer.Ordinal)
                : new Dictionary<string, string>(StringComparer.Ordinal);
            Stats = stats != null
                ? new Dictionary<string, int>(stats, StringComparer.Ordinal)
                : new Dictionary<string, int>(StringComparer.Ordinal);
        }

        public string Id { get; }
        public string Category { get; }
        public string ItemType { get; }
        public string Name { get; }
        public string Description { get; }
        public int Price { get; }
        public int MaxStack { get; }
        public string Slot { get; }
        public string Rarity { get; }
        public string CropId { get; }
        public string Source { get; }
        public string Element { get; }
        public IReadOnlyDictionary<string, string> Effects { get; }
        public IReadOnlyDictionary<string, int> Stats { get; }

        public bool HasEffect(string key)
        {
            return Effects.ContainsKey(NormalizeKey(key));
        }

        public string GetEffectString(string key)
        {
            return GetEffectString(key, string.Empty);
        }

        public string GetEffectString(string key, string fallback)
        {
            return Effects.TryGetValue(NormalizeKey(key), out string value) ? value : fallback;
        }

        public int GetEffectInt(string key)
        {
            return GetEffectInt(key, 0);
        }

        public int GetEffectInt(string key, int fallback)
        {
            if (!Effects.TryGetValue(NormalizeKey(key), out string value) ||
                string.IsNullOrWhiteSpace(value))
            {
                return fallback;
            }

            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed)
                ? parsed
                : fallback;
        }

        public int GetStat(string key)
        {
            return Stats.TryGetValue(NormalizeKey(key), out int value) ? value : 0;
        }

        private static string NormalizeKey(string key)
        {
            return string.IsNullOrWhiteSpace(key) ? string.Empty : key.Trim().ToLowerInvariant();
        }
    }
}
