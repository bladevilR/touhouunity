using System;
using System.Collections.Generic;
using TouhouMigration.Runtime.Combat;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    // Covers MigrationLootDropRoller: the rank-evaluation bonus-drop rolls (Godot LootDropManager
    // grant_rank_bonus_drops + RANK_DROP_MULTIPLIERS), RNG-injected for determinism.
    public static class LootDropRollerSmokeTests
    {
        private const double Tol = 1e-9;

        [MenuItem("Touhou Migration/Tests/Run Loot Drop Roller Smoke Tests")]
        public static void RunAll()
        {
            TestRankMultipliers();
            TestLowRankNoBonus();
            TestRankCrystalsAndCompost();
            TestDeepFloorBonusDropsWhenRolled();
            TestDeepFloorNoBonusOnHighRolls();
            Debug.Log("Loot drop roller smoke tests passed.");
        }

        private static Func<double> Floats(params double[] values)
        {
            int i = 0;
            return () => values[Math.Min(i++, values.Length - 1)];
        }

        private static int CountOf(IReadOnlyList<LootDrop> drops, string itemId)
        {
            int total = 0;
            foreach (LootDrop drop in drops)
            {
                if (drop.ItemId == itemId)
                {
                    total += drop.Count;
                }
            }

            return total;
        }

        private static void TestRankMultipliers()
        {
            MigrationLootDropRoller roller = new MigrationLootDropRoller();
            AssertTrue(Math.Abs(2.5 - roller.RankMultiplier("S")) < Tol, "S rank multiplier is 2.5.");
            AssertTrue(Math.Abs(1.0 - roller.RankMultiplier("B")) < Tol, "B rank multiplier is 1.0.");
            AssertTrue(Math.Abs(1.0 - roller.RankMultiplier("unranked")) < Tol, "An unknown rank defaults to 1.0.");
        }

        private static void TestLowRankNoBonus()
        {
            MigrationLootDropRoller roller = new MigrationLootDropRoller();
            IReadOnlyList<LootDrop> drops = roller.RollRankBonusDrops("B", floorLevel: 0, Floats(0.0), _ => 0);
            AssertEqual(0, drops.Count, "A B-rank shallow run grants no bonus drops.");
        }

        private static void TestRankCrystalsAndCompost()
        {
            MigrationLootDropRoller roller = new MigrationLootDropRoller();

            // A rank -> 1 crystal; floor 0 -> no deep drops; not S -> no compost.
            IReadOnlyList<LootDrop> a = roller.RollRankBonusDrops("A", floorLevel: 0, Floats(1.0), _ => 0);
            AssertEqual(1, a.Count, "A rank grants one crystal at a shallow floor.");
            AssertEqual("element_crystal_fire", a[0].ItemId, "randIndex 0 picks the first crystal.");

            // S rank -> 2 crystals + 2 dungeon_compost; floor 0 -> no deep drops.
            IReadOnlyList<LootDrop> s = roller.RollRankBonusDrops("S", floorLevel: 0, Floats(1.0), _ => 0);
            AssertEqual(2, CountOf(s, "element_crystal_fire"), "S rank grants two crystals.");
            AssertEqual(2, CountOf(s, "dungeon_compost"), "S rank grants two dungeon compost.");
        }

        private static void TestDeepFloorBonusDropsWhenRolled()
        {
            MigrationLootDropRoller roller = new MigrationLootDropRoller();
            // S rank, floor 3, all rolls 0.0 (always under threshold): 2 crystals + spirit_soil + rare seed + compost.
            IReadOnlyList<LootDrop> drops = roller.RollRankBonusDrops("S", floorLevel: 3, Floats(0.0, 0.0), _ => 0);
            AssertEqual(2, CountOf(drops, "element_crystal_fire"), "S rank deep run keeps the two crystals.");
            AssertEqual(1, CountOf(drops, "spirit_soil"), "A low roll grants spirit soil at depth.");
            AssertEqual(1, CountOf(drops, "seed_spirit_bloom"), "A low roll grants a rare seed (index 0) at depth.");
            AssertEqual(2, CountOf(drops, "dungeon_compost"), "S rank still grants compost.");
        }

        private static void TestDeepFloorNoBonusOnHighRolls()
        {
            MigrationLootDropRoller roller = new MigrationLootDropRoller();
            // A rank, floor 3, rolls 1.0 (above any threshold): just the 1 crystal, no deep drops.
            IReadOnlyList<LootDrop> drops = roller.RollRankBonusDrops("A", floorLevel: 3, Floats(1.0, 1.0), _ => 0);
            AssertEqual(1, drops.Count, "High rolls grant no deep bonus drops.");
            AssertEqual(0, CountOf(drops, "spirit_soil"), "No spirit soil on a high roll.");
        }

        private static void AssertTrue(bool condition, string message)
        {
            if (!condition)
            {
                throw new Exception(message);
            }
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
