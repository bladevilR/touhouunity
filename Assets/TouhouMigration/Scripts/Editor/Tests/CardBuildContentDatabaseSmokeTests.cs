using System;
using TouhouMigration.Runtime.CardBuild;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    // Covers MigrationCardBuildContentDatabase: loading relics.json + upgrades.json into queryable
    // definitions that feed MigrationCardProgression (Godot CardBuildDatabase relics/upgrades indexing).
    public static class CardBuildContentDatabaseSmokeTests
    {
        private const string RelicsPath = "Assets/TouhouMigration/Data/CardBuild/relics.json";
        private const string UpgradesPath = "Assets/TouhouMigration/Data/CardBuild/upgrades.json";
        private const double Tol = 1e-9;

        [MenuItem("Touhou Migration/Tests/Run CardBuild Content Database Smoke Tests")]
        public static void RunAll()
        {
            TestLoadsRelicsAndUpgrades();
            TestUpgradeFeedsProgression();
            Debug.Log("CardBuild content database smoke tests passed.");
        }

        private static void TestLoadsRelicsAndUpgrades()
        {
            MigrationCardBuildContentDatabase db = new MigrationCardBuildContentDatabase();
            AssertEqual(true, db.LoadFromPaths(RelicsPath, UpgradesPath), "Content loads. Errors: " + string.Join("; ", db.Errors));
            AssertEqual(3, db.RelicCount, "All three relics load.");
            AssertEqual(4, db.UpgradeCount, "All four upgrades load.");

            MigrationRelic relic = db.GetRelic("phoenix_ash_lantern");
            AssertEqual(true, relic != null, "The phoenix ash lantern relic loads.");
            AssertEqual(true, relic.EffectBlocks.Count > 0, "Its effect blocks are parsed.");

            MigrationCardUpgrade upgrade = db.GetUpgrade("quickened_fire_bird");
            AssertEqual(true, upgrade != null, "The quickened fire bird upgrade loads.");
            AssertEqual("mokou_starter_fire_bird", upgrade.TargetCardId, "The upgrade targets the fire bird card.");
            AssertEqual("set_cooldown", upgrade.Operation, "Its operation is set_cooldown.");
            AssertEqual(true, upgrade.Cooldown.HasValue && Math.Abs(1.4 - upgrade.Cooldown.Value) < Tol, "Its cooldown is 1.4.");
        }

        private static void TestUpgradeFeedsProgression()
        {
            MigrationCardBuildContentDatabase db = new MigrationCardBuildContentDatabase();
            db.LoadFromPaths(RelicsPath, UpgradesPath);

            // The loaded upgrade applies through the (already-tested) progression.
            MigrationCardProgression progression = new MigrationCardProgression();
            MigrationUpgradeableCard card = new MigrationUpgradeableCard { Id = "mokou_starter_fire_bird", Cooldown = 5.0 };
            progression.ApplyUpgradeToCard(card, db.GetUpgrade("quickened_fire_bird"));
            AssertEqual(true, Math.Abs(1.4 - card.Cooldown) < Tol, "The loaded upgrade lowers the card's cooldown via progression.");

            // The loaded relics' effect blocks flatten through the progression.
            System.Collections.Generic.List<MigrationRelic> relics = new System.Collections.Generic.List<MigrationRelic> { db.GetRelic("phoenix_ash_lantern") };
            AssertEqual(true, progression.CollectRelicEffectBlocks(relics).Count > 0, "Loaded relic effect blocks collect through progression.");
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
