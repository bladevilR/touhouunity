namespace TouhouMigration.Runtime.Social
{
    // One entry in an NPC's daily schedule (Godot NPCScheduleData.schedule): the [start, end) hour
    // window and the location the NPC occupies during it.
    public sealed class MigrationNpcScheduleEntry
    {
        public int StartHour { get; }
        public int EndHour { get; }
        public string Location { get; }

        public MigrationNpcScheduleEntry(int startHour, int endHour, string location)
        {
            StartHour = startHour;
            EndHour = endHour;
            Location = location ?? string.Empty;
        }
    }
}
