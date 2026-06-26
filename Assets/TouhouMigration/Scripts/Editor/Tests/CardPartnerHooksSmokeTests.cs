using System;
using System.Collections.Generic;
using TouhouMigration.Runtime.CardBuild;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    // Covers MigrationCardPartnerHooks: partner-hook registration + event resolution (Godot
    // CardPartnerHookController register_hook / resolve_event / _matches_filter).
    public static class CardPartnerHooksSmokeTests
    {
        [MenuItem("Touhou Migration/Tests/Run Card Partner Hooks Smoke Tests")]
        public static void RunAll()
        {
            TestTriggerMatch();
            TestTagFilter();
            TestFieldFilter();
            TestMultipleHooks();
            Debug.Log("Card partner hooks smoke tests passed.");
        }

        private static void TestTriggerMatch()
        {
            MigrationCardPartnerHooks hooks = new MigrationCardPartnerHooks();
            hooks.RegisterHook(new MigrationPartnerHook { Id = "h1", Trigger = "on_hit", PartnerEventId = "pe_strike" });
            AssertEqual(1, hooks.HookCount, "A registered hook is counted.");

            int fired = hooks.ResolveEvent(new MigrationPartnerEventTrigger { Trigger = "on_hit" });
            AssertEqual(1, fired, "A matching trigger fires the hook.");
            AssertEqual(1, hooks.FiredPartnerEvents.Count, "The partner event is recorded.");

            AssertEqual(0, hooks.ResolveEvent(new MigrationPartnerEventTrigger { Trigger = "on_dodge" }),
                "A non-matching trigger fires nothing.");
        }

        private static void TestTagFilter()
        {
            MigrationCardPartnerHooks hooks = new MigrationCardPartnerHooks();
            hooks.RegisterHook(new MigrationPartnerHook { Id = "h", Trigger = "on_hit", PartnerEventId = "pe", RequiredTag = "fire" });

            MigrationPartnerEventTrigger withTag = new MigrationPartnerEventTrigger { Trigger = "on_hit" };
            withTag.Tags.Add("fire");
            AssertEqual(1, hooks.ResolveEvent(withTag), "An event carrying the required tag fires the hook.");

            AssertEqual(0, hooks.ResolveEvent(new MigrationPartnerEventTrigger { Trigger = "on_hit" }),
                "An event missing the required tag does not fire.");
        }

        private static void TestFieldFilter()
        {
            MigrationCardPartnerHooks hooks = new MigrationCardPartnerHooks();
            MigrationPartnerHook hook = new MigrationPartnerHook { Id = "h", Trigger = "on_hit", PartnerEventId = "pe" };
            hook.RequiredFields["element"] = "fire";
            hooks.RegisterHook(hook);

            MigrationPartnerEventTrigger match = new MigrationPartnerEventTrigger { Trigger = "on_hit" };
            match.Fields["element"] = "fire";
            AssertEqual(1, hooks.ResolveEvent(match), "A matching field fires the hook.");

            MigrationPartnerEventTrigger mismatch = new MigrationPartnerEventTrigger { Trigger = "on_hit" };
            mismatch.Fields["element"] = "ice";
            AssertEqual(0, hooks.ResolveEvent(mismatch), "A mismatched field does not fire.");
        }

        private static void TestMultipleHooks()
        {
            MigrationCardPartnerHooks hooks = new MigrationCardPartnerHooks();
            hooks.RegisterHook(new MigrationPartnerHook { Id = "a", Trigger = "on_hit", PartnerEventId = "pe_a" });
            hooks.RegisterHook(new MigrationPartnerHook { Id = "b", Trigger = "on_hit", PartnerEventId = "pe_b" });
            hooks.RegisterHook(new MigrationPartnerHook { Id = "c", Trigger = "on_dodge", PartnerEventId = "pe_c" });

            AssertEqual(2, hooks.ResolveEvent(new MigrationPartnerEventTrigger { Trigger = "on_hit" }),
                "Both on_hit hooks fire for an on_hit event.");
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
