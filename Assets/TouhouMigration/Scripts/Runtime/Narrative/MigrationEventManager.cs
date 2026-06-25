using System.Collections.Generic;

namespace TouhouMigration.Runtime.Narrative
{
    // Event lifecycle status (Godot EventManager.EventStatus).
    public enum EventStatus { Inactive, Active, Completed, Failed }

    // Tracks the lifecycle of world/story events (Godot EventManager): each event moves through
    // Inactive -> Active -> Completed/Failed (or back to Inactive on cancel), carries an integer progress, and
    // at most one exclusive "story event" can run at a time. Free of UnityEngine. The trigger cooldown (real-
    // time), the random-event pool, custom per-event data, and the SignalBus emissions are deferred.
    public sealed class MigrationEventManager
    {
        private sealed class EventState
        {
            public EventStatus Status = EventStatus.Inactive;
            public int Progress;
        }

        private readonly Dictionary<string, EventState> events = new Dictionary<string, EventState>();
        private string currentStoryEvent = string.Empty;

        public string CurrentStoryEvent => currentStoryEvent;

        // Activate an event (Godot trigger_event, minus the cooldown gate): fails if it is already active or
        // already completed; a previously failed/cancelled event can be triggered again.
        public bool TriggerEvent(string eventId)
        {
            if (IsEventCompleted(eventId) || IsEventActive(eventId))
            {
                return false;
            }

            Ensure(eventId).Status = EventStatus.Active;
            return true;
        }

        public void CompleteEvent(string eventId)
        {
            SetStatusIfKnown(eventId, EventStatus.Completed);
        }

        public void FailEvent(string eventId)
        {
            SetStatusIfKnown(eventId, EventStatus.Failed);
        }

        public void CancelEvent(string eventId)
        {
            SetStatusIfKnown(eventId, EventStatus.Inactive);
        }

        public bool IsEventActive(string eventId) => GetEventStatus(eventId) == EventStatus.Active;
        public bool IsEventCompleted(string eventId) => GetEventStatus(eventId) == EventStatus.Completed;
        public bool IsEventFailed(string eventId) => GetEventStatus(eventId) == EventStatus.Failed;

        public EventStatus GetEventStatus(string eventId)
        {
            return events.TryGetValue(eventId ?? string.Empty, out EventState state) ? state.Status : EventStatus.Inactive;
        }

        public int GetEventProgress(string eventId)
        {
            return events.TryGetValue(eventId ?? string.Empty, out EventState state) ? state.Progress : 0;
        }

        public void SetEventProgress(string eventId, int progress)
        {
            if (events.TryGetValue(eventId ?? string.Empty, out EventState state))
            {
                state.Progress = progress;
            }
        }

        // Start the exclusive story event (Godot start_story_event): fails if one is already running, or if the
        // event itself cannot be triggered (e.g. already completed).
        public bool StartStoryEvent(string eventId)
        {
            if (!string.IsNullOrEmpty(currentStoryEvent))
            {
                return false;
            }

            if (!TriggerEvent(eventId))
            {
                return false;
            }

            currentStoryEvent = eventId;
            return true;
        }

        // End the current story event (Godot end_story_event): completes it and clears the slot.
        public void EndStoryEvent()
        {
            if (string.IsNullOrEmpty(currentStoryEvent))
            {
                return;
            }

            string eventId = currentStoryEvent;
            CompleteEvent(eventId);
            currentStoryEvent = string.Empty;
        }

        public bool HasActiveStoryEvent() => !string.IsNullOrEmpty(currentStoryEvent);

        private void SetStatusIfKnown(string eventId, EventStatus status)
        {
            if (events.TryGetValue(eventId ?? string.Empty, out EventState state))
            {
                state.Status = status;
            }
        }

        private EventState Ensure(string eventId)
        {
            string key = eventId ?? string.Empty;
            if (!events.TryGetValue(key, out EventState state))
            {
                state = new EventState();
                events[key] = state;
            }

            return state;
        }
    }
}
