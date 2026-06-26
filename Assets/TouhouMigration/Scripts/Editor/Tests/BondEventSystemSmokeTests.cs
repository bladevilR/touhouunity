using System;
using System.Collections.Generic;
using TouhouMigration.Runtime.Social;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    // Covers MigrationBondEventSystem: the bond-event lifecycle (Godot BondEventSystem get_available_events
    // / start_event / complete_event) — bond-level gating, completion tracking, and cooldowns.
    public static class BondEventSystemSmokeTests
    {
        [MenuItem("Touhou Migration/Tests/Run Bond Event System Smoke Tests")]
        public static void RunAll()
        {
            TestBondLevelGating();
            TestStartAndCompleteRemovesFromAvailable();
            TestCannotStartUnavailable();
            TestCooldownGatesReavailability();
            Debug.Log("Bond event system smoke tests passed.");
        }

        private static bool Contains(IReadOnlyList<string> list, string id)
        {
            foreach (string entry in list)
            {
                if (entry == id)
                {
                    return true;
                }
            }

            return false;
        }

        private static void TestBondLevelGating()
        {
            MigrationBondEventSystem sys = new MigrationBondEventSystem();
            sys.RegisterEvent("keine", "keine_scrolls", requiredBondLevel: 2);

            sys.Evaluate("keine", bondLevel: 1);
            AssertEqual(false, sys.HasAvailableEvent("keine"), "An event below the bond requirement is unavailable.");

            sys.Evaluate("keine", bondLevel: 2);
            AssertEqual(true, sys.HasAvailableEvent("keine"), "Meeting the bond level makes the event available.");
            AssertEqual(true, Contains(sys.GetAvailableEvents("keine"), "keine_scrolls"), "The event id is listed.");
        }

        private static void TestStartAndCompleteRemovesFromAvailable()
        {
            MigrationBondEventSystem sys = new MigrationBondEventSystem();
            sys.RegisterEvent("keine", "keine_scrolls", requiredBondLevel: 2);
            sys.Evaluate("keine", 2);

            AssertEqual(true, sys.StartEvent("keine", "keine_scrolls"), "An available event can be started.");
            AssertEqual(true, sys.CompleteEvent("keine", "keine_scrolls"), "A started event can be completed.");
            AssertEqual(true, sys.IsEventCompleted("keine", "keine_scrolls"), "The event is marked completed.");

            sys.Evaluate("keine", 5); // even at higher bond, a completed one-time event stays unavailable
            AssertEqual(false, sys.HasAvailableEvent("keine"), "A completed event does not re-offer.");
        }

        private static void TestCannotStartUnavailable()
        {
            MigrationBondEventSystem sys = new MigrationBondEventSystem();
            sys.RegisterEvent("keine", "keine_scrolls", requiredBondLevel: 2);
            sys.Evaluate("keine", 1); // not available
            AssertEqual(false, sys.StartEvent("keine", "keine_scrolls"), "An unavailable event cannot be started.");
            AssertEqual(false, sys.StartEvent("keine", "unknown_event"), "An unknown event cannot be started.");
        }

        private static void TestCooldownGatesReavailability()
        {
            MigrationBondEventSystem sys = new MigrationBondEventSystem();
            // A repeatable daily-style event modelled with a cooldown.
            sys.RegisterEvent("reimu", "tea_time", requiredBondLevel: 1, cooldownDays: 2);
            sys.Evaluate("reimu", 1);
            sys.StartEvent("reimu", "tea_time");
            sys.CompleteEvent("reimu", "tea_time");

            // On cooldown -> not available even though bond is met (and completed-once is allowed to repeat).
            sys.Evaluate("reimu", 1);
            AssertEqual(false, sys.HasAvailableEvent("reimu"), "An event on cooldown is not re-offered.");

            sys.TickCooldowns();
            sys.TickCooldowns(); // cooldown 2 -> 0
            sys.Evaluate("reimu", 1);
            AssertEqual(true, sys.HasAvailableEvent("reimu"), "After the cooldown elapses the repeatable event returns.");
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
