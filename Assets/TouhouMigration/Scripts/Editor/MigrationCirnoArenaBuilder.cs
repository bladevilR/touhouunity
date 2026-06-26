using System.IO;
using TouhouMigration.Runtime.CardBuild;
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
    }
}
