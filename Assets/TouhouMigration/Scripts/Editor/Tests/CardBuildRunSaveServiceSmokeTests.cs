using System;
using System.IO;
using TouhouMigration.Runtime.CardBuild;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    // Covers MigrationCardBuildRunSaveService: writing/reading the per-character card-run store to/from a
    // JSON file (Godot CardBuildRunStore file IO).
    public static class CardBuildRunSaveServiceSmokeTests
    {
        [MenuItem("Touhou Migration/Tests/Run Card Build Run Save Service Smoke Tests")]
        public static void RunAll()
        {
            TestSaveLoadRoundTripsThroughDisk();
            TestLoadMissingFileIsFalse();
            Debug.Log("Card build run save service smoke tests passed.");
        }

        private static void TestSaveLoadRoundTripsThroughDisk()
        {
            string path = Path.Combine(Path.GetTempPath(), "cardrun_save_" + Guid.NewGuid().ToString("N") + ".json");
            try
            {
                MigrationCardBuildRunStore store = new MigrationCardBuildRunStore();
                store.SaveCurrentRun("fujiwara_no_mokou", new CardBuildRunSnapshot { bossHp = 275, rewrittenRuleCount = 3 });

                AssertEqual(true, MigrationCardBuildRunSaveService.SaveToFile(store, path), "Saving to disk succeeds.");
                AssertEqual(true, File.Exists(path), "The save file is written.");

                MigrationCardBuildRunStore loaded = new MigrationCardBuildRunStore();
                AssertEqual(true, MigrationCardBuildRunSaveService.LoadFromFile(loaded, path), "Loading from disk succeeds.");
                AssertEqual(275, loaded.LoadCurrentRun("fujiwara_no_mokou").bossHp, "The run survives the disk round-trip.");
                AssertEqual(3, loaded.LoadCurrentRun("fujiwara_no_mokou").rewrittenRuleCount, "Run scalars survive the disk round-trip.");
            }
            finally
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }

        private static void TestLoadMissingFileIsFalse()
        {
            MigrationCardBuildRunStore store = new MigrationCardBuildRunStore();
            string missing = Path.Combine(Path.GetTempPath(), "cardrun_absent_" + Guid.NewGuid().ToString("N") + ".json");
            AssertEqual(false, MigrationCardBuildRunSaveService.LoadFromFile(store, missing),
                "Loading a missing file reports failure.");
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
