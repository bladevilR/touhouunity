using System;
using TouhouMigration.Runtime.CardBuild;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    // Covers MigrationCardBalanceMetrics: the card-run telemetry recorder (Godot CardBalanceMetrics) —
    // action/card counts, resource overflow, window conversion, damage breakdown, dead draws, and the
    // per-minute / ratio derivations.
    public static class CardBalanceMetricsSmokeTests
    {
        private const double Tol = 1e-9;

        [MenuItem("Touhou Migration/Tests/Run Card Balance Metrics Smoke Tests")]
        public static void RunAll()
        {
            TestCountsAndStartRunReset();
            TestResourceOverflow();
            TestWindowConversionRate();
            TestDamageBreakdownAndDeadDraws();
            TestPerMinuteRates();
            Debug.Log("Card balance metrics smoke tests passed.");
        }

        private static void TestCountsAndStartRunReset()
        {
            MigrationCardBalanceMetrics m = new MigrationCardBalanceMetrics();
            m.RecordAction("dash", 1.0);
            m.RecordAction("dash", 2.0);
            m.RecordCardPlay("fire_bird", "manual", 1.5);
            AssertEqual(2, m.ActionCount, "Two actions are counted.");
            AssertEqual(1, m.CardPlayCount, "One card play is counted.");

            m.StartRun();
            AssertEqual(0, m.ActionCount, "StartRun resets the action count.");
            AssertEqual(0, m.CardPlayCount, "StartRun resets the card-play count.");
        }

        private static void TestResourceOverflow()
        {
            MigrationCardBalanceMetrics m = new MigrationCardBalanceMetrics();
            m.RecordResource("ember", before: 3, after: 8, cap: 5);   // overflow
            m.RecordResource("ember", before: 2, after: 4, cap: 5);   // ok
            m.RecordResource("fate", before: 1, after: 9);            // no cap -> no overflow
            AssertEqual(1, m.ResourceOverflowCount, "Only the over-cap resource event counts as overflow.");
        }

        private static void TestWindowConversionRate()
        {
            MigrationCardBalanceMetrics m = new MigrationCardBalanceMetrics();
            m.RecordWindow(0.0, converted: true);
            m.RecordWindow(0.0, converted: true);
            m.RecordWindow(0.0, converted: false);
            AssertEqual(3, m.WindowCount, "Three windows recorded.");
            AssertEqual(2, m.ConvertedWindowCount, "Two windows converted.");
            AssertTrue(Math.Abs(2.0 / 3.0 - m.WindowConversionRate) < Tol, "Conversion rate is converted/total.");

            MigrationCardBalanceMetrics empty = new MigrationCardBalanceMetrics();
            AssertEqual(0.0, empty.WindowConversionRate, "No windows -> 0 conversion rate (no divide-by-zero).");
        }

        private static void TestDamageBreakdownAndDeadDraws()
        {
            MigrationCardBalanceMetrics m = new MigrationCardBalanceMetrics();
            m.RecordDamage("fire", 50);
            m.RecordDamage("fire", 20);
            m.RecordDamage("ice", 10);
            AssertEqual(70.0, m.DamageBySource("fire"), "Damage accumulates per source.");
            AssertEqual(80.0, m.TotalDamage, "Total damage sums all sources.");
            AssertEqual(0.0, m.DamageBySource("wind"), "An unrecorded source has 0 damage.");

            m.RecordDeadDraw("idle_card");
            m.RecordDeadDraw("idle_card");
            AssertEqual(2, m.DeadDrawCount("idle_card"), "Dead draws accumulate per card.");

            m.RecordClauseBreak("terrain_tyranny", 42.5);
            AssertTrue(Math.Abs(42.5 - m.ClauseBreakTime("terrain_tyranny")) < Tol, "Clause break time is recorded.");
        }

        private static void TestPerMinuteRates()
        {
            MigrationCardBalanceMetrics m = new MigrationCardBalanceMetrics();
            for (int i = 0; i < 6; i++)
            {
                m.RecordAction("a", i);
            }

            AssertTrue(Math.Abs(3.0 - m.ActionsPerMinute(120.0)) < Tol, "6 actions over 2 minutes is 3/min.");
            AssertEqual(0.0, m.ActionsPerMinute(0.0), "Zero duration -> 0 rate (no divide-by-zero).");
        }

        private static void AssertTrue(bool condition, string message)
        {
            if (!condition)
            {
                throw new Exception(message);
            }
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
