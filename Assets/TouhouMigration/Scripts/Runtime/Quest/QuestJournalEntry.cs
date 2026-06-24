namespace TouhouMigration.Runtime.Quest
{
    public sealed class QuestJournalEntry
    {
        public string QuestId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string ProgressText { get; set; } = string.Empty;
        public string RewardText { get; set; } = string.Empty;
        public int ObjectiveCount { get; set; }
        public int CompletedObjectiveCount { get; set; }
    }
}
