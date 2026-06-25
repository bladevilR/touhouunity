using System.Collections.Generic;
using TouhouMigration.Runtime.Foundation;

namespace TouhouMigration.Runtime.Social
{
    // An NPC's daily schedule (Godot NPCScheduleManager): the first entry whose [start, end) hour
    // window contains the current hour sets the NPC's location; otherwise the NPC is at its home.
    public sealed class MigrationNpcSchedule
    {
        private readonly List<MigrationNpcScheduleEntry> entries = new List<MigrationNpcScheduleEntry>();

        public void AddEntry(MigrationNpcScheduleEntry entry)
        {
            if (entry != null)
            {
                entries.Add(entry);
            }
        }

        public string LocationAt(int hour, string homeLocation)
        {
            foreach (MigrationNpcScheduleEntry entry in entries)
            {
                if (MigrationHourRange.Contains(entry.StartHour, entry.EndHour, hour))
                {
                    return entry.Location;
                }
            }

            return homeLocation ?? string.Empty;
        }
    }
}
