using System;
using System.Collections.Generic;
using TouhouMigration.Runtime.Cooking;
using TouhouMigration.Runtime.Quest;

namespace TouhouMigration.Runtime.Social
{
    public sealed class QuestDeliveryService
    {
        private readonly QuestDatabase questDatabase;
        private readonly QuestRewardLedger rewardLedger;
        private readonly CookingDatabase cookingDatabase;
        private readonly QuestRewardSink rewardSink;
        private readonly Dictionary<string, QuestRuntimeState> activeQuests = new Dictionary<string, QuestRuntimeState>();
        private readonly HashSet<string> completedQuests = new HashSet<string>();
        private readonly Dictionary<string, HashSet<string>> deliveryTagsByItemId = new Dictionary<string, HashSet<string>>();
        private readonly Dictionary<string, int> questProgressCounters = new Dictionary<string, int>();
        private readonly HashSet<string> unlockedNpcs = new HashSet<string>();

        public event Action<string> QuestStarted;
        public event Action<string> QuestCompleted;
        public event Action<string, int, int, int> QuestProgressUpdated;
        public event Action<string, int> CounterChanged;
        public event Action<string> NpcUnlocked;

        public int DeliveryEventCount { get; private set; }
        public string LastItemId { get; private set; } = string.Empty;
        public string LastNpcId { get; private set; } = string.Empty;
        public int LastAmount { get; private set; }
        public string LastCompletedQuestId { get; private set; } = string.Empty;
        public int LastDailyResetDay { get; private set; } = -1;
        public int ActiveQuestCount => activeQuests.Count;
        public int CompletedQuestCount => completedQuests.Count;
        public QuestRewardLedger RewardLedger => rewardLedger;

        public QuestDeliveryService()
            : this(null, null, null, null)
        {
        }

        public QuestDeliveryService(QuestDatabase questDatabase)
            : this(questDatabase, null, null, null)
        {
        }

        public QuestDeliveryService(QuestDatabase questDatabase, QuestRewardLedger rewardLedger)
            : this(questDatabase, rewardLedger, null, null)
        {
        }

        public QuestDeliveryService(QuestDatabase questDatabase, QuestRewardLedger rewardLedger, CookingDatabase cookingDatabase)
            : this(questDatabase, rewardLedger, cookingDatabase, null)
        {
        }

        public QuestDeliveryService(
            QuestDatabase questDatabase,
            QuestRewardLedger rewardLedger,
            CookingDatabase cookingDatabase,
            QuestRewardSink rewardSink)
        {
            this.questDatabase = questDatabase;
            this.rewardLedger = rewardLedger;
            this.cookingDatabase = cookingDatabase;
            this.rewardSink = rewardSink;
        }

        public void NotifyGiftDelivery(GiftDeliveryResult result, int amount)
        {
            if (result == null || !result.Success)
            {
                return;
            }

            NotifyDelivery(result.GiftId, amount, result.NpcId);
        }

        public void NotifyCraftCompleted(string itemId, int amount)
        {
            string normalizedItemId = NormalizeId(itemId);
            if (questDatabase == null || string.IsNullOrEmpty(normalizedItemId) || amount <= 0)
            {
                return;
            }

            List<string> questIds = new List<string>(activeQuests.Keys);
            foreach (string questId in questIds)
            {
                if (!activeQuests.ContainsKey(questId))
                {
                    continue;
                }

                QuestDefinition definition = questDatabase.GetQuest(questId);
                if (definition == null)
                {
                    continue;
                }

                for (int index = 0; index < definition.Objectives.Count; index++)
                {
                    QuestObjectiveDefinition objective = definition.Objectives[index];
                    bool matches = objective.Type switch
                    {
                        "craft" => objective.ItemId == normalizedItemId,
                        "craft_tier" => cookingDatabase != null &&
                            cookingDatabase.DishMatchesTier(normalizedItemId, objective.Tier),
                        "craft_stat" => cookingDatabase != null &&
                            cookingDatabase.DishMatchesStatRequirement(
                                normalizedItemId,
                                objective.Stat,
                                objective.RequiredStat),
                        _ => false
                    };

                    if (!matches)
                    {
                        continue;
                    }

                    UpdateQuestProgress(questId, index, amount);
                    if (!activeQuests.ContainsKey(questId))
                    {
                        break;
                    }
                }
            }
        }

