using UnityEngine;

namespace TouhouMigration.Runtime.Foundation
{
    public sealed class WorldSimulationBehaviour : MonoBehaviour
    {
        [SerializeField] private bool advanceClock = true;
        [SerializeField] private DayNightLightingController lightingController;

        private int lastSyncedDay = -1;
        private int lastSyncedHour = -1;
        private GameSeason lastSyncedSeason;

        public GameClock Clock { get; private set; }
        public WeatherService Weather { get; private set; }
        public DayNightPalette DayNightPalette { get; private set; }

        public void Initialize()
        {
            if (Clock == null)
            {
                Clock = new GameClock();
            }

            if (Weather == null)
            {
                Weather = new WeatherService();
            }

            if (DayNightPalette == null)
            {
                DayNightPalette = new DayNightPalette();
            }

            SyncWeather(force: true);
            ApplyLighting();
        }

        public void SetLightingController(DayNightLightingController controller)
        {
            lightingController = controller;
        }

        public WorldTimeSnapshot GetTimeSnapshot()
        {
            Initialize();
            return Clock.GetSnapshot();
        }

        public WorldWeatherSnapshot GetWeatherSnapshot()
        {
            Initialize();
            return Weather.GetSnapshot();
        }

        public void ForceWeather(GameWeather weather, float durationHours)
        {
            Initialize();
            Weather.ForceWeather(weather.ToString(), durationHours);
            ApplyLighting();
        }

        private void Awake()
        {
            Initialize();
        }

        private void Update()
        {
            Initialize();

            if (!advanceClock)
            {
                return;
            }

            float gameHoursAdvanced = Time.deltaTime * Clock.TimeScale / Clock.RealSecondsPerGameMinute / GameClock.MinutesPerHour;
            Clock.AdvanceSeconds(Time.deltaTime);
            Weather.AdvanceHours(gameHoursAdvanced);
            SyncWeather(force: false);
            ApplyLighting();
        }

        private void SyncWeather(bool force)
        {
            WorldTimeSnapshot time = Clock.GetSnapshot();
            if (force || time.Day != lastSyncedDay || time.Season != lastSyncedSeason)
            {
                Weather.UpdateForDate(time.Day, time.Season.ToString());
                lastSyncedDay = time.Day;
                lastSyncedSeason = time.Season;
            }

            if (force || time.Hour != lastSyncedHour)
            {
                Weather.UpdateForHour(time.Hour);
                lastSyncedHour = time.Hour;
            }
        }

        private void ApplyLighting()
        {
            if (lightingController == null)
            {
                return;
            }

            lightingController.Apply(Clock.GetSnapshot(), Weather.GetSnapshot(), DayNightPalette);
        }
    }
}
