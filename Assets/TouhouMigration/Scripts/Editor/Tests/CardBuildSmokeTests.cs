using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    public static class CardBuildSmokeTests
    {
        private const string DatabaseTypeName = "TouhouMigration.Runtime.CardBuild.CardBuildDatabase, Assembly-CSharp";
        private const string ProfileStoreTypeName = "TouhouMigration.Runtime.CardBuild.CardBuildProfileStore, Assembly-CSharp";
        private const string DeckEditorTypeName = "TouhouMigration.Runtime.UI.CardBuild.MokouDeckEditorController, Assembly-CSharp";
        private const string DataDirectory = "Assets/TouhouMigration/Data/CardBuild";
        private const string TitleScenePath = "Assets/TouhouMigration/Scenes/TitleScreen.unity";

        [MenuItem("Touhou Migration/Tests/Run CardBuild Smoke Tests")]
        public static void RunAll()
        {
            TestDatabaseLoadsGodotCardBuildData();
            TestDefaultMokouProfileValidatesAndPersists();
            TestTitleSceneHostsDeckEditor();
            Debug.Log("CardBuild smoke tests passed.");
        }

        private static void TestDatabaseLoadsGodotCardBuildData()
        {
            object database = LoadDatabase();

            AssertEqual(12, GetProperty<int>(database, "ArchetypeCount"), "Archetype count should match Godot cardbuild data.");
            AssertEqual(36, GetProperty<int>(database, "CharacterCount"), "Character count should match Godot cardbuild data.");
            AssertEqual(156, GetProperty<int>(database, "CardCount"), "Card count should include generated archetype cards plus explicit cards.");
            AssertEqual(12, GetProperty<int>(database, "BossRuleCount"), "Boss rule count should match Godot cardbuild data.");
            AssertEqual(5, GetProperty<int>(database, "ResourceCount"), "Resource count should match Godot cardbuild data.");
            AssertEqual(5, GetProperty<int>(database, "StatusCount"), "Status count should match Godot cardbuild data.");
            AssertEqual(3, GetProperty<int>(database, "RelicCount"), "Relic count should match Godot cardbuild data.");
            AssertEqual(4, GetProperty<int>(database, "UpgradeCount"), "Upgrade count should match Godot cardbuild data.");
            AssertEqual(true, Invoke<bool>(database, "HasCard", "mokou_starter_fire_bird"), "Mokou starter card should exist.");
            AssertEqual(true, Invoke<bool>(database, "HasCharacter", "fujiwara_no_mokou"), "Mokou character record should exist.");

            object availableCards = Invoke(database, "GetAvailableCardIds", "fujiwara_no_mokou");
            AssertEqual(24, CountEnumerable(availableCards), "Mokou should have 24 first-slice available cards.");
        }

        private static void TestDefaultMokouProfileValidatesAndPersists()
        {
            object database = LoadDatabase();
            Type storeType = RequiredType(ProfileStoreTypeName);
            string profilePath = Path.Combine(Path.GetTempPath(), "touhou_migration_cardbuild_profile_test.json");
            if (File.Exists(profilePath))
            {
                File.Delete(profilePath);
            }

            object store = Activator.CreateInstance(storeType, database, profilePath);
            object profile = Invoke(store, "CreateDefaultProfile");
            AssertEqual("fujiwara_no_mokou", GetProperty<string>(profile, "CharacterId"), "Default profile should be for Mokou.");
            AssertEqual(12, CountEnumerable(GetProperty<object>(profile, "ActiveDeck")), "Default Mokou deck should contain 12 cards.");
            AssertEqual(6, CountEnumerable(GetProperty<object>(profile, "ActionLoadout")), "Default Mokou action loadout should contain 6 slots.");

            object validation = Invoke(store, "ValidateProfile", profile);
            AssertEqual(true, GetProperty<bool>(validation, "IsValid"), "Default Mokou profile should validate.");

            AssertEqual(true, Invoke<bool>(store, "SaveProfile", profile), "Default Mokou profile should save.");
            object loaded = Invoke(store, "LoadProfile", "fujiwara_no_mokou");
            AssertEqual("fujiwara_no_mokou", GetProperty<string>(loaded, "CharacterId"), "Stored profile should reload Mokou.");
            AssertEqual(12, CountEnumerable(GetProperty<object>(loaded, "ActiveDeck")), "Stored deck should keep all cards.");
        }

        private static void TestTitleSceneHostsDeckEditor()
        {
            Type deckEditorType = RequiredType(DeckEditorTypeName);
            EditorSceneManager.OpenScene(TitleScenePath);
            AssertEqual(1, CountComponents(deckEditorType), "Title scene should host the Mokou deck editor controller.");
        }

        private static object LoadDatabase()
        {
            Type databaseType = RequiredType(DatabaseTypeName);
            object database = Activator.CreateInstance(databaseType);
            AssertEqual(true, Invoke<bool>(database, "LoadFromDirectory", DataDirectory), "CardBuild database should load from Unity data directory.");
            return database;
        }

        private static int CountComponents(Type componentType)
        {
            int count = 0;
            foreach (GameObject gameObject in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include))
            {
                if (gameObject.GetComponent(componentType) != null)
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountEnumerable(object target)
        {
            int count = 0;
            foreach (object _ in (IEnumerable)target)
            {
                count++;
            }

            return count;
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

        private static T Invoke<T>(object target, string methodName, params object[] args)
        {
            return (T)Invoke(target, methodName, args);
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