        public bool StartQuest(string questId)
        {
            string normalizedQuestId = NormalizeId(questId);
            if (questDatabase == null || !questDatabase.HasQuest(normalizedQuestId) ||
                activeQuests.ContainsKey(normalizedQuestId) || completedQuests.Contains(normalizedQuestId))
            {
                return false;
            }

            QuestDefinition definition = questDatabase.GetQuest(normalizedQuestId);
            foreach (string prerequisite in definition.Prerequisites)
            {
                if (!completedQuests.Contains(NormalizeId(prerequisite)))
                {
                    return false;
                }
            }

            activeQuests[normalizedQuestId] = QuestRuntimeState.Create(definition);
            QuestStarted?.Invoke(normalizedQuestId);
            return true;
        }

        public void MarkQuestCompleted(string questId)
        {
            string normalizedQuestId = NormalizeId(questId);
            if (string.IsNullOrEmpty(normalizedQuestId))
            {
                return;
            }

            activeQuests.Remove(normalizedQuestId);
            completedQuests.Add(normalizedQuestId);
            LastCompletedQuestId = normalizedQuestId;
        }

        public int IncrementCounter(string counterId)
        {
            return IncrementCounter(counterId, 1);
        }

        public int IncrementCounter(string counterId, int amount)
        {
            string normalizedCounterId = NormalizeId(counterId);
            if (string.IsNullOrEmpty(normalizedCounterId))
            {
                return 0;
            }

            questProgressCounters.TryGetValue(normalizedCounterId, out int current);
            int next = current + Math.Max(0, amount);
            questProgressCounters[normalizedCounterId] = next;
            CounterChanged?.Invoke(normalizedCounterId, next);
            return next;
        }

        public int GetCounter(string counterId)
        {
            return questProgressCounters.TryGetValue(NormalizeId(counterId), out int value) ? value : 0;
        }

        public void NotifyEnemyKilled()
        {
            NotifyEnemyKilled(1);
        }

        public void NotifyEnemyKilled(int amount)
        {
            if (questDatabase == null || amount <= 0)
            {
                return;
            }

            List<string> questIds = new List<string>(activeQuests.Keys);
            foreach (string questId in questIds)
            {
                if (!activeQuests.ContainsKey(questId))
                {
                    continue;
                }

                QuestDefinition definition = questDatabase.GetQuest(questId);
                if (definition == null)
                {
                    continue;
                }

                for (int index = 0; index < definition.Objectives.Count; index++)
                {
                    if (definition.Objectives[index].Type == "kill")
                    {
                        UpdateQuestProgress(questId, index, amount);
                    }
                }
            }
        }

        public bool UnlockNpc(string npcId)
        {
            string normalizedNpcId = NormalizeId(npcId);
            if (string.IsNullOrEmpty(normalizedNpcId))
            {
                return false;
            }

            bool added = unlockedNpcs.Add(normalizedNpcId);
            if (added)
            {
                NpcUnlocked?.Invoke(normalizedNpcId);
            }

            return added;
        }

        public bool IsNpcUnlocked(string npcId)
        {
            return unlockedNpcs.Contains(NormalizeId(npcId));
        }

        public bool IsQuestActive(string questId)
        {
            return activeQuests.ContainsKey(NormalizeId(questId));
        }

        public bool IsQuestCompleted(string questId)
        {
            return completedQuests.Contains(NormalizeId(questId));
        }

        public bool IsQuestStarted(string questId)
        {
            string normalizedQuestId = NormalizeId(questId);
            return activeQuests.ContainsKey(normalizedQuestId) || completedQuests.Contains(normalizedQuestId);
        }

        public List<string> GetActiveQuestIds()
        {
            List<string> questIds = new List<string>(activeQuests.Keys);
            questIds.Sort(StringComparer.Ordinal);
            return questIds;
        }

        public List<string> GetCompletedQuestIds()
        {
            List<string> questIds = new List<string>(completedQuests);
            questIds.Sort(StringComparer.Ordinal);
            return questIds;
        }

        public int[] GetQuestProgress(string questId)
        {
            return activeQuests.TryGetValue(NormalizeId(questId), out QuestRuntimeState state)
                ? state.Progress.ToArray()
                : Array.Empty<int>();
        }

