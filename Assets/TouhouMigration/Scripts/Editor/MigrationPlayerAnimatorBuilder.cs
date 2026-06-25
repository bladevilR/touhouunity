using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace TouhouMigration.Editor
{
    // Builds the player locomotion AnimatorController: a 1D blend tree on "Speed" (0..1) blending the
    // Humanoid Mixamo clips Idle -> Standard Run -> Fast Run. The clips retarget onto Mokou's avatar
    // (created by MigrationCharacterRigImporter). MigrationLocomotionAnimatorBridge drives "Speed" from
    // the player's normalized locomotion speed.
    public static class MigrationPlayerAnimatorBuilder
    {
        private const string ClipsRoot = "Assets/TouhouMigration/Animations/Characters/MokouValidation";
        private const string ControllerPath = "Assets/TouhouMigration/Animations/Characters/MokouLocomotion.controller";

        public static void BuildMokouLocomotion()
        {
            AnimationClip idle = LoadClip($"{ClipsRoot}/Standing Idle.fbx", loop: true);
            AnimationClip run = LoadClip($"{ClipsRoot}/Standard Run.fbx", loop: true);
            AnimationClip fast = LoadClip($"{ClipsRoot}/Fast Run.fbx", loop: true);

            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("Grounded", AnimatorControllerParameterType.Bool);

            controller.CreateBlendTreeInController("Locomotion", out BlendTree tree);
            tree.blendType = BlendTreeType.Simple1D;
            tree.blendParameter = "Speed";
            tree.useAutomaticThresholds = false;
            if (idle != null)
            {
                tree.AddChild(idle, 0f);
            }
            if (run != null)
            {
                tree.AddChild(run, 0.5f);
            }
            if (fast != null)
            {
                tree.AddChild(fast, 1f);
            }

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[anim] MokouLocomotion built at {ControllerPath} idle={idle != null} run={run != null} fast={fast != null}");
        }

        // Load the animation clip embedded in a Mixamo FBX, ensuring it loops if requested.
        private static AnimationClip LoadClip(string fbxPath, bool loop)
        {
            if (loop)
            {
                ModelImporter importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
                if (importer != null && importer.clipAnimations.Length == 0)
                {
                    ModelImporterClipAnimation[] defaults = importer.defaultClipAnimations;
                    for (int i = 0; i < defaults.Length; i++)
                    {
                        defaults[i].loopTime = true;
                    }

                    if (defaults.Length > 0)
                    {
                        importer.clipAnimations = defaults;
                        importer.SaveAndReimport();
                    }
                }
            }

            foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(fbxPath))
            {
                if (o is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                {
                    return clip;
                }
            }

            Debug.LogWarning($"[anim] No AnimationClip found in {fbxPath}");
            return null;
        }
    }
}
