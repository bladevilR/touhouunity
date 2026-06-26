using System;
using TouhouMigration.Runtime.Foundation;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    // Covers MigrationFestivalCalendar: the calendar festival table + weekday/season formatting (Godot
    // CalendarManager festivals / is_festival / get_festival_* / get_weekday / get_short_date).
    public static class FestivalCalendarSmokeTests
    {
        [MenuItem("Touhou Migration/Tests/Run Festival Calendar Smoke Tests")]
        public static void RunAll()
        {
            TestFestivalLookup();
            TestNonFestivalDays();
            TestWeekdayCycle();
            TestSeasonNameAndShortDate();
            Debug.Log("Festival calendar smoke tests passed.");
        }

        private static void TestFestivalLookup()
        {
            MigrationFestivalCalendar cal = new MigrationFestivalCalendar();
            AssertEqual(true, cal.IsFestival("spring", 1), "Spring day 1 is a festival.");
            AssertEqual("new_year", cal.GetFestivalId("spring", 1), "Spring day 1 is the new year festival.");
            AssertEqual("新年祭", cal.GetFestivalName("spring", 1), "Its name is 新年祭.");

            AssertEqual("moon_festival", cal.GetFestivalId("autumn", 15), "Autumn day 15 is the moon festival.");
            AssertEqual("christmas", cal.GetFestivalId("winter", 24), "Winter day 24 is christmas.");
            AssertEqual("tanabata", cal.GetFestivalId("summer", 7), "Summer day 7 is tanabata.");
        }

        private static void TestNonFestivalDays()
        {
            MigrationFestivalCalendar cal = new MigrationFestivalCalendar();
            AssertEqual(false, cal.IsFestival("spring", 2), "An ordinary day is not a festival.");
            AssertEqual("", cal.GetFestivalId("spring", 2), "A non-festival day has no festival id.");
            AssertEqual("", cal.GetFestivalName("autumn", 3), "A non-festival day has no festival name.");
        }

        private static void TestWeekdayCycle()
        {
            MigrationFestivalCalendar cal = new MigrationFestivalCalendar();
            AssertEqual("月", cal.Weekday(1), "Day 1 is 月.");
            AssertEqual("日", cal.Weekday(7), "Day 7 is 日.");
            AssertEqual("月", cal.Weekday(8), "Day 8 wraps back to 月.");
        }

        private static void TestSeasonNameAndShortDate()
        {
            MigrationFestivalCalendar cal = new MigrationFestivalCalendar();
            AssertEqual("夏", cal.SeasonName("summer"), "summer -> 夏.");
            AssertEqual("秋15", cal.ShortDate("autumn", 15), "ShortDate concatenates the season name and day.");
        }

        private static void AssertEqual<T>(T expected, T actual, string message)
        {
            if (!Equals(expected, actual))
            {
                throw new Exception($"{message} Expected: {expected}. Actual: {actual}.");
            }
        }
    }
}
