using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TouhouMigration.Runtime.Environment;

namespace TouhouMigration.Editor
{
    // Real play-mode close-up of BakedStory_reimu: the player loop runs (so SkinnedMeshRenderer actually
    // re-skins) and the recorder captures a burst across the walk cycle, then exits batchmode itself.
    // -executeMethod TouhouMigration.Editor.NpcCloseupPlayRunner.Run   (do NOT pass -quit)
    public static class NpcCloseupPlayRunner
    {
        public static void Run()
        {
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions =
                EnterPlayModeOptions.DisableDomainReload | EnterPlayModeOptions.DisableSceneReload;
            EditorSceneManager.OpenScene(
                "Assets/TouhouMigration/Scenes/HumanVillageVerticalSlice.unity", OpenSceneMode.Single);

            GameObject go = new GameObject("__PMRecorder");
            var rec = go.AddComponent<MigrationPlayModeRecorder>();
            rec.outDir = "Verification/VisualChecks/npccloseup_real";
            rec.stamp = System.DateTime.Now.ToString("HHmmss");
            rec.followName = "BakedStory_reimu";
            rec.followOffset = new Vector3(2.2f, 1.0f, -1.4f);
            rec.orbit = false;
            rec.fov = 38f;
            rec.frames = 10;
            rec.interval = 0.14f;
            EditorApplication.EnterPlaymode();
            Debug.Log("[NpcCloseupPlayRunner] entering play mode to capture BakedStory_reimu walk");
        }
    }
}
