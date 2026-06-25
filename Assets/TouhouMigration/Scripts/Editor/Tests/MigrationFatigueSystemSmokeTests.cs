using System;
using TouhouMigration.Runtime.Player;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    // Covers MigrationFatigueSystem: the player fatigue stat (Godot FatigueSystem) — accumulation/clamping,
    // level thresholds, exhausted/collapse flags, and sleep/rest recovery. The signal emissions and the
    // collapse->teleport-home flow are scene-coupled and deferred.
    public static class MigrationFatigueSystemSmokeTests
    {
        [MenuItem("Touhou Migration/Tests/Run Migration Fatigue System Smoke Tests")]
        public static void RunAll()
        {
            TestAddFatigueClampsToRange();
            TestFatigueLevelAndDescriptionByThreshold();
            TestExhaustedAndCollapseFlagsSet();
            TestSleepFullRecoveryResetsEverything();
            TestRestRecoveryReducesAndClearsExhausted();
            Debug.Log("Migration fatigue system smoke tests passed.");
        }

        private static void TestAddFatigueClampsToRange()
        {
            MigrationFatigueSystem fatigue = new MigrationFatigueSystem();
            fatigue.AddFatigue(50.0);
            AssertEqual(50.0, fatigue.CurrentFatigue, "Adding fatigue accumulates the value.");
            fatigue.AddFatigue(70.0);
            AssertEqual(100.0, fatigue.CurrentFatigue, "Fatigue clamps at 100.");
            fatigue.AddFatigue(-130.0);
            AssertEqual(0.0, fatigue.CurrentFatigue, "Fatigue clamps at 0.");
        }

        private static void TestFatigueLevelAndDescriptionByThreshold()
        {
            MigrationFatigueSystem fatigue = new MigrationFatigueSystem();
            AssertEqual(FatigueLevel.Normal, fatigue.Level, "Zero fatigue is Normal.");
            AssertEqual("正常", fatigue.GetFatigueDescription(), "Normal fatigue description.");
            fatigue.AddFatigue(60.0);
            AssertEqual(FatigueLevel.Tired, fatigue.Level, "60 fatigue is Tired.");
            AssertEqual("疲惫", fatigue.GetFatigueDescription(), "Tired description.");
            fatigue.AddFatigue(20.0);
            AssertEqual(FatigueLevel.Exhausted, fatigue.Level, "80 fatigue is Exhausted.");
            fatigue.AddFatigue(20.0);
            AssertEqual(FatigueLevel.Collapse, fatigue.Level, "100 fatigue is Collapse.");
            AssertEqual("即将昏倒", fatigue.GetFatigueDescription(), "Collapse description.");
        }

        private static void TestExhaustedAndCollapseFlagsSet()
        {
            MigrationFatigueSystem fatigue = new MigrationFatigueSystem();
            fatigue.AddFatigue(80.0);
            AssertEqual(true, fatigue.IsExhausted, "Crossing into Exhausted sets the exhausted flag.");
            AssertEqual(false, fatigue.HasCollapsed, "Exhausted is not yet collapsed.");
            fatigue.AddFatigue(20.0);
            AssertEqual(true, fatigue.HasCollapsed, "Reaching 100 sets the collapsed flag.");
        }

        private static void TestSleepFullRecoveryResetsEverything()
        {
            MigrationFatigueSystem fatigue = new MigrationFatigueSystem();
            fatigue.AddFatigue(90.0);
            AssertEqual(true, fatigue.IsExhausted, "Precondition: exhausted before sleep.");
            fatigue.SleepFullRecovery();
            AssertEqual(0.0, fatigue.CurrentFatigue, "Sleep clears fatigue to zero.");
            AssertEqual(false, fatigue.IsExhausted, "Sleep clears the exhausted flag.");
            AssertEqual(FatigueLevel.Normal, fatigue.Level, "Sleep returns to Normal.");
        }

        private static void TestRestRecoveryReducesAndClearsExhausted()
        {
            MigrationFatigueSystem fatigue = new MigrationFatigueSystem();
            fatigue.AddFatigue(85.0);
            AssertEqual(true, fatigue.IsExhausted, "Precondition: exhausted at 85.");
            fatigue.RestRecovery(20.0);
            AssertEqual(65.0, fatigue.CurrentFatigue, "Rest reduces fatigue by the amount.");
            AssertEqual(false, fatigue.IsExhausted, "Resting below 80 clears the exhausted flag.");
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
