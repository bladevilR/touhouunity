namespace TouhouMigration.Runtime.Economy
{
    // Shop open-hours check (Godot ShopData.is_shop_open). The end hour is exclusive; when end < start
    // the window wraps past midnight (e.g. a 22..2 shop is open 22:00-01:59). A 0..24 window is always
    // open. Hours are 0..23 clock hours; this is pure so it stays unit-testable.
    public static class MigrationShopHours
    {
        public static bool IsOpen(int startHour, int endHour, int currentHour)
        {
            if (endHour < startHour)
            {
                return currentHour >= startHour || currentHour < endHour;
            }

            return currentHour >= startHour && currentHour < endHour;
        }
    }
}
