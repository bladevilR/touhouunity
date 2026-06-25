using System;
using TouhouMigration.Runtime.Economy;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    public static class ShopHoursSmokeTests
    {
        [MenuItem("Touhou Migration/Tests/Run Shop Hours Smoke Tests")]
        public static void RunAll()
        {
            TestNormalHoursAreInclusiveStartExclusiveEnd();
            TestWrapAroundMidnightHours();
            TestAllDayRangeIsAlwaysOpen();
            Debug.Log("Shop hours smoke tests passed.");
        }

        private static void TestNormalHoursAreInclusiveStartExclusiveEnd()
        {
            AssertEqual(true, MigrationShopHours.IsOpen(9, 18, 9), "Shop opens at the start hour.");
            AssertEqual(true, MigrationShopHours.IsOpen(9, 18, 17), "Shop is open in the middle of its hours.");
            AssertEqual(false, MigrationShopHours.IsOpen(9, 18, 18), "End hour is exclusive: the shop is closed at the end hour.");
            AssertEqual(false, MigrationShopHours.IsOpen(9, 18, 8), "Shop is closed before opening.");
            AssertEqual(false, MigrationShopHours.IsOpen(9, 18, 20), "Shop is closed after closing.");
        }

        private static void TestWrapAroundMidnightHours()
        {
            AssertEqual(true, MigrationShopHours.IsOpen(22, 2, 22), "A wrap-around shop is open at its evening start.");
            AssertEqual(true, MigrationShopHours.IsOpen(22, 2, 23), "A wrap-around shop is open before midnight.");
            AssertEqual(true, MigrationShopHours.IsOpen(22, 2, 0), "A wrap-around shop is open just after midnight.");
            AssertEqual(true, MigrationShopHours.IsOpen(22, 2, 1), "A wrap-around shop is open before its end hour.");
            AssertEqual(false, MigrationShopHours.IsOpen(22, 2, 2), "End hour is exclusive across midnight too.");
            AssertEqual(false, MigrationShopHours.IsOpen(22, 2, 12), "A wrap-around shop is closed during the day.");
        }

        private static void TestAllDayRangeIsAlwaysOpen()
        {
            AssertEqual(true, MigrationShopHours.IsOpen(0, 24, 0), "A 0..24 shop is open at midnight.");
            AssertEqual(true, MigrationShopHours.IsOpen(0, 24, 12), "A 0..24 shop is open at noon.");
            AssertEqual(true, MigrationShopHours.IsOpen(0, 24, 23), "A 0..24 shop is open at the last hour.");
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
