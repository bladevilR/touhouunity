using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    public static class EnemyVisualPrefabSmokeTests
    {
        private const string VisualSourceTypeName = "TouhouMigration.Runtime.Combat.MigrationEnemyVisualSource, Assembly-CSharp";
        private const string EnemyPrefabsRoot = "Assets/TouhouMigration/Prefabs/Enemies";
        private const string EnemyArtRoot = "Assets/TouhouMigration/Art/Enemies";

        private static readonly string[] ExistingSceneMonsterIds =
        {
            "bat", "bee", "bird", "bumble", "ghost", "phantom", "spook", "sting",
            "fungi", "mushroom", "seed", "shade", "shadow", "sprout", "toadstool",
            "chick", "egg", "fledgling", "spider"
        };

        [MenuItem("Touhou Migration/Tests/Run Enemy Visual Prefab Smoke Tests")]
        public static void RunAll()
        {
            TestExistingSceneMonsterAssetsArePromotedIntoUnityArtFolder();
            TestExistingSceneEnemyPrefabsMountImportedVisualModels();
            TestRepresentativePrefabPreservesSceneReferencedTextures();
            TestMissingVampireSceneIsMarkedAsFallbackVisual();
        }

        private static void TestExistingSceneMonsterAssetsArePromotedIntoUnityArtFolder()
        {
            foreach (string monsterId in ExistingSceneMonsterIds)
            {
                string modelPath = ModelPath(monsterId);
                AssertEqual(true, File.Exists(modelPath), $"Primary model file should be promoted for '{monsterId}'.");
                AssertEqual(true, AssetDatabase.LoadAssetAtPath<GameObject>(modelPath) != null, $"Primary model asset should import as a Unity GameObject for '{monsterId}'.");
            }
        }

        private static void TestExistingSceneEnemyPrefabsMountImportedVisualModels()
        {
            Type visualSourceType = RequiredType(VisualSourceTypeName);
            foreach (string monsterId in ExistingSceneMonsterIds)
            {
                GameObject prefab = RequiredPrefab(monsterId);
                object visualSource = RequiredComponent(prefab, visualSourceType, $"Prefab '{monsterId}' should record its migrated visual source.");
                AssertEqual(monsterId, GetProperty<string>(visualSource, "VariantId"), $"Visual source should preserve variant id for '{monsterId}'.");
                AssertEqual($"res://scenes/monsters/{ToPascal(monsterId)}Monster.tscn", GetProperty<string>(visualSource, "GodotScenePath"), $"Visual source should preserve Godot scene path for '{monsterId}'.");
                AssertEqual(ModelPath(monsterId), GetProperty<string>(visualSource, "UnityModelAssetPath"), $"Visual source should point to the promoted Unity model asset for '{monsterId}'.");
                AssertEqual(false, GetProperty<bool>(visualSource, "UsesFallbackVisual"), $"Existing monster scene '{monsterId}' should not use a fallback visual.");

                Transform visual = prefab.transform.Find("Visual");
                AssertEqual(true, visual != null, $"Prefab '{monsterId}' should have a Visual child.");
                AssertEqual(true, visual.childCount > 0, $"Prefab '{monsterId}' Visual child should contain the imported model instance.");
                AssertEqual(true, visual.GetComponentsInChildren<Renderer>(true).Length > 0, $"Prefab '{monsterId}' Visual child should contain imported renderers.");
                AssertEqual(0, visual.GetComponentsInChildren<Collider>(true).Length, $"Prefab '{monsterId}' Visual child should not carry source-model colliders.");

                Renderer rootRenderer = prefab.GetComponent<Renderer>();
                AssertEqual(true, rootRenderer != null, $"Prefab '{monsterId}' should keep its primitive root renderer component for fallback safety.");
                AssertEqual(false, rootRenderer.enabled, $"Prefab '{monsterId}' should hide the primitive placeholder renderer when a real visual model is mounted.");
            }
        }

        private static void TestMissingVampireSceneIsMarkedAsFallbackVisual()
        {
            Type visualSourceType = RequiredType(VisualSourceTypeName);
            GameObject prefab = RequiredPrefab("vampire");
            object visualSource = RequiredComponent(prefab, visualSourceType, "Vampire prefab should record that its source scene is missing.");
            AssertEqual("vampire", GetProperty<string>(visualSource, "VariantId"), "Vampire visual source should preserve variant id.");
            AssertEqual("res://scenes/monsters/VampireMonster.tscn", GetProperty<string>(visualSource, "GodotScenePath"), "Vampire visual source should preserve intended Godot scene path.");
            AssertEqual(true, GetProperty<bool>(visualSource, "UsesFallbackVisual"), "Vampire prefab should explicitly use a fallback visual until a source scene exists.");
            AssertEqual(string.Empty, GetProperty<string>(visualSource, "UnityModelAssetPath"), "Vampire fallback should not pretend to have a promoted model asset.");
        }

        private static void TestRepresentativePrefabPreservesSceneReferencedTextures()
        {
            Type visualSourceType = RequiredType(VisualSourceTypeName);
            GameObject bat = RequiredPrefab("bat");
            object batVisualSource = RequiredComponent(bat, visualSourceType, "Bat prefab should record scene-referenced textures.");
            AssertEqual(
                "Assets/TouhouMigration/Art/Enemies/Bat/Textures/Vampire Bat.png",
                GetProperty<string>(batVisualSource, "PrimaryTextureAssetPath"),
                "Bat primary texture should match the texture referenced by BatMonster.tscn, not the folder's similarly named base Bat texture.");

            string[] batTextures = GetProperty<string[]>(batVisualSource, "TextureAssetPaths");
            AssertEqual(
                true,
                Contains(batTextures, "Assets/TouhouMigration/Art/Enemies/Bat/Textures/Vampire Bat Emission.png"),
                "Bat visual source should preserve the scene-referenced emission texture.");
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

        private static string ModelPath(string monsterId)
        {
            string pascal = ToPascal(monsterId);
            return $"{EnemyArtRoot}/{pascal}/Models/{pascal}.fbx";
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

        private static bool Contains(string[] values, string expected)
        {
            foreach (string value in values)
            {
                if (value == expected)
                {
                    return true;
                }
            }

            return false;
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
    }
}
