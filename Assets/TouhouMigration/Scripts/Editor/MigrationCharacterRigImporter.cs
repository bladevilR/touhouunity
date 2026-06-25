using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor
{
    // Imports converted character FBX models (glb→FBX via Blender) as Mecanim Humanoid rigs so the
    // Mixamo-style humanoid clips can retarget onto them. The source glb files import via glTFast with
    // no Humanoid avatar (the E1.5 blocker), and Unity's auto-mapper cannot map the Character-Creator
    // (CC_Base_*) skeleton on its own — so we supply an explicit CC→Unity human bone mapping plus a
    // skeleton built from the model's bind pose.
    public static class MigrationCharacterRigImporter
    {
        private const string CharactersRoot = "Assets/TouhouMigration/Art/Characters";

        // CC (Character Creator / Reallusion) bone name -> Unity Humanoid bone name.
        private static readonly Dictionary<string, string> CcToHuman = new Dictionary<string, string>
        {
            { "CC_Base_Hip", "Hips" },
            { "CC_Base_Waist", "Spine" },
            { "CC_Base_Spine01", "Chest" },
            { "CC_Base_Spine02", "UpperChest" },
            { "CC_Base_NeckTwist01", "Neck" },
            { "CC_Base_Head", "Head" },
            { "CC_Base_L_Thigh", "LeftUpperLeg" },
            { "CC_Base_L_Calf", "LeftLowerLeg" },
            { "CC_Base_L_Foot", "LeftFoot" },
            { "CC_Base_L_ToeBase", "LeftToes" },
            { "CC_Base_R_Thigh", "RightUpperLeg" },
            { "CC_Base_R_Calf", "RightLowerLeg" },
            { "CC_Base_R_Foot", "RightFoot" },
            { "CC_Base_R_ToeBase", "RightToes" },
            { "CC_Base_L_Clavicle", "LeftShoulder" },
            { "CC_Base_L_Upperarm", "LeftUpperArm" },
            { "CC_Base_L_Forearm", "LeftLowerArm" },
            { "CC_Base_L_Hand", "LeftHand" },
            { "CC_Base_R_Clavicle", "RightShoulder" },
            { "CC_Base_R_Upperarm", "RightUpperArm" },
            { "CC_Base_R_Forearm", "RightLowerArm" },
            { "CC_Base_R_Hand", "RightHand" },
            { "CC_Base_L_Thumb1", "Left Thumb Proximal" }, { "CC_Base_L_Thumb2", "Left Thumb Intermediate" }, { "CC_Base_L_Thumb3", "Left Thumb Distal" },
            { "CC_Base_L_Index1", "Left Index Proximal" }, { "CC_Base_L_Index2", "Left Index Intermediate" }, { "CC_Base_L_Index3", "Left Index Distal" },
            { "CC_Base_L_Mid1", "Left Middle Proximal" }, { "CC_Base_L_Mid2", "Left Middle Intermediate" }, { "CC_Base_L_Mid3", "Left Middle Distal" },
            { "CC_Base_L_Ring1", "Left Ring Proximal" }, { "CC_Base_L_Ring2", "Left Ring Intermediate" }, { "CC_Base_L_Ring3", "Left Ring Distal" },
            { "CC_Base_L_Pinky1", "Left Little Proximal" }, { "CC_Base_L_Pinky2", "Left Little Intermediate" }, { "CC_Base_L_Pinky3", "Left Little Distal" },
            { "CC_Base_R_Thumb1", "Right Thumb Proximal" }, { "CC_Base_R_Thumb2", "Right Thumb Intermediate" }, { "CC_Base_R_Thumb3", "Right Thumb Distal" },
            { "CC_Base_R_Index1", "Right Index Proximal" }, { "CC_Base_R_Index2", "Right Index Intermediate" }, { "CC_Base_R_Index3", "Right Index Distal" },
            { "CC_Base_R_Mid1", "Right Middle Proximal" }, { "CC_Base_R_Mid2", "Right Middle Intermediate" }, { "CC_Base_R_Mid3", "Right Middle Distal" },
            { "CC_Base_R_Ring1", "Right Ring Proximal" }, { "CC_Base_R_Ring2", "Right Ring Intermediate" }, { "CC_Base_R_Ring3", "Right Ring Distal" },
            { "CC_Base_R_Pinky1", "Right Little Proximal" }, { "CC_Base_R_Pinky2", "Right Little Intermediate" }, { "CC_Base_R_Pinky3", "Right Little Distal" },
        };

        public static void ConfigureMokouHumanoid()
        {
            ConfigureHumanoid(CharactersRoot + "/Mokou/Models/mokou.fbx");
        }

        public static void ConfigureAllHumanoids()
        {
            string[] guids = AssetDatabase.FindAssets("t:Model", new[] { CharactersRoot });
            int valid = 0, total = 0;
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                total++;
                if (ConfigureHumanoid(path))
                {
                    valid++;
                }
            }

            Debug.Log($"[rig] ConfigureAllHumanoids: {valid}/{total} FBX produced a valid human avatar.");
        }

        public static bool ConfigureHumanoid(string fbxPath)
        {
            ModelImporter importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
            if (importer == null)
            {
                Debug.LogError($"[rig] No ModelImporter at {fbxPath}");
                return false;
            }

            // First pass: import as Human so the model hierarchy/bind pose is available.
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.SaveAndReimport();

            GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (root == null)
            {
                Debug.LogError($"[rig] Could not load model at {fbxPath}");
                return false;
            }

            // Build the skeleton[] from every transform's bind-pose local TRS.
            List<SkeletonBone> skeleton = new List<SkeletonBone>();
            HashSet<string> presentBones = new HashSet<string>();
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                skeleton.Add(new SkeletonBone
                {
                    name = t.name,
                    position = t.localPosition,
                    rotation = t.localRotation,
                    scale = t.localScale,
                });
                presentBones.Add(t.name);
            }

            // Build human[] from the explicit CC→Unity map, including only bones actually present.
            List<HumanBone> humanBones = new List<HumanBone>();
            int requiredMissing = 0;
            foreach (KeyValuePair<string, string> kv in CcToHuman)
            {
                if (!presentBones.Contains(kv.Key))
                {
                    continue;
                }

                HumanBone bone = new HumanBone { humanName = kv.Value, boneName = kv.Key };
                bone.limit.useDefaultValues = true;
                humanBones.Add(bone);
            }

            HumanDescription description = new HumanDescription
            {
                human = humanBones.ToArray(),
                skeleton = skeleton.ToArray(),
                upperArmTwist = 0.5f,
                lowerArmTwist = 0.5f,
                upperLegTwist = 0.5f,
                lowerLegTwist = 0.5f,
                armStretch = 0.05f,
                legStretch = 0.05f,
                feetSpacing = 0f,
                hasTranslationDoF = false,
            };

            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.humanDescription = description;
            importer.SaveAndReimport();

            Avatar avatar = null;
            foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(fbxPath))
            {
                if (o is Avatar candidate)
                {
                    avatar = candidate;
                    break;
                }
            }

            bool valid = avatar != null && avatar.isValid && avatar.isHuman;
            Debug.Log($"[rig] {fbxPath} mapped={humanBones.Count} bones, skeleton={skeleton.Count} -> " +
                      $"avatar={(avatar != null)} valid={valid} missingRequired={requiredMissing}");
            return valid;
        }

        // Report the Mokou FBX's natural size, materials/textures, and avatar so we can fix the in-scene
        // visual (scale/material) without guessing.
        public static void DiagnoseMokou()
        {
            string fbx = CharactersRoot + "/Mokou/Models/mokou.fbx";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(fbx);
            if (prefab == null)
            {
                Debug.LogError("[diag] could not load mokou.fbx");
                return;
            }

            GameObject inst = Object.Instantiate(prefab);
            Renderer[] renderers = inst.GetComponentsInChildren<Renderer>();
            Bounds bounds = renderers.Length > 0 ? renderers[0].bounds : new Bounds(inst.transform.position, Vector3.zero);
            foreach (Renderer r in renderers)
            {
                bounds.Encapsulate(r.bounds);
            }

            Debug.Log($"[diag] renderers={renderers.Length} localScale={inst.transform.localScale} boundsSize={bounds.size} heightY={bounds.size.y:F3}");
            foreach (Renderer r in renderers)
            {
                foreach (Material m in r.sharedMaterials)
                {
                    Debug.Log($"[diag] mat='{(m != null ? m.name : "null")}' shader='{(m != null && m.shader != null ? m.shader.name : "null")}' mainTex={(m != null && m.mainTexture != null)} color={(m != null ? m.color.ToString() : "n/a")}");
                }
            }

            Animator animator = inst.GetComponent<Animator>();
            Debug.Log($"[diag] animator={(animator != null)} avatar='{(animator != null && animator.avatar != null ? animator.avatar.name : "null")}' avatarValid={(animator != null && animator.avatar != null && animator.avatar.isValid)}");
            Object.DestroyImmediate(inst);
        }
    }
}
