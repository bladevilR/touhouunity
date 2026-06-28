using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using TouhouMigration.Runtime.Environment;

namespace TouhouMigration.Editor
{
    /// <summary>
    /// Village NPC walk via PROPER humanoid retarget. The tokenrig GLBs were converted to FBX in Blender
    /// (stripping the stray Icosphere placeholder) so Unity's REAL importer humanoid solver (Enforce T-Pose
    /// + axis calibration) builds the avatar — this is what fixed the "traffic-cop / arms-out" that my
    /// hand-built AvatarBuilder produced (muscles retargeted fine; only the avatar's rest calibration was
    /// wrong). Any humanoid clip now retargets cleanly. Textures are rebound from the source GLB (FBX shows
    /// white in the built-in pipeline). Apply:
    ///   -executeMethod TouhouMigration.Editor.MigrationHumanoidRetargetNpcs.ApplyToScene
    /// </summary>
    public static class MigrationHumanoidRetargetNpcs
    {
        private const string FbxDir = "Assets/RealModels/HumanoidNpc/";
        private const string GlbDir = "Assets/RealModels/HumanVillageKit/npc/";
        // Walk clip source (Mixamo Female Walk — natural carry + good leg lift on the chibi; swap freely now
        // that humanoid normalization works: HumanF / Quaternius Walk_Loop / etc.)
        private const string WalkFbx = "Assets/RealModels/WalkClips/FemaleWalk.fbx";
        private const string AnimDir = "Assets/TouhouMigration/Animations/Characters/Villagers/";
        private const string CtrlPath = AnimDir + "humanoid_femalewalk.controller";
        private const float TargetHeight = 1.7f;
        public static float ModelYaw = 0f;     // FBX reimu faces +Z at identity; tune in-scene if needed
        private const float WalkSpeed = 0.95f;

        private static readonly string[] CharKeys = { "reimu", "meiling", "marisa_blackwhite" };
        private static string Fbx(string k) => FbxDir + k + ".fbx";
        private static string Glb(string k) => GlbDir + k + "_tokenrig_walk_baked.glb";

        public static void ApplyToScene()
        {
            SetupFbxHumanoid();
            AnimatorController ctrl = EnsureWalkController();

            UnityEngine.SceneManagement.Scene scene = EditorSceneManager.OpenScene(
                "Assets/TouhouMigration/Scenes/HumanVillageVerticalSlice.unity", OpenSceneMode.Single);

            // collect NPC roots FIRST (we destroy children below, which would invalidate a live iterator)
            var npcRoots = new List<Transform>();
            foreach (GameObject rootGo in scene.GetRootGameObjects())
                foreach (Transform t in rootGo.GetComponentsInChildren<Transform>(true))
                    if (t.name.StartsWith("BakedStory_") || t.name.StartsWith("Villager_")) npcRoots.Add(t);

            int done = 0;
            foreach (Transform t in npcRoots)
                {
                    string key = KeyForRoot(t.name);
                    if (key == null) continue;
                    GameObject fbxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Fbx(key));
                    if (fbxPrefab == null) { Debug.LogWarning("[HRetarget] missing fbx " + key); continue; }

                    // carry over patrol/ground, then strip old model + walkers
                    Vector3[] wp = null; float gy = 0f; Vector3 worldPos = t.position; Quaternion worldRot = t.rotation;
                    var proc = t.GetComponent<MigrationProceduralWalker>();
                    if (proc != null) { wp = proc.waypoints; gy = proc.groundY; Object.DestroyImmediate(proc); }
                    var oldW = t.GetComponent<MigrationNpcWalker>();
                    if (oldW != null) { if (wp == null) wp = oldW.waypoints; gy = oldW.groundY; Object.DestroyImmediate(oldW); }
                    for (int c = t.childCount - 1; c >= 0; c--) Object.DestroyImmediate(t.GetChild(c).gameObject);

                    // new FBX model child
                    GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(fbxPrefab, t);
                    model.transform.localPosition = Vector3.zero;
                    model.transform.localRotation = Quaternion.Euler(0f, ModelYaw, 0f);
                    model.transform.localScale = Vector3.one;
                    var rs = model.GetComponentsInChildren<Renderer>();
                    if (rs.Length > 0)
                    {
                        Bounds b = rs[0].bounds; for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
                        model.transform.localScale = Vector3.one * (TargetHeight / Mathf.Max(b.size.y, 0.01f));
                    }
                    BindTexture(model, key);

                    var anim = model.GetComponentInChildren<Animator>(true);
                    if (anim == null) anim = model.AddComponent<Animator>();
                    // importer humanoid avatar comes with the FBX prefab; ensure it's set
                    Avatar av = FirstAvatar(Fbx(key));
                    if (av != null) anim.avatar = av;
                    anim.runtimeAnimatorController = ctrl;
                    anim.applyRootMotion = false;
                    anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;

                    var walker = t.gameObject.AddComponent<MigrationNpcWalker>();
                    walker.waypoints = wp; walker.speed = WalkSpeed; walker.groundY = gy;
                    t.position = worldPos; t.rotation = worldRot;

                    Debug.Log($"[HRetarget] {t.name} -> FBX humanoid '{(av ? av.name : "?")}', yaw {ModelYaw}");
                    done++;
                }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[HRetarget] applied to {done} NPCs");
        }

