using System;
using System.Reflection;
using TouhouMigration.Runtime.Combat;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    public static class EnemyAnimationPrefabSmokeTests
    {
        private const string AnimationSourceTypeName = "TouhouMigration.Runtime.Combat.MigrationEnemyAnimationSource, Assembly-CSharp";
        private const string EnemyPrefabsRoot = "Assets/TouhouMigration/Prefabs/Enemies";
        private const string EnemyAnimationsRoot = "Assets/TouhouMigration/Animations/Enemies";

        private static readonly RepresentativeAnimationExpectation[] RepresentativeExpectations =
        {
            new RepresentativeAnimationExpectation(
                "bat",
                "Bat@Idle.fbx",
                "Bat@Fly Forward In Place.fbx",
                "Bat@Bite Attack.fbx",
                "Bat@Projectile Attack.fbx",
                "Bat@Take Damage.fbx",
                "Bat@Die.fbx"),
            new RepresentativeAnimationExpectation(
                "spider",
                "Spider@Idle.fbx",
                "Spider@Crawl Forward Slow In Place.fbx",
                "Spider@Bite Attack.fbx",
                "Spider@Projectile Attack.fbx",
                "Spider@Take Damage.fbx",
                "Spider@Die.fbx"),
            new RepresentativeAnimationExpectation(
                "fungi",
                "Fungi@Idle.fbx",
                "Fungi@Walk Forward In Place.fbx",
                "Fungi@Stab Attack.fbx",
                "Fungi@Projectile Attack.fbx",
                "Fungi@Take Damage.fbx",
                "Fungi@Die.fbx")
        };

        [MenuItem("Touhou Migration/Tests/Run Enemy Animation Prefab Smoke Tests")]
        public static void RunAll()
        {
            TestEnemyAnimationBuilderEntryPointExists();
            TestRepresentativeAnimationAssetsArePromotedAndConfiguredGeneric();
            TestRepresentativePrefabsHaveAnimatorControllersAndSourceMetadata();
            TestMissingVampireSceneKeepsAnimationFallback();
        }

        private static void TestEnemyAnimationBuilderEntryPointExists()
        {
            Type builderType = RequiredType("TouhouMigration.Editor.TouhouMigrationProjectBuilder, Assembly-CSharp-Editor");
            MethodInfo method = builderType.GetMethod("BuildEnemyAnimationControllers", BindingFlags.Static | BindingFlags.Public);
            AssertEqual(true, method != null, "Builder should expose a focused batchmode entry point for enemy animation import/controller generation.");
        }

        private static void TestRepresentativeAnimationAssetsArePromotedAndConfiguredGeneric()
        {
            foreach (RepresentativeAnimationExpectation expectation in RepresentativeExpectations)
            {
                AssertGenericAnimationClip(expectation.Id, expectation.IdleFileName);
                AssertGenericAnimationClip(expectation.Id, expectation.MoveFileName);
                AssertGenericAnimationClip(expectation.Id, expectation.AttackFileName);
                AssertGenericAnimationClip(expectation.Id, expectation.ProjectileFileName);
                AssertGenericAnimationClip(expectation.Id, expectation.TakeDamageFileName);
                AssertGenericAnimationClip(expectation.Id, expectation.DieFileName);

                string controllerPath = ControllerPath(expectation.Id);
                AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
                AssertEqual(true, controller != null, $"Enemy animator controller should exist for '{expectation.Id}' at {controllerPath}.");
                AssertEqual(true, ControllerHasState(controller, "Idle"), $"Enemy controller for '{expectation.Id}' should expose Idle state.");
                AssertEqual(true, ControllerHasState(controller, "Move"), $"Enemy controller for '{expectation.Id}' should expose Move state.");
                AssertEqual(true, ControllerHasState(controller, "Attack"), $"Enemy controller for '{expectation.Id}' should expose Attack state.");
                AssertEqual(true, ControllerHasState(controller, "Projectile"), $"Enemy controller for '{expectation.Id}' should expose Projectile state.");
                AssertEqual(true, ControllerHasState(controller, "TakeDamage"), $"Enemy controller for '{expectation.Id}' should expose TakeDamage state.");
                AssertEqual(true, ControllerHasState(controller, "Die"), $"Enemy controller for '{expectation.Id}' should expose Die state.");
            }
        }

        private static void TestRepresentativePrefabsHaveAnimatorControllersAndSourceMetadata()
        {
            Type animationSourceType = RequiredType(AnimationSourceTypeName);
            foreach (RepresentativeAnimationExpectation expectation in RepresentativeExpectations)
            {
                GameObject prefab = RequiredPrefab(expectation.Id);
                object animationSource = RequiredComponent(prefab, animationSourceType, $"Prefab '{expectation.Id}' should record its migrated animation source.");
                AssertEqual(expectation.Id, GetProperty<string>(animationSource, "VariantId"), $"Animation source should preserve variant id for '{expectation.Id}'.");
                AssertEqual(ControllerPath(expectation.Id), GetProperty<string>(animationSource, "AnimatorControllerAssetPath"), $"Animation source should record controller path for '{expectation.Id}'.");
                AssertEqual(false, GetProperty<bool>(animationSource, "UsesFallbackAnimations"), $"Existing monster scene '{expectation.Id}' should not use fallback animations.");
                AssertEqual(true, GetProperty<bool>(animationSource, "HasIdle"), $"Animation source should record idle clip for '{expectation.Id}'.");
                AssertEqual(true, GetProperty<bool>(animationSource, "HasMove"), $"Animation source should record move clip for '{expectation.Id}'.");
                AssertEqual(true, GetProperty<bool>(animationSource, "HasAttack"), $"Animation source should record attack clip for '{expectation.Id}'.");
                AssertEqual(true, GetProperty<bool>(animationSource, "HasProjectile"), $"Animation source should record projectile/cast clip for '{expectation.Id}'.");
                AssertEqual(true, GetProperty<bool>(animationSource, "HasTakeDamage"), $"Animation source should record take-damage clip for '{expectation.Id}'.");
                AssertEqual(true, GetProperty<bool>(animationSource, "HasDie"), $"Animation source should record die clip for '{expectation.Id}'.");

                Transform visual = prefab.transform.Find("Visual");
                AssertEqual(true, visual != null, $"Prefab '{expectation.Id}' should have a Visual child before animation binding.");
                Animator animator = visual.GetComponentInChildren<Animator>(true);
                AssertEqual(true, animator != null, $"Prefab '{expectation.Id}' should mount an Animator on its imported visual model.");
                string runtimeControllerPath = AssetDatabase.GetAssetPath(animator.runtimeAnimatorController);
                AssertEqual(ControllerPath(expectation.Id), runtimeControllerPath, $"Prefab '{expectation.Id}' Animator should use the generated controller.");
            }
        }

        private static void TestMissingVampireSceneKeepsAnimationFallback()
        {
            Type animationSourceType = RequiredType(AnimationSourceTypeName);
            GameObject prefab = RequiredPrefab("vampire");
            object animationSource = RequiredComponent(prefab, animationSourceType, "Vampire prefab should record animation fallback metadata.");
            AssertEqual("vampire", GetProperty<string>(animationSource, "VariantId"), "Vampire animation source should preserve variant id.");
            AssertEqual(true, GetProperty<bool>(animationSource, "UsesFallbackAnimations"), "Vampire should explicitly use fallback animations until a formal source scene exists.");
            AssertEqual(string.Empty, GetProperty<string>(animationSource, "AnimatorControllerAssetPath"), "Vampire fallback should not pretend to have an animation controller.");
        }

        private static void AssertGenericAnimationClip(string monsterId, string fileName)
        {
            string path = ClipPath(monsterId, fileName);
            ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
            AssertEqual(true, importer != null, $"Animation FBX should be promoted for '{monsterId}' at {path}.");
            AssertEqual(true, importer.importAnimation, $"Animation importer should import animation for '{monsterId}' clip '{fileName}'.");
            AssertEqual(ModelImporterAnimationType.Generic, importer.animationType, $"Monster animation clip '{fileName}' should use Generic import, not Humanoid.");

            AnimationClip[] clips = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>();
            AssertEqual(true, clips.Length > 0, $"Animation FBX should expose at least one AnimationClip for '{monsterId}' clip '{fileName}'.");
        }

        private static bool ControllerHasState(AnimatorController controller, string stateName)
        {
            foreach (ChildAnimatorState state in controller.layers[0].stateMachine.states)
            {
                if (state.state.name == stateName)
                {
                    return true;
                }
            }

            return false;
        }

        private static GameObject RequiredPrefab(string monsterId)
        {
            string path = $"{EnemyPrefabsRoot}/MigrationEnemy_{ToPascal(monsterId)}.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                throw new Exception($"Missing enemy prefab for '{monsterId}' at {path}.");
            }

            return prefab;
        }

        private static object RequiredComponent(GameObject gameObject, Type componentType, string message)
        {
            object component = gameObject.GetComponent(componentType);
            AssertEqual(true, component != null, message);
            return component;
        }

        private static string ClipPath(string monsterId, string fileName)
        {
            return $"{EnemyAnimationsRoot}/{ToPascal(monsterId)}/Clips/{fileName}";
        }

        private static string ControllerPath(string monsterId)
        {
            string pascal = ToPascal(monsterId);
            return $"{EnemyAnimationsRoot}/{pascal}/{pascal}_Enemy.controller";
        }

        private static string ToPascal(string value)
        {
            string[] parts = value.Split('_', StringSplitOptions.RemoveEmptyEntries);
            string result = string.Empty;
            foreach (string part in parts)
            {
                result += char.ToUpperInvariant(part[0]) + part.Substring(1);
            }

            return result;
        }

        private static Type RequiredType(string typeName)
        {
            Type type = Type.GetType(typeName);
            if (type == null)
            {
                throw new Exception($"Missing required type: {typeName}");
            }

            return type;
        }

        private static T GetProperty<T>(object target, string propertyName)
        {
            PropertyInfo property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            if (property == null)
            {
                throw new Exception($"Missing property {target.GetType().FullName}.{propertyName}");
            }

            return (T)property.GetValue(target);
        }

        private static void AssertEqual<T>(T expected, T actual, string message)
        {
            if (!Equals(expected, actual))
            {
                throw new Exception($"{message} Expected: {expected}. Actual: {actual}.");
            }
        }

        private readonly struct RepresentativeAnimationExpectation
        {
            public RepresentativeAnimationExpectation(
                string id,
                string idleFileName,
                string moveFileName,
                string attackFileName,
                string projectileFileName,
                string takeDamageFileName,
                string dieFileName)
            {
                Id = id;
                IdleFileName = idleFileName;
                MoveFileName = moveFileName;
                AttackFileName = attackFileName;
                ProjectileFileName = projectileFileName;
                TakeDamageFileName = takeDamageFileName;
                DieFileName = dieFileName;
            }

            public string Id { get; }
            public string IdleFileName { get; }
            public string MoveFileName { get; }
            public string AttackFileName { get; }
            public string ProjectileFileName { get; }
            public string TakeDamageFileName { get; }
            public string DieFileName { get; }
        }
    }

    internal static class EnemyAnimationSmokeEnumerableExtensions
    {
        public static T[] OfType<T>(this UnityEngine.Object[] source)
        {
            System.Collections.Generic.List<T> results = new System.Collections.Generic.List<T>();
            foreach (UnityEngine.Object item in source)
            {
                if (item is T typed)
                {
                    results.Add(typed);
                }
            }

            return results.ToArray();
        }
    }
}
