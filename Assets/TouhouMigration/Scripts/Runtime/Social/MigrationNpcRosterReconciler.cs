using System.Collections.Generic;

namespace TouhouMigration.Runtime.Social
{
    // The result of reconciling roster entries to canonical npc ids.
    public sealed class NpcRosterReconcileResult
    {
        public Dictionary<string, string> Matched { get; } = new Dictionary<string, string>();
        public List<string> Unmatched { get; } = new List<string>();
    }

    // Reconciles village-roster entries (whose ids are sometimes Chinese display names or nicknames) to the
    // canonical npc ids from the dialogue name->id map: an entry already carrying a canonical id keeps it;
    // otherwise its id/display-name is matched against the npc names by exact, then substring, match. Entries
    // that resolve to nothing are surfaced (not guessed) so an author can decide. Pure data-derivation — no
    // invention.
    public static class MigrationNpcRosterReconciler
    {
        public static NpcRosterReconcileResult Reconcile(
            IEnumerable<MigrationNpcRosterEntry> entries,
            IReadOnlyDictionary<string, string> nameToCanonicalId)
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

                string resolved = Resolve(entry, nameMap, canonicalIds);
                if (resolved != null)
                {
                    result.Matched[entry.NpcId] = resolved;
                }
                else
                {
                    result.Unmatched.Add(entry.NpcId);
                }
            }

            return result;
        }

        private static string Resolve(MigrationNpcRosterEntry entry, IReadOnlyDictionary<string, string> nameMap, HashSet<string> canonicalIds)
        {
            // 1) The roster id is already a canonical npc id.
            if (!string.IsNullOrEmpty(entry.NpcId) && canonicalIds.Contains(entry.NpcId))
            {
                return entry.NpcId;
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
