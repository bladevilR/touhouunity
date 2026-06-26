using System.Collections.Generic;

namespace TouhouMigration.Runtime.Social
{
    // The bond-event lifecycle (Godot BondEventSystem get_available_events / start_event / complete_event):
    // per-NPC bond milestone events become available once the bond level is met (and any cooldown has
    // elapsed), can be started + completed, and one-time events stay done while cooldown'd (repeatable)
    // events re-offer after their cooldown. UnityEngine-free; the event data + dialogue + rewards are
    // injected/scene-side (registered with their bond requirement + cooldown).
    public sealed class MigrationBondEventSystem
    {
        private sealed class BondEvent
        {
            public string NpcId;
            public string EventId;
            public int RequiredBondLevel;
            public int CooldownDays;
            public bool Completed;
            public int CooldownRemaining;
        }

        private readonly Dictionary<string, BondEvent> events = new Dictionary<string, BondEvent>();
        private readonly Dictionary<string, List<string>> available = new Dictionary<string, List<string>>();

        private static string Key(string npcId, string eventId) => npcId + "/" + eventId;

        public void RegisterEvent(string npcId, string eventId, int requiredBondLevel, int cooldownDays = 0)
        {
            if (string.IsNullOrEmpty(npcId) || string.IsNullOrEmpty(eventId))
            {
                return;
            }

            events[Key(npcId, eventId)] = new BondEvent
            {
                NpcId = npcId,
                EventId = eventId,
                RequiredBondLevel = requiredBondLevel,
                CooldownDays = cooldownDays,
            };
        }

        // Recompute which of an NPC's events are currently available (Godot available_events): bond level
        // met, off cooldown, and — for one-time events (no cooldown) — not already completed.
        public void Evaluate(string npcId, int bondLevel)
        {
            List<string> list = new List<string>();
            foreach (BondEvent ev in events.Values)
            {
                if (ev.NpcId != npcId)
                {
                    continue;
                }

                bool oneTimeDone = ev.CooldownDays <= 0 && ev.Completed;
                if (bondLevel >= ev.RequiredBondLevel && ev.CooldownRemaining <= 0 && !oneTimeDone)
                {
                    list.Add(ev.EventId);
                }
            }

            available[npcId] = list;
        }

        public IReadOnlyList<string> GetAvailableEvents(string npcId)
        {
            return npcId != null && available.TryGetValue(npcId, out List<string> list) ? list : System.Array.Empty<string>();
        }

        public bool HasAvailableEvent(string npcId)
        {
            return npcId != null && available.TryGetValue(npcId, out List<string> list) && list.Count > 0;
        }

        public bool StartEvent(string npcId, string eventId)
        {
            return events.ContainsKey(Key(npcId ?? string.Empty, eventId ?? string.Empty))
                && IsListed(npcId, eventId);
        }

        // Complete an event (Godot complete_event): mark it done, drop it from available, and start its
        // cooldown.
        public bool CompleteEvent(string npcId, string eventId)
        {
            if (!events.TryGetValue(Key(npcId ?? string.Empty, eventId ?? string.Empty), out BondEvent ev))
            {
                return false;
            }

            ev.Completed = true;
            ev.CooldownRemaining = ev.CooldownDays;
            if (available.TryGetValue(npcId, out List<string> list))
            {
                list.Remove(eventId);
            }

            return true;
        }

        public bool IsEventCompleted(string npcId, string eventId)
        {
            return events.TryGetValue(Key(npcId ?? string.Empty, eventId ?? string.Empty), out BondEvent ev) && ev.Completed;
        }

        // Count every cooldown down one day (Godot day-start hook).
        public void TickCooldowns()
        {
            foreach (BondEvent ev in events.Values)
            {
                if (ev.CooldownRemaining > 0)
                {
                    ev.CooldownRemaining--;
                }
            }
        }

        private bool IsListed(string npcId, string eventId)
        {
            if (npcId == null || !available.TryGetValue(npcId, out List<string> list))
            {
                return false;
            }

            return list.Contains(eventId);
        }
    }
}
