using System;
using TouhouMigration.Runtime.Foundation;
using TouhouMigration.Runtime.Player;
using TouhouMigration.Runtime.Quest;
using TouhouMigration.Runtime.Social;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    // Covers MigrationDayCycle: the day-loop orchestrator that advances the clock on sleep and fans the
    // per-day resets (farming growth, daily quests, bond daily interactions) + sleep fatigue recovery
    // out to the life-sim services via GameClock.DayStarted.
    public static class MigrationDayCycleSmokeTests
    {
        private const string QuestDataPath = "Assets/TouhouMigration/Data/Quests/quests.json";

        [MenuItem("Touhou Migration/Tests/Run Migration Day Cycle Smoke Tests")]
        public static void RunAll()
        {
            TestSleepAdvancesOneDayAndRestoresFatigue();
            TestSleepRunsDailyQuestReset();
            TestNaturalMidnightCrossingRunsDailyReset();
            TestNullServicesAreSafe();
            Debug.Log("Migration day cycle smoke tests passed.");
        }

        private static void TestSleepAdvancesOneDayAndRestoresFatigue()
        {
            GameClock clock = new GameClock();
            clock.SetTime(22, 0);
            MigrationFatigueSystem fatigue = new MigrationFatigueSystem();
            fatigue.AddFatigue(90.0);

            int startDay = clock.Day;
            MigrationDayCycle cycle = new MigrationDayCycle(clock, null, null, null, fatigue);
            cycle.Sleep();

            AssertEqual(startDay + 1, clock.Day, "Sleep advances exactly one day.");
            AssertEqual(MigrationDayCycle.WakeHour, clock.Hour, "Sleep wakes at the wake hour.");
            AssertEqual(0, clock.Minute, "Sleep wakes on the hour.");
            AssertEqual(0.0, fatigue.CurrentFatigue, "Sleep fully restores fatigue.");
            AssertEqual(1, cycle.DailyResetsRun, "Exactly one day-reset runs per sleep.");
            AssertEqual(clock.Day, cycle.LastResetDay, "The day-reset uses the new day number.");
            cycle.Detach();
        }

        private static void TestSleepRunsDailyQuestReset()
        {
            QuestDatabase database = new QuestDatabase();
            AssertEqual(true, database.LoadFromPath(QuestDataPath), "Quest database loads for the day-cycle test.");
            QuestDeliveryService quests = new QuestDeliveryService(database);

            GameClock clock = new GameClock();
            clock.SetTime(23, 0);
            MigrationDayCycle cycle = new MigrationDayCycle(clock, null, quests, null, null);
            cycle.Sleep();

            AssertEqual(clock.Day, quests.LastDailyResetDay, "Sleeping resets the daily quests to the new day.");
            cycle.Detach();
        }

        private static void TestNaturalMidnightCrossingRunsDailyReset()
        {
            GameClock clock = new GameClock();
            clock.SetTime(23, 30);
            int startDay = clock.Day;
            MigrationDayCycle cycle = new MigrationDayCycle(clock);

            // Time passing naturally across midnight (not via Sleep) must still run the day reset.
            clock.AdvanceMinutes(60);

            AssertEqual(startDay + 1, clock.Day, "Advancing past midnight rolls the day.");
            AssertEqual(1, cycle.DailyResetsRun, "Crossing midnight runs the day reset once.");
            AssertEqual(clock.Day, cycle.LastResetDay, "The natural day-reset uses the new day.");
            cycle.Detach();
        }

        private static void TestNullServicesAreSafe()
        {
            GameClock clock = new GameClock();
            clock.SetTime(20, 0);
            int startDay = clock.Day;
            MigrationDayCycle cycle = new MigrationDayCycle(clock);

            cycle.Sleep();

            AssertEqual(startDay + 1, clock.Day, "Sleep advances the day even with no life-sim services.");
            AssertEqual(1, cycle.DailyResetsRun, "Day reset still runs (no-op) with all services null.");
            cycle.Detach();
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
