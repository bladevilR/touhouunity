using System;
using System.Collections.Generic;

namespace TouhouMigration.Runtime.Cooking
{
    [Serializable]
    public sealed class CookingBuffRuntimeSnapshot
    {
        public int schema_version = 1;
        public List<CookingBuffSlotSnapshot> buff_slots = new List<CookingBuffSlotSnapshot>();
        public CookingBuffDrinkSnapshot active_drink = new CookingBuffDrinkSnapshot();
        public List<CookingBuffStatValue> total_stats = new List<CookingBuffStatValue>();
        public List<string> unlocked_thresholds = new List<string>();

        public IReadOnlyList<CookingBuffSlotSnapshot> BuffSlots => buff_slots;

        public CookingBuffDrinkSnapshot ActiveDrink
        {
            get => active_drink;
            set => active_drink = value ?? new CookingBuffDrinkSnapshot();
        }

        public IReadOnlyList<CookingBuffStatValue> TotalStats => total_stats;
        public IReadOnlyList<string> UnlockedThresholds => unlocked_thresholds;

        public void SetTotalStat(string statKey, int value)
        {
            string normalizedStat = NormalizeId(statKey);
            if (string.IsNullOrEmpty(normalizedStat))
            {
                return;
            }

            CookingBuffStatValue existing = total_stats.Find(stat => stat.stat_key == normalizedStat);
            if (existing == null)
            {
                total_stats.Add(new CookingBuffStatValue { stat_key = normalizedStat, value = value });
            }
            else
            {
                existing.value = value;
            }
        }

        public int GetTotalStat(string statKey)
        {
            string normalizedStat = NormalizeId(statKey);
            CookingBuffStatValue existing = total_stats.Find(stat => stat.stat_key == normalizedStat);
            return existing != null ? existing.value : 0;
        }

        public void AddUnlockedThreshold(string token)
        {
            string normalizedToken = NormalizeId(token);
            if (!string.IsNullOrEmpty(normalizedToken) && !unlocked_thresholds.Contains(normalizedToken))
            {
                unlocked_thresholds.Add(normalizedToken);
            }
        }

        public bool HasUnlockedThreshold(string token)
        {
            return unlocked_thresholds.Contains(NormalizeId(token));
        }

        private static string NormalizeId(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToLowerInvariant();
        }
    }

    [Serializable]
    public sealed class CookingBuffSlotSnapshot
    {
        public int slot_index;
        public string dish_id = string.Empty;
        public string main_stat = string.Empty;
        public int quality;
        public int atk;
        public int def;
        public int spd;
        public int spi;
        public float duration;
        public float remaining;
        public List<string> special_effects = new List<string>();

        public int SlotIndex => slot_index;
        public string DishId => dish_id ?? string.Empty;
        public string MainStat => main_stat ?? string.Empty;
        public int Quality => quality;
        public float Duration => duration;
        public float Remaining => remaining;
        public IReadOnlyList<string> SpecialEffects => special_effects;
        public bool IsEmpty => string.IsNullOrWhiteSpace(dish_id);

        public int GetStat(string statKey)
        {
            return NormalizeId(statKey) switch
            {
                "atk" => atk,
                "def" => def,
                "spd" => spd,
                "spi" => spi,
                _ => 0
            };
        }

        public void SetStat(string statKey, int value)
        {
            switch (NormalizeId(statKey))
            {
                case "atk":
                    atk = value;
                    break;
                case "def":
                    def = value;
                    break;
                case "spd":
                    spd = value;
                    break;
                case "spi":
                    spi = value;
                    break;
            }
        }

        private static string NormalizeId(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToLowerInvariant();
        }
    }

    [Serializable]
    public sealed class CookingBuffDrinkSnapshot
    {
        public string dish_id = string.Empty;
        public int atk;
        public int def;
        public int spd;
        public int spi;
        public float duration;
        public float remaining;
        public List<string> drink_effects = new List<string>();

        public string DishId => dish_id ?? string.Empty;
        public float Duration => duration;
        public float Remaining => remaining;
        public IReadOnlyList<string> DrinkEffects => drink_effects;
        public bool IsEmpty => string.IsNullOrWhiteSpace(dish_id);

        public int GetStat(string statKey)
        {
            return NormalizeId(statKey) switch
            {
                "atk" => atk,
                "def" => def,
                "spd" => spd,
                "spi" => spi,
                _ => 0
            };
        }

        public void SetStat(string statKey, int value)
        {
            switch (NormalizeId(statKey))
            {
                case "atk":
                    atk = value;
                    break;
                case "def":
                    def = value;
                    break;
                case "spd":
                    spd = value;
                    break;
                case "spi":
                    spi = value;
                    break;
            }
        }

        private static string NormalizeId(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToLowerInvariant();
        }
    }

    [Serializable]
    public sealed class CookingBuffStatValue
    {
        public string stat_key = string.Empty;
        public int value;

        public string StatKey => stat_key ?? string.Empty;
        public int Value => value;
    }
}
