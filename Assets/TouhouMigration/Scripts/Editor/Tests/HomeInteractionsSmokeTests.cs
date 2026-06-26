using System;
using TouhouMigration.Runtime.Home;
using TouhouMigration.Runtime.Player;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    // Covers MigrationHomeInteractions: the bamboo-home interactions' fatigue effects (Godot
    // HomeInteractionSystem interact_sleep / tea / meal / read_book).
    public static class HomeInteractionsSmokeTests
    {
        private const double Tol = 1e-9;

        [MenuItem("Touhou Migration/Tests/Run Home Interactions Smoke Tests")]
        public static void RunAll()
        {
            TestTeaAndMealRecover();
            TestReadBookAddsFatigue();
            TestSleepFullyRecovers();
            TestNullFatigueIsSafe();
            Debug.Log("Home interactions smoke tests passed.");
        }

        private static void TestTeaAndMealRecover()
        {
            MigrationFatigueSystem fatigue = new MigrationFatigueSystem();
            fatigue.AddFatigue(50.0);
            MigrationHomeInteractions home = new MigrationHomeInteractions(fatigue);

            home.Tea();
            AssertTrue(Math.Abs(45.0 - fatigue.CurrentFatigue) < Tol, "Tea recovers 5 fatigue.");

            home.Meal();
            AssertTrue(Math.Abs(35.0 - fatigue.CurrentFatigue) < Tol, "A meal recovers 10 fatigue.");
        }

        private static void TestReadBookAddsFatigue()
        {
            MigrationFatigueSystem fatigue = new MigrationFatigueSystem();
            fatigue.AddFatigue(20.0);
            MigrationHomeInteractions home = new MigrationHomeInteractions(fatigue);

            home.ReadBook();
            AssertTrue(Math.Abs(22.0 - fatigue.CurrentFatigue) < Tol, "Reading a book costs 2 fatigue.");
        }

        private static void TestSleepFullyRecovers()
        {
            MigrationFatigueSystem fatigue = new MigrationFatigueSystem();
            fatigue.AddFatigue(90.0);
            MigrationHomeInteractions home = new MigrationHomeInteractions(fatigue);

            home.Sleep();
            AssertEqual(0.0, fatigue.CurrentFatigue, "Sleep fully recovers fatigue.");
        }

        private static void TestNullFatigueIsSafe()
        {
            MigrationHomeInteractions home = new MigrationHomeInteractions(null);
            home.Tea();
            home.Meal();
            home.ReadBook();
            home.Sleep();
            // No exception thrown.
        }

        private static void AssertTrue(bool condition, string message)
        {
            if (!condition)
            {
                throw new Exception(message);
            }
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
