using System;
using System.Collections.Generic;
using TouhouMigration.Runtime.Weapons;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    // Covers MigrationWeaponFusion: the spell-card weapon fusion logic (Godot FusionSystem + WeaponData
    // recipes) — order-independent recipe matching, MAX-level gating, and available-fusion discovery.
    public static class WeaponFusionSmokeTests
    {
        [MenuItem("Touhou Migration/Tests/Run Weapon Fusion Smoke Tests")]
        public static void RunAll()
        {
            TestRecipesLoad();
            TestFindRecipeIsOrderIndependent();
            TestCanFuseGating();
            TestAvailableFusions();
            Debug.Log("Weapon fusion smoke tests passed.");
        }

        // Test weapon levels: a small inventory of owned weapons at given levels (0 = not owned).
        private static Func<string, int> Levels(Dictionary<string, int> levels)
        {
            return id => id != null && levels.TryGetValue(id, out int level) ? level : 0;
        }

        private static void TestRecipesLoad()
        {
            MigrationWeaponFusion fusion = new MigrationWeaponFusion();
            AssertEqual(3, fusion.Recipes.Count, "There are three spell-card fusion recipes.");
        }

        private static void TestFindRecipeIsOrderIndependent()
        {
            MigrationWeaponFusion fusion = new MigrationWeaponFusion();
            MigrationWeaponRecipe dream = fusion.FindRecipe("homing_amulet", "yin_yang_orb");
            AssertEqual(true, dream != null, "homing_amulet + yin_yang_orb has a recipe.");
            AssertEqual("dream_seal", dream.ResultWeaponId, "That recipe yields dream_seal.");

            MigrationWeaponRecipe reversed = fusion.FindRecipe("yin_yang_orb", "homing_amulet");
            AssertEqual("dream_seal", reversed.ResultWeaponId, "Recipe matching is order-independent.");

            AssertEqual(true, fusion.FindRecipe("knives", "homing_amulet") == null, "A non-recipe pair has no recipe.");
        }

        private static void TestCanFuseGating()
        {
            MigrationWeaponFusion fusion = new MigrationWeaponFusion();

            // Both MAX -> can fuse.
            var both = Levels(new Dictionary<string, int> { ["star_dust"] = 3, ["laser"] = 3 });
            WeaponFusionCheck ok = fusion.CanFuse("star_dust", "laser", both);
            AssertEqual(true, ok.CanFuse, "Two MAX-level recipe weapons can fuse.");
            AssertEqual("master_spark", ok.Recipe.ResultWeaponId, "The fusion yields master_spark.");

            // One under MAX -> blocked on level.
            var under = Levels(new Dictionary<string, int> { ["star_dust"] = 2, ["laser"] = 3 });
            WeaponFusionCheck low = fusion.CanFuse("star_dust", "laser", under);
            AssertEqual(false, low.CanFuse, "A sub-MAX weapon cannot fuse.");
            AssertEqual(true, low.Reason.Contains("MAX"), "The block reason mentions the MAX-level requirement.");

            // Not owned -> blocked.
            var missing = Levels(new Dictionary<string, int> { ["laser"] = 3 });
            WeaponFusionCheck notOwned = fusion.CanFuse("star_dust", "laser", missing);
            AssertEqual(false, notOwned.CanFuse, "An unowned weapon cannot fuse.");

            // Both MAX but no recipe -> blocked on recipe.
            var noRecipe = Levels(new Dictionary<string, int> { ["star_dust"] = 3, ["knives"] = 3 });
            WeaponFusionCheck none = fusion.CanFuse("star_dust", "knives", noRecipe);
            AssertEqual(false, none.CanFuse, "MAX weapons with no recipe cannot fuse.");
            AssertEqual("没有匹配的融合配方", none.Reason, "The reason reports the missing recipe.");
        }

        private static void TestAvailableFusions()
        {
            MigrationWeaponFusion fusion = new MigrationWeaponFusion();
            var owned = new List<string> { "homing_amulet", "yin_yang_orb", "laser" };
            var levels = Levels(new Dictionary<string, int>
            {
                ["homing_amulet"] = 3,
                ["yin_yang_orb"] = 3,
                ["laser"] = 3, // star_dust not owned -> master_spark unavailable
            });

            IReadOnlyList<MigrationWeaponRecipe> available = fusion.AvailableFusions(owned, levels);
            AssertEqual(1, available.Count, "Only the fully-MAX recipe pair is available.");
            AssertEqual("dream_seal", available[0].ResultWeaponId, "The available fusion is dream_seal.");
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