        public void UpdateQuestProgress(string questId, int objectiveIndex, int amount)
        {
            string normalizedQuestId = NormalizeId(questId);
            if (questDatabase == null || !activeQuests.TryGetValue(normalizedQuestId, out QuestRuntimeState state))
            {
                return;
            }

            QuestDefinition definition = questDatabase.GetQuest(normalizedQuestId);
            if (definition == null || objectiveIndex < 0 || objectiveIndex >= definition.Objectives.Count)
            {
                return;
            }

            int required = Math.Max(1, definition.Objectives[objectiveIndex].Required);
            state.Progress[objectiveIndex] = Math.Min(required, state.Progress[objectiveIndex] + Math.Max(0, amount));
            QuestProgressUpdated?.Invoke(normalizedQuestId, objectiveIndex, state.Progress[objectiveIndex], required);
            CheckQuestCompletion(normalizedQuestId);
        }

        public void RegisterDeliveryTag(string itemId, string tag)
        {
            string normalizedItemId = NormalizeId(itemId);
            string normalizedTag = NormalizeId(tag);
            if (string.IsNullOrEmpty(normalizedItemId) || string.IsNullOrEmpty(normalizedTag))
            {
                return;
            }

            if (!deliveryTagsByItemId.TryGetValue(normalizedItemId, out HashSet<string> tags))
            {
                tags = new HashSet<string>();
                deliveryTagsByItemId[normalizedItemId] = tags;
            }

            tags.Add(normalizedTag);
        }

        public void NotifyDelivery(string itemId, int amount, string npcId)
        {
            string normalizedItemId = NormalizeId(itemId);
            string normalizedNpcId = NormalizeId(npcId);
            DeliveryEventCount++;
            LastItemId = normalizedItemId;
            LastNpcId = normalizedNpcId;
            LastAmount = amount;

            if (questDatabase == null || string.IsNullOrEmpty(normalizedItemId) || amount <= 0)
            {
                return;
            }

            List<string> questIds = new List<string>(activeQuests.Keys);
            foreach (string questId in questIds)
            {
                if (!activeQuests.TryGetValue(questId, out QuestRuntimeState state))
                {
                    continue;
                }

                QuestDefinition definition = questDatabase.GetQuest(questId);
                if (definition == null)
                {
                    continue;
                }

                for (int index = 0; index < definition.Objectives.Count; index++)
                {
                    QuestObjectiveDefinition objective = definition.Objectives[index];
                    if (objective.Type != "deliver" && objective.Type != "deliver_variety")
                    {
                        continue;
                    }

                    if (!string.IsNullOrEmpty(objective.NpcId) && objective.NpcId != normalizedNpcId)
                    {
                        continue;
                    }

                    if (!IsDeliveryItemMatch(normalizedItemId, objective))
                    {
                        continue;
                    }

                    if (objective.Type == "deliver_variety")
                    {
                        ApplyVarietyDelivery(questId, state, objective, index, normalizedItemId);
                    }
                    else
                    {
                        UpdateQuestProgress(questId, index, amount);
                    }
                }
            }
        }

        public QuestRuntimeSnapshot CreateSnapshot()
        {
            QuestRuntimeSnapshot snapshot = new QuestRuntimeSnapshot();
            foreach (KeyValuePair<string, QuestRuntimeState> pair in activeQuests)
            {
                snapshot.ActiveQuests.Add(pair.Value.CreateSnapshot(pair.Key));
            }

            snapshot.CompletedQuests.AddRange(completedQuests);
            snapshot.DeliveryEventCount = DeliveryEventCount;
            snapshot.LastItemId = LastItemId;
            snapshot.LastNpcId = LastNpcId;
            snapshot.LastAmount = LastAmount;
            snapshot.LastCompletedQuestId = LastCompletedQuestId;
            snapshot.LastDailyResetDay = LastDailyResetDay;
            snapshot.RewardLedger = rewardLedger?.CreateSnapshot();
            snapshot.UnlockedNpcs.AddRange(unlockedNpcs);
            foreach (KeyValuePair<string, int> pair in questProgressCounters)
            {
                snapshot.Counters.Add(new QuestCounterSnapshot
                {
                    CounterId = pair.Key,
                    Value = pair.Value
                });
            }

            return snapshot;
        }

