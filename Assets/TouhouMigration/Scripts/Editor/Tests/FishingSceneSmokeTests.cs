using System;
using System.IO;
using TouhouMigration.Runtime.Fishing;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TouhouMigration.Editor.Tests
{
    // Covers the standalone playable fishing scene: it exists and carries a MigrationFishDriver so the
    // fishing minigame is launchable on its own, without the (concurrent) global UI controller.
    public static class FishingSceneSmokeTests
    {
        private const string ScenePath = "Assets/TouhouMigration/Scenes/MigrationFishingPlayable.unity";

        [MenuItem("Touhou Migration/Tests/Run Fishing Scene Smoke Tests")]
        public static void RunAll()
        {
            TestSceneExistsWithDriver();
            Debug.Log("Fishing scene smoke tests passed.");
        }

        private static void TestSceneExistsWithDriver()
        {
            AssertEqual(true, File.Exists(ScenePath), "The standalone fishing scene asset exists at " + ScenePath);

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            try
            {
                bool hasDriver = false;
                bool hasCamera = false;
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    if (root.GetComponentInChildren<MigrationFishDriver>() != null)
                    {
                        hasDriver = true;
                    }

                    if (root.GetComponentInChildren<Camera>() != null)
                    {
                        hasCamera = true;
                    }
                }

                AssertEqual(true, hasDriver, "The scene carries a MigrationFishDriver (the fishing bootstrapper).");
                AssertEqual(true, hasCamera, "The scene has a camera to render the IMGUI fishing minigame.");
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
