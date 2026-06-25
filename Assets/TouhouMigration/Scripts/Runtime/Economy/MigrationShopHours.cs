using TouhouMigration.Runtime.Foundation;

namespace TouhouMigration.Runtime.Economy
{
    // Shop open-hours check (Godot ShopData.is_shop_open). The end hour is exclusive; when end < start
    // the window wraps past midnight (e.g. a 22..2 shop is open 22:00-01:59). A 0..24 window is always
    // open. Delegates to the shared MigrationHourRange so shop hours and NPC schedules share one rule.
    public static class MigrationShopHours
    {
        public static bool IsOpen(int startHour, int endHour, int currentHour)
        {
            return MigrationHourRange.Contains(startHour, endHour, currentHour);
        }
    }
}