        public void LoadSnapshot(QuestRuntimeSnapshot snapshot)
        {
            activeQuests.Clear();
            completedQuests.Clear();
            questProgressCounters.Clear();
            unlockedNpcs.Clear();
            if (snapshot == null)
            {
                rewardLedger?.LoadSnapshot(null);
                DeliveryEventCount = 0;
                LastItemId = string.Empty;
                LastNpcId = string.Empty;
                LastAmount = 0;
                LastCompletedQuestId = string.Empty;
                LastDailyResetDay = -1;
                return;
            }

            foreach (QuestRuntimeStateSnapshot stateSnapshot in snapshot.ActiveQuests)
            {
                string questId = NormalizeId(stateSnapshot.QuestId);
                if (string.IsNullOrEmpty(questId))
                {
                    continue;
                }

                activeQuests[questId] = QuestRuntimeState.FromSnapshot(stateSnapshot);
            }

            foreach (string questId in snapshot.CompletedQuests)
            {
                string normalizedQuestId = NormalizeId(questId);
                if (!string.IsNullOrEmpty(normalizedQuestId))
                {
                    completedQuests.Add(normalizedQuestId);
                }
            }

            DeliveryEventCount = snapshot.DeliveryEventCount;
            LastItemId = snapshot.LastItemId ?? string.Empty;
            LastNpcId = snapshot.LastNpcId ?? string.Empty;
            LastAmount = snapshot.LastAmount;
            LastCompletedQuestId = snapshot.LastCompletedQuestId ?? string.Empty;
            LastDailyResetDay = snapshot.LastDailyResetDay;
            rewardLedger?.LoadSnapshot(snapshot.RewardLedger);

            foreach (string npcId in snapshot.UnlockedNpcs)
            {
                string normalizedNpcId = NormalizeId(npcId);
                if (!string.IsNullOrEmpty(normalizedNpcId))
                {
                    unlockedNpcs.Add(normalizedNpcId);
                }
            }

            foreach (QuestCounterSnapshot counter in snapshot.Counters)
            {
                string normalizedCounterId = NormalizeId(counter.CounterId);
                if (!string.IsNullOrEmpty(normalizedCounterId))
                {
                    questProgressCounters[normalizedCounterId] = Math.Max(0, counter.Value);
                }
            }
        }

        public bool ResetDailyQuests(int day)
        {
            if (LastDailyResetDay == day || questDatabase == null)
            {
                return false;
            }

            bool removedCompletedDaily = false;
            List<string> completedToRemove = new List<string>();
            foreach (string questId in completedQuests)
            {
                QuestDefinition definition = questDatabase.GetQuest(questId);
                if (definition != null && definition.Type == "daily")
                {
                    completedToRemove.Add(questId);
                }
            }

            foreach (string questId in completedToRemove)
            {
                completedQuests.Remove(questId);
                removedCompletedDaily = true;
            }

            List<string> activeToRemove = new List<string>();
            foreach (string questId in activeQuests.Keys)
            {
                QuestDefinition definition = questDatabase.GetQuest(questId);
                if (definition != null && definition.Type == "daily")
                {
                    activeToRemove.Add(questId);
                }
            }

            foreach (string questId in activeToRemove)
            {
                activeQuests.Remove(questId);
            }

            LastDailyResetDay = day;
            return removedCompletedDaily;
        }

        public List<QuestJournalEntry> GetJournalEntries(string status)
        {
            string normalizedStatus = NormalizeId(status);
            if (string.IsNullOrEmpty(normalizedStatus))
            {
                normalizedStatus = "active";
            }

            List<QuestJournalEntry> entries = new List<QuestJournalEntry>();
            if (questDatabase == null)
            {
                return entries;
            }

            if (normalizedStatus == "active" || normalizedStatus == "all")
            {
                foreach (KeyValuePair<string, QuestRuntimeState> pair in activeQuests)
                {
                    QuestDefinition definition = questDatabase.GetQuest(pair.Key);
                    if (definition != null)
                    {
                        entries.Add(CreateJournalEntry(definition, "active", pair.Value));
                    }
                }
            }

            if (normalizedStatus == "completed" || normalizedStatus == "all")
            {
                foreach (string questId in completedQuests)
                {
                    QuestDefinition definition = questDatabase.GetQuest(questId);
                    if (definition != null)
                    {
                        entries.Add(CreateJournalEntry(definition, "completed", null));
                    }
                }
            }

            entries.Sort((left, right) => string.CompareOrdinal(left.QuestId, right.QuestId));
            return entries;
        }

