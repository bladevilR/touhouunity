using System;
using System.Collections.Generic;

namespace TouhouMigration.Runtime.Quest
{
    // A quest the board can offer (Godot QuestBoard quest entry, minimal fields for the board logic).
    public sealed class MigrationBoardQuest
    {
        public MigrationBoardQuest(string id, string title, string type)
        {
            Id = id ?? string.Empty;
            Title = title ?? string.Empty;
            Type = type ?? string.Empty;
        }

        public string Id { get; }
        public string Title { get; }
        public string Type { get; }
    }

    // The quest board (Godot QuestBoard): each day it refreshes the available daily quests from a pool
    // (up to 3), and the player accepts an available quest that the quest manager doesn't already hold.
    // UnityEngine-free; the open/close-board + quest_accepted signals and the QuestManager hand-off are
    // scene wiring (the already-accepted check is injected).
    public sealed class MigrationQuestBoard
    {
        private const int DailyOfferCount = 3;

        private static readonly MigrationBoardQuest[] DailyPool =
        {
            new MigrationBoardQuest("daily_gather_bamboo", "采集竹笋", "daily"),
            new MigrationBoardQuest("daily_defeat_fairies", "清理妖精", "daily"),
            new MigrationBoardQuest("daily_fishing", "钓鱼任务", "daily"),
        };

        private readonly List<MigrationBoardQuest> available = new List<MigrationBoardQuest>();

        public IReadOnlyList<MigrationBoardQuest> AvailableQuests => available;

        // Re-roll the available daily quests (Godot _refresh_daily_quests): shuffle the pool and take up to
        // three. randomIndex(maxExclusive) drives the shuffle; null = identity order.
        public void RefreshDailyQuests(Func<int, int> randomIndex = null)
        {
            available.Clear();
            List<MigrationBoardQuest> pool = new List<MigrationBoardQuest>(DailyPool);
            if (randomIndex != null)
            {
                for (int i = pool.Count - 1; i > 0; i--)
                {
                    int bound = i + 1;
                    int j = ((randomIndex(bound) % bound) + bound) % bound;
                    (pool[i], pool[j]) = (pool[j], pool[i]);
                }
            }

            for (int i = 0; i < pool.Count && i < DailyOfferCount; i++)
            {
                available.Add(pool[i]);
            }
        }

        public bool IsAvailable(string questId)
        {
            return Find(questId) != null;
        }

        // Accept an available quest (Godot accept_quest): succeeds only when the quest is on the board and
        // the quest manager doesn't already hold it.
        public bool AcceptQuest(string questId, Func<string, bool> isAlreadyAccepted)
        {
            MigrationBoardQuest quest = Find(questId);
            if (quest == null)
            {
                return false;
            }

            if (isAlreadyAccepted != null && isAlreadyAccepted(questId))
            {
                return false;
            }

            return true;
        }

        private MigrationBoardQuest Find(string questId)
        {
            if (questId == null)
            {
                return null;
            }

            foreach (MigrationBoardQuest quest in available)
            {
                if (quest.Id == questId)
                {
                    return quest;
                }
            }

            return null;
        }
    }
}
