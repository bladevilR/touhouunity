using System;
using System.Collections.Generic;
using TouhouMigration.Runtime.CardBuild;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    // Covers MigrationCardRunState: the CardBuild run resource + per-target status substrate (Godot
    // CardBuildRuntimeState resource/status methods) that card resolution builds on.
    public static class CardRunStateSmokeTests
    {
        [MenuItem("Touhou Migration/Tests/Run Card Run State Smoke Tests")]
        public static void RunAll()
        {
            TestResourceAccrualAndDefault();
            TestSpendResourcePartialAllAndEmpty();
            TestStatusIsPerTargetAndDefaults();
            TestConsumeStatusPartialAllAndErase();
            TestMissingCostResource();
            TestSnapshotRoundTrip();
            Debug.Log("Card run state smoke tests passed.");
        }

        private static void TestResourceAccrualAndDefault()
        {
            MigrationCardRunState state = new MigrationCardRunState();
            AssertEqual(0, state.GetResource("ember"), "An untouched resource reads 0.");

            state.AddResource("ember", 3);
            state.AddResource("ember", 2);
            AssertEqual(5, state.GetResource("ember"), "AddResource accumulates.");

            state.AddResource("ember", 0);
            state.AddResource("", 4);
            AssertEqual(5, state.GetResource("ember"), "Zero amount and empty id are ignored.");
        }

        private static void TestSpendResourcePartialAllAndEmpty()
        {
            MigrationCardRunState state = new MigrationCardRunState();
            state.AddResource("fate", 5);

            AssertEqual(2, state.SpendResource("fate", 2), "Spending 2 returns 2 spent.");
            AssertEqual(3, state.GetResource("fate"), "Spending decrements the pool.");

            AssertEqual(3, state.SpendResource("fate", 10), "Spending more than available clamps to the current pool.");
            AssertEqual(0, state.GetResource("fate"), "Over-spend drains the pool to 0.");

            AssertEqual(0, state.SpendResource("fate", 1), "Spending an empty pool returns 0.");

            state.AddResource("seal", 4);
            AssertEqual(4, state.SpendResource("seal"), "A negative/default amount spends the entire pool.");
            AssertEqual(0, state.GetResource("seal"), "Spending all empties the pool.");
        }

        private static void TestStatusIsPerTargetAndDefaults()
        {
            MigrationCardRunState state = new MigrationCardRunState();
            AssertEqual(0, state.GetStatus("enemy", "burn"), "An untouched status reads 0.");

            state.ApplyStatus("enemy", "burn", 3);
            state.ApplyStatus("player", "burn", 1);
            state.ApplyStatus("enemy", "burn", 2);

            AssertEqual(5, state.GetStatus("enemy", "burn"), "Statuses accumulate per target.");
            AssertEqual(1, state.GetStatus("player", "burn"), "Statuses on a different target are independent.");

            state.ApplyStatus("enemy", "burn", 0);
            state.ApplyStatus("", "burn", 4);
            AssertEqual(5, state.GetStatus("enemy", "burn"), "Zero amount and empty target/status are ignored.");
        }

        private static void TestConsumeStatusPartialAllAndErase()
        {
            MigrationCardRunState state = new MigrationCardRunState();
            state.ApplyStatus("enemy", "fate_lock", 5);

            AssertEqual(2, state.ConsumeStatus("enemy", "fate_lock", 2), "Consuming 2 returns 2.");
            AssertEqual(3, state.GetStatus("enemy", "fate_lock"), "Consuming decrements the stack.");

            AssertEqual(3, state.ConsumeStatus("enemy", "fate_lock"), "A default amount consumes the whole stack.");
            AssertEqual(0, state.GetStatus("enemy", "fate_lock"), "Consuming all erases the status.");

            AssertEqual(0, state.ConsumeStatus("enemy", "fate_lock", 1), "Consuming an absent status returns 0.");
        }

        private static void TestMissingCostResource()
        {
            MigrationCardRunState state = new MigrationCardRunState();
            state.AddResource("ember", 2);
            state.AddResource("fate", 1);

            AssertEqual("", state.MissingCostResource(new Dictionary<string, int> { ["ember"] = 2, ["fate"] = 1 }),
                "An affordable cost reports no missing resource.");
            AssertEqual("fate", state.MissingCostResource(new Dictionary<string, int> { ["ember"] = 1, ["fate"] = 2 }),
                "The first unaffordable resource is reported.");
            AssertEqual("", state.MissingCostResource(new Dictionary<string, int> { ["ember"] = 0 }),
                "A zero requirement is always satisfied.");
            AssertEqual("", state.MissingCostResource(null),
                "A null cost has nothing missing.");
            AssertEqual("seal", state.MissingCostResource(new Dictionary<string, int> { ["seal"] = 1 }),
                "An entirely-absent resource is reported as missing.");
        }

        private static void TestSnapshotRoundTrip()
        {
            MigrationCardRunState state = new MigrationCardRunState();
            state.AddResource("ember", 3);
            state.AddResource("seal", 1);
            state.ApplyStatus("enemy", "burn", 4);
            state.ApplyStatus("player", "guard", 2);

            CardRunStateSnapshot snapshot = state.CreateSnapshot();

            MigrationCardRunState restored = new MigrationCardRunState();
            restored.AddResource("stale", 99); // overwritten by the load
            restored.LoadSnapshot(snapshot);

            AssertEqual(3, restored.GetResource("ember"), "Resources round-trip through the snapshot.");
            AssertEqual(1, restored.GetResource("seal"), "All resources are restored.");
            AssertEqual(0, restored.GetResource("stale"), "Loading clears prior resource state.");
            AssertEqual(4, restored.GetStatus("enemy", "burn"), "Per-target statuses round-trip.");
            AssertEqual(2, restored.GetStatus("player", "guard"), "Statuses are restored per target.");

            // The snapshot is independent of later source mutations.
            state.AddResource("ember", 5);
            MigrationCardRunState again = new MigrationCardRunState();
            again.LoadSnapshot(snapshot);
            AssertEqual(3, again.GetResource("ember"), "The snapshot is independent of later source changes.");
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
