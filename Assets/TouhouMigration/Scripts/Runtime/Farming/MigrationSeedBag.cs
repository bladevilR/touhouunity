using System;
using System.Collections.Generic;

namespace TouhouMigration.Runtime.Farming
{
    // The seed-bag gacha (Godot ShopManager.open_seed_bag + _roll_*_bag): a shop-bought bag opens into
    // three seeds rolled by rarity, with the active season's crops weighted x3 in the pool. Silver/gold
    // bags carry pity counters (silver: a RARE forced on the 3rd seed of the 10th unrewarded roll; gold:
    // a LEGENDARY forced on the 3rd seed of the 5th). UnityEngine-free with injected RNG so the rolls are
    // deterministic and unit-testable.
    public sealed class MigrationSeedBag
    {
        // Godot ShopManager fallback seed when a rarity bucket is unexpectedly empty.
        private const string FallbackSeed = "seed_pumpkin";

        private readonly MigrationCropDatabase crops;
        private readonly Func<double> randFloat;   // [0,1) rarity roll (Godot randf)
        private readonly Func<int, int> randIndex; // [0,n) pool pick (Godot randi % n)

        private int silverPity;
        private int goldPity;

        public MigrationSeedBag(MigrationCropDatabase crops, Func<double> randFloat, Func<int, int> randIndex)
        {
            this.crops = crops;
            this.randFloat = randFloat ?? (() => 0.0);
            this.randIndex = randIndex ?? (_ => 0);
        }

        // Open a bag ("bamboo" | "silver" | "gold") in the given season -> three seed item ids. An unknown
        // bag type yields an empty list (Godot push_error + return []).
        public IReadOnlyList<string> Open(string bagType, MigrationCropSeason season)
        {
            switch (bagType)
            {
                case "bamboo": return RollBamboo(season);
                case "silver": return RollSilver(season);
                case "gold": return RollGold(season);
                default: return Array.Empty<string>();
            }
        }

        private List<string> RollBamboo(MigrationCropSeason season)
        {
            List<string> seeds = new List<string>();
            for (int i = 0; i < 3; i++)
            {
                double roll = randFloat();
                MigrationCropRarity rarity = roll < 0.70
                    ? MigrationCropRarity.Common
                    : roll < 0.95
                        ? MigrationCropRarity.Uncommon
                        : MigrationCropRarity.Rare;
                seeds.Add(RandomSeedByRarity(rarity, season));
            }

            return seeds;
        }

        private List<string> RollSilver(MigrationCropSeason season)
        {
            List<string> seeds = new List<string>();
            silverPity++;

            for (int i = 0; i < 3; i++)
            {
                double roll = randFloat();
                MigrationCropRarity rarity;
                if (silverPity >= 10 && i == 2)
                {
                    rarity = MigrationCropRarity.Rare;
                    silverPity = 0;
                }
                else if (roll < 0.20)
                {
                    rarity = MigrationCropRarity.Common;
                }
                else if (roll < 0.70)
                {
                    rarity = MigrationCropRarity.Uncommon;
                }
                else if (roll < 0.95)
                {
                    rarity = MigrationCropRarity.Rare;
                }
                else
                {
                    rarity = MigrationCropRarity.Legendary;
                }

                seeds.Add(RandomSeedByRarity(rarity, season));

                if (rarity >= MigrationCropRarity.Rare)
                {
                    silverPity = 0;
                }
            }

            return seeds;
        }

        private List<string> RollGold(MigrationCropSeason season)
        {
            List<string> seeds = new List<string>();
            goldPity++;

            for (int i = 0; i < 3; i++)
            {
                double roll = randFloat();
                MigrationCropRarity rarity;
                if (goldPity >= 5 && i == 2)
                {
                    rarity = MigrationCropRarity.Legendary;
                    goldPity = 0;
                }
                else if (roll < 0.30)
                {
                    rarity = MigrationCropRarity.Uncommon;
                }
                else if (roll < 0.80)
                {
                    rarity = MigrationCropRarity.Rare;
                }
                else
                {
                    rarity = MigrationCropRarity.Legendary;
                }

                seeds.Add(RandomSeedByRarity(rarity, season));

                if (rarity == MigrationCropRarity.Legendary)
                {
                    goldPity = 0;
                }
            }

            return seeds;
        }

        // Godot _get_random_seed_by_rarity: build a weighted pool of the rarity's crops (in-season x3,
        // off-season x1), pick one, and map crop_<x> -> seed_<x>. Empty pool -> the fallback seed.
        private string RandomSeedByRarity(MigrationCropRarity rarity, MigrationCropSeason season)
        {
            List<string> weightedPool = new List<string>();
            if (crops != null)
            {
                foreach (string cropId in crops.GetCropsByRarity(rarity))
                {
                    int weight = crops.CanPlantInSeason(cropId, season) ? 3 : 1;
                    for (int w = 0; w < weight; w++)
                    {
                        weightedPool.Add(cropId);
                    }
                }
            }

            if (weightedPool.Count == 0)
            {
                return FallbackSeed;
            }

            int index = randIndex(weightedPool.Count);
            if (index < 0 || index >= weightedPool.Count)
            {
                index = 0;
            }

            return ToSeedId(weightedPool[index]);
        }

        private static string ToSeedId(string cropId)
        {
            const string prefix = "crop_";
            string trimmed = cropId != null && cropId.StartsWith(prefix, StringComparison.Ordinal)
                ? cropId.Substring(prefix.Length)
                : cropId ?? string.Empty;
            return "seed_" + trimmed;
        }
    }
}
