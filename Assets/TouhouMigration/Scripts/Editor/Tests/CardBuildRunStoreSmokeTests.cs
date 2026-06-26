using System;
using System.Collections.Generic;
using TouhouMigration.Runtime.CardBuild;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    // Covers MigrationCardBuildRunStore: per-character current-run persistence (Godot CardBuildRunStore
    // save/load/clear_current_run), storing whole-run snapshots.
    public static class CardBuildRunStoreSmokeTests
    {
        [MenuItem("Touhou Migration/Tests/Run Card Build Run Store Smoke Tests")]
        public static void RunAll()
        {
            TestSaveLoadClear();
            TestRunResumesFromStore();
            TestFileSnapshotRoundTrip();
            Debug.Log("Card build run store smoke tests passed.");
        }

        private static void TestSaveLoadClear()
        {
            MigrationCardBuildRunStore store = new MigrationCardBuildRunStore();
            AssertEqual(false, store.HasCurrentRun("fujiwara_no_mokou"), "No run is stored initially.");
            AssertEqual(true, store.LoadCurrentRun("fujiwara_no_mokou") == null, "Loading an absent run returns null.");

            CardBuildRunSnapshot snap = new CardBuildRunSnapshot { bossHp = 321 };
            store.SaveCurrentRun("fujiwara_no_mokou", snap);
            AssertEqual(true, store.HasCurrentRun("fujiwara_no_mokou"), "A saved run is present.");
            AssertEqual(321, store.LoadCurrentRun("fujiwara_no_mokou").bossHp, "The saved run round-trips.");

            // Runs are per-character.
            AssertEqual(false, store.HasCurrentRun("reimu"), "A different character has no run.");

            store.ClearCurrentRun("fujiwara_no_mokou");
            AssertEqual(false, store.HasCurrentRun("fujiwara_no_mokou"), "Clearing removes the run.");
        }

        private static void TestRunResumesFromStore()
        {
            // A live run is captured, stored, then resumed into a fresh controller via the store.
            MigrationCardBuildRunController run = new MigrationCardBuildRunController(
                new List<string> { "a", "b", "c" });
            run.SetupCirnoRun();
            run.State.AddResource("ember", 4);
            run.Boss.Damage(200); // boss hp 340

            MigrationCardBuildRunStore store = new MigrationCardBuildRunStore();
            store.SaveCurrentRun("fujiwara_no_mokou", run.CaptureSnapshot());

            MigrationCardBuildRunController resumed = new MigrationCardBuildRunController(new List<string> { "x" });
            resumed.RestoreSnapshot(store.LoadCurrentRun("fujiwara_no_mokou"));

            AssertEqual(340, resumed.BossHp, "The resumed run keeps the boss HP.");
            AssertEqual(4, resumed.State.GetResource("ember"), "The resumed run keeps resources.");
            AssertEqual(true, resumed.Clauses.IsExposed("terrain_tyranny"), "The resumed run keeps the boss setup.");
        }

        private static void TestFileSnapshotRoundTrip()
        {
            MigrationCardBuildRunStore store = new MigrationCardBuildRunStore();
            store.SaveCurrentRun("fujiwara_no_mokou", new CardBuildRunSnapshot { bossHp = 250, rewrittenRuleCount = 2 });
            store.SaveCurrentRun("reimu", new CardBuildRunSnapshot { bossHp = 99 });

            CardBuildRunStoreFile file = store.CreateFileSnapshot();
            AssertEqual(2, file.runs.Count, "The file snapshot holds both characters' runs.");

            // The file snapshot is JsonUtility-safe end to end (what a save service writes/reads).
            string json = JsonUtility.ToJson(file);
            CardBuildRunStoreFile roundTripped = JsonUtility.FromJson<CardBuildRunStoreFile>(json);

            MigrationCardBuildRunStore restored = new MigrationCardBuildRunStore();
            restored.LoadFileSnapshot(roundTripped);

            AssertEqual(250, restored.LoadCurrentRun("fujiwara_no_mokou").bossHp, "Mokou's run survives a JSON round-trip.");
            AssertEqual(2, restored.LoadCurrentRun("fujiwara_no_mokou").rewrittenRuleCount, "Run scalars survive the round-trip.");
            AssertEqual(99, restored.LoadCurrentRun("reimu").bossHp, "Reimu's run survives too.");
            AssertEqual(false, restored.HasCurrentRun("marisa"), "An unsaved character has no run after load.");
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
