using System;
using System.Collections.Generic;

namespace TouhouMigration.Runtime.Social
{
    // Memory categories (Godot NPCMemorySystem.MemoryType).
    public enum NpcMemoryType
    {
        FirstMeeting,
        GiftReceived,
        DialogueChoice,
        QuestHelp,
        CombatTogether,
        WitnessedAction,
        SpecialEvent,
        Betrayal,
        Kindness,
        RepeatedVisit,
    }

    // Overall impression bands (Godot NPCMemorySystem.Impression).
    public enum NpcImpression
    {
        Unknown,
        Stranger,
        Acquaintance,
        Friend,
        CloseFriend,
        Romantic,
        Distrustful,
        Hostile,
    }

    // Optional context for a memory (Godot's context Dictionary), modelled as typed optional fields.
    public sealed class NpcMemoryContext
    {
        public bool? Liked { get; set; }     // GiftReceived: was the gift liked? (default true)
        public bool? WasGood { get; set; }   // WitnessedAction: was the witnessed action good? (default true)
        public string Aspect { get; set; }   // DialogueChoice: which relationship aspect the choice affects
    }

    // Lets NPCs remember the player's actions and form an impression (Godot NPCMemorySystem). Each memory
    // carries a weight and a positive/negative valence; adding one shifts the NPC's relationship aspects
    // (trust/affection/respect/familiarity, 0-100) and recomputes the impression. Memory storage is capped
    // per NPC by personality. Free of UnityEngine.
    //
    // Memory decay (driven by the day_started signal), save/load, dialogue modifiers, notable-memory queries,
    // and the SignalBus emissions are deferred to later slices.
    public sealed class MigrationNpcMemorySystem
    {
        private static readonly Dictionary<NpcMemoryType, int> MemoryWeights = new Dictionary<NpcMemoryType, int>
        {
            { NpcMemoryType.FirstMeeting, 100 },
            { NpcMemoryType.GiftReceived, 30 },
            { NpcMemoryType.DialogueChoice, 20 },
            { NpcMemoryType.QuestHelp, 50 },
            { NpcMemoryType.CombatTogether, 40 },
            { NpcMemoryType.WitnessedAction, 60 },
            { NpcMemoryType.SpecialEvent, 80 },
            { NpcMemoryType.Betrayal, 150 },
            { NpcMemoryType.Kindness, 35 },
            { NpcMemoryType.RepeatedVisit, 15 },
        };

        // Per-NPC memory capacity (Godot NPC_PERSONALITY.memory_capacity); others use the default.
        private static readonly Dictionary<string, int> MemoryCapacity = new Dictionary<string, int>
        {
            { "keine", 50 },
            { "reimu", 40 },
            { "marisa", 35 },
            { "koishi", 10 },
            { "sakuya", 60 },
            { "kaguya", 100 },
        };

        private const int DefaultMemoryCapacity = 30;
        private const double NotableWeight = 80.0;

        private sealed class Memory
        {
            public NpcMemoryType Type;
            public double Weight;
            public bool IsPositive;
        }

        private sealed class NpcRecord
        {
            public readonly List<Memory> Memories = new List<Memory>();
            public readonly List<Memory> NotableMemories = new List<Memory>();
            public readonly Dictionary<string, double> Aspects = new Dictionary<string, double>
            {
                { "trust", 50.0 },
                { "affection", 50.0 },
                { "respect", 50.0 },
                { "familiarity", 0.0 },
            };

            public NpcImpression Impression = NpcImpression.Unknown;
        }

        private readonly Dictionary<string, NpcRecord> npcs = new Dictionary<string, NpcRecord>();

        // Form a new memory for an NPC (Godot add_memory): evicts the weakest memory if at capacity, then
        // updates the relationship aspects and impression and flags notable (high-weight) memories.
        public void AddMemory(string npcId, NpcMemoryType type, NpcMemoryContext context = null)
        {
            NpcRecord record = EnsureNpc(npcId);
            Memory memory = new Memory
            {
                Type = type,
                Weight = MemoryWeights.TryGetValue(type, out int weight) ? weight : 10,
                IsPositive = IsPositiveMemory(type, context),
            };

            int capacity = MemoryCapacity.TryGetValue(npcId ?? string.Empty, out int cap) ? cap : DefaultMemoryCapacity;
            if (record.Memories.Count >= capacity)
            {
                RemoveWeakestMemory(record);
            }

            record.Memories.Add(memory);
            UpdateRelationshipFromMemory(record, memory, context);

            if (memory.Weight >= NotableWeight)
            {
                record.NotableMemories.Add(memory);
            }
        }

        public NpcImpression GetImpression(string npcId)
        {
            return EnsureNpc(npcId).Impression;
        }

