using System;
using System.IO;
using TouhouMigration.Runtime.CardBuild;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TouhouMigration.Editor.Tests
{
    // Covers the standalone playable Cirno arena scene: it exists and carries a MigrationCirnoBossController
    // so the card-fight is launchable on its own, without the (concurrent) project builder / global UI.
    public static class CirnoArenaSceneSmokeTests
    {
        private const string ScenePath = "Assets/TouhouMigration/Scenes/MigrationCirnoBossPlayable.unity";

        [MenuItem("Touhou Migration/Tests/Run Cirno Arena Scene Smoke Tests")]
        public static void RunAll()
        {
            TestSceneExistsWithController();
            Debug.Log("Cirno arena scene smoke tests passed.");
        }

        private static void TestSceneExistsWithController()
        {
            AssertEqual(true, File.Exists(ScenePath), "The standalone Cirno arena scene asset exists at " + ScenePath);

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            try
            {
                bool hasController = false;
                bool hasCamera = false;
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    if (root.GetComponentInChildren<MigrationCirnoBossController>() != null)
                    {
                        hasController = true;
                    }

                    if (root.GetComponentInChildren<Camera>() != null)
                    {
                        hasCamera = true;
                    }
                }

                AssertEqual(true, hasController, "The scene carries a MigrationCirnoBossController (the fight driver).");
                AssertEqual(true, hasCamera, "The scene has a camera to render the IMGUI fight.");
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
