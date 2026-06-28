using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using TouhouMigration.Runtime.Environment;

namespace TouhouMigration.Editor
{
    /// <summary>
    /// Ambient walking villagers built from the source's REAL token-rig characters with a baked walk
    /// animation (reimu / meiling / marisa _tokenrig_walk_baked.glb — skinned, textured, animated).
    /// Each character's baked walk is copied into a looping clip + a one-state AnimatorController; a
    /// MigrationNpcWalker patrols the streets so the feet travel. The baked rig faces +X, so the model
    /// is yawed +90 under a movement-facing root. For the headless still each villager is posed
    /// mid-stride via SampleAnimation (the editor Animator doesn't tick in batchmode).
    /// </summary>
    public static class MigrationVillageNpcs
    {
        private const string NpcDir = "Assets/RealModels/HumanVillageKit/npc/";
        private const string AnimDir = "Assets/TouhouMigration/Animations/Characters/Villagers/";
        private const float TargetHeight = 1.7f;
        private const float ModelForwardYaw = 90f; // baked tokenrig faces +X → +90 aligns it to the root's +Z

        private static readonly string[] CharKeys = { "reimu", "meiling", "marisa_blackwhite" };
        private static string Glb(string k) => NpcDir + k + "_tokenrig_walk_baked.glb";
        private static string Ctrl(string k) => AnimDir + k + "_walk.controller";
        private static string Anim(string k) => AnimDir + k + "_walk.anim";
        private static string AvatarPath(string k) => AnimDir + k + "_avatar.asset";

        public static void SetupAssets()
        {
            Directory.CreateDirectory(AnimDir);
            foreach (string k in CharKeys)
            {
                if (File.Exists(Ctrl(k))) continue;
                AnimationClip src = LoadClip(Glb(k));
                if (src == null) { Debug.LogWarning("[Villagers] no baked walk clip in " + k); continue; }
                // GLB clips can't be looped via importer; copy into an editable looping clip asset.
                AnimationClip loop = new AnimationClip();
                EditorUtility.CopySerialized(src, loop);
                AnimationClipSettings s = AnimationUtility.GetAnimationClipSettings(loop);
                s.loopTime = true;
                AnimationUtility.SetAnimationClipSettings(loop, s);
                AssetDatabase.CreateAsset(loop, Anim(k));
                AnimatorController.CreateAnimatorControllerAtPathWithClip(Ctrl(k), loop);
                Debug.Log("[Villagers] controller built for " + k);
            }
            AssetDatabase.SaveAssets();
        }

        private static AnimationClip LoadClip(string glb)
        {
            foreach (Object a in AssetDatabase.LoadAllAssetsAtPath(glb))
                if (a is AnimationClip c && !c.name.StartsWith("__preview")) return c;
            return null;
        }

        // One-shot repair for the ALREADY-BUILT human-village scene: every villager Animator gets its
        // missing Avatar (+ AlwaysAnimate) so the walk clip drives the bones in Play mode. Logs each
        // villager's before/after avatar so the NULL-avatar root cause is confirmed on the spot.
        // -executeMethod TouhouMigration.Editor.MigrationVillageNpcs.FixVillagerAnimators
        public static void FixVillagerAnimators()
        {
            Directory.CreateDirectory(AnimDir);
            UnityEngine.SceneManagement.Scene scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(
                "Assets/TouhouMigration/Scenes/HumanVillageVerticalSlice.unity",
                UnityEditor.SceneManagement.OpenSceneMode.Single);

            // The glb ships NO Avatar sub-asset, so build + save one generic avatar per character from a
            // fresh bind-pose instance (this is what glTFast does at runtime). A generic Animator without
            // an avatar never binds/drives the skeleton, so it stays frozen in bind pose.
            var avByKey = BuildVillagerAvatars();

            int fixedCount = 0, total = 0;
            foreach (GameObject root in scene.GetRootGameObjects())
                foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                {
                    if (!t.name.StartsWith("Villager_")) continue;
                    total++;
                    string key = null;
                    foreach (string k in CharKeys) if (t.name.EndsWith("_" + k)) { key = k; break; }
                    if (key == null) { Debug.LogWarning("[FixVillager] unknown char in " + t.name); continue; }
                    Animator anim = t.GetComponentInChildren<Animator>();
                    if (anim == null) { Debug.LogWarning("[FixVillager] NO Animator under " + t.name); continue; }
                    string before = "avatar=" + (anim.avatar ? anim.avatar.name : "NULL") + " ctrl=" + (anim.runtimeAnimatorController ? anim.runtimeAnimatorController.name : "NULL");
                    if (avByKey.TryGetValue(key, out Avatar a) && a != null) anim.avatar = a;
                    if (anim.runtimeAnimatorController == null) anim.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<AnimatorController>(Ctrl(key));
                    anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                    anim.applyRootMotion = false;
                    Debug.Log("[FixVillager] " + t.name + " BEFORE " + before + " -> AFTER avatar=" + (anim.avatar ? anim.avatar.name : "NULL"));
                    fixedCount++;
                }
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
            Debug.Log("[FixVillager] DONE fixed " + fixedCount + "/" + total + " villagers");
        }

