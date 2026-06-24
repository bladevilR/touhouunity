namespace TouhouMigration.Runtime.Quest
{
    public sealed class QuestObjectiveDefinition
    {
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ItemId { get; set; } = string.Empty;
        public string NpcId { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string TraceId { get; set; } = string.Empty;
        public string Tier { get; set; } = string.Empty;
        public string Stat { get; set; } = string.Empty;
        public int RequiredStat { get; set; }
        public int Required { get; set; } = 1;
        public int UniqueRequired { get; set; }
    }
}
