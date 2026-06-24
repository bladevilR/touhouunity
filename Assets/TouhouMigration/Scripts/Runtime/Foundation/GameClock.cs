using System;

namespace TouhouMigration.Runtime.Foundation
{
    public sealed class GameClock
    {
        public const int MinutesPerHour = 60;
        public const int HoursPerDay = 24;
        public const int DaysPerSeason = 28;
        public const float DefaultRealSecondsPerGameMinute = 1f;

        private float accumulatedSeconds;

        public GameClock()
        {
            Hour = 7;
            Minute = 0;
            Day = 1;
            Year = 1;
            Season = GameSeason.Spring;
            TimeScale = 1f;
            RealSecondsPerGameMinute = DefaultRealSecondsPerGameMinute;
        }

        public event Action<int> MinuteChanged;
        public event Action<int> HourChanged;
        public event Action<int, GameSeason, int> DayStarted;
        public event Action<GameSeason, GameSeason> SeasonChanged;
        public event Action<GameTimePeriod, GameTimePeriod> PeriodChanged;

        public int Hour { get; private set; }
        public int Minute { get; private set; }
        public int Day { get; private set; }
        public int Year { get; private set; }
        public GameSeason Season { get; private set; }
        public bool IsPaused { get; private set; }
        public float TimeScale { get; private set; }
        public float RealSecondsPerGameMinute { get; private set; }

        public void AdvanceSeconds(float realSeconds)
        {
            if (IsPaused || realSeconds <= 0f)
            {
                return;
            }

            accumulatedSeconds += realSeconds * TimeScale;
            while (accumulatedSeconds >= RealSecondsPerGameMinute)
            {
                accumulatedSeconds -= RealSecondsPerGameMinute;
                AdvanceOneMinute();
            }
        }

        public void AdvanceMinutes(int minutes)
        {
            if (minutes <= 0)
            {
                return;
            }

            for (int i = 0; i < minutes; i++)
            {
                AdvanceOneMinute();
            }
        }

        public void SetTime(int hour, int minute)
        {
            GameTimePeriod oldPeriod = GetTimePeriod();
            Hour = Math.Clamp(hour, 0, 23);
            Minute = Math.Clamp(minute, 0, 59);
            accumulatedSeconds = 0f;
            EmitPeriodIfChanged(oldPeriod);
        }

        public void SetDate(int day, string season, int year)
        {
            Day = Math.Clamp(day, 1, DaysPerSeason);
            Season = ParseSeason(season);
            Year = Math.Max(1, year);
        }

        public void Pause()
        {
            IsPaused = true;
        }

        public void Resume()
        {
            IsPaused = false;
        }

        public void SetTimeScale(float timeScale)
        {
            TimeScale = Math.Max(0f, timeScale);
        }

        public WorldTimeSnapshot GetSnapshot()
        {
            return new WorldTimeSnapshot(Hour, Minute, Day, Year, Season, GetTimePeriod());
        }

        public GameTimePeriod GetTimePeriod()
        {
            if (Hour >= 5 && Hour < 7)
            {
                return GameTimePeriod.Dawn;
            }

            if (Hour >= 7 && Hour < 12)
            {
                return GameTimePeriod.Morning;
            }

            if (Hour >= 12 && Hour < 14)
            {
                return GameTimePeriod.Noon;
            }

            if (Hour >= 14 && Hour < 17)
            {
                return GameTimePeriod.Afternoon;
            }

            if (Hour >= 17 && Hour < 20)
            {
                return GameTimePeriod.Evening;
            }

            if (Hour >= 20)
            {
                return GameTimePeriod.Night;
            }

            return GameTimePeriod.Midnight;
        }

        private void AdvanceOneMinute()
        {
            GameTimePeriod oldPeriod = GetTimePeriod();
            Minute++;
            if (Minute >= MinutesPerHour)
            {
                Minute = 0;
                AdvanceOneHour();
            }

            MinuteChanged?.Invoke(Hour * MinutesPerHour + Minute);
            EmitPeriodIfChanged(oldPeriod);
        }

        private void AdvanceOneHour()
        {
            Hour++;
            if (Hour >= HoursPerDay)
            {
                Hour = 0;
                AdvanceOneDay();
            }

            HourChanged?.Invoke(Hour);
        }

        private void AdvanceOneDay()
        {
            Day++;
            if (Day > DaysPerSeason)
            {
                Day = 1;
                AdvanceSeason();
            }

            DayStarted?.Invoke(Day, Season, Year);
        }

        private void AdvanceSeason()
        {
            GameSeason oldSeason = Season;
            Season = Season switch
            {
                GameSeason.Spring => GameSeason.Summer,
                GameSeason.Summer => GameSeason.Autumn,
                GameSeason.Autumn => GameSeason.Winter,
                _ => GameSeason.Spring
            };

            if (oldSeason == GameSeason.Winter && Season == GameSeason.Spring)
            {
                Year++;
            }

            SeasonChanged?.Invoke(oldSeason, Season);
        }

        private void EmitPeriodIfChanged(GameTimePeriod oldPeriod)
        {
            GameTimePeriod newPeriod = GetTimePeriod();
            if (oldPeriod != newPeriod)
            {
                PeriodChanged?.Invoke(oldPeriod, newPeriod);
            }
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
