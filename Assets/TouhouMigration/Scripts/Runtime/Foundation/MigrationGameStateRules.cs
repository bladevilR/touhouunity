namespace TouhouMigration.Runtime.Foundation
{
    // Pure mode → behaviour rules for the game loop. The owner uses these to gate gameplay input,
    // HUD visibility, and world-time progression based on the active MigrationGameStateMode.
    public static class MigrationGameStateRules
    {
        // The player has direct control and gameplay input is live.
        public static bool AllowsGameplayInput(MigrationGameStateMode mode)
        {
            return mode == MigrationGameStateMode.Home
                || mode == MigrationGameStateMode.Overworld
                || mode == MigrationGameStateMode.Combat;
        }

        // The world HUD (time/date, coins, hotbar) is shown.
        public static bool ShowsHud(MigrationGameStateMode mode)
        {
            return mode == MigrationGameStateMode.Home
                || mode == MigrationGameStateMode.Overworld
                || mode == MigrationGameStateMode.Combat;
        }

        // World time/simulation is frozen. Dialogue/Cutscene/Menu freeze it; Sleeping fast-forwards
        // (handled elsewhere) and Home/Overworld/Combat run normally, so none of those freeze time.
        public static bool FreezesWorldTime(MigrationGameStateMode mode)
        {
            return mode == MigrationGameStateMode.Menu
                || mode == MigrationGameStateMode.Dialogue
                || mode == MigrationGameStateMode.Cutscene;
        }
    }
}