        private void ApplyVarietyDelivery(
            string questId,
            QuestRuntimeState state,
            QuestObjectiveDefinition objective,
            int objectiveIndex,
            string itemId)
        {
            if (!state.DeliveryVariety.TryGetValue(objectiveIndex, out HashSet<string> deliveredIds))
            {
                deliveredIds = new HashSet<string>();
                state.DeliveryVariety[objectiveIndex] = deliveredIds;
            }

            if (!deliveredIds.Add(itemId))
            {
                return;
            }

            int uniqueRequired = objective.UniqueRequired > 0 ? objective.UniqueRequired : objective.Required;
            int targetProgress = Math.Min(deliveredIds.Count, uniqueRequired);
            int addAmount = Math.Max(0, targetProgress - state.Progress[objectiveIndex]);
            if (addAmount > 0)
            {
                UpdateQuestProgress(questId, objectiveIndex, addAmount);
            }
        }

        private bool IsDeliveryItemMatch(string itemId, QuestObjectiveDefinition objective)
        {
            if (string.IsNullOrEmpty(objective.ItemId))
            {
                return false;
            }

            if (objective.ItemId == itemId)
            {
                return true;
            }

            if (cookingDatabase != null && cookingDatabase.IsSymbolicItemMatch(itemId, objective.ItemId))
            {
                return true;
            }

            return deliveryTagsByItemId.TryGetValue(itemId, out HashSet<string> tags) && tags.Contains(objective.ItemId);
        }

        private void CheckQuestCompletion(string questId)
        {
            if (questDatabase == null || !activeQuests.TryGetValue(questId, out QuestRuntimeState state))
            {
                return;
            }

            QuestDefinition definition = questDatabase.GetQuest(questId);
            if (definition == null)
            {
                return;
            }

            for (int index = 0; index < definition.Objectives.Count; index++)
            {
                if (state.Progress[index] < Math.Max(1, definition.Objectives[index].Required))
                {
                    return;
                }
            }

            CompleteQuest(questId, definition);
        }

        private void CompleteQuest(string questId, QuestDefinition definition)
        {
            activeQuests.Remove(questId);
            completedQuests.Add(questId);
            LastCompletedQuestId = questId;
            rewardLedger?.ApplyRewards(definition);
            rewardSink?.ApplyRewards(definition.Rewards);
            QuestCompleted?.Invoke(questId);

            if (!string.IsNullOrEmpty(definition.NextQuest))
            {
                StartQuest(definition.NextQuest);
            }
        }

        private static QuestJournalEntry CreateJournalEntry(QuestDefinition definition, string status, QuestRuntimeState state)
        {
            int objectiveCount = definition.Objectives.Count;
            int completedObjectiveCount = 0;
            List<string> progressParts = new List<string>();
            for (int index = 0; index < objectiveCount; index++)
            {
                QuestObjectiveDefinition objective = definition.Objectives[index];
                int required = Math.Max(1, objective.Required);
                int current = status == "completed" ? required : GetProgressValue(state, index);
                if (current >= required)
                {
                    completedObjectiveCount++;
                }

                progressParts.Add($"{objective.Description} ({current}/{required})");
            }

            return new QuestJournalEntry
            {
                QuestId = definition.Id,
                Title = definition.Title,
                Description = definition.Description,
                Type = definition.Type,
                Status = status,
                ProgressText = string.Join(" / ", progressParts),
                RewardText = FormatRewardText(definition.Rewards),
                ObjectiveCount = objectiveCount,
                CompletedObjectiveCount = Math.Min(completedObjectiveCount, objectiveCount)
            };
        }

        private static int GetProgressValue(QuestRuntimeState state, int index)
        {
            return state != null && index >= 0 && index < state.Progress.Count ? state.Progress[index] : 0;
        }

        private static string FormatRewardText(QuestRewardDefinition rewards)
        {
            if (rewards == null)
            {
                return string.Empty;
            }

            List<string> parts = new List<string>();
            if (rewards.Exp > 0)
            {
                parts.Add($"经验 {rewards.Exp}");
            }

            if (rewards.Coins > 0)
            {
                parts.Add($"金 {rewards.Coins}");
            }

            foreach (KeyValuePair<string, int> pair in rewards.Items)
            {
                if (pair.Value > 0)
                {
                    parts.Add($"{pair.Key} x{pair.Value}");
                }
            }

            return string.Join(" / ", parts);
        }

        private static string NormalizeId(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToLowerInvariant();
        }

        private sealed class QuestRuntimeState
        {
            public List<int> Progress { get; } = new List<int>();
            public Dictionary<int, HashSet<string>> DeliveryVariety { get; } = new Dictionary<int, HashSet<string>>();

            public static QuestRuntimeState Create(QuestDefinition definition)
            {
                QuestRuntimeState state = new QuestRuntimeState();
                foreach (QuestObjectiveDefinition _ in definition.Objectives)
                {
                    state.Progress.Add(0);
                }

                return state;
            }

