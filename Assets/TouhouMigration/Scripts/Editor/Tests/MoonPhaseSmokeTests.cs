using System;
using TouhouMigration.Runtime.Foundation;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    public static class MoonPhaseSmokeTests
    {
        [MenuItem("Touhou Migration/Tests/Run Moon Phase Smoke Tests")]
        public static void RunAll()
        {
            TestPhaseIndexCycle();
            TestFullMoonWindowAndWrap();
            Debug.Log("Moon phase smoke tests passed.");
        }

        private static void TestPhaseIndexCycle()
        {
            // 8 phases over 32 days, 4 days each (Godot WeatherSystem._update_moon_phase).
            AssertEqual(0, MigrationMoonPhase.PhaseIndex(0), "Day 0 is the new moon (phase 0).");
            AssertEqual(0, MigrationMoonPhase.PhaseIndex(3), "Day 3 is still phase 0.");
            AssertEqual(1, MigrationMoonPhase.PhaseIndex(4), "Day 4 advances to phase 1.");
            AssertEqual(4, MigrationMoonPhase.PhaseIndex(16), "Day 16 is the full moon (phase 4).");
            AssertEqual(7, MigrationMoonPhase.PhaseIndex(31), "Day 31 is the last phase (7).");
        }

        private static void TestFullMoonWindowAndWrap()
        {
            AssertEqual(false, MigrationMoonPhase.IsFullMoon(15), "Day 15 (phase 3) is not a full moon.");
            AssertEqual(true, MigrationMoonPhase.IsFullMoon(16), "Day 16 begins the full-moon window.");
            AssertEqual(true, MigrationMoonPhase.IsFullMoon(19), "Day 19 is still in the full-moon window.");
            AssertEqual(false, MigrationMoonPhase.IsFullMoon(20), "Day 20 (phase 5) leaves the full-moon window.");
            AssertEqual(true, MigrationMoonPhase.IsFullMoon(48), "The cycle wraps: day 48 (== day 16) is a full moon.");
            AssertEqual(false, MigrationMoonPhase.IsFullMoon(-1), "A negative day normalizes (day -1 -> phase 7, not full).");
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
