using System;
using System.Collections.Generic;
using TouhouMigration.Runtime.Dialogue;
using TouhouMigration.Runtime.Player;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    public static class DialogueHumanityEffectSmokeTests
    {
        [MenuItem("Touhou Migration/Tests/Run Dialogue Humanity Effect Smoke Tests")]
        public static void RunAll()
        {
            TestHumanityServiceDefaultsAndClamps();
            TestHumanityServiceLevelThresholds();
            TestHumanityEffectRoutesToService();
            TestHumanityActionRoutesToService();
            TestHumanityEffectNoOpWithoutBoundService();
            Debug.Log("Dialogue humanity effect smoke tests passed.");
        }

        private static void TestHumanityServiceDefaultsAndClamps()
        {
            HumanityService humanity = new HumanityService();
            AssertEqual(100, humanity.Humanity, "Humanity should default to 100 (Mokou's full humanity).");

            humanity.Adjust(-30);
            AssertEqual(70, humanity.Humanity, "Adjust should apply a negative delta.");

            humanity.Adjust(-1000);
            AssertEqual(0, humanity.Humanity, "Adjust should clamp at the floor (0).");

            humanity.Adjust(1000);
            AssertEqual(100, humanity.Humanity, "Adjust should clamp at the ceiling (100).");
        }

        private static void TestHumanityServiceLevelThresholds()
        {
            HumanityService humanity = new HumanityService();
            humanity.Set(70);
            AssertEqual(HumanityLevel.High, humanity.Level, "Humanity >= 70 is High (Godot MokouMonologueSystem).");
            humanity.Set(40);
            AssertEqual(HumanityLevel.Medium, humanity.Level, "Humanity in [40,70) is Medium.");
            humanity.Set(39);
            AssertEqual(HumanityLevel.Low, humanity.Level, "Humanity < 40 is Low.");
        }

        private static void TestHumanityEffectRoutesToService()
        {
            HumanityService humanity = new HumanityService();
            DialogueEffectRouter router = new DialogueEffectRouter(null, null);
            router.BindHumanity(humanity);

            bool handled = router.Apply("mokou", new Dictionary<string, object>
            {
                ["humanity"] = -5
            });
            AssertEqual(true, handled, "humanity effect should route to the bound service.");
            AssertEqual(95, humanity.Humanity, "humanity effect should adjust the stat by the data value.");
        }

        private static void TestHumanityActionRoutesToService()
        {
            HumanityService humanity = new HumanityService();
            DialogueEffectRouter router = new DialogueEffectRouter(null, null);
            router.BindHumanity(humanity);
            humanity.Set(50);

            bool handled = router.ApplyAction("humanity", new Dictionary<string, object>
            {
                ["npc_id"] = "keine",
                ["value"] = 15
            });
            AssertEqual(true, handled, "humanity action should route through the live ActionRequested path.");
            AssertEqual(65, humanity.Humanity, "humanity action should adjust the stat by the payload value.");
        }

        private static void TestHumanityEffectNoOpWithoutBoundService()
        {
            DialogueEffectRouter router = new DialogueEffectRouter(null, null);
            bool handled = router.Apply("mokou", new Dictionary<string, object>
            {
                ["humanity"] = -5
            });
            AssertEqual(false, handled, "humanity effect is a no-op when no humanity service is bound.");
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