        public static void SetupFbxHumanoid()
        {
            foreach (string k in CharKeys) ConfigHumanoid(Fbx(k));
            ConfigHumanoid(WalkFbx);
        }

        private static void ConfigHumanoid(string path)
        {
            var imp = AssetImporter.GetAtPath(path) as ModelImporter;
            if (imp == null) { Debug.LogWarning("[HRetarget] no importer " + path); return; }
            bool dirty = false;
            if (imp.animationType != ModelImporterAnimationType.Human)
            { imp.animationType = ModelImporterAnimationType.Human; imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel; dirty = true; }
            var clips = imp.defaultClipAnimations;
            if (clips.Length > 0 && !clips[0].loopTime) { clips[0].loopTime = true; imp.clipAnimations = clips; dirty = true; }
            if (dirty) { EditorUtility.SetDirty(imp); imp.SaveAndReimport(); }
        }

        private static void BindTexture(GameObject model, string key)
        {
            Texture2D tex = null;
            foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(Glb(key)))
                if (o is Texture2D tt && !tt.name.Contains("Normal") && !tt.name.Contains("normal")) { tex = tt; break; }
            if (tex == null) { Debug.LogWarning("[HRetarget] no GLB texture for " + key); return; }
            Material mat = MigrationUrpMaterialUtility.CreatePreferredLitMaterial(key + "_npc", Color.white);
            MigrationUrpMaterialUtility.SetBaseTexture(mat, tex);
            MigrationUrpMaterialUtility.SetSmoothness(mat, 0.1f);
            foreach (var r in model.GetComponentsInChildren<Renderer>(true))
            {
                var ms = new Material[r.sharedMaterials.Length];
                for (int i = 0; i < ms.Length; i++) ms[i] = mat;
                r.sharedMaterials = ms;
            }
        }

        private static AnimatorController EnsureWalkController()
        {
            var existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(CtrlPath);
            if (existing != null) return existing;
            ConfigHumanoid(WalkFbx);
            AnimationClip walk = null;
            foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(WalkFbx))
                if (o is AnimationClip c && !c.name.StartsWith("__preview")) { walk = c; break; }
            Directory.CreateDirectory(AnimDir);
            return AnimatorController.CreateAnimatorControllerAtPathWithClip(CtrlPath, walk);
        }

        private static string KeyForRoot(string n)
        {
            if (n.Contains("marisa")) return "marisa_blackwhite";
            if (n.Contains("meiling")) return "meiling";
            if (n.Contains("reimu")) return "reimu";
            return null;
        }
        private static Avatar FirstAvatar(string p)
        { foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(p)) if (o is Avatar a) return a; return null; }
    }
}
