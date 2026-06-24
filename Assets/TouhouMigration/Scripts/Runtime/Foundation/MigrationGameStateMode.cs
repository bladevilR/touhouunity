namespace TouhouMigration.Runtime.Foundation
{
    // Top-level game modes, mirroring the Godot GameStateManager intent. Drives which input,
    // HUD, pause, and update rules are active. Unity-native owner/drivers come with E2 scene flow.
    public enum MigrationGameStateMode
    {
        Menu,
        Home,
        Overworld,
        Combat,
        Dialogue,
        Cutscene,
        Sleeping
    }
}
