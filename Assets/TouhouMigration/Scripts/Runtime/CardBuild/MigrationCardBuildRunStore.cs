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

        // Serialize the whole store for disk (a save service writes this via JsonUtility).
        public CardBuildRunStoreFile CreateFileSnapshot()
        {
            CardBuildRunStoreFile file = new CardBuildRunStoreFile();
            foreach (KeyValuePair<string, CardBuildRunSnapshot> pair in currentRuns)
            {
                file.runs.Add(new CardBuildRunEntry { characterId = pair.Key, snapshot = pair.Value });
            }

            return file;
        }

        public void LoadFileSnapshot(CardBuildRunStoreFile file)
        {
            currentRuns.Clear();
            if (file?.runs == null)
            {
                return;
            }

            foreach (CardBuildRunEntry entry in file.runs)
            {
                if (entry != null && !string.IsNullOrEmpty(entry.characterId) && entry.snapshot != null)
                {
                    currentRuns[entry.characterId] = entry.snapshot;
                }
            }
        }
    }

    // The whole run store serialized for disk (Godot CardBuildRunStore's "current_runs" file). A save
    // service writes/reads this via JsonUtility — every member is [Serializable]/JsonUtility-safe.
    [System.Serializable]
    public sealed class CardBuildRunStoreFile
    {
        public List<CardBuildRunEntry> runs = new List<CardBuildRunEntry>();
    }

    [System.Serializable]
    public sealed class CardBuildRunEntry
    {
        public string characterId = string.Empty;
        public CardBuildRunSnapshot snapshot = new CardBuildRunSnapshot();
    }
}
