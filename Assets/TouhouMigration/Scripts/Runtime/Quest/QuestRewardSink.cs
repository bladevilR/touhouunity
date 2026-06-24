using System.Collections.Generic;
using TouhouMigration.Runtime.Inventory;
using TouhouMigration.Runtime.Player;

namespace TouhouMigration.Runtime.Quest
{
    public sealed class QuestRewardSink
    {
        private readonly InventoryService inventoryService;
        private readonly MigrationPlayerProgressService playerProgress;

        public QuestRewardSink(InventoryService inventoryService, MigrationPlayerProgressService playerProgress)
        {
            this.inventoryService = inventoryService;
            this.playerProgress = playerProgress;
        }

        public int FailedItemGrantCount { get; private set; }

        public void ApplyRewards(QuestRewardDefinition rewards)
        {
            if (rewards == null)
            {
                return;
            }

            playerProgress?.GainExperience(rewards.Exp);
            playerProgress?.AddCoins(rewards.Coins);
            foreach (KeyValuePair<string, int> pair in rewards.Items)
            {
                if (inventoryService == null || !inventoryService.AddItem(pair.Key, pair.Value))
                {
                    FailedItemGrantCount++;
                }
            }
        }
    }
}
