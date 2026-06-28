using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TouhouMigration.Runtime.Environment;

namespace TouhouMigration.Editor
{
    /// <summary>
    /// Converts the human-village NPCs to the project's procedural gait (MigrationProceduralWalker), which
    /// swings the real token-rig leg/arm bones forward-back about the travel axis. This replaces both the
    /// weak in-place baked clip (skating) and the failed Humanoid retarget (the AI rig's bind pose isn't a
    /// clean T-pose, so muscle calibration degenerates → sideways "crab" walk with vertical arm-swing).
    /// Restores the +90 model facing the token-rig needs and strips any Animator controller that would fight
    /// the procedural bone writes.
    /// -executeMethod TouhouMigration.Editor.MigrationProceduralWalkNpcs.ConvertExistingScene
    /// </summary>
    public static class MigrationProceduralWalkNpcs
    {
        private const float ModelForwardYaw = 90f;

        public static void ConvertExistingScene()
        {
            UnityEngine.SceneManagement.Scene scene = EditorSceneManager.OpenScene(
                "Assets/TouhouMigration/Scenes/HumanVillageVerticalSlice.unity", OpenSceneMode.Single);

            int done = 0;
            foreach (GameObject rootGo in scene.GetRootGameObjects())
                foreach (Transform t in rootGo.GetComponentsInChildren<Transform>(true))
                {
                    if (!(t.name.StartsWith("BakedStory_") || t.name.StartsWith("Villager_"))) continue;
                    Transform root = t;

                    // model = the GLB instance child carrying the Animator/bones
                    Animator anim = root.GetComponentInChildren<Animator>(true);
                    Transform model = anim != null ? anim.transform : (root.childCount > 0 ? root.GetChild(0) : null);
                    if (model == null) { Debug.LogWarning("[ProcWalk] no model under " + root.name); continue; }

                    model.localRotation = Quaternion.Euler(0f, ModelForwardYaw, 0f);
                    if (anim != null) { anim.runtimeAnimatorController = null; anim.avatar = null; } // detach the failed humanoid setup

                    // carry over patrol from the old MigrationNpcWalker, then swap it for the procedural walker
                    Vector3[] waypoints = null; float speed = 0.95f; float groundY = 0f;
                    MigrationNpcWalker old = root.GetComponent<MigrationNpcWalker>();
                    if (old != null) { waypoints = old.waypoints; speed = old.speed; groundY = old.groundY; Object.DestroyImmediate(old); }

                    MigrationProceduralWalker pw = root.GetComponent<MigrationProceduralWalker>();
                    if (pw == null) pw = root.gameObject.AddComponent<MigrationProceduralWalker>();
                    pw.waypoints = waypoints;
                    pw.speed = Mathf.Clamp(speed, 0.85f, 1.1f);
                    pw.groundY = groundY;

                    Debug.Log($"[ProcWalk] {root.name} -> procedural gait, speed {pw.speed:0.00}");
                    done++;
                }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[ProcWalk] converted {done} NPCs to procedural gait");
        }
    }
}
