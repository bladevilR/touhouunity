using UnityEngine;

namespace TouhouMigration.Runtime.Foundation
{
    public readonly struct DayNightLightingProfile
    {
        public DayNightLightingProfile(Color tint, float brightness)
        {
            Tint = tint;
            Brightness = brightness;
        }

        public Color Tint { get; }
        public float Brightness { get; }
    }
}
