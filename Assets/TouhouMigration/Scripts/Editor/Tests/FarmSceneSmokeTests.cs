using System;
using System.IO;
using TouhouMigration.Runtime.Farming;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TouhouMigration.Editor.Tests
{
    // Covers the standalone playable farm scene: it exists and carries a MigrationFarmDriver so the closed
    // E4 farming loop is launchable on its own, without the (concurrent) global UI controller.
    public static class FarmSceneSmokeTests
    {
        private const string ScenePath = "Assets/TouhouMigration/Scenes/MigrationFarmPlayable.unity";

        [MenuItem("Touhou Migration/Tests/Run Farm Scene Smoke Tests")]
        public static void RunAll()
        {
            TestSceneExistsWithDriver();
            Debug.Log("Farm scene smoke tests passed.");
        }

        private static void TestSceneExistsWithDriver()
        {
            AssertEqual(true, File.Exists(ScenePath), "The standalone farm scene asset exists at " + ScenePath);

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            try
            {
                bool hasDriver = false;
                bool hasCamera = false;
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    if (root.GetComponentInChildren<MigrationFarmDriver>() != null)
                    {
                        hasDriver = true;
                    }

                    if (root.GetComponentInChildren<Camera>() != null)
                    {
                        hasCamera = true;
                    }
                }

                AssertEqual(true, hasDriver, "The scene carries a MigrationFarmDriver (the farm bootstrapper).");
                AssertEqual(true, hasCamera, "The scene has a camera to render the IMGUI farm loop.");
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
