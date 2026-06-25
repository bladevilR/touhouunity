namespace TouhouMigration.Runtime.Foundation
{
    // Inclusive-start / exclusive-end hour-in-range test with wrap-around past midnight (end < start),
    // e.g. 22..2 covers 22:00-01:59; 0..24 is always in range. Shared by shop open-hours
    // (MigrationShopHours) and NPC schedules (Godot is_shop_open / _is_hour_in_range). Pure.
    public static class MigrationHourRange
    {
        public static bool Contains(int startHour, int endHour, int hour)
        {
            if (endHour < startHour)
            {
                return hour >= startHour || hour < endHour;
            }

            return hour >= startHour && hour < endHour;
        }
    }
}
