using System;

namespace TouhouMigration.Runtime.Foundation
{
    public sealed class WeatherService
    {
        private float forcedWeatherHoursRemaining;
        private int currentHour = 7;
        private GameSeason currentSeason = GameSeason.Spring;

        public WeatherService()
        {
            Weather = GameWeather.Clear;
            MoonPhase = MoonPhase.NewMoon;
        }

        public event Action<GameWeather, GameWeather> WeatherChanged;
        public event Action<MoonPhase> MoonPhaseChanged;
        public event Action<bool> FullMoonActiveChanged;

        public GameWeather Weather { get; private set; }
        public MoonPhase MoonPhase { get; private set; }
        public bool IsForced { get; private set; }
        public bool IsFullMoonActive { get; private set; }

        public void UpdateForDate(int day, string season)
        {
            currentSeason = ParseSeason(season);
            MoonPhase oldPhase = MoonPhase;
            MoonPhase = CalculateMoonPhase(day);

            if (oldPhase != MoonPhase)
            {
                MoonPhaseChanged?.Invoke(MoonPhase);
            }

            UpdateFullMoonState();
        }

        public void UpdateForHour(int hour)
        {
            currentHour = ((hour % 24) + 24) % 24;
            UpdateFullMoonState();
        }

        public void AdvanceHours(float hours)
        {
            if (hours <= 0f || !IsForced)
            {
                return;
            }

            forcedWeatherHoursRemaining -= hours;
            if (forcedWeatherHoursRemaining <= 0f)
            {
                IsForced = false;
                forcedWeatherHoursRemaining = 0f;
                SetWeather(ChooseDefaultWeather(currentSeason));
            }
        }

        public void ForceWeather(string weather, float durationHours)
        {
            if (!Enum.TryParse(weather, true, out GameWeather parsed))
            {
                parsed = GameWeather.Clear;
            }

            IsForced = true;
            forcedWeatherHoursRemaining = Math.Max(0f, durationHours);
            SetWeather(parsed);
        }

        public WorldWeatherSnapshot GetSnapshot()
        {
            return new WorldWeatherSnapshot(
                Weather,
                MoonPhase,
                IsFullMoonActive,
                IsForced,
                GetVisibilityModifier(),
                GetMovementModifier());
        }

        public float GetVisibilityModifier()
        {
            return Weather switch
            {
                GameWeather.Clear => 1f,
                GameWeather.Cloudy => 0.9f,
                GameWeather.Rain => 0.7f,
                GameWeather.Storm => 0.5f,
                GameWeather.Snow => 0.6f,
                GameWeather.Fog => 0.3f,
                GameWeather.Mist => 0.7f,
                _ => 1f
            };
        }

        public float GetMovementModifier()
        {
            return Weather switch
            {
                GameWeather.Rain => 0.9f,
                GameWeather.Storm => 0.7f,
                GameWeather.Snow => 0.8f,
                GameWeather.Fog => 0.95f,
                _ => 1f
            };
        }

        private void SetWeather(GameWeather weather)
        {
            GameWeather oldWeather = Weather;
            Weather = weather;
            if (oldWeather != weather)
            {
                WeatherChanged?.Invoke(oldWeather, weather);
            }
        }

        private void UpdateFullMoonState()
        {
            bool oldValue = IsFullMoonActive;
            IsFullMoonActive = MoonPhase == MoonPhase.FullMoon && (currentHour >= 19 || currentHour < 5);
            if (oldValue != IsFullMoonActive)
            {
                FullMoonActiveChanged?.Invoke(IsFullMoonActive);
            }
        }

        private static MoonPhase CalculateMoonPhase(int day)
        {
            int normalizedDay = Math.Max(1, day);
            int phaseIndex = (normalizedDay % 32) / 4;
            return (MoonPhase)Math.Clamp(phaseIndex, 0, 7);
        }

        private static GameWeather ChooseDefaultWeather(GameSeason season)
        {
            return season == GameSeason.Winter ? GameWeather.Snow : GameWeather.Clear;
        }

        private static GameSeason ParseSeason(string season)
        {
            if (Enum.TryParse(season, true, out GameSeason parsed))
            {
                return parsed;
            }

            return GameSeason.Spring;
        }
    }
}
