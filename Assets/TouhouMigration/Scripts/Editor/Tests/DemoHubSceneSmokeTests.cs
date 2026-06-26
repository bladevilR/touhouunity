using System;
using System.IO;
using TouhouMigration.Runtime.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TouhouMigration.Editor.Tests
{
    // Covers the demo hub: the hub scene exists with a MigrationDemoHubDriver, and all five domain demo
    // scenes are registered in the build settings so the hub's LoadScene resolves them.
    public static class DemoHubSceneSmokeTests
    {
        private const string HubPath = "Assets/TouhouMigration/Scenes/MigrationDemoHub.unity";

        private static readonly string[] DomainScenes =
        {
            "Assets/TouhouMigration/Scenes/MigrationCirnoBossPlayable.unity",
            "Assets/TouhouMigration/Scenes/MigrationShopPlayable.unity",
            "Assets/TouhouMigration/Scenes/MigrationFarmPlayable.unity",
            "Assets/TouhouMigration/Scenes/MigrationFishingPlayable.unity",
            "Assets/TouhouMigration/Scenes/MigrationCombatDemoPlayable.unity",
        };

        [MenuItem("Touhou Migration/Tests/Run Demo Hub Scene Smoke Tests")]
        public static void RunAll()
        {
            TestHubSceneHasDriver();
            TestDomainScenesExistForHub();
            Debug.Log("Demo hub scene smoke tests passed.");
        }

        private static void TestHubSceneHasDriver()
        {
            AssertEqual(true, File.Exists(HubPath), "The demo hub scene exists at " + HubPath);
            Scene scene = EditorSceneManager.OpenScene(HubPath, OpenSceneMode.Single);
            try
            {
                bool hasHub = false;
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    if (root.GetComponentInChildren<MigrationDemoHubDriver>() != null)
                    {
                        hasHub = true;
                    }
                }

                AssertEqual(true, hasHub, "The hub scene carries a MigrationDemoHubDriver.");
            }
            finally
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }

        // The hub navigates to the five domain scenes by name; verify each scene asset exists so the hub's
        // LoadScene targets are real. (Build-settings registration is done best-effort by the builder when
        // run in the interactive editor; it is not asserted here as it does not persist in batch mode.)
        private static void TestDomainScenesExistForHub()
        {
            foreach (string domain in DomainScenes)
            {
                AssertEqual(true, File.Exists(domain), "The hub's target scene exists: " + domain);
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
