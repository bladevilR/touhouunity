using System;
using TouhouMigration.Runtime.Foundation;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    // Covers MigrationLocationState: player location + unlocked-location tracking (Godot GlobalGameState
    // location management).
    public static class LocationStateSmokeTests
    {
        [MenuItem("Touhou Migration/Tests/Run Location State Smoke Tests")]
        public static void RunAll()
        {
            TestDefaults();
            TestSetLocation();
            TestUnlockIsIdempotent();
            TestReset();
            Debug.Log("Location state smoke tests passed.");
        }

        private static void TestDefaults()
        {
            MigrationLocationState loc = new MigrationLocationState();
            AssertEqual("human_village", loc.CurrentLocation, "The player starts in the human village.");
            AssertEqual(true, loc.IsLocationUnlocked("human_village"), "The human village is unlocked by default.");
            AssertEqual(true, loc.IsLocationUnlocked("keine_house"), "Keine's house is unlocked by default.");
            AssertEqual(true, loc.IsLocationUnlocked("bamboo_forest"), "The bamboo forest is unlocked by default.");
            AssertEqual(false, loc.IsLocationUnlocked("magic_forest"), "The magic forest starts locked.");
            AssertEqual(3, loc.UnlockedLocations.Count, "Three locations are unlocked at the start.");
        }

        private static void TestSetLocation()
        {
            MigrationLocationState loc = new MigrationLocationState();
            loc.SetLocation("magic_forest");
            AssertEqual("magic_forest", loc.CurrentLocation, "SetLocation moves the player.");
        }

        private static void TestUnlockIsIdempotent()
        {
            MigrationLocationState loc = new MigrationLocationState();
            loc.UnlockLocation("magic_forest");
            AssertEqual(true, loc.IsLocationUnlocked("magic_forest"), "Unlocking adds the location.");
            AssertEqual(4, loc.UnlockedLocations.Count, "The unlocked set grows by one.");

            loc.UnlockLocation("magic_forest"); // already unlocked
            AssertEqual(4, loc.UnlockedLocations.Count, "Re-unlocking does not duplicate the location.");
        }

        private static void TestReset()
        {
            MigrationLocationState loc = new MigrationLocationState();
            loc.SetLocation("magic_forest");
            loc.UnlockLocation("magic_forest");

            loc.Reset();
            AssertEqual("human_village", loc.CurrentLocation, "Reset returns the player to the human village.");
            AssertEqual(false, loc.IsLocationUnlocked("magic_forest"), "Reset relocks runtime-unlocked locations.");
            AssertEqual(3, loc.UnlockedLocations.Count, "Reset restores the three default locations.");
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