        // Builds (once) and returns a saved generic Avatar per character. glTFast does NOT bake an Avatar
        // sub-asset into the glb, so we construct one with AvatarBuilder from a fresh prefab instance.
        private static Dictionary<string, Avatar> BuildVillagerAvatars()
        {
            Directory.CreateDirectory(AnimDir);
            var map = new Dictionary<string, Avatar>();
            foreach (string k in CharKeys)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Glb(k));
                if (prefab == null) { Debug.LogWarning("[Villagers] missing model " + k); continue; }
                GameObject tmp = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                Avatar av = AvatarBuilder.BuildGenericAvatar(tmp, "");
                Object.DestroyImmediate(tmp);
                if (av == null || !av.isValid) { Debug.LogWarning("[Villagers] avatar build FAILED for " + k); continue; }
                av.name = k + "_avatar";
                Avatar existing = AssetDatabase.LoadAssetAtPath<Avatar>(AvatarPath(k));
                if (existing != null) { EditorUtility.CopySerialized(av, existing); Object.DestroyImmediate(av); av = existing; }
                else AssetDatabase.CreateAsset(av, AvatarPath(k));
                map[k] = av;
                Debug.Log("[Villagers] generic avatar built for " + k + " valid=" + av.isValid);
            }
            AssetDatabase.SaveAssets();
            return map;
        }

        public static Transform SpawnVillagers(Transform parent, System.Random rng)
        {
            Transform grp = new GameObject("Villagers").transform;
            grp.SetParent(parent);

            // Procedural gait on the token-rig (the baked clip was a weak in-place shuffle; humanoid
            // retarget degenerates on this AI rig → crab walk). MigrationProceduralWalker swings the real
            // leg/arm bones forward-back about the travel axis and handles locomotion, so stride matches
            // travel by construction. Model keeps the +90 facing fixup; no Animator controller (would fight it).
            var prefabs = new List<GameObject>();
            foreach (string k in CharKeys)
            {
                GameObject p = AssetDatabase.LoadAssetAtPath<GameObject>(Glb(k));
                if (p == null) { Debug.LogWarning("[Villagers] missing model " + k); continue; }
                prefabs.Add(p);
            }
            if (prefabs.Count == 0) { Debug.LogWarning("[Villagers] no character prefabs"); return grp; }

            // Patrol segments along the streets (z=14 main, z=47 residential) + plaza.
            (float z, float x0, float x1)[] lanes =
            {
                (14f, -54f, 54f), (14f, 52f, -52f), (47f, -52f, 50f),
                (12f, -18f, 26f), (16f, 32f, -34f), (45f, 30f, -28f),
            };

            int n = 12;
            for (int i = 0; i < n; i++)
            {
                int ci = i % prefabs.Count;
                var lane = lanes[i % lanes.Length];
                float ax = Mathf.Lerp(lane.x0, lane.x1, (float)rng.NextDouble());
                float bx = Mathf.Lerp(lane.x0, lane.x1, (float)rng.NextDouble());
                float zoff = (float)(rng.NextDouble() * 2.0 - 1.0);
                Vector3 a = new Vector3(ax, 0f, lane.z + zoff);
                Vector3 b = new Vector3(bx, 0f, lane.z - zoff);

                GameObject root = new GameObject("Villager_" + i + "_" + CharKeys[ci]);
                root.transform.SetParent(grp);

                GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(prefabs[ci], root.transform);
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.identity;
                model.transform.localScale = Vector3.one;
                float nh = Mathf.Max(MeasureHeight(model), 0.01f);
                float h = TargetHeight * (0.94f + (float)rng.NextDouble() * 0.12f);
                model.transform.localScale = Vector3.one * (h / nh);
                model.transform.localRotation = Quaternion.Euler(0f, ModelForwardYaw, 0f); // token-rig faces +X

                Animator anim = model.GetComponent<Animator>();
                if (anim != null) anim.runtimeAnimatorController = null; // procedural walker drives the bones

                MigrationProceduralWalker walker = root.AddComponent<MigrationProceduralWalker>();
                walker.waypoints = new[] { a, b };
                walker.speed = 0.9f + (float)rng.NextDouble() * 0.25f;
                walker.groundY = 0f;
                root.transform.position = a;
                root.transform.rotation = Quaternion.LookRotation((b - a).normalized, Vector3.up);
            }
            Debug.Log("[Villagers] spawned " + n + " real-character walking villagers (" + prefabs.Count + " types)");
            return grp;
        }

        public static Transform SpawnFixedBakedStoryCharacters(Transform parent, System.Random rng)
        {
            Transform grp = new GameObject("StoryNpcs").transform;
            grp.SetParent(parent);

            // Procedural gait on the token-rig (humanoid retarget degenerates on this AI rig → crab walk).
            (string key, string name, float x, float z, float phase)[] cast =
            {
                ("reimu", "reimu", 0f, 15f, 0.18f),
                ("meiling", "meiling", 9f, 18f, 0.43f),
                ("marisa_blackwhite", "marisa", 19f, 20f, 0.68f),
            };

            int ok = 0;
            foreach (var c in cast)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Glb(c.key));
                if (prefab == null)
                {
                    Debug.LogWarning("[BakedStory] missing model for " + c.key);
                    continue;
                }

                const float half = 2.4f;
                Vector3 a = new Vector3(c.x - half, 0f, c.z);
                Vector3 b = new Vector3(c.x + half, 0f, c.z);
                GameObject root = new GameObject("BakedStory_" + c.name);
                root.transform.SetParent(grp);

                GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root.transform);
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.identity;
                model.transform.localScale = Vector3.one;
                float nativeHeight = Mathf.Max(MeasureHeight(model), 0.01f);
                model.transform.localScale = Vector3.one * (TargetHeight / nativeHeight);
                model.transform.localRotation = Quaternion.Euler(0f, ModelForwardYaw, 0f); // token-rig faces +X

                Animator anim = model.GetComponent<Animator>();
                if (anim != null) anim.runtimeAnimatorController = null; // procedural walker drives the bones

                MigrationProceduralWalker walker = root.AddComponent<MigrationProceduralWalker>();
                walker.waypoints = new[] { a, b };
                walker.speed = 0.9f + (float)rng.NextDouble() * 0.2f;
                walker.groundY = 0f;
                root.transform.position = a;
                root.transform.rotation = Quaternion.LookRotation((b - a).normalized, Vector3.up);

                ok++;
                Debug.Log("[BakedStory] " + c.name + " using procedural gait");
            }

            Debug.Log("[HumanVillage] fixed baked story NPCs=" + ok + "/" + cast.Length);
            return grp;
        }

        private static float MeasureHeight(GameObject go)
        {
            Renderer[] rs = go.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return 1f;
            Bounds b = rs[0].bounds;
            for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
            return b.size.y;
        }

        // ACTUAL motion verification: side-on burst of one villager sampled ACROSS the walk cycle while
        // translating, so the frame strip shows legs cycling + the body travelling (not one static pose).
        // -executeMethod TouhouMigration.Editor.MigrationVillageNpcs.VerifyWalk
        public static void VerifyWalk()
        {
            SetupAssets();
            UnityEditor.SceneManagement.EditorSceneManager.NewScene(
                UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,
                UnityEditor.SceneManagement.NewSceneMode.Single);

            GameObject sunGo = new GameObject("Sun");
            Light sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional; sun.intensity = 1.15f; sun.color = new Color(1f, 0.96f, 0.86f);
            sunGo.transform.rotation = Quaternion.Euler(45f, 20f, 0f);
            RenderSettings.ambientLight = new Color(0.55f, 0.58f, 0.62f);
            Material sky = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Skybox.mat");
            if (sky != null) RenderSettings.skybox = sky;

            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.transform.localScale = new Vector3(3f, 1f, 3f);
            ground.GetComponent<Renderer>().sharedMaterial = new Material(Shader.Find("Standard")) { color = new Color(0.55f, 0.6f, 0.42f) };

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Glb("reimu"));
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(Anim("reimu"));
            if (prefab == null || clip == null) { Debug.LogError("[VerifyWalk] missing reimu model/clip"); return; }

            GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            model.transform.localScale = Vector3.one;
            model.transform.rotation = Quaternion.identity; model.transform.position = Vector3.zero;
            float nh = Mathf.Max(MeasureHeight(model), 0.01f);
            model.transform.localScale = Vector3.one * (TargetHeight / nh);
            model.transform.rotation = Quaternion.Euler(0f, 90f, 0f); // native rig faces +X → walk toward +X

            GameObject camGo = new GameObject("__cam");
            Camera cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.Skybox; cam.fieldOfView = 42f; cam.farClipPlane = 100f;
            cam.transform.position = new Vector3(0f, 1.25f, -4.6f);
            cam.transform.LookAt(new Vector3(0f, 0.95f, 0f));

            string dir = "Verification/VisualChecks/walkverify";
            System.IO.Directory.CreateDirectory(dir);
            string stamp = System.DateTime.Now.ToString("HHmmss");
            int frames = 6;
            for (int f = 0; f < frames; f++)
            {
                float u = f / (float)(frames - 1);          // 0..1 across the cycle
                clip.SampleAnimation(model, u * Mathf.Max(clip.length, 0.01f));
                model.transform.position = new Vector3(-2.3f + 4.6f * u, 0f, 0f); // travel left→right
                Capture(cam, dir + "/walk_" + stamp + "_" + f + ".png");
            }
            Debug.Log("[VerifyWalk] wrote " + frames + " frames to " + dir + " (" + stamp + ")");
        }

        // Real play-mode motion verification: enter Play with the live scene, capture a fixed-camera
        // burst (recorder quits the editor when done), so batchmode exits on its own.
        // -executeMethod TouhouMigration.Editor.MigrationVillageNpcs.PlayModeVerify   (NO -quit flag)
        public static void PlayModeVerify()
        {
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload | EnterPlayModeOptions.DisableSceneReload;
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(
                "Assets/TouhouMigration/Scenes/HumanVillageVerticalSlice.unity",
                UnityEditor.SceneManagement.OpenSceneMode.Single);
            GameObject go = new GameObject("__PMRecorder");
            MigrationPlayModeRecorder rec = go.AddComponent<MigrationPlayModeRecorder>();
            rec.stamp = System.DateTime.Now.ToString("HHmmss");
            rec.followName = "";
            rec.orbit = false;                            // fixed plaza framing to watch the NPCs walk their patrols
            rec.camPos = new Vector3(-9f, 2.6f, 7f);
            rec.camLook = new Vector3(7f, 1.1f, 16f);
            rec.fov = 56f;
            rec.frames = 14;
            rec.interval = 0.18f;
            EditorApplication.EnterPlaymode();
            Debug.Log("[PMVerify] entering play mode for motion capture");
        }

        // Close-up live play-mode capture of ONE villager (meiling) so the actual Animator-driven walk is
        // visible at limb scale (not a far plaza dot). NO -quit flag; the recorder exits batchmode.
        // -executeMethod TouhouMigration.Editor.MigrationVillageNpcs.PlayModeVerifyNpcCloseup
        public static void PlayModeVerifyNpcCloseup()
        {
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload | EnterPlayModeOptions.DisableSceneReload;
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(
                "Assets/TouhouMigration/Scenes/HumanVillageVerticalSlice.unity",
                UnityEditor.SceneManagement.OpenSceneMode.Single);
            GameObject go = new GameObject("__PMRecorder");
            MigrationPlayModeRecorder rec = go.AddComponent<MigrationPlayModeRecorder>();
            rec.outDir = "Verification/VisualChecks/npccloseup";
            rec.stamp = System.DateTime.Now.ToString("HHmmss");
            rec.followName = "Villager_1_";            // Villager_1_meiling (i=1 -> ci=1 -> meiling)
            rec.followOffset = new Vector3(2.0f, 1.15f, -2.2f);
            rec.orbit = false;
            rec.fov = 36f;
            rec.frames = 14;
            rec.interval = 0.12f;                       // ~1.7s burst -> full walk cycle
            EditorApplication.EnterPlaymode();
            Debug.Log("[NpcCloseup] entering play mode to close-follow Villager_1_meiling");
        }

        private static void Capture(Camera cam, string outPath)
        {
            RenderTexture rt = new RenderTexture(900, 700, 24);
            RenderTexture prev = RenderTexture.active;
            try
            {
                cam.targetTexture = rt; RenderTexture.active = rt; cam.Render();
                Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); tex.Apply();
                System.IO.File.WriteAllBytes(outPath, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
            }
            finally
            {
                cam.targetTexture = null; RenderTexture.active = prev; rt.Release(); Object.DestroyImmediate(rt);
            }
        }
    }
}
