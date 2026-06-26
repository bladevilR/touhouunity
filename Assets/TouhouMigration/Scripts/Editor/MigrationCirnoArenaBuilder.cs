using System.IO;
using TouhouMigration.Runtime.CardBuild;
using TouhouMigration.Runtime.Combat;
using TouhouMigration.Runtime.Farming;
using TouhouMigration.Runtime.Fishing;
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

        private const string FishScenePath = SceneDir + "/MigrationFishingPlayable.unity";

        // Authors a standalone playable fishing scene (camera + MigrationFishDriver), routing around the
        // concurrent owner like the Cirno + shop + farm scenes.
        [MenuItem("Touhou Migration/Build/Build Fishing Playable Scene")]
        public static void BuildFishingScene()
        {
            UnityEngine.SceneManagement.Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject cameraGo = new GameObject("Main Camera");
            Camera camera = cameraGo.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.20f, 0.42f, 0.55f); // lake blue
            camera.orthographic = true;
            cameraGo.tag = "MainCamera";

            GameObject driverGo = new GameObject("FishDriver");
            driverGo.AddComponent<MigrationFishDriver>();

            if (!Directory.Exists(SceneDir))
            {
                Directory.CreateDirectory(SceneDir);
            }

            EditorSceneManager.SaveScene(scene, FishScenePath);
            AssetDatabase.Refresh();
            Debug.Log("[MigrationCirnoArenaBuilder] authored " + FishScenePath);
        }

        private const string CombatScenePath = SceneDir + "/MigrationCombatDemoPlayable.unity";

        // Authors a standalone enemy-side combat demo scene (camera + MigrationCombatDemoDriver): attack ->
        // defeat -> loot, without the player MonoBehaviour, so it stays clear of the concurrent locomotion
        // work while still showing the combat-defeat-loot pipeline.
        [MenuItem("Touhou Migration/Build/Build Combat Demo Playable Scene")]
        public static void BuildCombatScene()
        {
            UnityEngine.SceneManagement.Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject cameraGo = new GameObject("Main Camera");
            Camera camera = cameraGo.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.22f, 0.10f, 0.12f); // battle crimson
            camera.orthographic = true;
            cameraGo.tag = "MainCamera";

            GameObject driverGo = new GameObject("CombatDemoDriver");
            driverGo.AddComponent<MigrationCombatDemoDriver>();

            if (!Directory.Exists(SceneDir))
            {
                Directory.CreateDirectory(SceneDir);
            }

            EditorSceneManager.SaveScene(scene, CombatScenePath);
            AssetDatabase.Refresh();
            Debug.Log("[MigrationCirnoArenaBuilder] authored " + CombatScenePath);
        }

        private const string HubScenePath = SceneDir + "/MigrationDemoHub.unity";

        private static readonly string[] DemoScenePaths =
        {
            HubScenePath, ScenePath, ShopScenePath, FarmScenePath, FishScenePath, CombatScenePath,
        };

        // Authors the demo hub scene (camera + MigrationDemoHubDriver) and registers all six demo scenes in
        // the build settings (appended, never clobbering existing entries) so the hub's LoadScene resolves
        // them. The single navigable entry point to every standalone domain demo.
        [MenuItem("Touhou Migration/Build/Build Demo Hub Scene")]
        public static void BuildDemoHub()
        {
            UnityEngine.SceneManagement.Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject cameraGo = new GameObject("Main Camera");
            Camera camera = cameraGo.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.10f, 0.09f, 0.12f);
            camera.orthographic = true;
            cameraGo.tag = "MainCamera";

            GameObject hubGo = new GameObject("DemoHub");
            hubGo.AddComponent<MigrationDemoHubDriver>();

            if (!Directory.Exists(SceneDir))
            {
                Directory.CreateDirectory(SceneDir);
            }

            EditorSceneManager.SaveScene(scene, HubScenePath);
            RegisterDemoScenesInBuildSettings();
            AssetDatabase.Refresh();
            Debug.Log("[MigrationCirnoArenaBuilder] authored " + HubScenePath + " + registered demo scenes");
        }

        // Append any missing demo scenes to EditorBuildSettings (so SceneManager.LoadScene resolves them),
        // preserving every existing entry.
        private static void RegisterDemoScenesInBuildSettings()
        {
            System.Collections.Generic.List<EditorBuildSettingsScene> scenes =
                new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

            System.Collections.Generic.HashSet<string> present = new System.Collections.Generic.HashSet<string>();
            foreach (EditorBuildSettingsScene existing in scenes)
            {
                present.Add(existing.path);
            }

            foreach (string path in DemoScenePaths)
            {
                if (!present.Contains(path))
                {
                    scenes.Add(new EditorBuildSettingsScene(path, true));
                }
            }

            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
