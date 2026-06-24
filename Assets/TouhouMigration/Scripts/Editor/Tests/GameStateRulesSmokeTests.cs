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

        private static void AssertEqual<T>(T expected, T actual, string message)
        {
            if (!Equals(expected, actual))
            {
                throw new Exception($"{message} Expected: {expected}. Actual: {actual}.");
            }
        }
    }
}
