using System.Collections.Generic;

namespace TouhouMigration.Runtime.Social
{
    // The result of reconciling roster entries to canonical npc ids.
    public sealed class NpcRosterReconcileResult
    {
        // Roster id -> canonical npc id, for entries that have a dialogue NPC.
        public Dictionary<string, string> Matched { get; } = new Dictionary<string, string>();

        // Entries with no dialogue NPC but a spawnable model: intentional background NPCs (faithful to Godot,
        // which also has no dialogue for them) — spawn the model, no dialogue. NOT errors.
        public List<string> ModelOnlySpawns { get; } = new List<string>();

        // Entries with neither a dialogue NPC nor a model: genuinely need author attention.
        public List<string> Unmatched { get; } = new List<string>();
    }

    // Reconciles village-roster entries (whose ids are sometimes Chinese display names or nicknames) to the
    // canonical npc ids from the dialogue name->id map: an entry already carrying a canonical id keeps it;
    // otherwise its id/display-name is matched against the npc names by exact, then substring, match. Entries
    // that resolve to nothing are surfaced (not guessed) so an author can decide. Pure data-derivation — no
    // invention.
    public static class MigrationNpcRosterReconciler
    {
        // Canonical Touhou species/nickname -> npc id, for roster entries whose name is a known character
        // epithet (not a guess: each maps to an existing _npc_<id> dialogue file). Established identity, the
        // same kind of fact as Reimu = 博丽灵梦.
        public static readonly IReadOnlyDictionary<string, string> CanonicalAliases = new Dictionary<string, string>
        {
            ["夜雀"] = "mystia",    // "night sparrow" — Mystia Lorelei
            ["大狸子"] = "mamizou", // "big tanuki" — Mamizou Futatsuiwa
            ["河童"] = "nitori",    // "kappa" — Nitori Kawashiro
        };

        public static NpcRosterReconcileResult Reconcile(
            IEnumerable<MigrationNpcRosterEntry> entries,
            IReadOnlyDictionary<string, string> nameToCanonicalId,
            IReadOnlyDictionary<string, string> aliases = null)
        {
            NpcRosterReconcileResult result = new NpcRosterReconcileResult();
            if (entries == null)
            {
                return result;
            }

            IReadOnlyDictionary<string, string> nameMap = nameToCanonicalId ?? new Dictionary<string, string>();
            HashSet<string> canonicalIds = new HashSet<string>(nameMap.Values);

            foreach (MigrationNpcRosterEntry entry in entries)
            {
                if (entry == null)
                {
                    continue;
                }

                string resolved = Resolve(entry, nameMap, canonicalIds, aliases);
                if (resolved != null)
                {
                    result.Matched[entry.NpcId] = resolved;
                }
                else if (!string.IsNullOrEmpty(entry.ModelPath))
                {
                    // Dialogue-less but spawnable: an intentional background NPC (faithful to Godot).
                    result.ModelOnlySpawns.Add(entry.NpcId);
                }
                else
                {
                    result.Unmatched.Add(entry.NpcId);
                }
            }

            return result;
        }

        private static string Resolve(MigrationNpcRosterEntry entry, IReadOnlyDictionary<string, string> nameMap, HashSet<string> canonicalIds, IReadOnlyDictionary<string, string> aliases)
        {
            // 1) The roster id is already a canonical npc id.
            if (!string.IsNullOrEmpty(entry.NpcId) && canonicalIds.Contains(entry.NpcId))
            {
                return entry.NpcId;
            }

            // 1b) A curated canonical alias (species/nickname) on the roster id or display name.
            if (aliases != null)
            {
                foreach (string candidate in new[] { entry.NpcId, entry.DisplayName })
                {
                    if (!string.IsNullOrEmpty(candidate) && aliases.TryGetValue(candidate, out string aliasId))
                    {
                        return aliasId;
                    }
                }
            }

            // 2) Exact name match (on the roster id or its display name).
            foreach (string candidate in new[] { entry.NpcId, entry.DisplayName })
            {
                if (!string.IsNullOrEmpty(candidate) && nameMap.TryGetValue(candidate, out string exactId))
                {
                    return exactId;
                }
            }

            // 3) Substring match: a short form contained in (or containing) a canonical npc name.
            foreach (string candidate in new[] { entry.DisplayName, entry.NpcId })
            {
                if (string.IsNullOrEmpty(candidate))
                {
                    continue;
                }

                foreach (KeyValuePair<string, string> pair in nameMap)
                {
                    if (pair.Key.Contains(candidate) || candidate.Contains(pair.Key))
                    {
                        return pair.Value;
                    }
                }
            }

            return null;
        }
    }
}
