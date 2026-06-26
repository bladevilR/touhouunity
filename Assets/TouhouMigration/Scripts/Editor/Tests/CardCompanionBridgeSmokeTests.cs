using System;
using System.Collections.Generic;
using TouhouMigration.Runtime.CardBuild;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    // Covers MigrationCardCompanionBridge: draining new run partner events + queued intents into companion
    // intents via a consumed cursor (Godot CardCompanionBridge collect/consume/queue_partner_intent).
    public static class CardCompanionBridgeSmokeTests
    {
        [MenuItem("Touhou Migration/Tests/Run Card Companion Bridge Smoke Tests")]
        public static void RunAll()
        {
            TestCollectIsNonDestructive();
            TestConsumeAdvancesTheCursor();
            TestQueuedIntents();
            Debug.Log("Card companion bridge smoke tests passed.");
        }

        private static MigrationCardBuildRunController NewRun()
        {
            return new MigrationCardBuildRunController(new List<string> { "a", "b" });
        }

        private static void TestCollectIsNonDestructive()
        {
            MigrationCardBuildRunController run = NewRun();
            run.AddPartnerEvent(new MigrationCardEffectBlock { Type = "trigger_partner", Id = "pe_strike" });
            MigrationCardCompanionBridge bridge = new MigrationCardCompanionBridge();

            IReadOnlyList<MigrationCompanionIntent> first = bridge.CollectIntents(run);
            AssertEqual(1, first.Count, "A new partner event is collected as an intent.");
            AssertEqual("pe_strike", first[0].Action, "The intent action is the partner-event id.");
            AssertEqual("partner_event", first[0].Source, "Its source is the partner event.");

            // Collect again without consuming -> still the same (cursor not advanced).
            AssertEqual(1, bridge.CollectIntents(run).Count, "Collect without consume is non-destructive.");
        }

        private static void TestConsumeAdvancesTheCursor()
        {
            MigrationCardBuildRunController run = NewRun();
            run.AddPartnerEvent(new MigrationCardEffectBlock { Type = "trigger_partner", Id = "pe1" });
            MigrationCardCompanionBridge bridge = new MigrationCardCompanionBridge();

            AssertEqual(1, bridge.ConsumeIntents(run).Count, "Consume returns the pending intents.");
            AssertEqual(0, bridge.CollectIntents(run).Count, "After consuming, the partner event is not re-collected.");

            // A newly fired partner event is collected again.
            run.AddPartnerEvent(new MigrationCardEffectBlock { Type = "trigger_partner", Id = "pe2" });
            IReadOnlyList<MigrationCompanionIntent> next = bridge.CollectIntents(run);
            AssertEqual(1, next.Count, "A new partner event after consume is collected.");
            AssertEqual("pe2", next[0].Action, "The newly fired partner event is the one collected.");
        }

        private static void TestQueuedIntents()
        {
            MigrationCardBuildRunController run = NewRun();
            MigrationCardCompanionBridge bridge = new MigrationCardCompanionBridge();
            bridge.QueueIntent("bridge_signal");

            IReadOnlyList<MigrationCompanionIntent> intents = bridge.CollectIntents(run);
            AssertEqual(1, intents.Count, "A queued intent is collected.");
            AssertEqual("bridge_signal", intents[0].Action, "The queued intent's action is preserved.");
            AssertEqual("bridge_queue", intents[0].Source, "Its source is the bridge queue.");

            bridge.ConsumeIntents(run);
            AssertEqual(0, bridge.CollectIntents(run).Count, "A consumed queued intent is not re-collected.");
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
