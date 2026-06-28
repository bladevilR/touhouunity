using System;
using System.Collections.Generic;
using System.Reflection;
using TouhouMigration.Editor;
using TouhouMigration.Runtime.Environment;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    public static class HumanVillageNpcRigSmokeTests
    {
        [MenuItem("Touhou Migration/Tests/Run Human Village NPC Rig Smoke Tests")]
        public static void RunAll()
        {
            AssertBakedStorySource("reimu");
            AssertBakedStorySource("meiling");
            AssertBakedStorySource("marisa_blackwhite");
            AssertHumanVillageStoryCullKeepsOnlyFixedBakedCharacters();
            AssertCulledPlayableSceneContents();
            Debug.Log("Human Village NPC rig smoke tests passed.");
        }

        public static void DumpReimuPoseDiagnostics()
        {
            GameObject root = Spawn("reimu");
            try
            {
                DumpBoneVector(root.transform, "L", new[] { "CC_Base_L_Clavicle", "L_Clavicle", "LeftShoulder" }, new[] { "CC_Base_L_Upperarm", "L_Upperarm", "LeftUpperArm" }, new[] { "CC_Base_L_Forearm", "L_Forearm", "LeftLowerArm" });
                DumpBoneVector(root.transform, "R", new[] { "CC_Base_R_Clavicle", "R_Clavicle", "RightShoulder" }, new[] { "CC_Base_R_Upperarm", "R_Upperarm", "RightUpperArm" }, new[] { "CC_Base_R_Forearm", "R_Forearm", "RightLowerArm" });

                foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
                {
                    if (renderer is SkinnedMeshRenderer skinned)
                    {
                        Mesh mesh = skinned.sharedMesh;
                        int weights = mesh != null && mesh.boneWeights != null ? mesh.boneWeights.Length : 0;
                        Debug.Log($"[PoseDiag] Skinned renderer={skinned.name} rootBone={(skinned.rootBone ? skinned.rootBone.name : "NULL")} bones={(skinned.bones != null ? skinned.bones.Length : 0)} weights={weights}");
                    }
                    else
                    {
                        Debug.Log($"[PoseDiag] Static renderer={renderer.GetType().Name} name={renderer.name} parent={renderer.transform.parent?.name}");
                    }
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void AssertRiggedStoryWalkerUsesRiggedSkeleton(string id)
        {
            string riggedPath = MigrationRiggedCharacterImport.RiggedPath(id);
            if (riggedPath == null)
            {
                throw new Exception($"{id} should have a rigged FBX source for this test.");
            }

            GameObject root = Spawn(id);
            try
            {
                if (root.GetComponent<MigrationProceduralWalker>() == null)
                {
                    throw new Exception($"{id} story walker should have MigrationProceduralWalker.");
                }

                if (Find(root.transform, "CC_Base_Hip") == null)
                {
                    throw new Exception($"{id} story walker should use the Rigged FBX CC_Base skeleton.");
                }

                Transform model = root.transform.childCount > 0 ? root.transform.GetChild(0) : null;
                if (model == null)
                {
                    throw new Exception($"{id} story walker should instantiate a model child.");
                }

                float x = NormalizeAngle(model.localEulerAngles.x);
                if (Mathf.Abs(x - 90f) > 0.5f)
                {
                    throw new Exception($"{id} rigged model child should carry the +90 X axis correction. Actual localEuler={model.localEulerAngles}.");
                }

                AssertRenderableMaterials(root, id);
                AssertRelaxedArmPose(root.transform, id);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void AssertProceduralWalkerHasCoreBones(string id)
        {
            GameObject root = Spawn(id);
            try
            {
                string[][] aliases =
                {
                    new[] { "CC_Base_Hip", "Hip", "Hips" },
                    new[] { "CC_Base_L_Thigh", "L_Thigh", "LeftUpperLeg" },
                    new[] { "CC_Base_R_Thigh", "R_Thigh", "RightUpperLeg" },
                    new[] { "CC_Base_L_Calf", "L_Calf", "LeftLowerLeg" },
                    new[] { "CC_Base_R_Calf", "R_Calf", "RightLowerLeg" },
                    new[] { "CC_Base_L_Upperarm", "L_Upperarm", "LeftUpperArm" },
                    new[] { "CC_Base_R_Upperarm", "R_Upperarm", "RightUpperArm" },
                };

                foreach (string[] group in aliases)
                {
                    if (FindAny(root.transform, group) == null)
                    {
                        throw new Exception($"{id} story walker is missing procedural bone aliases: {string.Join("/", group)}.");
                    }
                }

                AssertRenderableMaterials(root, id);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static GameObject Spawn(string id)
        {
            GameObject root = MigrationWalkingNpcs.SpawnWalkingCharacter(
                id,
                null,
                Vector3.zero,
                new[] { Vector3.zero, new Vector3(1f, 0f, 0f) },
                0.5f,
                1.62f);

            if (root == null)
            {
                throw new Exception($"{id} story walker did not spawn.");
            }

            return root;
        }

        private static void AssertBakedStorySource(string key)
        {
            string modelPath = $"Assets/RealModels/HumanVillageKit/npc/{key}_tokenrig_walk_baked.glb";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (prefab == null)
            {
                throw new Exception($"{key} should keep a real token-rig baked walk GLB.");
            }

            bool hasClip = false;
            foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(modelPath))
            {
                if (asset is AnimationClip clip && !clip.name.StartsWith("__preview"))
                {
                    hasClip = true;
                    break;
                }
            }

            if (!hasClip)
            {
                throw new Exception($"{key} baked GLB should include a real walk AnimationClip.");
            }
        }

        private static void AssertHumanVillageStoryCullKeepsOnlyFixedBakedCharacters()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Transform settlement = new GameObject("Settlement").transform;

            Type builder = typeof(MigrationHumanVillageBuilder);
            BindingFlags flags = BindingFlags.Static | BindingFlags.NonPublic;
            FieldInfo rootField = builder.GetField("_root", flags);
            FieldInfo rngField = builder.GetField("_rng", flags);
            MethodInfo buildStoryNpcs = builder.GetMethod("BuildStoryNpcs", flags);
            if (rootField == null || rngField == null || buildStoryNpcs == null)
            {
                throw new Exception("Human Village story NPC builder internals changed; update the cull smoke test.");
            }

            rootField.SetValue(null, settlement);
            rngField.SetValue(null, new System.Random(1234));
            buildStoryNpcs.Invoke(null, null);

            Transform story = settlement.Find("StoryNpcs");
            if (story == null)
            {
                throw new Exception("Human Village should create a StoryNpcs group.");
            }

            var expected = FixedHumanVillageStoryCharacters();

            var actual = new HashSet<string>();
            foreach (Transform child in story)
            {
                actual.Add(child.name);
                if (!expected.Contains(child.name))
                {
                    throw new Exception("Human Village should not keep unverified story character: " + child.name);
                }
            }

            foreach (string id in expected)
            {
                if (!actual.Contains(id))
                {
                    throw new Exception("Human Village should keep fixed story character: " + id);
                }
            }

            if (actual.Count != expected.Count)
            {
                throw new Exception($"Human Village should keep exactly {expected.Count} fixed story characters, found {actual.Count}.");
            }
        }

        private static void AssertCulledPlayableSceneContents()
        {
            EditorSceneManager.OpenScene("Assets/TouhouMigration/Scenes/HumanVillageVerticalSlice.unity", OpenSceneMode.Single);

            var expected = FixedHumanVillageStoryCharacters();
            var actual = new List<string>();
            foreach (GameObject root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            {
                foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name.StartsWith("Villager_"))
                    {
                        throw new Exception("Human Village playable scene should not keep ambient villager: " + t.name);
                    }

                    if (t.name.StartsWith("Walk_"))
                    {
                        throw new Exception("Human Village playable scene should not keep rigged FBX walker: " + t.name);
                    }

                    if (t.name.StartsWith("NPC_"))
                    {
                        throw new Exception("Human Village playable scene should not keep placeholder NPC: " + t.name);
                    }

                    if (t.name.StartsWith("BakedStory_"))
                    {
                        actual.Add(t.name);
                    }
                }
            }

            foreach (string name in actual)
            {
                if (!expected.Contains(name))
                {
                    throw new Exception("Human Village playable scene should not keep unverified walker: " + name);
                }

                GameObject walker = GameObject.Find(name);
                if (walker == null)
                {
                    throw new Exception("Human Village playable scene walker disappeared during pose check: " + name);
                }

                AssertBakedStoryAnimator(walker, name);
            }

            foreach (string name in expected)
            {
                if (!actual.Contains(name))
                {
                    throw new Exception("Human Village playable scene is missing fixed walker: " + name);
                }
            }

            if (actual.Count != expected.Count)
            {
                throw new Exception($"Human Village playable scene should contain exactly {expected.Count} fixed walkers, found {actual.Count}.");
            }
        }

        private static HashSet<string> FixedHumanVillageStoryCharacters()
        {
            return new HashSet<string>
            {
                "BakedStory_reimu",
                "BakedStory_meiling",
                "BakedStory_marisa",
            };
        }

        private static void AssertBakedStoryAnimator(GameObject root, string name)
        {
            // Village NPCs walk via a real humanoid-retargeted clip on a Unity importer Humanoid avatar
            // (the tokenrig GLB was converted to FBX, minus the stray Icosphere, so the importer's
            // Enforce-T-Pose solver builds a clean avatar — see MigrationHumanoidRetargetNpcs).
            Animator animator = root.GetComponentInChildren<Animator>(true);
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                throw new Exception($"{name} should drive its walk with an Animator + retarget controller.");
            }
            if (animator.avatar == null || !animator.avatar.isValid || !animator.avatar.isHuman)
            {
                throw new Exception($"{name} should use a valid Humanoid avatar for clean retargeting.");
            }

            AssertRenderableMaterials(root, name);
        }

        private static Transform FindAny(Transform root, string[] names)
        {
            foreach (string name in names)
            {
                Transform t = Find(root, name);
                if (t != null) return t;
            }

            return null;
        }

        private static void AssertRelaxedArmPose(Transform root, string id)
        {
            AssertShoulderDrops(root, id, new[] { "CC_Base_L_Clavicle", "L_Clavicle", "LeftShoulder" }, new[] { "CC_Base_L_Upperarm", "L_Upperarm", "LeftUpperArm" });
            AssertShoulderDrops(root, id, new[] { "CC_Base_R_Clavicle", "R_Clavicle", "RightShoulder" }, new[] { "CC_Base_R_Upperarm", "R_Upperarm", "RightUpperArm" });
            AssertUpperArmDrops(root, id, new[] { "CC_Base_L_Upperarm", "L_Upperarm", "LeftUpperArm" }, new[] { "CC_Base_L_Forearm", "L_Forearm", "LeftLowerArm" });
            AssertUpperArmDrops(root, id, new[] { "CC_Base_R_Upperarm", "R_Upperarm", "RightUpperArm" }, new[] { "CC_Base_R_Forearm", "R_Forearm", "RightLowerArm" });
        }

        private static void AssertShoulderDrops(Transform root, string id, string[] shoulderNames, string[] upperNames)
        {
            Transform shoulder = FindAny(root, shoulderNames);
            Transform upper = FindAny(root, upperNames);
            if (shoulder == null || upper == null)
            {
                throw new Exception($"{id} is missing shoulder/upper-arm bones for pose check.");
            }

            Vector3 shoulderVector = (upper.position - shoulder.position).normalized;
            if (shoulderVector.y > -0.90f)
            {
                throw new Exception($"{id} still reads as T-pose: shoulder sleeve is not dropped enough, vector={shoulderVector}.");
            }
        }

        private static void AssertUpperArmDrops(Transform root, string id, string[] upperNames, string[] forearmNames)
        {
            Transform upper = FindAny(root, upperNames);
            Transform forearm = FindAny(root, forearmNames);
            if (upper == null || forearm == null)
            {
                throw new Exception($"{id} is missing upper-arm/forearm bones for pose check.");
            }

            Vector3 upperArmVector = (forearm.position - upper.position).normalized;
            if (upperArmVector.y > -0.90f)
            {
                throw new Exception($"{id} still reads as T-pose: upper arm is not dropped enough, vector={upperArmVector}.");
            }
        }

        private static void DumpBoneVector(Transform root, string side, string[] upperNames, string[] forearmNames, string[] handNames)
        {
            Transform upper = FindAny(root, upperNames);
            Transform forearm = FindAny(root, forearmNames);
            Transform hand = FindAny(root, handNames);
            Vector3 upperVector = upper != null && forearm != null ? (forearm.position - upper.position).normalized : Vector3.zero;
            Vector3 forearmVector = forearm != null && hand != null ? (hand.position - forearm.position).normalized : Vector3.zero;
            Debug.Log($"[PoseDiag] {side} upper={(upper ? upper.name : "NULL")} forearm={(forearm ? forearm.name : "NULL")} hand={(hand ? hand.name : "NULL")} upperVec={upperVector} forearmVec={forearmVector}");
        }

        private static Transform Find(Transform root, string name)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == name) return t;
            }

            return null;
        }

        private static void AssertRenderableMaterials(GameObject root, string id)
        {
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                foreach (Material material in renderer.sharedMaterials)
                {
                    if (material == null || material.shader == null)
                    {
                        throw new Exception($"{id} has a renderer with a missing material or shader.");
                    }

                    string shaderName = material.shader.name;
                    if (shaderName == "Hidden/InternalErrorShader" || shaderName == "Standard")
                    {
                        throw new Exception($"{id} uses an unsupported runtime material shader: {shaderName}.");
                    }
                }
            }
        }

        private static float NormalizeAngle(float degrees)
        {
            degrees %= 360f;
            if (degrees < 0f) degrees += 360f;
            return degrees;
        }
    }
}
