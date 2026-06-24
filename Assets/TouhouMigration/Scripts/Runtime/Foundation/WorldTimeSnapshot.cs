namespace TouhouMigration.Runtime.Foundation
{
    public readonly struct WorldTimeSnapshot
    {
        public WorldTimeSnapshot(int hour, int minute, int day, int year, GameSeason season, GameTimePeriod period)
        {
            Hour = hour;
            Minute = minute;
            Day = day;
            Year = year;
            Season = season;
            Period = period;
        }

        public int Hour { get; }
        public int Minute { get; }
        public int Day { get; }
        public int Year { get; }
        public GameSeason Season { get; }
        public GameTimePeriod Period { get; }
    }
}
