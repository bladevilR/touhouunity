using System;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    public static class ContentSmokeTests
    {
        private const string MokouVisualPath = "Assets/TouhouMigration/Art/Characters/Mokou/Models/mokou.glb";
        private const string MokouReferenceRigPath = "Assets/TouhouMigration/Art/Characters/ReferenceRigs/ReimuMokouCc/reimu_mokou_cc.glb";
        private const string MokouCharacterValidationScenePath = "Assets/TouhouMigration/Scenes/MokouCharacterValidation.unity";
        private const string MokouValidationAnimationsRoot = "Assets/TouhouMigration/Animations/Characters/MokouValidation";

        private static readonly string[] MokouValidationAnimations =
        {
            "Standing Idle.fbx",
            "Standard Run.fbx",
            "Fast Run.fbx",
            "Jump.fbx",
            "Stand To Roll.fbx",
            "Mma Kick.fbx",
            "Uppercut Jab.fbx"
        };

        [MenuItem("Touhou Migration/Tests/Run Content Smoke Tests")]
        public static void RunAll()
        {
            AssertAsset<GameObject>(MokouVisualPath, "Mokou visual GLB should import as a Unity GameObject.");
            AssertAsset<GameObject>(MokouReferenceRigPath, "Reference rig GLB should import as a Unity GameObject.");
            AssertAsset<SceneAsset>(MokouCharacterValidationScenePath, "Mokou validation scene should be generated.");
            AssertMokouAnimationClips();

            Debug.Log("Content smoke tests passed.");
        }

        private static void AssertMokouAnimationClips()
        {
            foreach (string fileName in MokouValidationAnimations)
            {
                string assetPath = $"{MokouValidationAnimationsRoot}/{fileName}";
                UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
                bool hasClip = false;

                foreach (UnityEngine.Object asset in assets)
                {
                    if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__", StringComparison.Ordinal))
                    {
                        hasClip = true;
                        break;
                    }
                }

                if (!hasClip)
                {
                    throw new Exception($"Missing imported AnimationClip in {assetPath}.");
                }
            }
        }

        private static void AssertAsset<T>(string assetPath, string message)
            where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (asset == null)
            {
                throw new Exception($"{message} Missing asset at {assetPath}.");
            }
        }
    }
}
