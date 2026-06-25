namespace TouhouMigration.Runtime.Foundation
{
    // Maps a clock hour to a Godot TimeManager time-of-day period name, consumed by the dialogue
    // `time_of_day` condition (and available to HUD/lighting). Bands match Godot get_time_period().
    // Pure so the bands are unit-tested.
    public static class MigrationTimeOfDay
    {
        public const string Dawn = "dawn";
        public const string Morning = "morning";
        public const string Noon = "noon";
        public const string Afternoon = "afternoon";
        public const string Evening = "evening";
        public const string Night = "night";
        public const string Midnight = "midnight";

        public static string FromHour(int hour)
        {
            int normalized = ((hour % 24) + 24) % 24;
            if (normalized >= 5 && normalized < 7)
            {
                return Dawn;
            }

            if (normalized >= 7 && normalized < 12)
            {
                return Morning;
            }

            if (normalized >= 12 && normalized < 14)
            {
                return Noon;
            }

            if (normalized >= 14 && normalized < 17)
            {
                return Afternoon;
            }

            if (normalized >= 17 && normalized < 20)
            {
                return Evening;
            }

            if (normalized >= 20 && normalized < 24)
            {
                return Night;
            }

            return Midnight;
        }
    }
}
