using System;
using System.Collections.Generic;
using TouhouMigration.Runtime.Social;

namespace TouhouMigration.Runtime.Dialogue
{
    public sealed class DialogueEffectRouter
    {
        private readonly SocialBondService bondService;
        private readonly QuestDeliveryService questDeliveryService;

        public DialogueEffectRouter(SocialBondService bondService, QuestDeliveryService questDeliveryService)
        {
            this.bondService = bondService;
            this.questDeliveryService = questDeliveryService;
        }

        public bool Apply(string npcId, Dictionary<string, object> effects)
        {
            if (effects == null || effects.Count == 0)
            {
                return false;
            }

            bool handledAny = false;
            foreach (KeyValuePair<string, object> effect in effects)
            {
                handledAny = ApplyEffect(npcId, NormalizeId(effect.Key), effect.Value) || handledAny;
            }

            return handledAny;
        }

        public bool ApplyAction(string actionId, Dictionary<string, object> payload)
        {
            if (string.IsNullOrWhiteSpace(actionId) || payload == null)
            {
                return false;
            }

            string npcId = payload.TryGetValue("npc_id", out object rawNpcId)
                ? Convert.ToString(rawNpcId) ?? string.Empty
                : string.Empty;
            object value = payload.TryGetValue("value", out object rawValue) ? rawValue : null;
            return ApplyEffect(npcId, NormalizeId(actionId), value);
        }

        private bool ApplyEffect(string npcId, string effectId, object value)
        {
            switch (effectId)
            {
                case "bond":
                    if (bondService == null)
                    {
                        return false;
                    }

                    bondService.AddBondPoints(npcId, "dialogue", ToInt(value));
                    return true;
                case "quest":
                case "start_quest":
                    return questDeliveryService != null && questDeliveryService.StartQuest(Convert.ToString(value) ?? string.Empty);
                case "quest_complete":
                case "complete_quest":
                    if (questDeliveryService == null)
                    {
                        return false;
                    }

                    questDeliveryService.MarkQuestCompleted(Convert.ToString(value) ?? string.Empty);
                    return true;
                case "counter":
                    return ApplyCounter(value);
                case "unlock_npc":
                    return questDeliveryService != null && questDeliveryService.UnlockNpc(Convert.ToString(value) ?? string.Empty);
                case "quest_progress":
                    return ApplyQuestProgress(value);
                default:
                    return false;
            }
        }

        private bool ApplyCounter(object value)
        {
            if (questDeliveryService == null)
            {
                return false;
            }

            if (value is Dictionary<string, object> dictionary)
            {
                string counterId = dictionary.TryGetValue("id", out object rawId)
                    ? Convert.ToString(rawId) ?? string.Empty
                    : string.Empty;
                int amount = dictionary.TryGetValue("amount", out object rawAmount) ? ToInt(rawAmount, 1) : 1;
                if (string.IsNullOrWhiteSpace(counterId))
                {
                    return false;
                }

                questDeliveryService.IncrementCounter(counterId, amount);
                return true;
            }

            string id = Convert.ToString(value) ?? string.Empty;
            if (string.IsNullOrWhiteSpace(id))
            {
                return false;
            }

            questDeliveryService.IncrementCounter(id);
            return true;
        }

        private bool ApplyQuestProgress(object value)
        {
            if (questDeliveryService == null || value is not Dictionary<string, object> dictionary)
            {
                return false;
            }

            string questId = dictionary.TryGetValue("quest_id", out object rawQuestId)
                ? Convert.ToString(rawQuestId) ?? string.Empty
                : string.Empty;
            int objectiveIndex = dictionary.TryGetValue("objective_index", out object rawObjectiveIndex)
                ? ToInt(rawObjectiveIndex)
                : 0;
            int amount = dictionary.TryGetValue("amount", out object rawAmount) ? ToInt(rawAmount, 1) : 1;
            if (string.IsNullOrWhiteSpace(questId))
            {
                return false;
            }

            questDeliveryService.UpdateQuestProgress(questId, objectiveIndex, amount);
            return true;
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
