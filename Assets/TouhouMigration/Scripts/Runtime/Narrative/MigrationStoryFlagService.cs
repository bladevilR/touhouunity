using System.Collections.Generic;

namespace TouhouMigration.Runtime.Narrative
{
    // Records narrative events fired by dialogue/story (Godot dialogue "event" effects + story flags).
    // A set of fired event ids; consumers gate dialogue/quests/content on HasEvent. Free of UnityEngine
    // so it stays unit-testable. Save round-trip is a later slice.
    public sealed class MigrationStoryFlagService
    {
        private readonly HashSet<string> firedEvents = new HashSet<string>();

        public int EventCount => firedEvents.Count;

        // Returns true if the event was newly recorded; marking an already-fired event is a no-op.
        public bool MarkEvent(string eventId)
        {
            string normalized = Normalize(eventId);
            return normalized.Length > 0 && firedEvents.Add(normalized);
        }

        public bool HasEvent(string eventId)
        {
            string normalized = Normalize(eventId);
            return normalized.Length > 0 && firedEvents.Contains(normalized);
        }

        public List<string> CreateSnapshot()
        {
            return new List<string>(firedEvents);
        }

        public void LoadSnapshot(IEnumerable<string> events)
        {
            firedEvents.Clear();
            if (events == null)
            {
                return;
            }

            foreach (string eventId in events)
            {
                string normalized = Normalize(eventId);
                if (normalized.Length > 0)
                {
                    firedEvents.Add(normalized);
                }
            }
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToLowerInvariant();
        }
    }
}