            public QuestRuntimeStateSnapshot CreateSnapshot(string questId)
            {
                QuestRuntimeStateSnapshot snapshot = new QuestRuntimeStateSnapshot
                {
                    QuestId = questId
                };
                snapshot.Progress.AddRange(Progress);
                foreach (KeyValuePair<int, HashSet<string>> pair in DeliveryVariety)
                {
                    QuestDeliveryVarietySnapshot variety = new QuestDeliveryVarietySnapshot
                    {
                        ObjectiveIndex = pair.Key
                    };
                    variety.DeliveredItemIds.AddRange(pair.Value);
                    snapshot.DeliveryVariety.Add(variety);
                }

                return snapshot;
            }

            public static QuestRuntimeState FromSnapshot(QuestRuntimeStateSnapshot snapshot)
            {
                QuestRuntimeState state = new QuestRuntimeState();
                state.Progress.AddRange(snapshot.Progress);
                foreach (QuestDeliveryVarietySnapshot variety in snapshot.DeliveryVariety)
                {
                    state.DeliveryVariety[variety.ObjectiveIndex] = new HashSet<string>(variety.DeliveredItemIds);
                }

                return state;
            }
        }
    }

    [Serializable]
    public sealed class QuestRuntimeSnapshot
    {
        public List<QuestRuntimeStateSnapshot> active_quests = new List<QuestRuntimeStateSnapshot>();
        public List<string> completed_quests = new List<string>();
        public List<string> unlocked_npcs = new List<string>();
        public List<QuestCounterSnapshot> counters = new List<QuestCounterSnapshot>();
        public QuestRewardLedgerSnapshot reward_ledger;
        public int delivery_event_count;
        public string last_item_id = string.Empty;
        public string last_npc_id = string.Empty;
        public int last_amount;
        public string last_completed_quest_id = string.Empty;
        public int last_daily_reset_day = -1;

        public List<QuestRuntimeStateSnapshot> ActiveQuests => active_quests ??= new List<QuestRuntimeStateSnapshot>();
        public List<string> CompletedQuests => completed_quests ??= new List<string>();
        public List<string> UnlockedNpcs => unlocked_npcs ??= new List<string>();
        public List<QuestCounterSnapshot> Counters => counters ??= new List<QuestCounterSnapshot>();

        public QuestRewardLedgerSnapshot RewardLedger
        {
            get => reward_ledger;
            set => reward_ledger = value;
        }

        public int DeliveryEventCount
        {
            get => delivery_event_count;
            set => delivery_event_count = value;
        }

        public string LastItemId
        {
            get => last_item_id;
            set => last_item_id = value ?? string.Empty;
        }

        public string LastNpcId
        {
            get => last_npc_id;
            set => last_npc_id = value ?? string.Empty;
        }

        public int LastAmount
        {
            get => last_amount;
            set => last_amount = value;
        }

        public string LastCompletedQuestId
        {
            get => last_completed_quest_id;
            set => last_completed_quest_id = value ?? string.Empty;
        }

        public int LastDailyResetDay
        {
            get => last_daily_reset_day;
            set => last_daily_reset_day = value;
        }
    }

    [Serializable]
    public sealed class QuestCounterSnapshot
    {
        public string counter_id = string.Empty;
        public int value;

        public string CounterId
        {
            get => counter_id;
            set => counter_id = value ?? string.Empty;
        }

        public int Value
        {
            get => value;
            set => this.value = value;
        }
    }

    [Serializable]
    public sealed class QuestRuntimeStateSnapshot
    {
        public string quest_id = string.Empty;
        public List<int> progress = new List<int>();
        public List<QuestDeliveryVarietySnapshot> delivery_variety = new List<QuestDeliveryVarietySnapshot>();

        public string QuestId
        {
            get => quest_id;
            set => quest_id = value ?? string.Empty;
        }

        public List<int> Progress => progress;
        public List<QuestDeliveryVarietySnapshot> DeliveryVariety => delivery_variety;
    }

    [Serializable]
    public sealed class QuestDeliveryVarietySnapshot
    {
        public int objective_index;
        public List<string> delivered_item_ids = new List<string>();

        public int ObjectiveIndex
        {
            get => objective_index;
            set => objective_index = value;
        }

        public List<string> DeliveredItemIds => delivered_item_ids;
    }
}
