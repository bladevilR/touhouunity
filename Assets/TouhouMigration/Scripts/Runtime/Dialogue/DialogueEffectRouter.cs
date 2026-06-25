using System;
using System.Collections.Generic;
using TouhouMigration.Runtime.Inventory;
using TouhouMigration.Runtime.Narrative;
using TouhouMigration.Runtime.Player;
using TouhouMigration.Runtime.Social;

namespace TouhouMigration.Runtime.Dialogue
{
    public sealed class DialogueEffectRouter
    {
        private readonly SocialBondService bondService;
        private readonly QuestDeliveryService questDeliveryService;
        private InventoryService inventoryService;
        private HumanityService humanityService;
        private MigrationStoryFlagService storyFlagService;
        private MigrationNpcMemorySystem npcMemory;

        public DialogueEffectRouter(SocialBondService bondService, QuestDeliveryService questDeliveryService)
        {
            this.bondService = bondService;
            this.questDeliveryService = questDeliveryService;
        }

        // Optional inventory routing for dialogue give/take-item effects (non-breaking; existing
        // callers that never bind an inventory keep their current behavior).
        public void BindInventory(InventoryService inventory)
        {
            inventoryService = inventory;
        }

        // Optional humanity routing for dialogue "humanity" fx (non-breaking; callers that never
        // bind a humanity service keep their current no-op behavior for that effect).
        public void BindHumanity(HumanityService humanity)
        {
            humanityService = humanity;
        }

        // Optional story-flag routing for dialogue "event" fx (narrative events); no-op when unbound.
        public void BindStoryFlags(MigrationStoryFlagService storyFlags)
        {
            storyFlagService = storyFlags;
        }

        // Optional NPC-memory routing: a dialogue interaction that applies effects forms a
        // DialogueChoice memory for that NPC (non-breaking; unbound callers keep current behavior).
        public void BindMemory(MigrationNpcMemorySystem memory)
        {
            npcMemory = memory;
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

            if (handledAny && !string.IsNullOrEmpty(npcId))
            {
                npcMemory?.AddMemory(npcId, NpcMemoryType.DialogueChoice);
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
            // Cross-NPC bond effects (Godot bond_<npcId>, e.g. bond_keine raised while talking to
            // someone else). Plain "bond" (no target suffix) is handled by the switch below and
            // applies to the current NPC.
            if (effectId != null
                && effectId.StartsWith("bond_", StringComparison.Ordinal)
                && effectId.Length > "bond_".Length)
            {
                if (bondService == null)
                {
                    return false;
                }

                string targetNpcId = effectId.Substring("bond_".Length);
                bondService.AddBondPoints(targetNpcId, "dialogue", ToInt(value));
                return true;
            }

            switch (effectId)
            {
                case "bond":
                    if (bondService == null)
                    {
                        return false;
                    }

                    bondService.AddBondPoints(npcId, "dialogue", ToInt(value));
                    return true;
                case "humanity":
                    if (humanityService == null)
                    {
                        return false;
                    }

                    humanityService.Adjust(ToInt(value));
                    return true;
                case "event":
                    if (storyFlagService == null)
                    {
                        return false;
                    }

                    string eventId = Convert.ToString(value) ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(eventId))
                    {
                        return false;
                    }

                    storyFlagService.MarkEvent(eventId);
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
                case "give_item":
                case "add_item":
                    return ApplyInventoryChange(value, give: true);
                case "take_item":
                case "remove_item":
                    return ApplyInventoryChange(value, give: false);
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

        private bool ApplyInventoryChange(object value, bool give)
        {
            if (inventoryService == null)
            {
                return false;
            }

            string itemId;
            int amount;
            if (value is Dictionary<string, object> dictionary)
            {
                itemId = dictionary.TryGetValue("item_id", out object rawItemId)
                    ? Convert.ToString(rawItemId) ?? string.Empty
                    : string.Empty;
                amount = dictionary.TryGetValue("amount", out object rawAmount) ? ToInt(rawAmount, 1) : 1;
            }
            else
            {
                itemId = Convert.ToString(value) ?? string.Empty;
                amount = 1;
            }

            if (string.IsNullOrWhiteSpace(itemId) || amount <= 0)
            {
                return false;
            }

            return give
                ? inventoryService.AddItem(itemId, amount)
                : inventoryService.RemoveItem(itemId, amount);
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
