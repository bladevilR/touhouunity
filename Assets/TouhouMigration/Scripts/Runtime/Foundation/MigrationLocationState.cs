using System.Collections.Generic;

namespace TouhouMigration.Runtime.Foundation
{
    // Player location + unlocked-location tracking (Godot GlobalGameState location management): the current
    // location plus the runtime-growable set of unlocked locations (seeded with the three starting
    // locations). UnityEngine-free; the location_changed / show_notification signals are scene wiring.
    public sealed class MigrationLocationState
    {
        public const string DefaultLocation = "human_village";

        private static readonly string[] DefaultUnlocked = { "human_village", "keine_house", "bamboo_forest" };

        private readonly HashSet<string> unlocked = new HashSet<string>();

        public MigrationLocationState()
        {
            Reset();
        }

        public string CurrentLocation { get; private set; }

        public IReadOnlyCollection<string> UnlockedLocations => unlocked;

        public void SetLocation(string location)
        {
            if (!string.IsNullOrEmpty(location))
            {
                CurrentLocation = location;
            }
        }

        public void UnlockLocation(string location)
        {
            if (!string.IsNullOrEmpty(location))
            {
                unlocked.Add(location);
            }
        }

        public bool IsLocationUnlocked(string location)
        {
            return location != null && unlocked.Contains(location);
        }

        public void Reset()
        {
            CurrentLocation = DefaultLocation;
            unlocked.Clear();
            foreach (string location in DefaultUnlocked)
            {
                unlocked.Add(location);
            }
        }
    }
}
