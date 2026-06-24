using System;
using TouhouMigration.Runtime.Foundation;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    public static class GameStateMachineSmokeTests
    {
        [MenuItem("Touhou Migration/Tests/Run Game State Machine Smoke Tests")]
        public static void RunAll()
        {
            TestChangeModeTransitionsAndFiresOnce();
            TestSameModeChangeIsNoOp();
            TestPushPopRestoresPreviousMode();
            Debug.Log("Game state machine smoke tests passed.");
        }

        private static void TestChangeModeTransitionsAndFiresOnce()
        {
            MigrationGameStateMachine machine = new MigrationGameStateMachine(MigrationGameStateMode.Menu);
            int events = 0;
            MigrationGameStateMode lastFrom = MigrationGameStateMode.Menu;
            MigrationGameStateMode lastTo = MigrationGameStateMode.Menu;
            machine.ModeChanged += (from, to) => { events++; lastFrom = from; lastTo = to; };

            AssertEqual(true, machine.ChangeMode(MigrationGameStateMode.Overworld), "Changing to a new mode succeeds.");
            AssertEqual(MigrationGameStateMode.Overworld, machine.CurrentMode, "Current mode updates.");
            AssertEqual(MigrationGameStateMode.Menu, machine.PreviousMode, "Previous mode records the prior mode.");
            AssertEqual(1, events, "ModeChanged fires once per transition.");
            AssertEqual(MigrationGameStateMode.Menu, lastFrom, "Event reports the from-mode.");
            AssertEqual(MigrationGameStateMode.Overworld, lastTo, "Event reports the to-mode.");
        }

        private static void TestSameModeChangeIsNoOp()
        {
            MigrationGameStateMachine machine = new MigrationGameStateMachine(MigrationGameStateMode.Overworld);
            int events = 0;
            machine.ModeChanged += (from, to) => events++;

            AssertEqual(false, machine.ChangeMode(MigrationGameStateMode.Overworld), "Changing to the current mode is a no-op.");
            AssertEqual(0, events, "No event fires for a no-op mode change.");
        }

        private static void TestPushPopRestoresPreviousMode()
        {
            MigrationGameStateMachine machine = new MigrationGameStateMachine(MigrationGameStateMode.Overworld);

            machine.Push(MigrationGameStateMode.Dialogue);
            AssertEqual(MigrationGameStateMode.Dialogue, machine.CurrentMode, "Push enters the transient mode.");
            AssertEqual(1, machine.Depth, "Push records one stacked mode.");

            machine.Push(MigrationGameStateMode.Combat);
            AssertEqual(MigrationGameStateMode.Combat, machine.CurrentMode, "Nested push enters combat over dialogue.");
            AssertEqual(2, machine.Depth, "Two modes are stacked.");

            AssertEqual(true, machine.Pop(), "Pop succeeds while the stack is non-empty.");
            AssertEqual(MigrationGameStateMode.Dialogue, machine.CurrentMode, "Pop restores dialogue.");
            AssertEqual(true, machine.Pop(), "Pop succeeds again.");
            AssertEqual(MigrationGameStateMode.Overworld, machine.CurrentMode, "Pop restores the base overworld mode.");
            AssertEqual(false, machine.Pop(), "Pop on an empty stack returns false.");
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
