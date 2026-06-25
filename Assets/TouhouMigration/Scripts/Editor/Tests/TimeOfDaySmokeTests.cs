using System;
using TouhouMigration.Runtime.Foundation;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    public static class TimeOfDaySmokeTests
    {
        [MenuItem("Touhou Migration/Tests/Run Time Of Day Smoke Tests")]
        public static void RunAll()
        {
            TestHourBandsMatchGodotPeriods();
            TestBoundaryHoursAndNormalization();
            Debug.Log("Time of day smoke tests passed.");
        }

        private static void TestHourBandsMatchGodotPeriods()
        {
            AssertEqual("midnight", MigrationTimeOfDay.FromHour(2), "00:00-05:00 is midnight.");
            AssertEqual("dawn", MigrationTimeOfDay.FromHour(6), "05:00-07:00 is dawn.");
            AssertEqual("morning", MigrationTimeOfDay.FromHour(9), "07:00-12:00 is morning.");
            AssertEqual("noon", MigrationTimeOfDay.FromHour(13), "12:00-14:00 is noon.");
            AssertEqual("afternoon", MigrationTimeOfDay.FromHour(15), "14:00-17:00 is afternoon.");
            AssertEqual("evening", MigrationTimeOfDay.FromHour(18), "17:00-20:00 is evening.");
            AssertEqual("night", MigrationTimeOfDay.FromHour(22), "20:00-24:00 is night.");
        }

        private static void TestBoundaryHoursAndNormalization()
        {
            AssertEqual("midnight", MigrationTimeOfDay.FromHour(0), "Hour 0 is midnight.");
            AssertEqual("dawn", MigrationTimeOfDay.FromHour(5), "Hour 5 starts dawn (inclusive).");
            AssertEqual("morning", MigrationTimeOfDay.FromHour(7), "Hour 7 starts morning (dawn is exclusive at 7).");
            AssertEqual("midnight", MigrationTimeOfDay.FromHour(24), "Hour 24 normalizes to 0 (midnight).");
            AssertEqual("night", MigrationTimeOfDay.FromHour(-2), "A negative hour normalizes into the 0..23 range (22 -> night).");
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
