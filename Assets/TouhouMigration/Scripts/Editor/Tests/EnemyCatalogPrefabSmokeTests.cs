using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    public static class EnemyCatalogPrefabSmokeTests
    {
        private const string EnemyCatalogTypeName = "TouhouMigration.Runtime.Combat.MigrationEnemyCatalog, Assembly-CSharp";
        private const string SimpleEnemyControllerTypeName = "TouhouMigration.Runtime.Combat.MigrationSimpleEnemyController, Assembly-CSharp";
        private const string CombatTargetBehaviourTypeName = "TouhouMigration.Runtime.Combat.MigrationCombatTargetBehaviour, Assembly-CSharp";
        private const string CombatLootDropHandlerTypeName = "TouhouMigration.Runtime.Combat.MigrationCombatLootDropHandler, Assembly-CSharp";
        private const string CombatDefeatHandlerTypeName = "TouhouMigration.Runtime.Combat.MigrationCombatDefeatHandler, Assembly-CSharp";
        private const string EnemyPrefabsRoot = "Assets/TouhouMigration/Prefabs/Enemies";

        private static readonly string[] FormalMonsterIds =
        {
            "bat", "bee", "bird", "bumble", "ghost", "phantom", "spook", "sting",
            "fungi", "mushroom", "seed", "shade", "shadow", "sprout", "toadstool",
            "chick", "egg", "fledgling", "spider", "vampire"
        };

        [MenuItem("Touhou Migration/Tests/Run Enemy Catalog Prefab Smoke Tests")]
        public static void RunAll()
        {
            TestEnemyPrefabBuilderEntryPointExists();
            TestEnemyPrefabFolderContainsOnePrefabPerCatalogProfile();
            TestRepresentativePrefabsPreserveFormalProfileShape();
        }

        private static void TestEnemyPrefabBuilderEntryPointExists()
        {
            Type builderType = RequiredType("TouhouMigration.Editor.TouhouMigrationProjectBuilder, Assembly-CSharp-Editor");
            MethodInfo method = builderType.GetMethod("BuildEnemyCatalogPrefabs", BindingFlags.Static | BindingFlags.Public);
            AssertEqual(true, method != null, "Builder should expose a batchmode entry point for enemy catalog prefab generation.");
        }

        private static void TestEnemyPrefabFolderContainsOnePrefabPerCatalogProfile()
        {
            object catalog = Activator.CreateInstance(RequiredType(EnemyCatalogTypeName));
            Invoke(catalog, "LoadGodotDefaults");
            AssertEqual(FormalMonsterIds.Length, GetProperty<int>(catalog, "Count"), "Test ids should match the formal catalog count.");

            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { EnemyPrefabsRoot });
            AssertEqual(FormalMonsterIds.Length, prefabGuids.Length, "Enemy prefab folder should contain one prefab per formal Godot monster.");

            foreach (string monsterId in FormalMonsterIds)
            {
                string path = PrefabPath(monsterId);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                AssertEqual(true, prefab != null, $"Enemy prefab should exist for formal monster '{monsterId}' at {path}.");
                AssertEqual($"MigrationEnemy_{ToPascal(monsterId)}", prefab.name, $"Enemy prefab root should preserve the formal monster id '{monsterId}'.");
            }
        }

        private static void TestRepresentativePrefabsPreserveFormalProfileShape()
        {
            Type controllerType = RequiredType(SimpleEnemyControllerTypeName);
            Type targetType = RequiredType(CombatTargetBehaviourTypeName);
            Type lootType = RequiredType(CombatLootDropHandlerTypeName);
            Type defeatType = RequiredType(CombatDefeatHandlerTypeName);

            GameObject bat = RequiredPrefab("bat");
            object batController = RequiredComponent(bat, controllerType, "Bat prefab should mount the reusable enemy controller.");
            object batTarget = RequiredComponent(bat, targetType, "Bat prefab should mount a combat target.");
            RequiredComponent(bat, lootType, "Bat prefab should mount loot handling.");
            RequiredComponent(bat, defeatType, "Bat prefab should mount defeat handling.");
            AssertEqual("bat", GetProperty<string>(batController, "CurrentVariantId"), "Bat prefab should serialize the formal Godot monster id.");
            AssertApproximately(45f, GetProperty<float>(batTarget, "MaxHp"), 0.001f, "Bat prefab should serialize Godot MonsterDatabase HP.");

            GameObject egg = RequiredPrefab("egg");
            object eggController = RequiredComponent(egg, controllerType, "Egg prefab should mount the reusable enemy controller even though it cannot attack.");
            object eggTarget = RequiredComponent(egg, targetType, "Egg prefab should mount a combat target.");
            AssertEqual("egg", GetProperty<string>(eggController, "CurrentVariantId"), "Egg prefab should serialize the formal Godot monster id.");
            AssertApproximately(100f, GetProperty<float>(eggTarget, "MaxHp"), 0.001f, "Egg prefab should serialize Godot MonsterDatabase HP.");
        }

        private static GameObject RequiredPrefab(string monsterId)
        {
            string path = PrefabPath(monsterId);
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

        private static string PrefabPath(string monsterId)
        {
            return $"{EnemyPrefabsRoot}/MigrationEnemy_{ToPascal(monsterId)}.prefab";
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

        private static object Invoke(object target, string methodName, params object[] args)
        {
            MethodInfo method = null;
            foreach (MethodInfo candidate in target.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public))
            {
                if (candidate.Name == methodName && candidate.GetParameters().Length == args.Length)
                {
                    method = candidate;
                    break;
                }
            }

            if (method == null)
            {
                throw new Exception($"Missing method {target.GetType().FullName}.{methodName}");
            }

            return method.Invoke(target, args);
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

        private static void AssertApproximately(float expected, float actual, float tolerance, string message)
        {
            if (Mathf.Abs(expected - actual) > tolerance)
            {
                throw new Exception($"{message} Expected: {expected}. Actual: {actual}.");
            }
        }
    }
}
