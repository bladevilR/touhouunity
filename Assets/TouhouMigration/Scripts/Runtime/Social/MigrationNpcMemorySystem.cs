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
        public string Topic { get; set; }    // conversation topic this memory is about (for dialogue modifiers)
    }

    // How an NPC greets the player, derived from its impression (Godot get_dialogue_modifier greeting_style).
    public enum GreetingStyle { Neutral, Cold, Wary, Polite, Warm, Affectionate }

    // Coarse trust banding for dialogue (Godot get_dialogue_modifier trust_level).
    public enum TrustLevel { Low, Normal, High }

    // Dialogue adjustments an NPC applies based on its memories of the player (Godot get_dialogue_modifier).
    public sealed class NpcDialogueModifier
    {
        public NpcDialogueModifier(GreetingStyle greetingStyle, TrustLevel trustLevel, System.Collections.Generic.IReadOnlyList<string> specialTopics, System.Collections.Generic.IReadOnlyList<string> avoidTopics)
        {
            GreetingStyle = greetingStyle;
            TrustLevel = trustLevel;
            SpecialTopics = specialTopics;
            AvoidTopics = avoidTopics;
        }

        public GreetingStyle GreetingStyle { get; }
        public TrustLevel TrustLevel { get; }
        public System.Collections.Generic.IReadOnlyList<string> SpecialTopics { get; }
        public System.Collections.Generic.IReadOnlyList<string> AvoidTopics { get; }
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

        // Per-NPC personality (Godot NPC_PERSONALITY): memory capacity + decay rates. Others use the default.
        private sealed class Personality
        {
            public int MemoryCapacity;
            public double ForgivenessRate; // negative memories decay *= this (lower = holds grudges longer)
            public double GratitudeRate;   // positive memories decay /= this (higher = cherishes good memories)
        }

        private static readonly Dictionary<string, Personality> Personalities = new Dictionary<string, Personality>
        {
            { "keine", new Personality { MemoryCapacity = 50, ForgivenessRate = 0.7, GratitudeRate = 1.2 } },
            { "reimu", new Personality { MemoryCapacity = 40, ForgivenessRate = 0.5, GratitudeRate = 0.8 } },
            { "marisa", new Personality { MemoryCapacity = 35, ForgivenessRate = 0.9, GratitudeRate = 1.0 } },
            { "koishi", new Personality { MemoryCapacity = 10, ForgivenessRate = 1.0, GratitudeRate = 0.5 } },
            { "sakuya", new Personality { MemoryCapacity = 60, ForgivenessRate = 0.3, GratitudeRate = 1.0 } },
            { "kaguya", new Personality { MemoryCapacity = 100, ForgivenessRate = 0.6, GratitudeRate = 1.5 } },
        };

        private static readonly Personality DefaultPersonality =
            new Personality { MemoryCapacity = 30, ForgivenessRate = 0.5, GratitudeRate = 1.0 };

        private const double NotableWeight = 80.0;
        private const double MemoryDecayRate = 2.0;
        private const double MinMemoryWeight = 5.0;

        private static Personality GetPersonality(string npcId)
        {
            return Personalities.TryGetValue(npcId ?? string.Empty, out Personality personality) ? personality : DefaultPersonality;
        }

        private sealed class Memory
        {
            public NpcMemoryType Type;
            public double Weight;
            public bool IsPositive;
            public string Topic;
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
                Topic = context?.Topic,
            };

            int capacity = GetPersonality(npcId).MemoryCapacity;
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

        // Decay every NPC's memories by one day (Godot _decay_all_memories, driven by the day_started signal).
        public void DecayAllMemories()
        {
            foreach (KeyValuePair<string, NpcRecord> pair in npcs)
            {
                DecayNpcMemories(pair.Key, pair.Value);
            }
        }

        // Reduce each memory's weight by the personality-adjusted decay (Godot _decay_npc_memories): positive
        // memories fade slower for grateful NPCs, negative memories fade slower for unforgiving ones. Memories
        // below the minimum weight are forgotten.
        private static void DecayNpcMemories(string npcId, NpcRecord record)
        {
            Personality personality = GetPersonality(npcId);
            List<Memory> forgotten = null;
            foreach (Memory memory in record.Memories)
            {
                double decay = MemoryDecayRate;
                if (memory.IsPositive)
                {
                    decay /= personality.GratitudeRate;
                }
                else
                {
                    decay *= personality.ForgivenessRate;
                }

                memory.Weight -= decay;
                if (memory.Weight < MinMemoryWeight)
                {
                    (forgotten ??= new List<Memory>()).Add(memory);
                }
            }

            if (forgotten != null)
            {
                foreach (Memory memory in forgotten)
                {
                    record.Memories.Remove(memory);
                }
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

        // Dialogue adjustments from this NPC's memories of the player (Godot get_dialogue_modifier): a greeting
        // style + trust band from the current impression/trust, plus special topics (from notable memories) and
        // topics to avoid (from negative memories).
        public NpcDialogueModifier GetDialogueModifier(string npcId)
        {
            NpcRecord record = EnsureNpc(npcId);

            GreetingStyle greeting;
            switch (record.Impression)
            {
                case NpcImpression.Hostile: greeting = GreetingStyle.Cold; break;
                case NpcImpression.Distrustful: greeting = GreetingStyle.Wary; break;
                case NpcImpression.Stranger: greeting = GreetingStyle.Polite; break;
                case NpcImpression.Friend:
                case NpcImpression.CloseFriend: greeting = GreetingStyle.Warm; break;
                case NpcImpression.Romantic: greeting = GreetingStyle.Affectionate; break;
                default: greeting = GreetingStyle.Neutral; break;
            }

            double trust = record.Aspects["trust"];
            TrustLevel trustLevel = trust < 30.0 ? TrustLevel.Low : (trust > 70.0 ? TrustLevel.High : TrustLevel.Normal);

            List<string> specialTopics = new List<string>();
            foreach (Memory memory in record.NotableMemories)
            {
                if (!string.IsNullOrEmpty(memory.Topic))
                {
                    specialTopics.Add(memory.Topic);
                }
            }

            List<string> avoidTopics = new List<string>();
            foreach (Memory memory in record.Memories)
            {
                if (!memory.IsPositive && !string.IsNullOrEmpty(memory.Topic))
                {
                    avoidTopics.Add(memory.Topic);
                }
            }

            return new NpcDialogueModifier(greeting, trustLevel, specialTopics, avoidTopics);
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

        // Number of high-weight "notable" memories (Godot get_notable_memories).
        public int GetNotableMemoryCount(string npcId)
        {
            return EnsureNpc(npcId).NotableMemories.Count;
        }

        // Whether the NPC remembers first meeting the player (Godot recall_first_meeting non-empty).
        public bool HasFirstMeetingMemory(string npcId)
        {
            foreach (Memory memory in EnsureNpc(npcId).NotableMemories)
            {
                if (memory.Type == NpcMemoryType.FirstMeeting)
                {
                    return true;
                }
            }

            return false;
        }

        // Number of memories of a given type (Godot get_memories_of_type).
        public int GetMemoryCountOfType(string npcId, NpcMemoryType type)
        {
            int count = 0;
            foreach (Memory memory in EnsureNpc(npcId).Memories)
            {
                if (memory.Type == type)
                {
                    count++;
                }
            }

            return count;
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

        // Snapshot every NPC's memories + relationship aspects + impression for save/load (flat entry
        // lists for JsonUtility safety). Personalities + memory weights are static catalog data and are
        // not persisted; notable memories are rebuilt from the restored memory weights.
        public NpcMemorySnapshot CreateSnapshot()
        {
            NpcMemorySnapshot snapshot = new NpcMemorySnapshot();
            foreach (KeyValuePair<string, NpcRecord> pair in npcs)
            {
                NpcRecord record = pair.Value;
                snapshot.npcs.Add(new NpcAspectEntry
                {
                    npcId = pair.Key,
                    trust = record.Aspects.TryGetValue("trust", out double trust) ? trust : 50.0,
                    affection = record.Aspects.TryGetValue("affection", out double affection) ? affection : 50.0,
                    respect = record.Aspects.TryGetValue("respect", out double respect) ? respect : 50.0,
                    familiarity = record.Aspects.TryGetValue("familiarity", out double familiarity) ? familiarity : 0.0,
                    impression = (int)record.Impression,
                });

                foreach (Memory memory in record.Memories)
                {
                    snapshot.memories.Add(new NpcMemoryEntry
                    {
                        npcId = pair.Key,
                        type = (int)memory.Type,
                        weight = memory.Weight,
                        isPositive = memory.IsPositive,
                        topic = memory.Topic,
                    });
                }
            }

            return snapshot;
        }

        public void LoadSnapshot(NpcMemorySnapshot snapshot)
        {
            npcs.Clear();
            if (snapshot == null)
            {
                return;
            }

            if (snapshot.npcs != null)
            {
                foreach (NpcAspectEntry aspect in snapshot.npcs)
                {
                    if (string.IsNullOrEmpty(aspect.npcId))
                    {
                        continue;
                    }

                    NpcRecord record = new NpcRecord();
                    record.Aspects["trust"] = aspect.trust;
                    record.Aspects["affection"] = aspect.affection;
                    record.Aspects["respect"] = aspect.respect;
                    record.Aspects["familiarity"] = aspect.familiarity;
                    record.Impression = (NpcImpression)aspect.impression;
                    npcs[aspect.npcId] = record;
                }
            }

            if (snapshot.memories != null)
            {
                foreach (NpcMemoryEntry entry in snapshot.memories)
                {
                    if (string.IsNullOrEmpty(entry.npcId))
                    {
                        continue;
                    }

                    if (!npcs.TryGetValue(entry.npcId, out NpcRecord record))
                    {
                        record = new NpcRecord();
                        npcs[entry.npcId] = record;
                    }

                    Memory memory = new Memory
                    {
                        Type = (NpcMemoryType)entry.type,
                        Weight = entry.weight,
                        IsPositive = entry.isPositive,
                        Topic = entry.topic,
                    };
                    record.Memories.Add(memory);
                    if (memory.Weight >= NotableWeight)
                    {
                        record.NotableMemories.Add(memory);
                    }
                }
            }
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

    // Persisted NPC memory state: per-NPC memories (flat) + relationship aspects + impression.
    [Serializable]
    public sealed class NpcMemorySnapshot
    {
        public List<NpcMemoryEntry> memories = new List<NpcMemoryEntry>();
        public List<NpcAspectEntry> npcs = new List<NpcAspectEntry>();
    }

    [Serializable]
    public sealed class NpcMemoryEntry
    {
        public string npcId;
        public int type;
        public double weight;
        public bool isPositive;
        public string topic;
    }

    [Serializable]
    public sealed class NpcAspectEntry
    {
        public string npcId;
        public double trust;
        public double affection;
        public double respect;
        public double familiarity;
        public int impression;
    }
}
