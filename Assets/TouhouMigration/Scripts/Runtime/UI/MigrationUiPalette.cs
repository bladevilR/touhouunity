using UnityEngine;

namespace TouhouMigration.Runtime.UI
{
    // The canonical washi-paper UI colour palette (Godot GameUiStyle), carried over with exact RGBA values
    // so the IMGUI shells + any future uGUI/UITK reskin share one source of truth. The Godot StyleBoxFlat /
    // Control styling helpers are UI-toolkit-specific and stay in the view layer.
    public static class MigrationUiPalette
    {
        public static Color Paper => new Color(0.91f, 0.85f, 0.72f, 0.98f);
        public static Color PaperDark => new Color(0.78f, 0.67f, 0.50f, 0.98f);
        public static Color PaperSoft => new Color(0.96f, 0.91f, 0.78f, 0.96f);
        public static Color Ink => new Color(0.25f, 0.18f, 0.14f, 1.0f);
        public static Color Muted => new Color(0.46f, 0.36f, 0.29f, 1.0f);
        public static Color Crimson => new Color(0.55f, 0.16f, 0.13f, 1.0f);
        public static Color CrimsonDark => new Color(0.34f, 0.08f, 0.07f, 1.0f);
        public static Color Gold => new Color(0.90f, 0.58f, 0.18f, 1.0f);
        public static Color Green => new Color(0.22f, 0.50f, 0.28f, 1.0f);
        public static Color Warning => new Color(0.72f, 0.24f, 0.18f, 1.0f);
        public static Color Overlay => new Color(0.04f, 0.035f, 0.03f, 0.62f);
    }
}
