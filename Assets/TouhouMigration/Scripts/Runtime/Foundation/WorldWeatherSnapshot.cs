namespace TouhouMigration.Runtime.Foundation
{
    public readonly struct WorldWeatherSnapshot
    {
        public WorldWeatherSnapshot(
            GameWeather weather,
            MoonPhase moonPhase,
            bool isFullMoonActive,
            bool isForced,
            float visibilityModifier,
            float movementModifier)
        {
            Weather = weather;
            MoonPhase = moonPhase;
            IsFullMoonActive = isFullMoonActive;
            IsForced = isForced;
            VisibilityModifier = visibilityModifier;
            MovementModifier = movementModifier;
        }

        public GameWeather Weather { get; }
        public MoonPhase MoonPhase { get; }
        public bool IsFullMoonActive { get; }
        public bool IsForced { get; }
        public float VisibilityModifier { get; }
        public float MovementModifier { get; }
    }
}
