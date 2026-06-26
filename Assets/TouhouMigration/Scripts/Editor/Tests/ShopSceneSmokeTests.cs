using System;
using System.IO;
using TouhouMigration.Runtime.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TouhouMigration.Editor.Tests
{
    // Covers the standalone playable shop scene: it exists and carries a MigrationShopDriver so the shop is
    // launchable on its own, without the (concurrent) global UI controller wiring it.
    public static class ShopSceneSmokeTests
    {
        private const string ScenePath = "Assets/TouhouMigration/Scenes/MigrationShopPlayable.unity";

        [MenuItem("Touhou Migration/Tests/Run Shop Scene Smoke Tests")]
        public static void RunAll()
        {
            TestSceneExistsWithDriver();
            Debug.Log("Shop scene smoke tests passed.");
        }

        private static void TestSceneExistsWithDriver()
        {
            AssertEqual(true, File.Exists(ScenePath), "The standalone shop scene asset exists at " + ScenePath);

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            try
            {
                bool hasDriver = false;
                bool hasCamera = false;
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    if (root.GetComponentInChildren<MigrationShopDriver>() != null)
                    {
                        hasDriver = true;
                    }

                    if (root.GetComponentInChildren<Camera>() != null)
                    {
                        hasCamera = true;
                    }
                }

                AssertEqual(true, hasDriver, "The scene carries a MigrationShopDriver (the shop bootstrapper).");
                AssertEqual(true, hasCamera, "The scene has a camera to render the IMGUI shop.");
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
