using System.Collections.Generic;

namespace TouhouMigration.Runtime.Social
{
    // Registry of NPCs and their daily schedules (Godot NPCScheduleManager): resolves where an NPC is
    // at a given hour from its schedule, falling back to its home location. Free of UnityEngine.
    public sealed class MigrationNpcManager
    {
        private sealed class NpcRecord
        {
            public MigrationNpcSchedule Schedule;
            public string HomeLocation;
        }

        private readonly Dictionary<string, NpcRecord> npcs = new Dictionary<string, NpcRecord>();

        public void RegisterNpc(string npcId, MigrationNpcSchedule schedule, string homeLocation)
        {
            if (string.IsNullOrWhiteSpace(npcId))
            {
                return;
            }

            npcs[npcId] = new NpcRecord
            {
                Schedule = schedule ?? new MigrationNpcSchedule(),
                HomeLocation = homeLocation ?? string.Empty
            };
        }

        // Register spawn-enabled NPCs from a location roster: each gets a simple schedule (at its
        // work_location during [workStartHour, workEndHour), at its home otherwise).
        public void RegisterFrom(MigrationNpcRoster roster, int workStartHour, int workEndHour)
        {
            if (roster == null)
            {
                return;
            }

            foreach (KeyValuePair<string, MigrationNpcRosterEntry> pair in roster.GetAllEntries())
            {
                MigrationNpcRosterEntry entry = pair.Value;
                if (entry == null || !entry.SpawnEnabled)
                {
                    continue;
                }

                MigrationNpcSchedule schedule = new MigrationNpcSchedule();
                if (!string.IsNullOrWhiteSpace(entry.WorkLocation))
                {
                    schedule.AddEntry(new MigrationNpcScheduleEntry(workStartHour, workEndHour, entry.WorkLocation));
                }

                RegisterNpc(entry.NpcId, schedule, entry.Home);
            }
        }

        public bool IsRegistered(string npcId)
        {
            return !string.IsNullOrWhiteSpace(npcId) && npcs.ContainsKey(npcId);
        }

        public string LocationOf(string npcId, int hour)
        {
            if (!string.IsNullOrWhiteSpace(npcId) && npcs.TryGetValue(npcId, out NpcRecord record))
            {
                return record.Schedule.LocationAt(hour, record.HomeLocation);
            }

            return string.Empty;
        }
    }
}
