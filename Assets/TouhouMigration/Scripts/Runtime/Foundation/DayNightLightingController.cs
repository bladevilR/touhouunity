using UnityEngine;

namespace TouhouMigration.Runtime.Foundation
{
    public sealed class DayNightLightingController : MonoBehaviour
    {
        [SerializeField] private Light sunLight;
        [SerializeField] private Light moonLight;
        [SerializeField] private bool manageRenderSettings = true;
        [SerializeField] private bool manageFog = true;
        [SerializeField] private float sunYawDegrees = -30f;
        [SerializeField] private float moonYawDegrees = 150f;
        [SerializeField] private float baseFogDensity = 0.006f;

        public GameTimePeriod LastAppliedPeriod { get; private set; }
        public DayNightLightingProfile LastAppliedProfile { get; private set; }

        public void Bind(Light sun, Light moon)
        {
            sunLight = sun;
            moonLight = moon;
        }

        public void Apply(WorldTimeSnapshot time, WorldWeatherSnapshot weather, DayNightPalette palette)
        {
            DayNightPalette resolvedPalette = palette ?? new DayNightPalette();
            DayNightLightingProfile profile = resolvedPalette.GetProfileForPeriod(time.Period);
            float visibility = Mathf.Clamp01(weather.VisibilityModifier <= 0f ? 1f : weather.VisibilityModifier);

            LastAppliedPeriod = time.Period;
            LastAppliedProfile = profile;

            ApplySun(time, profile, visibility);
            ApplyMoon(time, weather, visibility);
            ApplyRenderSettings(profile, weather, visibility);
        }

        private void ApplySun(WorldTimeSnapshot time, DayNightLightingProfile profile, float visibility)
        {
            if (sunLight == null)
            {
                return;
            }

            sunLight.type = LightType.Directional;
            sunLight.color = profile.Tint;
            sunLight.intensity = profile.Brightness * visibility;
            sunLight.enabled = profile.Brightness > 0.02f;
            sunLight.transform.rotation = Quaternion.Euler(CalculateCelestialPitch(time), sunYawDegrees, 0f);
        }

        private void ApplyMoon(WorldTimeSnapshot time, WorldWeatherSnapshot weather, float visibility)
        {
            if (moonLight == null)
            {
                return;
            }

            bool nightPeriod = time.Period == GameTimePeriod.Night ||
                time.Period == GameTimePeriod.Midnight ||
                time.Period == GameTimePeriod.Dawn;
            float moonBoost = weather.IsFullMoonActive ? 1.35f : 1f;

            moonLight.type = LightType.Directional;
            moonLight.color = new Color(0.7f, 0.8f, 1f, 1f);
            moonLight.intensity = nightPeriod ? 0.35f * moonBoost * visibility : 0f;
            moonLight.enabled = nightPeriod;
            moonLight.transform.rotation = Quaternion.Euler(CalculateCelestialPitch(time) + 180f, moonYawDegrees, 0f);
        }

        private void ApplyRenderSettings(
            DayNightLightingProfile profile,
            WorldWeatherSnapshot weather,
            float visibility)
        {
            if (!manageRenderSettings)
            {
                return;
            }

            float ambientStrength = Mathf.Clamp01(profile.Brightness * 0.55f + 0.08f);
            RenderSettings.ambientLight = Color.Lerp(Color.black, profile.Tint, ambientStrength);

            if (!manageFog)
            {
                return;
            }

            RenderSettings.fog = true;
            RenderSettings.fogColor = Color.Lerp(profile.Tint, new Color(0.55f, 0.6f, 0.66f, 1f), 1f - visibility);
            RenderSettings.fogDensity = baseFogDensity * Mathf.Lerp(1f, 3.5f, 1f - visibility);
        }

        private static float CalculateCelestialPitch(WorldTimeSnapshot time)
        {
            float hour = time.Hour + time.Minute / 60f;
            return hour / 24f * 360f - 90f;
        }
    }
}
