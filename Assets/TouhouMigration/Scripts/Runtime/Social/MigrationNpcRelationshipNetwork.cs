using System;
using System.Collections.Generic;

namespace TouhouMigration.Runtime.Social
{
    // NPC-to-NPC relationship categories (Godot NPCRelationshipNetwork.RelationType).
    public enum NpcRelationType
    {
        Stranger,
        Acquaintance,
        Friend,
        CloseFriend,
        Rival,
        Enemy,
        MasterStudent,
        Family,
        Romantic,
    }

    // The NPC-to-NPC social graph (Godot NPCRelationshipNetwork): each unordered NPC pair has a fixed
    // relationship type plus a mutable value, and each NPC may belong to a faction with its own reputation.
    // Free of UnityEngine. Gossip, group events, and the player-bond reactions are deferred.
    public sealed class MigrationNpcRelationshipNetwork
    {
        private static readonly Dictionary<NpcRelationType, string> RelationshipNames = new Dictionary<NpcRelationType, string>
        {
            { NpcRelationType.Stranger, "陌生" },
            { NpcRelationType.Acquaintance, "认识" },
            { NpcRelationType.Friend, "朋友" },
            { NpcRelationType.CloseFriend, "好友" },
            { NpcRelationType.Rival, "对手" },
            { NpcRelationType.Enemy, "敌人" },
            { NpcRelationType.MasterStudent, "师徒" },
            { NpcRelationType.Family, "亲属" },
            { NpcRelationType.Romantic, "恋人" },
        };

        private sealed class Relationship
        {
            public NpcRelationType Type;
            public int BaseValue;
        }

        private readonly Dictionary<string, Relationship> relationships = new Dictionary<string, Relationship>();
        private readonly Dictionary<string, int> values = new Dictionary<string, int>();
        private readonly Dictionary<string, int> npcFactions = new Dictionary<string, int>();
        private readonly Dictionary<int, int> factionReputations = new Dictionary<int, int>();

        // Register a predefined relationship between two NPCs (Godot NPC_RELATIONSHIPS data).
        public void RegisterRelationship(string npc1, string npc2, NpcRelationType type, int value)
        {
            relationships[RelationshipId(npc1, npc2)] = new Relationship { Type = type, BaseValue = value };
        }

        public NpcRelationType GetRelationshipType(string npc1, string npc2)
        {
            return relationships.TryGetValue(RelationshipId(npc1, npc2), out Relationship rel)
                ? rel.Type
                : NpcRelationType.Stranger;
        }

        public string GetRelationshipName(string npc1, string npc2)
        {
            return RelationshipNames.TryGetValue(GetRelationshipType(npc1, npc2), out string name) ? name : "未知";
        }

        public bool AreFriends(string npc1, string npc2)
        {
            NpcRelationType type = GetRelationshipType(npc1, npc2);
            return type == NpcRelationType.Friend || type == NpcRelationType.CloseFriend;
        }

        public bool AreEnemies(string npc1, string npc2)
        {
            NpcRelationType type = GetRelationshipType(npc1, npc2);
            return type == NpcRelationType.Enemy || type == NpcRelationType.Rival;
        }

        // The pair's current value: the modified value if it has been touched, else the registered base, else 50.
        public int GetRelationshipValue(string npc1, string npc2)
        {
            string key = RelationshipId(npc1, npc2);
            if (values.TryGetValue(key, out int value))
            {
                return value;
            }

            return relationships.TryGetValue(key, out Relationship rel) ? rel.BaseValue : 50;
        }

        // Godot modify_relationship: the mutable value seeds at 50 on first modify (it does NOT carry over the
        // registered base value), then accumulates clamped to [-100, 100].
        public void ModifyRelationship(string npc1, string npc2, int amount)
        {
            string key = RelationshipId(npc1, npc2);
            int current = values.TryGetValue(key, out int value) ? value : 50;
            values[key] = Math.Clamp(current + amount, -100, 100);
        }

        // Faction membership + reputation (Godot NPCRelationshipNetwork faction system). NoFaction (-1) means
        // the NPC belongs to no faction.
        public const int NoFaction = -1;

        public void SetNpcFaction(string npcId, int faction)
        {
            npcFactions[npcId ?? string.Empty] = faction;
        }

        public int GetNpcFaction(string npcId)
        {
            return npcFactions.TryGetValue(npcId ?? string.Empty, out int faction) ? faction : NoFaction;
        }

        public bool AreSameFaction(string npc1, string npc2)
        {
            int faction = GetNpcFaction(npc1);
            return faction != NoFaction && faction == GetNpcFaction(npc2);
        }

        public int GetFactionReputation(int faction)
        {
            return factionReputations.TryGetValue(faction, out int reputation) ? reputation : 50;
        }

        public void ModifyFactionReputation(int faction, int amount)
        {
            factionReputations[faction] = Math.Clamp(GetFactionReputation(faction) + amount, 0, 100);
        }

        public IReadOnlyList<string> GetFactionMembers(int faction)
        {
            List<string> members = new List<string>();
            foreach (KeyValuePair<string, int> pair in npcFactions)
            {
                if (pair.Value == faction)
                {
                    members.Add(pair.Key);
                }
            }

            return members;
        }

        // Order-independent key for a pair (Godot _get_relationship_id sorts the two ids).
        private static string RelationshipId(string npc1, string npc2)
        {
            string a = npc1 ?? string.Empty;
            string b = npc2 ?? string.Empty;
            return string.CompareOrdinal(a, b) <= 0 ? a + "_" + b : b + "_" + a;
        }
    }
}