        public string GetImpressionName(string npcId)
        {
            switch (GetImpression(npcId))
            {
                case NpcImpression.Unknown: return "不了解";
                case NpcImpression.Stranger: return "陌生人";
                case NpcImpression.Acquaintance: return "熟人";
                case NpcImpression.Friend: return "朋友";
                case NpcImpression.CloseFriend: return "挚友";
                case NpcImpression.Romantic: return "特别的人";
                case NpcImpression.Distrustful: return "不信任";
                case NpcImpression.Hostile: return "敌对";
                default: return "未知";
            }
        }

        public int GetRelationshipAspect(string npcId, string aspect)
        {
            NpcRecord record = EnsureNpc(npcId);
            return (int)(record.Aspects.TryGetValue(aspect ?? string.Empty, out double value) ? value : 50.0);
        }

        public int GetMemoryCount(string npcId)
        {
            return EnsureNpc(npcId).Memories.Count;
        }

        public bool HasMemoryOf(string npcId, NpcMemoryType type)
        {
            foreach (Memory memory in EnsureNpc(npcId).Memories)
            {
                if (memory.Type == type)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsPositiveMemory(NpcMemoryType type, NpcMemoryContext context)
        {
            switch (type)
            {
                case NpcMemoryType.GiftReceived:
                    return context?.Liked ?? true;
                case NpcMemoryType.Betrayal:
                    return false;
                case NpcMemoryType.Kindness:
                    return true;
                case NpcMemoryType.WitnessedAction:
                    return context?.WasGood ?? true;
                default:
                    return true;
            }
        }

        // Shift the relationship aspects from a memory (Godot _update_relationship_from_memory), then refresh
        // the impression.
        private static void UpdateRelationshipFromMemory(NpcRecord record, Memory memory, NpcMemoryContext context)
        {
            double change = memory.Weight / 10.0;
            if (!memory.IsPositive)
            {
                change = -change;
            }

            switch (memory.Type)
            {
                case NpcMemoryType.GiftReceived:
                    ChangeAspect(record, "affection", change);
                    break;
                case NpcMemoryType.QuestHelp:
                    ChangeAspect(record, "trust", change);
                    ChangeAspect(record, "respect", change * 0.5);
                    break;
                case NpcMemoryType.CombatTogether:
                    ChangeAspect(record, "trust", change);
                    ChangeAspect(record, "familiarity", change);
                    break;
                case NpcMemoryType.Betrayal:
                    ChangeAspect(record, "trust", change * 2);
                    ChangeAspect(record, "affection", change);
                    break;
                case NpcMemoryType.Kindness:
                    ChangeAspect(record, "affection", change);
                    break;
                case NpcMemoryType.RepeatedVisit:
                    ChangeAspect(record, "familiarity", change);
                    break;
                case NpcMemoryType.DialogueChoice:
                    if (!string.IsNullOrEmpty(context?.Aspect))
                    {
                        ChangeAspect(record, context.Aspect, change);
                    }

                    break;
            }

            UpdateImpression(record);
        }

        private static void ChangeAspect(NpcRecord record, string aspect, double amount)
        {
            if (!record.Aspects.TryGetValue(aspect, out double old))
            {
                return;
            }

            record.Aspects[aspect] = Math.Clamp(old + amount, 0.0, 100.0);
        }

        // Recompute the overall impression from trust/affection/familiarity (Godot _update_impression).
        private static void UpdateImpression(NpcRecord record)
        {
            double trust = record.Aspects["trust"];
            double affection = record.Aspects["affection"];
            double familiarity = record.Aspects["familiarity"];
            double overall = (trust + affection + familiarity) / 3.0;

            NpcImpression impression;
            if (trust < 20)
            {
                impression = NpcImpression.Hostile;
            }
            else if (trust < 40)
            {
                impression = NpcImpression.Distrustful;
            }
            else if (familiarity < 20)
            {
                impression = NpcImpression.Stranger;
            }
            else if (overall < 50)
            {
                impression = NpcImpression.Acquaintance;
            }
            else if (overall < 70)
            {
                impression = NpcImpression.Friend;
            }
            else if (affection >= 85 && trust >= 80)
            {
                impression = NpcImpression.Romantic;
            }
            else if (overall >= 80)
            {
                impression = NpcImpression.CloseFriend;
            }
            else
            {
                impression = NpcImpression.Friend;
            }

            record.Impression = impression;
        }

        private static void RemoveWeakestMemory(NpcRecord record)
        {
            if (record.Memories.Count == 0)
            {
                return;
            }

            Memory weakest = record.Memories[0];
            foreach (Memory memory in record.Memories)
            {
                if (memory.Weight < weakest.Weight)
                {
                    weakest = memory;
                }
            }

            record.Memories.Remove(weakest);
        }

        private NpcRecord EnsureNpc(string npcId)
        {
            string key = npcId ?? string.Empty;
            if (!npcs.TryGetValue(key, out NpcRecord record))
            {
                record = new NpcRecord();
                npcs[key] = record;
            }

            return record;
        }
    }
}
