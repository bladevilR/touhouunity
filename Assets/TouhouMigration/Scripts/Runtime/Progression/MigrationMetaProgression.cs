using System;
using System.Collections.Generic;

namespace TouhouMigration.Runtime.Progression
{
    // A single meta (between-run) upgrade definition (Godot MetaProgressionData.MetaUpgrade): a leveled
    // upgrade whose cost grows geometrically and whose effect grows linearly with level.
    public sealed class MigrationMetaUpgrade
    {
        public string Id { get; }
        public int MaxLevel { get; }
        public int BaseCost { get; }
        public double CostMultiplier { get; }
        public double EffectPerLevel { get; }
        public string EffectType { get; }

        public MigrationMetaUpgrade(string id, int maxLevel, int baseCost, double costMultiplier, double effectPerLevel, string effectType)
        {
            Id = id;
            MaxLevel = maxLevel;
            BaseCost = baseCost;
            CostMultiplier = costMultiplier;
            EffectPerLevel = effectPerLevel;
            EffectType = effectType;
        }

        // Cost to buy the next level from the given current level (Godot get_cost_for_level): -1 if maxed,
        // else base_cost * multiplier^level, truncated.
        public int GetCostForLevel(int level)
        {
            if (level >= MaxLevel)
            {
                return -1;
            }

            return (int)(BaseCost * Math.Pow(CostMultiplier, level));
        }

        // Total effect at the given level (Godot get_effect_at_level).
        public double GetEffectAtLevel(int level)
        {
            return EffectPerLevel * level;
        }
    }

    // The between-run upgrade economy (Godot MetaProgressionManager): spend a meta currency to level up
    // upgrades from a catalog, and total their effects by type. Currency + levels are held in-memory here
    // (Godot delegates them to GameSaveManager). Free of UnityEngine. The concrete upgrade catalog
    // (MetaProgressionData.initialize) and the SignalBus emissions are deferred.
    public sealed class MigrationMetaProgression
    {
        private readonly Dictionary<string, MigrationMetaUpgrade> upgrades = new Dictionary<string, MigrationMetaUpgrade>();
        private readonly Dictionary<string, int> levels = new Dictionary<string, int>();
        private int currency;

        public int Currency => currency;

        public void RegisterUpgrade(MigrationMetaUpgrade upgrade)
        {
            if (upgrade != null && !string.IsNullOrWhiteSpace(upgrade.Id))
            {
                upgrades[upgrade.Id] = upgrade;
            }
        }

        public void AddCurrency(int amount)
        {
            currency += amount;
        }

        public bool CanAfford(int amount)
        {
            return currency >= amount;
        }

        public int GetUpgradeLevel(string id)
        {
            return levels.TryGetValue(id ?? string.Empty, out int level) ? level : 0;
        }

        public bool IsUpgradeMaxed(string id)
        {
            MigrationMetaUpgrade upgrade = GetUpgrade(id);
            if (upgrade == null)
            {
                return true;
            }

            return GetUpgradeLevel(id) >= upgrade.MaxLevel;
        }

        public int GetNextUpgradeCost(string id)
        {
            MigrationMetaUpgrade upgrade = GetUpgrade(id);
            if (upgrade == null)
            {
                return -1;
            }

            return upgrade.GetCostForLevel(GetUpgradeLevel(id));
        }

        public bool CanPurchaseUpgrade(string id)
        {
            MigrationMetaUpgrade upgrade = GetUpgrade(id);
            if (upgrade == null)
            {
                return false;
            }

            int level = GetUpgradeLevel(id);
            if (level >= upgrade.MaxLevel)
            {
                return false;
            }

            return CanAfford(upgrade.GetCostForLevel(level));
        }

        public bool PurchaseUpgrade(string id)
        {
            MigrationMetaUpgrade upgrade = GetUpgrade(id);
            if (upgrade == null)
            {
                return false;
            }

            int level = GetUpgradeLevel(id);
            if (level >= upgrade.MaxLevel)
            {
                return false;
            }

            int cost = upgrade.GetCostForLevel(level);
            if (!CanAfford(cost))
            {
                return false;
            }

            currency -= cost;
            levels[id] = level + 1;
            return true;
        }

        // Sum the effect of every owned upgrade of the given type (Godot get_total_bonus).
        public double GetTotalBonus(string effectType)
        {
            double total = 0.0;
            foreach (MigrationMetaUpgrade upgrade in upgrades.Values)
            {
                if (upgrade.EffectType == effectType)
                {
                    total += upgrade.GetEffectAtLevel(GetUpgradeLevel(upgrade.Id));
                }
            }

            return total;
        }

        private MigrationMetaUpgrade GetUpgrade(string id)
        {
            return upgrades.TryGetValue(id ?? string.Empty, out MigrationMetaUpgrade upgrade) ? upgrade : null;
        }
    }
}
