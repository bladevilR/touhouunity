using System.Collections.Generic;

namespace TouhouMigration.Runtime.Quest
{
    public sealed class QuestRewardDefinition
    {
        public int Exp { get; set; }
        public int Coins { get; set; }
        public Dictionary<string, int> Items { get; } = new Dictionary<string, int>();
    }
}
