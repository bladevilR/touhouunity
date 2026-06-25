namespace TouhouMigration.Runtime.Foundation
{
    // Lunar phase from the calendar day (Godot WeatherSystem._update_moon_phase): an 8-phase cycle
    // over 32 days, 4 days per phase. Phase index = (day % 32) / 4; the full moon is phase index 4
    // (Godot MoonPhase: NEW_MOON, WAXING_CRESCENT, FIRST_QUARTER, WAXING_GIBBOUS, FULL_MOON, ...).
    // Pure so the cycle is unit-tested; consumed by the dialogue `is_full_moon` condition.
    public static class MigrationMoonPhase
    {
        public const int PhaseCount = 8;
        public const int DaysPerPhase = 4;
        public const int CycleDays = PhaseCount * DaysPerPhase; // 32
        public const int FullMoonPhaseIndex = 4;

        public static int PhaseIndex(int day)
        {
            int normalized = ((day % CycleDays) + CycleDays) % CycleDays;
            return normalized / DaysPerPhase;
        }

        public static bool IsFullMoon(int day)
        {
            return PhaseIndex(day) == FullMoonPhaseIndex;
        }
    }
}
