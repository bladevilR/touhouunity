using System;
using TouhouMigration.Runtime.Social;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    public static class NpcScheduleSmokeTests
    {
        [MenuItem("Touhou Migration/Tests/Run Npc Schedule Smoke Tests")]
        public static void RunAll()
        {
            TestLocationByHourBlocks();
            TestWrapAroundNightSchedule();
            TestFallsBackToHomeWhenNoEntryMatches();
            Debug.Log("Npc schedule smoke tests passed.");
        }

        private static void TestLocationByHourBlocks()
        {
            MigrationNpcSchedule schedule = new MigrationNpcSchedule();
            schedule.AddEntry(new MigrationNpcScheduleEntry(8, 12, "shrine"));
            schedule.AddEntry(new MigrationNpcScheduleEntry(12, 18, "market"));

            AssertEqual("shrine", schedule.LocationAt(9, "home"), "Hour 9 is in the 8-12 shrine block.");
            AssertEqual("shrine", schedule.LocationAt(11, "home"), "Hour 11 is still in the shrine block.");
            AssertEqual("market", schedule.LocationAt(12, "home"), "Hour 12 starts the market block (8-12 end is exclusive).");
            AssertEqual("market", schedule.LocationAt(17, "home"), "Hour 17 is in the market block.");
            AssertEqual("home", schedule.LocationAt(20, "home"), "Hour 20 matches no block -> home fallback.");
        }

        private static void TestWrapAroundNightSchedule()
        {
            MigrationNpcSchedule schedule = new MigrationNpcSchedule();
            schedule.AddEntry(new MigrationNpcScheduleEntry(22, 2, "tavern"));

            AssertEqual("tavern", schedule.LocationAt(23, "home"), "A wrap-around night block covers 23:00.");
            AssertEqual("tavern", schedule.LocationAt(1, "home"), "A wrap-around night block covers 01:00.");
            AssertEqual("home", schedule.LocationAt(2, "home"), "End hour is exclusive across midnight.");
            AssertEqual("home", schedule.LocationAt(12, "home"), "Daytime is outside the night block -> home.");
        }

        private static void TestFallsBackToHomeWhenNoEntryMatches()
        {
            MigrationNpcSchedule schedule = new MigrationNpcSchedule();
            AssertEqual("bamboo_home", schedule.LocationAt(10, "bamboo_home"), "An empty schedule returns the home location.");
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
