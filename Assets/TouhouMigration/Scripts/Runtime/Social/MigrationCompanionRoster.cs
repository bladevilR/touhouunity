using System;
using System.Collections.Generic;

namespace TouhouMigration.Runtime.Social
{
    // Companion recruitment + the active party + per-skill cooldowns (Godot CompanionSystem). Companions are
    // recruited once, at most one follows the player at a time (Godot MAX_COMPANIONS_IN_PARTY = 1), and each
    // recruited companion's skills count down via TickSkillCooldowns. Free of UnityEngine. Combat stats, skill
    // data/effects/HP, recruitment-condition checks (bond level), schedule conflicts, and the SignalBus
    // emissions are deferred (data/signal-coupled).
    public sealed class MigrationCompanionRoster
    {
        private readonly HashSet<string> recruited = new HashSet<string>();
        private readonly Dictionary<(string Npc, string Skill), double> skillCooldowns = new Dictionary<(string, string), double>();
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

        // Put a recruited companion's skill on cooldown for the given seconds (Godot use_skill sets it from the
        // skill data; here it is supplied explicitly). Ignored for unrecruited companions.
        public void PutSkillOnCooldown(string npcId, string skillId, double seconds)
        {
            if (!IsRecruited(npcId))
            {
                return;
            }

            skillCooldowns[(npcId, skillId ?? string.Empty)] = Math.Max(0.0, seconds);
        }

        // Count every skill cooldown down by the elapsed seconds (Godot _update_skill_cooldowns), clamped at 0.
        public void TickSkillCooldowns(double deltaSeconds)
        {
            foreach ((string Npc, string Skill) key in new List<(string, string)>(skillCooldowns.Keys))
            {
                if (skillCooldowns[key] > 0.0)
                {
                    skillCooldowns[key] = Math.Max(skillCooldowns[key] - deltaSeconds, 0.0);
                }
            }
        }

        public double GetSkillCooldown(string npcId, string skillId)
        {
            return skillCooldowns.TryGetValue((npcId ?? string.Empty, skillId ?? string.Empty), out double cooldown) ? cooldown : 0.0;
        }

        public bool IsSkillReady(string npcId, string skillId)
        {
            return GetSkillCooldown(npcId, skillId) <= 0.0;
        }
    }
}
