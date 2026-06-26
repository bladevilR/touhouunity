using System.Collections.Generic;

namespace TouhouMigration.Runtime.CardBuild
{
    // Per-character current-run persistence (Godot CardBuildRunStore): stores the in-progress card-build
    // run snapshot for each character, so a fight can be resumed. UnityEngine-free; the snapshots are
    // JsonUtility-safe, so a save service can serialize the whole store to disk (the file-path IO is a
    // separate concern, like the other save snapshots).
    public sealed class MigrationCardBuildRunStore
    {
        private readonly Dictionary<string, CardBuildRunSnapshot> currentRuns =
            new Dictionary<string, CardBuildRunSnapshot>();

        public bool HasCurrentRun(string characterId)
        {
            return characterId != null && currentRuns.ContainsKey(characterId);
        }

        public CardBuildRunSnapshot LoadCurrentRun(string characterId)
        {
            return characterId != null && currentRuns.TryGetValue(characterId, out CardBuildRunSnapshot snapshot)
                ? snapshot
                : null;
        }

        public void SaveCurrentRun(string characterId, CardBuildRunSnapshot snapshot)
        {
            if (!string.IsNullOrEmpty(characterId) && snapshot != null)
            {
                currentRuns[characterId] = snapshot;
            }
        }

        public void ClearCurrentRun(string characterId)
        {
            if (characterId != null)
            {
                currentRuns.Remove(characterId);
            }
        }
    }
}
