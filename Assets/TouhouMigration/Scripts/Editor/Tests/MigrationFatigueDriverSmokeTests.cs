using System;
using TouhouMigration.Runtime.Foundation;
using TouhouMigration.Runtime.Player;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    // Covers MigrationFatigueDriver: the accrual half of the fatigue loop — each game hour adds activity
    // fatigue (Godot FatigueSystem per-hour rates), scaled by the current activity, driven off
    // GameClock.HourChanged.
    public static class MigrationFatigueDriverSmokeTests
    {
        [MenuItem("Touhou Migration/Tests/Run Migration Fatigue Driver Smoke Tests")]
        public static void RunAll()
        {
            TestActiveAccruesEachHour();
            TestIdleDoesNotAccrue();
            TestFarmingUsesFarmingRate();
            TestRateForMapping();
            Debug.Log("Migration fatigue driver smoke tests passed.");
        }

        private static void TestActiveAccruesEachHour()
        {
            GameClock clock = new GameClock();
            clock.SetTime(8, 0);
            MigrationFatigueSystem fatigue = new MigrationFatigueSystem();
            MigrationFatigueDriver driver = new MigrationFatigueDriver(clock, fatigue);

            clock.AdvanceMinutes(4 * GameClock.MinutesPerHour);

            AssertEqual(4, driver.HoursAccrued, "Active activity accrues once per game hour.");
            AssertEqual(4.0 * MigrationFatigueSystem.FatiguePerHourActive, fatigue.CurrentFatigue, "Active fatigue equals hours * per-hour rate.");
            driver.Detach();
        }

        private static void TestIdleDoesNotAccrue()
        {
            GameClock clock = new GameClock();
            clock.SetTime(8, 0);
            MigrationFatigueSystem fatigue = new MigrationFatigueSystem();
            MigrationFatigueDriver driver = new MigrationFatigueDriver(clock, fatigue)
            {
                CurrentActivity = MigrationFatigueDriver.Activity.Idle,
            };

            clock.AdvanceMinutes(3 * GameClock.MinutesPerHour);

            AssertEqual(0, driver.HoursAccrued, "Idle activity accrues no fatigue.");
            AssertEqual(0.0, fatigue.CurrentFatigue, "Idle leaves fatigue unchanged.");
            driver.Detach();
        }

        private static void TestFarmingUsesFarmingRate()
        {
            GameClock clock = new GameClock();
            clock.SetTime(8, 0);
            MigrationFatigueSystem fatigue = new MigrationFatigueSystem();
            MigrationFatigueDriver driver = new MigrationFatigueDriver(clock, fatigue)
            {
                CurrentActivity = MigrationFatigueDriver.Activity.Farming,
            };

            clock.AdvanceMinutes(2 * GameClock.MinutesPerHour);

            AssertEqual(2.0 * MigrationFatigueSystem.FatiguePerHourFarming, fatigue.CurrentFatigue, "Farming uses the farming per-hour rate.");
            driver.Detach();
        }

        private static void TestRateForMapping()
        {
            AssertEqual(MigrationFatigueSystem.FatiguePerHourActive, MigrationFatigueDriver.RateFor(MigrationFatigueDriver.Activity.Active), "Active rate.");
            AssertEqual(MigrationFatigueSystem.FatiguePerHourFarming, MigrationFatigueDriver.RateFor(MigrationFatigueDriver.Activity.Farming), "Farming rate.");
            AssertEqual(MigrationFatigueSystem.FatiguePerHourMining, MigrationFatigueDriver.RateFor(MigrationFatigueDriver.Activity.Mining), "Mining rate.");
            AssertEqual(0.0, MigrationFatigueDriver.RateFor(MigrationFatigueDriver.Activity.Idle), "Idle rate is zero.");
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
