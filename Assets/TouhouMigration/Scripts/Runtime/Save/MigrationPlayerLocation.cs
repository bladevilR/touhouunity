using System;

namespace TouhouMigration.Runtime.Save
{
    // The caller-owned player position/scene save scalars (the last gap in save parity). The owner
    // captures the active scene name + the player transform's position into MigrationSaveData on save,
    // and restores the position on load when the saved scene matches the active one. This helper holds
    // the pure mapping so it stays unit-testable apart from the MonoBehaviour/transform/scene plumbing;
    // a cross-scene restore (load the saved scene, then place the player) is a scene-flow follow-up.
    public static class MigrationPlayerLocation
    {
        // Write the active scene + player position into the save data. A blank scene name keeps the
        // existing value (so a missing active-scene name never clobbers a good one).
        public static void Write(MigrationSaveData data, string sceneName, float x, float y, float z)
        {
            if (data == null)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(sceneName))
            {
                data.CurrentScene = sceneName;
            }

            data.Position = new MigrationSavePosition { x = x, y = y, z = z };
        }

        // True when the save's recorded scene matches the given (active) scene, so the player position can
        // be restored in-place without a scene load.
        public static bool IsSameScene(MigrationSaveData data, string sceneName)
        {
            return data != null
                && !string.IsNullOrWhiteSpace(data.CurrentScene)
                && !string.IsNullOrWhiteSpace(sceneName)
                && string.Equals(data.CurrentScene, sceneName, StringComparison.OrdinalIgnoreCase);
        }
    }
}
