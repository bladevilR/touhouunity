using System.Collections.Generic;

namespace TouhouMigration.Runtime.Social
{
    // Companion recruitment + the active party (Godot CompanionSystem). Companions are recruited once, and at
    // most one recruited companion follows the player at a time (Godot MAX_COMPANIONS_IN_PARTY = 1). Free of
    // UnityEngine. Combat stats/skills/HP, recruitment-condition checks (bond level), schedule conflicts, and
    // the SignalBus emissions are deferred (data/signal-coupled).
    public sealed class MigrationCompanionRoster
    {
        private readonly HashSet<string> recruited = new HashSet<string>();
        private string activeCompanion = string.Empty;

        public string ActiveCompanionId => activeCompanion;

        // Recruit a companion (Godot recruit_companion): each companion is recruited at most once.
        public bool Recruit(string npcId)
        {
            if (string.IsNullOrEmpty(npcId) || recruited.Contains(npcId))
            {
                return false;
            }

            recruited.Add(npcId);
            return true;
        }

        public bool IsRecruited(string npcId)
        {
            return recruited.Contains(npcId ?? string.Empty);
        }

        // Make a recruited companion the active party member (Godot add_to_party): fails if it is not recruited
        // or if a different companion is already active (the party holds one).
        public bool AddToParty(string npcId)
        {
            if (!IsRecruited(npcId))
            {
                return false;
            }

            if (activeCompanion.Length != 0 && activeCompanion != npcId)
            {
                return false;
            }

            activeCompanion = npcId;
            return true;
        }

        // Remove a companion from the party (Godot remove_from_party): fails if it is not recruited or not in
        // the party; frees the active slot.
        public bool RemoveFromParty(string npcId)
        {
            if (!IsRecruited(npcId) || activeCompanion != npcId)
            {
                return false;
            }

            activeCompanion = string.Empty;
            return true;
        }

        public bool IsInParty(string npcId)
        {
            return activeCompanion.Length != 0 && activeCompanion == npcId;
        }

        public IReadOnlyCollection<string> GetAllRecruited()
        {
            return new List<string>(recruited);
        }
    }
}
