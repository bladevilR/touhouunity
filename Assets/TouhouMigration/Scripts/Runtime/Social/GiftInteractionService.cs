using System.Collections.Generic;
using TouhouMigration.Runtime.Dialogue;
using TouhouMigration.Runtime.Inventory;

namespace TouhouMigration.Runtime.Social
{
    public sealed class GiftInteractionService
    {
        private readonly GiftDatabase giftDatabase;
        private readonly InventoryService inventoryService;
        private readonly DialogueDatabase dialogueDatabase;
        private readonly DialogueRuntimeFacade dialogueFacade;
        private readonly SocialBondService bondService;
        private readonly QuestDeliveryService questDeliveryService;
        private readonly MigrationNpcMemorySystem npcMemory;

        public GiftInteractionService(
            GiftDatabase giftDatabase,
            InventoryService inventoryService,
            DialogueDatabase dialogueDatabase,
            DialogueRuntimeFacade dialogueFacade)
            : this(giftDatabase, inventoryService, dialogueDatabase, dialogueFacade, null, null, null)
        {
        }

        // 6-arg overload kept for existing (incl. reflection-based) callers that predate the npcMemory hook.
        public GiftInteractionService(
            GiftDatabase giftDatabase,
            InventoryService inventoryService,
            DialogueDatabase dialogueDatabase,
            DialogueRuntimeFacade dialogueFacade,
            SocialBondService bondService,
            QuestDeliveryService questDeliveryService)
            : this(giftDatabase, inventoryService, dialogueDatabase, dialogueFacade, bondService, questDeliveryService, null)
        {
        }

        public GiftInteractionService(
            GiftDatabase giftDatabase,
            InventoryService inventoryService,
            DialogueDatabase dialogueDatabase,
            DialogueRuntimeFacade dialogueFacade,
            SocialBondService bondService,
            QuestDeliveryService questDeliveryService,
            MigrationNpcMemorySystem npcMemory)
        {
            this.giftDatabase = giftDatabase;
            this.inventoryService = inventoryService;
            this.dialogueDatabase = dialogueDatabase;
            this.dialogueFacade = dialogueFacade;
            this.bondService = bondService;
            this.questDeliveryService = questDeliveryService;
            this.npcMemory = npcMemory;
        }

        public GiftDeliveryResult GiveGift(string npcId, string giftId)
        {
            return GiveGift(npcId, giftId, 1);
        }

        public GiftDeliveryResult GiveGift(string npcId, string giftId, int amount)
        {
            if (giftDatabase == null || !giftDatabase.HasGift(giftId))
            {
                return Failure(npcId, giftId, "unknown_gift");
            }

            if (inventoryService == null || inventoryService.GetItemCount(giftId) < amount)
            {
                return Failure(npcId, giftId, "insufficient_inventory");
            }

            if (!inventoryService.RemoveItem(giftId, amount))
            {
                return Failure(npcId, giftId, "remove_failed");
            }

            GiftReactionResult reaction = giftDatabase.GetReaction(npcId, giftId);
            StartReactionDialogue(npcId, reaction);

            GiftDeliveryResult result = new GiftDeliveryResult
            {
                Success = true,
                NpcId = npcId,
                GiftId = giftId,
                ReactionId = reaction.ReactionId,
                BondChange = reaction.BondChange,
                Dialogue = reaction.Dialogue,
                SpecialEvent = reaction.SpecialEvent,
                RemainingAmount = inventoryService.GetItemCount(giftId)
            };
            bondService?.ApplyGiftResult(result);
            questDeliveryService?.NotifyGiftDelivery(result, amount);
            npcMemory?.AddMemory(npcId, NpcMemoryType.GiftReceived, new NpcMemoryContext { Liked = result.BondChange >= 0 });
            return result;
        }

        public List<string> GetGiftableInventoryIds()
        {
            List<string> giftIds = new List<string>();
            if (inventoryService == null || giftDatabase == null)
            {
                return giftIds;
            }

            foreach (KeyValuePair<string, int> item in inventoryService.GetAllItems())
            {
                if (item.Value > 0 && giftDatabase.HasGift(item.Key))
                {
                    giftIds.Add(item.Key);
                }
            }

            giftIds.Sort();
            return giftIds;
        }

        private void StartReactionDialogue(string npcId, GiftReactionResult reaction)
        {
            if (dialogueFacade == null)
            {
                return;
            }

            string speaker = dialogueDatabase != null ? dialogueDatabase.GetNpcName(npcId) : string.Empty;
            if (string.IsNullOrWhiteSpace(speaker))
            {
                speaker = npcId;
            }

            dialogueFacade.StartLines(npcId, new[]
            {
                new DialogueLine
                {
                    Speaker = speaker,
                    Text = reaction.Dialogue,
                    Expression = ExpressionForReaction(reaction.ReactionId)
                }
            });
        }

        private static string ExpressionForReaction(string reactionId)
        {
            return reactionId switch
            {
                "LOVE" or "LIKE" or "SPECIAL" => "happy",
                "DISLIKE" => "sad",
                "HATE" => "angry",
                _ => "neutral"
            };
        }

        private static GiftDeliveryResult Failure(string npcId, string giftId, string reason)
        {
            return new GiftDeliveryResult
            {
                Success = false,
                NpcId = npcId,
                GiftId = giftId,
                FailureReason = reason
            };
        }
    }
}
