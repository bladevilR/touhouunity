using System;
using TouhouMigration.Runtime.Foundation;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    public static class GameStateRulesSmokeTests
    {
        [MenuItem("Touhou Migration/Tests/Run Game State Rules Smoke Tests")]
        public static void RunAll()
        {
            TestGameplayInputModes();
            TestHudModes();
            TestWorldTimeFreezeModes();
            TestWorldTimeScaleModes();
            Debug.Log("Game state rules smoke tests passed.");
        }

        private static void TestGameplayInputModes()
        {
            AssertEqual(true, MigrationGameStateRules.AllowsGameplayInput(MigrationGameStateMode.Overworld), "Overworld allows input.");
            AssertEqual(true, MigrationGameStateRules.AllowsGameplayInput(MigrationGameStateMode.Home), "Home allows input.");
            AssertEqual(true, MigrationGameStateRules.AllowsGameplayInput(MigrationGameStateMode.Combat), "Combat allows input.");
            AssertEqual(false, MigrationGameStateRules.AllowsGameplayInput(MigrationGameStateMode.Dialogue), "Dialogue blocks gameplay input.");
            AssertEqual(false, MigrationGameStateRules.AllowsGameplayInput(MigrationGameStateMode.Menu), "Menu blocks gameplay input.");
            AssertEqual(false, MigrationGameStateRules.AllowsGameplayInput(MigrationGameStateMode.Sleeping), "Sleeping blocks gameplay input.");
        }

        private static void TestHudModes()
        {
            AssertEqual(true, MigrationGameStateRules.ShowsHud(MigrationGameStateMode.Overworld), "Overworld shows HUD.");
            AssertEqual(false, MigrationGameStateRules.ShowsHud(MigrationGameStateMode.Menu), "Menu hides the world HUD.");
            AssertEqual(false, MigrationGameStateRules.ShowsHud(MigrationGameStateMode.Cutscene), "Cutscene hides the world HUD.");
        }

        private static void TestWorldTimeFreezeModes()
        {
            AssertEqual(true, MigrationGameStateRules.FreezesWorldTime(MigrationGameStateMode.Dialogue), "Dialogue freezes world time.");
            AssertEqual(true, MigrationGameStateRules.FreezesWorldTime(MigrationGameStateMode.Menu), "Menu freezes world time.");
            AssertEqual(false, MigrationGameStateRules.FreezesWorldTime(MigrationGameStateMode.Overworld), "Overworld runs world time.");
            AssertEqual(false, MigrationGameStateRules.FreezesWorldTime(MigrationGameStateMode.Sleeping), "Sleeping does not freeze (it fast-forwards) time.");
        }

        private static void TestWorldTimeScaleModes()
        {
            AssertEqual(0f, MigrationGameStateRules.WorldTimeScale(MigrationGameStateMode.Dialogue), "Dialogue freezes world time (scale 0).");
            AssertEqual(0f, MigrationGameStateRules.WorldTimeScale(MigrationGameStateMode.Menu), "Menu freezes world time (scale 0).");
            AssertEqual(0f, MigrationGameStateRules.WorldTimeScale(MigrationGameStateMode.Cutscene), "Cutscene freezes world time (scale 0).");
            AssertEqual(1f, MigrationGameStateRules.WorldTimeScale(MigrationGameStateMode.Overworld), "Overworld runs world time at normal speed.");
            AssertEqual(1f, MigrationGameStateRules.WorldTimeScale(MigrationGameStateMode.Home), "Home runs world time at normal speed.");
            AssertEqual(1f, MigrationGameStateRules.WorldTimeScale(MigrationGameStateMode.Combat), "Combat runs world time at normal speed.");
            AssertEqual(MigrationGameStateRules.SleepingTimeScale, MigrationGameStateRules.WorldTimeScale(MigrationGameStateMode.Sleeping), "Sleeping fast-forwards world time.");
            AssertEqual(true, MigrationGameStateRules.SleepingTimeScale > 1f, "Sleeping time scale must fast-forward (greater than normal speed).");

            // Consistency invariant: time scale is exactly 0 iff the mode freezes world time.
            foreach (MigrationGameStateMode mode in (MigrationGameStateMode[])Enum.GetValues(typeof(MigrationGameStateMode)))
            {
                bool frozen = MigrationGameStateRules.FreezesWorldTime(mode);
                AssertEqual(frozen, MigrationGameStateRules.WorldTimeScale(mode) == 0f, $"WorldTimeScale==0 must match FreezesWorldTime for {mode}.");
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
