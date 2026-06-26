using System;
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

        private static void AssertEqual<T>(T expected, T actual, string message)
        {
            if (!Equals(expected, actual))
            {
                throw new Exception($"{message} Expected: {expected}. Actual: {actual}.");
            }
        }
    }
}
