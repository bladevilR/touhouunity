using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TouhouMigration.Editor
{
    public static class MigrationSceneCapture
    {
        private const string HumanVillageScenePath = "Assets/TouhouMigration/Scenes/HumanVillageVerticalSlice.unity";
        private const string MokouCharacterValidationScenePath = "Assets/TouhouMigration/Scenes/MokouCharacterValidation.unity";
        private const string HumanVillageOutputPath = "Verification/VisualChecks/HumanVillage_M8.png";
        private const string MokouCharacterOutputPath = "Verification/VisualChecks/MokouCharacter_M9.png";

        [MenuItem("Touhou Migration/Capture/Human Village Preview")]
        public static void CaptureHumanVillagePreview()
        {
            CaptureScene(HumanVillageScenePath, HumanVillageOutputPath, "Human Village preview");
        }

        [MenuItem("Touhou Migration/Capture/Mokou Character Preview")]
        public static void CaptureMokouCharacterPreview()
        {
            CaptureScene(MokouCharacterValidationScenePath, MokouCharacterOutputPath, "Mokou character preview");
        }

        private static void CaptureScene(string scenePath, string outputPath, string label)
        {
            EditorSceneManager.OpenScene(scenePath);
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

            Camera camera = Camera.main;
            if (camera == null)
            {
                throw new IOException($"{label} capture failed: no Main Camera found.");
            }

            RenderTexture target = new RenderTexture(1280, 720, 24);
            RenderTexture previousTarget = camera.targetTexture;
            RenderTexture previousActive = RenderTexture.active;

            try
            {
                camera.targetTexture = target;
                RenderTexture.active = target;
                camera.Render();

                Texture2D screenshot = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
                screenshot.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
                screenshot.Apply();

                File.WriteAllBytes(outputPath, screenshot.EncodeToPNG());
                Object.DestroyImmediate(screenshot);
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                target.Release();
                Object.DestroyImmediate(target);
            }

            AssetDatabase.Refresh();
            Debug.Log($"{label} captured: {outputPath}");
        }
    }
}
