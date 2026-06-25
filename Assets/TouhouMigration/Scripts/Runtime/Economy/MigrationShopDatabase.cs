using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using TouhouMigration.Runtime.Serialization;

namespace TouhouMigration.Runtime.Economy
{
    // Loads the shop catalog (Godot data/shops.json): each shop's owner NPC, buy_rate, open hours, and
    // items (id/price/stock). Mirrors the ItemDatabase loader (reuses MigrationJson). Wiring into
    // MigrationShopService (gate Buy/Sell on the shop's catalog/hours/buy_rate) is a later slice.
    public sealed class MigrationShopDatabase
    {
        private readonly Dictionary<string, MigrationShopDefinition> shops = new Dictionary<string, MigrationShopDefinition>();
        private readonly List<string> errors = new List<string>();

        public IReadOnlyList<string> Errors => errors;
        public int ShopCount => shops.Count;

        public bool LoadFromPath(string assetPath)
        {
            shops.Clear();
            errors.Clear();

            string path = ResolvePath(assetPath);
            if (!File.Exists(path))
            {
                errors.Add($"missing shops data file: {assetPath}");
                return false;
            }

            object parsed;
            try
            {
                parsed = MigrationJson.Parse(File.ReadAllText(path));
            }
            catch (Exception exception)
            {
                errors.Add($"invalid shops json: {exception.Message}");
                return false;
            }

            if (parsed is not Dictionary<string, object> root)
            {
                errors.Add("shops json root must be an object");
                return false;
            }

            Dictionary<string, object> owners = root.TryGetValue("shop_owner_npc_ids", out object ownersObject)
                && ownersObject is Dictionary<string, object> ownerMap
                ? ownerMap
                : new Dictionary<string, object>();

            if (!root.TryGetValue("shops", out object shopsObject) || shopsObject is not Dictionary<string, object> shopsMap)
            {
                errors.Add("shops json must contain a 'shops' object");
                return false;
            }

            foreach (KeyValuePair<string, object> pair in shopsMap)
            {
                if (pair.Value is not Dictionary<string, object> data)
                {
                    errors.Add($"shop {pair.Key} must be an object");
                    continue;
                }

                string shopId = pair.Key;
                string ownerNpcId = owners.TryGetValue(shopId, out object owner) && owner != null
                    ? owner.ToString()
                    : string.Empty;
                float buyRate = GetFloat(data, "buy_rate", 0.5f);

                int openStart = 0;
                int openEnd = 24;
                if (data.TryGetValue("open_hours", out object hoursObject) && hoursObject is Dictionary<string, object> hours)
                {
                    openStart = GetInt(hours, "start", 0);
                    openEnd = GetInt(hours, "end", 24);
                }

                List<MigrationShopItem> items = new List<MigrationShopItem>();
                if (data.TryGetValue("items", out object itemsObject) && itemsObject is List<object> itemList)
                {
                    foreach (object entry in itemList)
                    {
                        if (entry is Dictionary<string, object> itemData)
                        {
                            items.Add(new MigrationShopItem(
                                GetString(itemData, "id"),
                                GetInt(itemData, "price", 0),
                                GetInt(itemData, "stock", 0)));
                        }
                    }
                }

                shops[shopId] = new MigrationShopDefinition(shopId, ownerNpcId, buyRate, openStart, openEnd, items);
            }

            return shops.Count > 0 && errors.Count == 0;
        }

        public MigrationShopDefinition GetShop(string shopId)
        {
            return shopId != null && shops.TryGetValue(shopId, out MigrationShopDefinition shop) ? shop : null;
        }

        public IReadOnlyDictionary<string, MigrationShopDefinition> GetAllShops()
        {
            return shops;
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

        private static float GetFloat(Dictionary<string, object> data, string key, float fallback)
        {
            if (!data.TryGetValue(key, out object value) || value == null)
            {
                return fallback;
            }

            return value switch
            {
                double doubleValue => (float)doubleValue,
                long longValue => longValue,
                int intValue => intValue,
                float floatValue => floatValue,
                _ => float.TryParse(value.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed) ? parsed : fallback
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
