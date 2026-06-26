using System.Collections.Generic;

namespace TouhouMigration.Runtime.Social
{
    // The ambient NPC-to-NPC dialogue trigger engine (Godot NPCCrossDialogueSystem): registered dual/triple
    // conversations fire when the right NPCs share a location, gated by a 5-minute per-dialogue cooldown,
    // with triples taking priority over duals. UnityEngine-free; the (large, hardcoded) dialogue line
    // content is data fed in via Register* — this is the matching + cooldown engine.
    public sealed class MigrationNpcCrossDialogue
    {
        public const double DialogueCooldownSeconds = 300.0;

        private readonly Dictionary<string, string> dualDialogues = new Dictionary<string, string>();
        private readonly Dictionary<string, string> tripleDialogues = new Dictionary<string, string>();
        private readonly Dictionary<string, double> cooldowns = new Dictionary<string, double>();

        public void RegisterDual(string npcA, string npcB, string dialogueId)
        {
            if (!string.IsNullOrEmpty(npcA) && !string.IsNullOrEmpty(npcB) && !string.IsNullOrEmpty(dialogueId))
            {
                dualDialogues[PairKey(npcA, npcB)] = dialogueId;
            }
        }

        public void RegisterTriple(string npcA, string npcB, string npcC, string dialogueId)
        {
            if (!string.IsNullOrEmpty(npcA) && !string.IsNullOrEmpty(npcB) && !string.IsNullOrEmpty(npcC)
                && !string.IsNullOrEmpty(dialogueId))
            {
                tripleDialogues[TripleKey(npcA, npcB, npcC)] = dialogueId;
            }
        }

        // Try to trigger a cross dialogue for the NPCs present (Godot check_and_trigger_dialogue): triples
        // first, then duals; skip any dialogue on cooldown; on a trigger start its cooldown. Returns the
        // triggered dialogue id, or null when none fires.
        public string CheckAndTrigger(IEnumerable<string> npcsPresent)
        {
            List<string> present = new List<string>();
            if (npcsPresent != null)
            {
                foreach (string npc in npcsPresent)
                {
                    if (!string.IsNullOrEmpty(npc) && !present.Contains(npc))
                    {
                        present.Add(npc);
                    }
                }
            }

            if (present.Count < 2)
            {
                return null;
            }

            // Triples take priority (Godot tries triple dialogues before dual).
            for (int i = 0; i < present.Count; i++)
            {
                for (int j = i + 1; j < present.Count; j++)
                {
                    for (int k = j + 1; k < present.Count; k++)
                    {
                        string id = MatchDialogue(tripleDialogues, TripleKey(present[i], present[j], present[k]));
                        if (id != null)
                        {
                            return Trigger(id);
                        }
                    }
                }
            }

            for (int i = 0; i < present.Count; i++)
            {
                for (int j = i + 1; j < present.Count; j++)
                {
                    string id = MatchDialogue(dualDialogues, PairKey(present[i], present[j]));
                    if (id != null)
                    {
                        return Trigger(id);
                    }
                }
            }

            return null;
        }

        public void TickCooldowns(double deltaSeconds)
        {
            double delta = deltaSeconds;
            if (delta <= 0.0)
            {
                return;
            }

            List<string> expired = null;
            List<string> keys = new List<string>(cooldowns.Keys);
            foreach (string id in keys)
            {
                double next = cooldowns[id] - delta;
                if (next <= 0.0)
                {
                    (expired ??= new List<string>()).Add(id);
                }
                else
                {
                    cooldowns[id] = next;
                }
            }

            if (expired != null)
            {
                foreach (string id in expired)
                {
                    cooldowns.Remove(id);
                }
            }
        }

        public bool IsOnCooldown(string dialogueId)
        {
            return dialogueId != null && cooldowns.TryGetValue(dialogueId, out double remaining) && remaining > 0.0;
        }

        private string MatchDialogue(Dictionary<string, string> table, string key)
        {
            return table.TryGetValue(key, out string id) && !IsOnCooldown(id) ? id : null;
        }

        private string Trigger(string dialogueId)
        {
            cooldowns[dialogueId] = DialogueCooldownSeconds;
            return dialogueId;
        }

        private static string PairKey(string a, string b)
        {
            return string.CompareOrdinal(a, b) <= 0 ? a + "_" + b : b + "_" + a;
        }

        private static string TripleKey(string a, string b, string c)
        {
            string[] ids = { a, b, c };
            System.Array.Sort(ids, System.StringComparer.Ordinal);
            return ids[0] + "_" + ids[1] + "_" + ids[2];
        }
    }
}
