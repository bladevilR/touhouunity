using System.Collections.Generic;

namespace TouhouMigration.Runtime.Foundation
{
    // The calendar's festival table + weekday/season formatting (Godot CalendarManager festivals /
    // is_festival / get_festival_* / get_weekday / get_short_date). A festival is keyed by "<season>_<day>".
    // UnityEngine-free + unit-testable; pairs with GameClock (which holds the live day/season).
    public sealed class MigrationFestivalCalendar
    {
        private const int DaysPerWeek = 7;

        private static readonly string[] Weekdays = { "月", "火", "水", "木", "金", "土", "日" };

        private static readonly Dictionary<string, string> SeasonNames = new Dictionary<string, string>
        {
            ["spring"] = "春", ["summer"] = "夏", ["autumn"] = "秋", ["winter"] = "冬",
        };

        private static readonly Dictionary<string, (string Id, string Name)> Festivals =
            new Dictionary<string, (string, string)>
            {
                ["spring_1"] = ("new_year", "新年祭"),
                ["spring_14"] = ("flower_festival", "花见祭"),
                ["summer_7"] = ("tanabata", "七夕祭"),
                ["summer_15"] = ("obon", "盂兰盆节"),
                ["autumn_15"] = ("moon_festival", "中秋祭"),
                ["winter_24"] = ("christmas", "圣诞祭"),
            };

        private static string Key(string season, int day) => (season ?? string.Empty) + "_" + day;

        public bool IsFestival(string season, int day)
        {
            return Festivals.ContainsKey(Key(season, day));
        }

        public string GetFestivalId(string season, int day)
        {
            return Festivals.TryGetValue(Key(season, day), out (string Id, string Name) festival) ? festival.Id : string.Empty;
        }

        public string GetFestivalName(string season, int day)
        {
            return Festivals.TryGetValue(Key(season, day), out (string Id, string Name) festival) ? festival.Name : string.Empty;
        }

        // Godot get_weekday: WEEKDAYS[(day - 1) % 7], cycling 月..日.
        public string Weekday(int day)
        {
            int index = ((day - 1) % DaysPerWeek + DaysPerWeek) % DaysPerWeek;
            return Weekdays[index];
        }

        public string SeasonName(string season)
        {
            return season != null && SeasonNames.TryGetValue(season, out string name) ? name : string.Empty;
        }

        // Godot get_short_date: "<season name><day>".
        public string ShortDate(string season, int day)
        {
            return SeasonName(season) + day;
        }
    }
}
