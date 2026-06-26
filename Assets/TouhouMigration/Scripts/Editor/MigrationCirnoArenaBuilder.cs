using System.IO;
using TouhouMigration.Runtime.CardBuild;
using TouhouMigration.Runtime.Farming;
using TouhouMigration.Runtime.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TouhouMigration.Editor
{
    // Authors a standalone, self-contained playable Cirno card-fight scene
    // (Assets/TouhouMigration/Scenes/MigrationCirnoBossPlayable.unity): a camera + a GameObject carrying the
    // MigrationCirnoBossController (which drives the tested MigrationCirnoBossSession with its IMGUI card UI).
    //
    // This deliberately authors a NEW scene via its own builder rather than placing the controller into the
    // existing CirnoBossArena through TouhouMigrationProjectBuilder — that file is a concurrent session's, so
    // this routes around it: the fight becomes launchable on its own with zero edits to in-flight files.
    public static class MigrationCirnoArenaBuilder
    {
        private const string SceneDir = "Assets/TouhouMigration/Scenes";
        private const string ScenePath = SceneDir + "/MigrationCirnoBossPlayable.unity";

        [MenuItem("Touhou Migration/Build/Build Cirno Boss Playable Scene")]
        public static void BuildScene()
        {
            UnityEngine.SceneManagement.Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Camera to render the IMGUI fight UI.
            GameObject cameraGo = new GameObject("Main Camera");
            Camera camera = cameraGo.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.62f, 0.81f, 0.93f); // Cirno's icy sky blue
            camera.orthographic = true;
            cameraGo.tag = "MainCamera";

            // The fight driver.
            GameObject controllerGo = new GameObject("CirnoBossController");
            controllerGo.AddComponent<MigrationCirnoBossController>();

            if (!Directory.Exists(SceneDir))
            {
                Directory.CreateDirectory(SceneDir);
            }

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();
            Debug.Log("[MigrationCirnoArenaBuilder] authored " + ScenePath);
        }

        private const string ShopScenePath = SceneDir + "/MigrationShopPlayable.unity";

        // Authors a standalone playable shop scene (camera + MigrationShopDriver), routing around the
        // concurrent MigrationGlobalUiController the same way the Cirno scene does.
        [MenuItem("Touhou Migration/Build/Build Shop Playable Scene")]
        public static void BuildShopScene()
        {
            UnityEngine.SceneManagement.Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject cameraGo = new GameObject("Main Camera");
            Camera camera = cameraGo.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.16f, 0.13f, 0.20f);
            camera.orthographic = true;
            cameraGo.tag = "MainCamera";

            GameObject driverGo = new GameObject("ShopDriver");
            driverGo.AddComponent<MigrationShopDriver>();

            if (!Directory.Exists(SceneDir))
            {
                Directory.CreateDirectory(SceneDir);
            }

            EditorSceneManager.SaveScene(scene, ShopScenePath);
            AssetDatabase.Refresh();
            Debug.Log("[MigrationCirnoArenaBuilder] authored " + ShopScenePath);
        }

        private const string FarmScenePath = SceneDir + "/MigrationFarmPlayable.unity";

        // Authors a standalone playable farm scene (camera + MigrationFarmDriver) showcasing the closed E4
        // economy loop, routing around the concurrent owner like the Cirno + shop scenes.
        [MenuItem("Touhou Migration/Build/Build Farm Playable Scene")]
        public static void BuildFarmScene()
        {
            UnityEngine.SceneManagement.Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject cameraGo = new GameObject("Main Camera");
            Camera camera = cameraGo.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.40f, 0.55f, 0.30f); // field green
            camera.orthographic = true;
            cameraGo.tag = "MainCamera";

            GameObject driverGo = new GameObject("FarmDriver");
            driverGo.AddComponent<MigrationFarmDriver>();

            if (!Directory.Exists(SceneDir))
            {
                Directory.CreateDirectory(SceneDir);
            }

            EditorSceneManager.SaveScene(scene, FarmScenePath);
            AssetDatabase.Refresh();
            Debug.Log("[MigrationCirnoArenaBuilder] authored " + FarmScenePath);
        }
    }
}
