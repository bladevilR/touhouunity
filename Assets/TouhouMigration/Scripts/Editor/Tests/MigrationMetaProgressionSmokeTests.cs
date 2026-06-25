using System;
using TouhouMigration.Runtime.Progression;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    // Covers MigrationMetaProgression: the between-run upgrade economy (Godot MetaProgressionManager +
    // MetaProgressionData.MetaUpgrade) — affordability, geometric cost scaling, level cap, and effect bonuses.
    // State is held in-memory here (Godot delegates currency/levels to GameSaveManager).
    public static class MigrationMetaProgressionSmokeTests
    {
        [MenuItem("Touhou Migration/Tests/Run Migration Meta Progression Smoke Tests")]
        public static void RunAll()
        {
            TestPurchaseDeductsCurrencyAndRaisesLevel();
            TestCannotAffordBlocksPurchase();
            TestCostScalesGeometrically();
            TestMaxLevelBlocksPurchase();
            TestTotalBonusSumsEffectsByType();
            TestUnknownUpgradeIsSafe();
            Debug.Log("Migration meta progression smoke tests passed.");
        }

        private static MigrationMetaUpgrade Hp()
        {
            // max 3 levels, base cost 100, x2 per level, +10 max_hp per level
            return new MigrationMetaUpgrade("hp", 3, 100, 2.0, 10.0, "max_hp");
        }

        private static MigrationMetaProgression BuildProgression(params MigrationMetaUpgrade[] upgrades)
        {
            MigrationMetaProgression meta = new MigrationMetaProgression();
            foreach (MigrationMetaUpgrade upgrade in upgrades)
            {
                meta.RegisterUpgrade(upgrade);
            }

            return meta;
        }

        private static void TestPurchaseDeductsCurrencyAndRaisesLevel()
        {
            MigrationMetaProgression meta = BuildProgression(Hp());
            meta.AddCurrency(100);
            AssertEqual(true, meta.CanPurchaseUpgrade("hp"), "Can purchase when affordable and below max level.");
            AssertEqual(true, meta.PurchaseUpgrade("hp"), "An affordable purchase succeeds.");
            AssertEqual(0, meta.Currency, "Purchase deducts the cost from currency.");
            AssertEqual(1, meta.GetUpgradeLevel("hp"), "Purchase raises the upgrade level.");
        }

        private static void TestCannotAffordBlocksPurchase()
        {
            MigrationMetaProgression meta = BuildProgression(Hp());
            meta.AddCurrency(50);
            AssertEqual(false, meta.CanPurchaseUpgrade("hp"), "Cannot purchase when the cost is unaffordable.");
            AssertEqual(false, meta.PurchaseUpgrade("hp"), "An unaffordable purchase fails.");
            AssertEqual(0, meta.GetUpgradeLevel("hp"), "A failed purchase does not raise the level.");
            AssertEqual(50, meta.Currency, "A failed purchase does not spend currency.");
        }

        private static void TestCostScalesGeometrically()
        {
            MigrationMetaProgression meta = BuildProgression(Hp());
            meta.AddCurrency(1000);
            AssertEqual(100, meta.GetNextUpgradeCost("hp"), "Level-0 cost is the base cost.");
            meta.PurchaseUpgrade("hp");
            AssertEqual(200, meta.GetNextUpgradeCost("hp"), "Cost scales by the multiplier (100*2^1).");
            meta.PurchaseUpgrade("hp");
            AssertEqual(400, meta.GetNextUpgradeCost("hp"), "Cost continues to scale (100*2^2).");
        }

        private static void TestMaxLevelBlocksPurchase()
        {
            MigrationMetaProgression meta = BuildProgression(Hp());
            meta.AddCurrency(1000);
            meta.PurchaseUpgrade("hp"); // 100
            meta.PurchaseUpgrade("hp"); // 200
            meta.PurchaseUpgrade("hp"); // 400 -> level 3 (max)
            AssertEqual(3, meta.GetUpgradeLevel("hp"), "Three purchases reach the max level.");
            AssertEqual(true, meta.IsUpgradeMaxed("hp"), "The upgrade is maxed at max level.");
            AssertEqual(false, meta.CanPurchaseUpgrade("hp"), "A maxed upgrade cannot be purchased.");
            AssertEqual(-1, meta.GetNextUpgradeCost("hp"), "A maxed upgrade reports -1 next cost.");
        }

        private static void TestTotalBonusSumsEffectsByType()
        {
            MigrationMetaUpgrade atk1 = new MigrationMetaUpgrade("atk1", 5, 10, 1.0, 5.0, "atk");
            MigrationMetaUpgrade atk2 = new MigrationMetaUpgrade("atk2", 5, 10, 1.0, 3.0, "atk");
            MigrationMetaProgression meta = BuildProgression(atk1, atk2);
            meta.AddCurrency(1000);
            meta.PurchaseUpgrade("atk1");
            meta.PurchaseUpgrade("atk1"); // atk1 level 2 -> 10
            meta.PurchaseUpgrade("atk2"); // atk2 level 1 -> 3
            AssertEqual(13.0, meta.GetTotalBonus("atk"), "Total bonus sums effect-per-level*level across same-type upgrades.");
            AssertEqual(0.0, meta.GetTotalBonus("other"), "An unused effect type contributes no bonus.");
        }

        private static void TestUnknownUpgradeIsSafe()
        {
            MigrationMetaProgression meta = BuildProgression(Hp());
            AssertEqual(-1, meta.GetNextUpgradeCost("nope"), "Unknown upgrade has -1 next cost.");
            AssertEqual(false, meta.CanPurchaseUpgrade("nope"), "Unknown upgrade cannot be purchased.");
            AssertEqual(true, meta.IsUpgradeMaxed("nope"), "Unknown upgrade is treated as maxed.");
            AssertEqual(false, meta.PurchaseUpgrade("nope"), "Purchasing an unknown upgrade fails.");
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
