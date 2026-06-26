using System;
using System.Collections.Generic;
using TouhouMigration.Runtime.Foundation;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    // Covers MigrationGameLogger: level-threshold filtering + tagged formatting (Godot GameLogger
    // debug/info/warning/error + current_level gate).
    public static class GameLoggerSmokeTests
    {
        [MenuItem("Touhou Migration/Tests/Run Game Logger Smoke Tests")]
        public static void RunAll()
        {
            TestLevelThresholdFilters();
            TestFormatAndRouting();
            Debug.Log("Game logger smoke tests passed.");
        }

        private static void TestLevelThresholdFilters()
        {
            MigrationGameLogger logger = new MigrationGameLogger { CurrentLevel = MigrationLogLevel.Info };

            logger.Debug("ai", "spawned"); // below Info -> dropped
            AssertEqual(0, logger.EmittedCount, "A message below the current level is dropped.");

            logger.Info("ai", "ready");
            AssertEqual(1, logger.EmittedCount, "An at-level message is emitted.");
            logger.Warning("ai", "low hp");
            AssertEqual(2, logger.EmittedCount, "An above-level message is emitted.");

            logger.CurrentLevel = MigrationLogLevel.Error;
            logger.Info("ai", "tick"); // now below threshold
            AssertEqual(2, logger.EmittedCount, "Raising the threshold drops lower-level messages.");
            logger.Error("ai", "crash");
            AssertEqual(3, logger.EmittedCount, "Errors still emit at the Error threshold.");
        }

        private static void TestFormatAndRouting()
        {
            List<(MigrationLogLevel, string)> captured = new List<(MigrationLogLevel, string)>();
            MigrationGameLogger logger = new MigrationGameLogger((level, line) => captured.Add((level, line)))
            {
                CurrentLevel = MigrationLogLevel.Debug,
            };

            logger.Warning("combat", "took damage");
            AssertEqual(1, captured.Count, "The sink receives the emitted line.");
            AssertEqual(MigrationLogLevel.Warning, captured[0].Item1, "The sink gets the level.");
            AssertEqual(true, captured[0].Item2.Contains("combat") && captured[0].Item2.Contains("took damage"),
                "The formatted line carries the tag + message.");
            AssertEqual(true, captured[0].Item2.Contains("WARNING"), "The formatted line carries the level name.");
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
