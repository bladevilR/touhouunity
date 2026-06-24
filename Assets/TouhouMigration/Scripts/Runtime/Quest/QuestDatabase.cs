using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TouhouMigration.Runtime.Serialization;

namespace TouhouMigration.Runtime.Quest
{
    public sealed class QuestDatabase
    {
        private readonly Dictionary<string, QuestDefinition> quests = new Dictionary<string, QuestDefinition>();
        private readonly List<string> errors = new List<string>();

        public int QuestCount => quests.Count;
        public int MainQuestCount => CountByType("main");
        public int SideQuestCount => CountByType("side");
        public int DailyQuestCount => CountByType("daily");
        public IReadOnlyList<string> Errors => errors;

        public bool LoadFromPath(string filePath)
        {
            quests.Clear();
            errors.Clear();

            if (!File.Exists(filePath))
            {
                errors.Add($"Quest data file does not exist: {filePath}");
                return false;
            }

            try
            {
                object parsed = MigrationJson.Parse(File.ReadAllText(filePath));
                if (parsed is not Dictionary<string, object> root)
                {
                    errors.Add("Quest data root is not an object.");
                    return false;
                }

                object questRoot = root.TryGetValue("quests", out object rawQuests) ? rawQuests : root;
                if (questRoot is not Dictionary<string, object> questDictionary)
                {
                    errors.Add("Quest data has no quest dictionary.");
                    return false;
                }

                foreach (KeyValuePair<string, object> pair in questDictionary)
                {
                    if (pair.Value is not Dictionary<string, object> rawQuest)
                    {
                        continue;
                    }

                    QuestDefinition quest = ParseQuest(pair.Key, rawQuest);
                    quests[quest.Id] = quest;
                }
            }
            catch (Exception exception)
            {
                errors.Add(exception.Message);
            }

            return quests.Count > 0 && errors.Count == 0;
        }

        public bool HasQuest(string questId)
        {
            return quests.ContainsKey(NormalizeId(questId));
        }

        public QuestDefinition GetQuest(string questId)
        {
            return quests.TryGetValue(NormalizeId(questId), out QuestDefinition quest) ? quest : null;
        }

        public IReadOnlyCollection<QuestDefinition> GetAllQuests()
        {
            return quests.Values;
        }

        private QuestDefinition ParseQuest(string fallbackId, Dictionary<string, object> rawQuest)
        {
            QuestDefinition quest = new QuestDefinition
            {
                Id = NormalizeId(GetString(rawQuest, "id", fallbackId)),
                Type = NormalizeId(GetString(rawQuest, "type")),
                Title = GetString(rawQuest, "title"),
                Description = GetString(rawQuest, "description"),
                Rewards = ParseRewards(rawQuest.TryGetValue("rewards", out object rewards) ? rewards : null),
                NextQuest = NormalizeId(GetString(rawQuest, "next_quest"))
            };

            foreach (string prerequisite in ToStringList(rawQuest.TryGetValue("prerequisites", out object prerequisites) ? prerequisites : null))
            {
                quest.Prerequisites.Add(NormalizeId(prerequisite));
            }

            if (rawQuest.TryGetValue("objectives", out object objectives) && objectives is IList objectiveList)
            {
                foreach (object objective in objectiveList)
                {
                    if (objective is Dictionary<string, object> rawObjective)
                    {
                        quest.Objectives.Add(ParseObjective(rawObjective));
                    }
                }
            }

            return quest;
        }

        private static QuestObjectiveDefinition ParseObjective(Dictionary<string, object> rawObjective)
        {
            int required = ToInt(rawObjective.TryGetValue("required", out object rawRequired) ? rawRequired : 1, 1);
            return new QuestObjectiveDefinition
            {
                Type = NormalizeId(GetString(rawObjective, "type")),
                Description = GetString(rawObjective, "description"),
                ItemId = NormalizeId(GetString(rawObjective, "item_id")),
                NpcId = NormalizeId(GetString(rawObjective, "npc_id")),
                Location = NormalizeId(GetString(rawObjective, "location")),
                TraceId = NormalizeId(GetString(rawObjective, "trace_id")),
                Tier = NormalizeId(GetString(rawObjective, "tier")),
                Stat = NormalizeId(GetString(rawObjective, "stat")),
                RequiredStat = ToInt(rawObjective.TryGetValue("required_stat", out object requiredStat) ? requiredStat : 0),
                Required = Math.Max(1, required),
                UniqueRequired = ToInt(rawObjective.TryGetValue("unique_required", out object uniqueRequired) ? uniqueRequired : 0)
            };
        }

        private static QuestRewardDefinition ParseRewards(object rawRewards)
        {
            QuestRewardDefinition rewards = new QuestRewardDefinition();
            if (rawRewards is not Dictionary<string, object> rewardDictionary)
            {
                return rewards;
            }

            rewards.Exp = ToInt(rewardDictionary.TryGetValue("exp", out object exp) ? exp : 0);
            rewards.Coins = ToInt(rewardDictionary.TryGetValue("coins", out object coins) ? coins : 0);
            if (rewardDictionary.TryGetValue("items", out object items) && items is Dictionary<string, object> itemDictionary)
            {
                foreach (KeyValuePair<string, object> pair in itemDictionary)
                {
                    rewards.Items[NormalizeId(pair.Key)] = ToInt(pair.Value);
                }
            }

            return rewards;
        }

        private int CountByType(string type)
        {
            int count = 0;
            foreach (QuestDefinition quest in quests.Values)
            {
                if (quest.Type == type)
                {
                    count++;
                }
            }

            return count;
        }

        private static List<string> ToStringList(object value)
        {
            List<string> result = new List<string>();
            if (value is not IList list)
            {
                return result;
            }

            foreach (object item in list)
            {
                string text = Convert.ToString(item) ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(text))
                {
                    result.Add(text);
                }
            }

            return result;
        }

        private static string GetString(Dictionary<string, object> dictionary, string key, string fallback = "")
        {
            return dictionary.TryGetValue(key, out object value) ? Convert.ToString(value) ?? fallback : fallback;
        }

        private static int ToInt(object value, int fallback = 0)
        {
            if (value == null)
            {
                return fallback;
            }

            try
            {
                return Convert.ToInt32(value);
            }
            catch
            {
                return fallback;
            }
        }

        private static string NormalizeId(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToLowerInvariant();
        }
    }
}
