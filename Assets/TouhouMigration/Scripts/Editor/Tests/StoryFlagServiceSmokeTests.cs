using System;
using System.Collections.Generic;
using TouhouMigration.Runtime.Dialogue;
using TouhouMigration.Runtime.Narrative;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    public static class StoryFlagServiceSmokeTests
    {
        [MenuItem("Touhou Migration/Tests/Run Story Flag Service Smoke Tests")]
        public static void RunAll()
        {
            TestMarkAndQueryEvents();
            TestEventEffectRoutesToStoryFlags();
            TestEventEffectNoOpWithoutBoundService();
            TestSnapshotRoundTrip();
            Debug.Log("Story flag service smoke tests passed.");
        }

        private static void TestMarkAndQueryEvents()
        {
            MigrationStoryFlagService flags = new MigrationStoryFlagService();
            AssertEqual(false, flags.HasEvent("mokou_fate"), "An unmarked event is not present.");

            flags.MarkEvent("mokou_fate");
            AssertEqual(true, flags.HasEvent("mokou_fate"), "A marked event is present.");

            flags.MarkEvent("mokou_fate");
            AssertEqual(1, flags.EventCount, "Marking the same event twice records it once.");
            AssertEqual(false, flags.HasEvent("keine_helps"), "Other events remain absent.");
        }

        private static void TestEventEffectRoutesToStoryFlags()
        {
            MigrationStoryFlagService flags = new MigrationStoryFlagService();
            DialogueEffectRouter router = new DialogueEffectRouter(null, null);
            router.BindStoryFlags(flags);

            bool handled = router.Apply("mokou", new Dictionary<string, object> { ["event"] = "elixir_rejection" });

            AssertEqual(true, handled, "An event effect should route to the bound story flag service.");
            AssertEqual(true, flags.HasEvent("elixir_rejection"), "An event effect should mark the narrative event.");
        }

        private static void TestEventEffectNoOpWithoutBoundService()
        {
            DialogueEffectRouter router = new DialogueEffectRouter(null, null);
            bool handled = router.Apply("mokou", new Dictionary<string, object> { ["event"] = "elixir_rejection" });
            AssertEqual(false, handled, "An event effect is a no-op when no story flag service is bound.");
        }

        private static void TestSnapshotRoundTrip()
        {
            MigrationStoryFlagService source = new MigrationStoryFlagService();
            source.MarkEvent("mokou_fate");
            source.MarkEvent("keine_helps");

            List<string> snapshot = source.CreateSnapshot();
            AssertEqual(2, snapshot.Count, "Snapshot should contain both fired events.");

            MigrationStoryFlagService restored = new MigrationStoryFlagService();
            restored.MarkEvent("stale_flag");
            restored.LoadSnapshot(snapshot);
            AssertEqual(true, restored.HasEvent("mokou_fate"), "LoadSnapshot should restore a fired event.");
            AssertEqual(true, restored.HasEvent("keine_helps"), "LoadSnapshot should restore all fired events.");
            AssertEqual(false, restored.HasEvent("stale_flag"), "LoadSnapshot should replace any prior flags.");
            AssertEqual(2, restored.EventCount, "Restored service should hold exactly the snapshot's events.");
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
