using System;
using System.IO;
using TouhouMigration.Runtime.Combat;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TouhouMigration.Editor.Tests
{
    // Covers the standalone enemy-side combat demo scene: it exists and carries a MigrationCombatDemoDriver
    // so the attack -> defeat -> loot pipeline is launchable on its own, without the player MonoBehaviour.
    public static class CombatSceneSmokeTests
    {
        private const string ScenePath = "Assets/TouhouMigration/Scenes/MigrationCombatDemoPlayable.unity";

        [MenuItem("Touhou Migration/Tests/Run Combat Scene Smoke Tests")]
        public static void RunAll()
        {
            TestSceneExistsWithDriver();
            Debug.Log("Combat scene smoke tests passed.");
        }

        private static void TestSceneExistsWithDriver()
        {
            AssertEqual(true, File.Exists(ScenePath), "The standalone combat demo scene asset exists at " + ScenePath);

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            try
            {
                bool hasDriver = false;
                bool hasCamera = false;
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    if (root.GetComponentInChildren<MigrationCombatDemoDriver>() != null)
                    {
                        hasDriver = true;
                    }

                    if (root.GetComponentInChildren<Camera>() != null)
                    {
                        hasCamera = true;
                    }
                }

                AssertEqual(true, hasDriver, "The scene carries a MigrationCombatDemoDriver.");
                AssertEqual(true, hasCamera, "The scene has a camera to render the IMGUI combat demo.");
            }
            finally
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
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
