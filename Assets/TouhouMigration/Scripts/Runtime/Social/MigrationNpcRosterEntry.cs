namespace TouhouMigration.Runtime.Social
{
    // One NPC in a location's spawn roster (Godot human_village_*_npc_roster.json): id, display name,
    // model path, spawn flag/tier/distance, and home/work/activity hints for placement + schedules.
    public sealed class MigrationNpcRosterEntry
    {
        public string NpcId { get; }
        public string DisplayName { get; }
        public string ModelPath { get; }
        public bool SpawnEnabled { get; }
        public string Tier { get; }
        public float MaxDistance { get; }
        public string Home { get; }
        public string WorkLocation { get; }
        public string Activity { get; }

        public MigrationNpcRosterEntry(string npcId, string displayName, string modelPath, bool spawnEnabled, string tier, float maxDistance, string home, string workLocation, string activity)
        {
            NpcId = npcId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            ModelPath = modelPath ?? string.Empty;
            SpawnEnabled = spawnEnabled;
            Tier = tier ?? string.Empty;
            MaxDistance = maxDistance;
            Home = home ?? string.Empty;
            WorkLocation = workLocation ?? string.Empty;
            Activity = activity ?? string.Empty;
        }
    }
}
