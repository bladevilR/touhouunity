using System;
using System.Collections.Generic;
using TouhouMigration.Runtime.Farming;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    // Covers MigrationSeedBag: the shop seed-bag gacha (Godot ShopManager.open_seed_bag + _roll_*_bag),
    // with deterministic injected RNG. Verifies the rarity thresholds, the crop->seed id mapping, the
    // gold pity (forced LEGENDARY on the 5th roll), and the unknown-bag guard.
    public static class SeedBagSmokeTests
    {
        private const string CropDataPath = "Assets/TouhouMigration/Data/Farming/crops.json";

        [MenuItem("Touhou Migration/Tests/Run Seed Bag Smoke Tests")]
        public static void RunAll()
        {
            TestBambooLowRollGivesThreeCommonSeeds();
            TestBambooHighRollGivesRareSeeds();
            TestGoldPityForcesLegendaryOnFifthRoll();
            TestUnknownBagTypeYieldsNothing();
            Debug.Log("Seed bag smoke tests passed.");
        }

        private static MigrationCropDatabase LoadCrops()
        {
            MigrationCropDatabase database = new MigrationCropDatabase();
            AssertEqual(true, database.LoadFromPath(CropDataPath),
                "crops.json should load. Errors: " + string.Join("; ", database.Errors));
            return database;
        }

        private static void TestBambooLowRollGivesThreeCommonSeeds()
        {
            MigrationCropDatabase crops = LoadCrops();
            // randFloat 0.0 -> COMMON for every bamboo seed; randIndex 0 -> first pool entry.
            MigrationSeedBag bag = new MigrationSeedBag(crops, () => 0.0, _ => 0);

            IReadOnlyList<string> seeds = bag.Open("bamboo", MigrationCropSeason.Spring);

            AssertEqual(3, seeds.Count, "A bamboo bag yields three seeds.");
            foreach (string seed in seeds)
            {
                AssertEqual(true, seed.StartsWith("seed_", StringComparison.Ordinal), "Each result is a seed id.");
                AssertEqual(MigrationCropRarity.Common, RarityOfSeed(crops, seed),
                    "A 0.0 roll yields a COMMON crop's seed.");
            }
        }

        private static void TestBambooHighRollGivesRareSeeds()
        {
            MigrationCropDatabase crops = LoadCrops();
            // randFloat 0.99 -> RARE (>= 0.95) for every bamboo seed.
            MigrationSeedBag bag = new MigrationSeedBag(crops, () => 0.99, _ => 0);

            IReadOnlyList<string> seeds = bag.Open("bamboo", MigrationCropSeason.Spring);

            AssertEqual(3, seeds.Count, "A bamboo bag yields three seeds.");
            AssertEqual(MigrationCropRarity.Rare, RarityOfSeed(crops, seeds[0]),
                "A 0.99 roll yields a RARE crop's seed.");
        }

        private static void TestGoldPityForcesLegendaryOnFifthRoll()
        {
            MigrationCropDatabase crops = LoadCrops();
            // 0.0 rolls -> gold gives UNCOMMON (< 0.30), which never resets the pity counter, so by the
            // 5th Open the counter hits 5 and the 3rd seed is forced LEGENDARY.
            MigrationSeedBag bag = new MigrationSeedBag(crops, () => 0.0, _ => 0);

            IReadOnlyList<string> fifth = null;
            for (int i = 0; i < 5; i++)
            {
                fifth = bag.Open("gold", MigrationCropSeason.Spring);
            }

            AssertEqual(MigrationCropRarity.Uncommon, RarityOfSeed(crops, fifth[0]),
                "Non-pity gold seeds at a 0.0 roll are UNCOMMON.");
            AssertEqual(MigrationCropRarity.Legendary, RarityOfSeed(crops, fifth[2]),
                "The 3rd seed of the 5th gold roll is the forced pity LEGENDARY.");
        }

        private static void TestUnknownBagTypeYieldsNothing()
        {
            MigrationSeedBag bag = new MigrationSeedBag(LoadCrops(), () => 0.0, _ => 0);
            AssertEqual(0, bag.Open("platinum", MigrationCropSeason.Spring).Count,
                "An unknown bag type yields no seeds.");
        }

        // Map seed_<x> back to crop_<x> and read its rarity from the catalog.
        private static MigrationCropRarity RarityOfSeed(MigrationCropDatabase crops, string seedId)
        {
            const string prefix = "seed_";
            string cropId = "crop_" + (seedId != null && seedId.StartsWith(prefix, StringComparison.Ordinal)
                ? seedId.Substring(prefix.Length)
                : seedId);
            MigrationCropDefinition crop = crops.GetCrop(cropId);
            if (crop == null)
            {
                throw new Exception($"Seed {seedId} did not map back to a known crop ({cropId}).");
            }

            return crop.Rarity;
        }

        private static void AssertEqual<T>(T expected, T actual, string message)
        {
            if (!Equals(expected, actual))
            {
                throw new Exception($"{message} Expected: {expected}. Actual: {actual}.");
            }
        }
    }
}
