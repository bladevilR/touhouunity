using System.Collections.Generic;

namespace TouhouMigration.Runtime.Quest
{
    public sealed class QuestDefinition
    {
        public string Id { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<QuestObjectiveDefinition> Objectives { get; } = new List<QuestObjectiveDefinition>();
        public QuestRewardDefinition Rewards { get; set; } = new QuestRewardDefinition();
        public List<string> Prerequisites { get; } = new List<string>();
        public string NextQuest { get; set; } = string.Empty;
    }
}
