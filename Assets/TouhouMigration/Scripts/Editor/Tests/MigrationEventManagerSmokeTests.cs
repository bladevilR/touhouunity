using System;
using TouhouMigration.Runtime.Narrative;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    // Covers MigrationEventManager: the event-status lifecycle (Godot EventManager) — trigger/complete/fail/
    // cancel transitions, status/progress queries, and the exclusive story event. Cooldowns, the random-event
    // pool, custom event data, and signals are deferred.
    public static class MigrationEventManagerSmokeTests
    {
        [MenuItem("Touhou Migration/Tests/Run Migration Event Manager Smoke Tests")]
        public static void RunAll()
        {
            TestTriggerActivatesEvent();
            TestCannotTriggerActiveOrCompletedEvent();
            TestCompleteFailCancelTransitions();
            TestUnknownEventDefaults();
            TestSetAndGetProgress();
            TestStoryEventIsExclusiveAndEndCompletes();
            TestCancelledEventCanRetrigger();
            Debug.Log("Migration event manager smoke tests passed.");
        }

        private static void TestTriggerActivatesEvent()
        {
            MigrationEventManager events = new MigrationEventManager();
            AssertEqual(true, events.TriggerEvent("e1"), "Triggering a new event succeeds.");
            AssertEqual(true, events.IsEventActive("e1"), "A triggered event is active.");
            AssertEqual(EventStatus.Active, events.GetEventStatus("e1"), "A triggered event's status is Active.");
        }

        private static void TestCannotTriggerActiveOrCompletedEvent()
        {
            MigrationEventManager events = new MigrationEventManager();
            events.TriggerEvent("e1");
            AssertEqual(false, events.TriggerEvent("e1"), "An already-active event cannot be re-triggered.");
            events.CompleteEvent("e1");
            AssertEqual(true, events.IsEventCompleted("e1"), "The event is completed.");
            AssertEqual(false, events.TriggerEvent("e1"), "A completed event cannot be re-triggered.");
        }

        private static void TestCompleteFailCancelTransitions()
        {
            MigrationEventManager events = new MigrationEventManager();
            events.TriggerEvent("done");
            events.CompleteEvent("done");
            AssertEqual(true, events.IsEventCompleted("done"), "Complete sets the completed status.");
            AssertEqual(false, events.IsEventActive("done"), "A completed event is no longer active.");

            events.TriggerEvent("lost");
            events.FailEvent("lost");
            AssertEqual(true, events.IsEventFailed("lost"), "Fail sets the failed status.");
            AssertEqual(false, events.IsEventActive("lost"), "A failed event is no longer active.");

            events.TriggerEvent("stopped");
            events.CancelEvent("stopped");
            AssertEqual(EventStatus.Inactive, events.GetEventStatus("stopped"), "Cancel resets to Inactive.");
            AssertEqual(false, events.IsEventActive("stopped"), "A cancelled event is no longer active.");
        }

        private static void TestUnknownEventDefaults()
        {
            MigrationEventManager events = new MigrationEventManager();
            AssertEqual(EventStatus.Inactive, events.GetEventStatus("none"), "An unknown event is Inactive.");
            AssertEqual(false, events.IsEventActive("none"), "An unknown event is not active.");
            AssertEqual(false, events.IsEventCompleted("none"), "An unknown event is not completed.");
            AssertEqual(0, events.GetEventProgress("none"), "An unknown event has zero progress.");
        }

        private static void TestSetAndGetProgress()
        {
            MigrationEventManager events = new MigrationEventManager();
            events.TriggerEvent("quest");
            events.SetEventProgress("quest", 5);
            AssertEqual(5, events.GetEventProgress("quest"), "Progress is stored and returned.");
            events.SetEventProgress("none", 9);
            AssertEqual(0, events.GetEventProgress("none"), "Setting progress on an unknown event is a no-op.");
        }

        private static void TestStoryEventIsExclusiveAndEndCompletes()
        {
            MigrationEventManager events = new MigrationEventManager();
            AssertEqual(true, events.StartStoryEvent("s1"), "A story event starts when none is running.");
            AssertEqual(true, events.HasActiveStoryEvent(), "A running story event is reported.");
            AssertEqual("s1", events.CurrentStoryEvent, "The current story event is tracked.");
            AssertEqual(true, events.IsEventActive("s1"), "The story event is active.");
            AssertEqual(false, events.StartStoryEvent("s2"), "Story events are exclusive; a second cannot start.");

            events.EndStoryEvent();
            AssertEqual(false, events.HasActiveStoryEvent(), "Ending clears the active story event.");
            AssertEqual(true, events.IsEventCompleted("s1"), "Ending a story event completes it.");
            AssertEqual(true, events.StartStoryEvent("s2"), "A new story event can start once the previous ended.");
        }

        private static void TestCancelledEventCanRetrigger()
        {
            MigrationEventManager events = new MigrationEventManager();
            events.TriggerEvent("e1");
            events.CancelEvent("e1");
            AssertEqual(true, events.TriggerEvent("e1"), "A cancelled (inactive) event can be triggered again.");
            AssertEqual(true, events.IsEventActive("e1"), "The re-triggered event is active again.");
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
