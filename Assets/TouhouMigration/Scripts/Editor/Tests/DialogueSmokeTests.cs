using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    public static class DialogueSmokeTests
    {
        private const string DialogueDatabaseTypeName = "TouhouMigration.Runtime.Dialogue.DialogueDatabase, Assembly-CSharp";
        private const string DialogueRuntimeTypeName = "TouhouMigration.Runtime.Dialogue.DialogueRuntime, Assembly-CSharp";
        private const string DialogueRuntimeFacadeTypeName = "TouhouMigration.Runtime.Dialogue.DialogueRuntimeFacade, Assembly-CSharp";
        private const string RuneDialogueControllerTypeName = "TouhouMigration.Runtime.UI.Dialogue.RuneDialogueController, Assembly-CSharp";
        private const string DialogueDataPath = "Assets/TouhouMigration/Data/Dialogue";
        private const string BambooHomeScenePath = "Assets/TouhouMigration/Scenes/BambooHomeVerticalSlice.unity";
        private const string HumanVillageScenePath = "Assets/TouhouMigration/Scenes/HumanVillageVerticalSlice.unity";

        [MenuItem("Touhou Migration/Tests/Run Dialogue Smoke Tests")]
        public static void RunAll()
        {
            TestDialogueDatabaseLoadsGodotNpcJson();
            TestDialogueRuntimeProgressesChoicesAndEnds();
            TestDialogueFacadeScopesSessionsAndActions();
            TestRuneDialogueControllerDisplaysViewModel();
            TestRuntimeScenesContainRuneDialogueController();
            Debug.Log("Dialogue smoke tests passed.");
        }

        private static void TestDialogueDatabaseLoadsGodotNpcJson()
        {
            object database = LoadDialogueDatabase();
            AssertEqual(35, GetProperty<int>(database, "NpcCount"), "Dialogue database should load all migrated _npc_*.json files.");
            AssertEqual(true, Invoke<bool>(database, "HasNpc", "marisa"), "Marisa dialogue should exist.");
            AssertEqual(true, Invoke<bool>(database, "HasNpc", "reimu"), "Reimu dialogue should exist.");
            AssertEqual("博丽灵梦", Invoke<string>(database, "GetNpcName", "reimu"), "NPC display name should come from Godot JSON.");

            Dictionary<string, object> marisaContext = new Dictionary<string, object>
            {
                ["bond_level"] = 1,
                ["time_of_day"] = "afternoon"
            };
            object marisaLines = Invoke(database, "GetDialogue", "marisa", "greeting", marisaContext);
            AssertEqual(1, CountEnumerable(marisaLines), "Marisa greeting should return one runtime line for the first valid compact entry.");
            object marisaLine = First(marisaLines);
            AssertEqual("雾雨魔理沙", GetProperty<string>(marisaLine, "Speaker"), "Dialogue line should normalize compact speaker field.");
            AssertContains(GetProperty<string>(marisaLine, "Text"), "市场路", "Dialogue line should normalize compact text field.");
            object marisaChoices = GetProperty<object>(marisaLine, "Choices");
            AssertEqual(3, CountEnumerable(marisaChoices), "Compact choices should be attached to the runtime line.");
            object firstChoice = First(marisaChoices);
            AssertEqual("一起逛市场", GetProperty<string>(firstChoice, "Text"), "Choice text should parse before the > separator.");
            AssertEqual(20, GetDictionaryValue<int>(GetProperty<object>(firstChoice, "Effects"), "bond"), "Choice effects should parse compact bond value.");

            Dictionary<string, object> reimuContext = new Dictionary<string, object>
            {
                ["bond_level"] = 1,
                ["time_of_day"] = "afternoon"
            };
            object reimuLines = Invoke(database, "GetDialogue", "reimu", "greeting", reimuContext);
            object reimuLine = First(reimuLines);
            object secondChoice = Nth(GetProperty<object>(reimuLine, "Choices"), 1);
            AssertEqual("买种子", GetProperty<string>(secondChoice, "Text"), "Reimu afternoon shop choice should migrate.");
            AssertEqual("open_shop_reimu_seeds", GetDictionaryValue<string>(GetProperty<object>(secondChoice, "Effects"), "event"), "Reimu shop event should migrate.");
        }

        private static void TestDialogueRuntimeProgressesChoicesAndEnds()
        {
            object database = LoadDialogueDatabase();
            Dictionary<string, object> context = new Dictionary<string, object> { ["bond_level"] = 5 };
            object lines = Invoke(database, "GetDialogue", "marisa", "question", context);

            object runtime = Activator.CreateInstance(RequiredType(DialogueRuntimeTypeName));
            Invoke(runtime, "StartLines", "marisa", lines);
            AssertEqual(true, GetProperty<bool>(runtime, "IsActive"), "Runtime should become active after starting non-empty lines.");

            object firstView = Invoke(runtime, "GetViewModel");
            AssertEqual(true, GetProperty<bool>(firstView, "Active"), "View model should be active.");
            AssertEqual("marisa", GetProperty<string>(firstView, "NpcId"), "View model should expose NPC id.");
            AssertEqual(0, GetProperty<int>(firstView, "Index"), "View model should start at line zero.");
            AssertEqual(3, CountEnumerable(GetProperty<object>(firstView, "Choices")), "View model should expose line choices.");

            AssertEqual(true, Invoke<bool>(runtime, "Choose", 0), "Runtime should accept a valid choice.");
            AssertEqual(false, GetProperty<bool>(runtime, "IsActive"), "Choosing from a one-line branch should finish the dialogue.");
            AssertEqual("choice", GetProperty<string>(runtime, "LastFinishReason"), "Runtime should record choice finish reason.");
            AssertEqual("你已经证明过了", GetProperty<string>(GetProperty<object>(runtime, "LastCommittedChoice"), "Text"), "Runtime should expose the committed choice for integration diagnostics.");
        }

        private static void TestDialogueFacadeScopesSessionsAndActions()
        {
            object database = LoadDialogueDatabase();
            Dictionary<string, object> context = new Dictionary<string, object>
            {
                ["bond_level"] = 1,
                ["time_of_day"] = "afternoon"
            };
            object lines = Invoke(database, "GetDialogue", "reimu", "greeting", context);

            object facade = Activator.CreateInstance(RequiredType(DialogueRuntimeFacadeTypeName));
            int sessionId = Invoke<int>(facade, "StartLines", "reimu", lines);
            AssertEqual(true, sessionId > 0, "Facade should allocate a positive session id.");
            object firstView = Invoke(facade, "GetViewModel");
            AssertEqual(sessionId, GetProperty<int>(firstView, "SessionId"), "Facade view model should be scoped to the current session.");
            AssertEqual(false, Invoke<bool>(facade, "ChooseForSession", sessionId + 1, 1), "Facade should reject stale session choices.");
            AssertEqual(true, Invoke<bool>(facade, "ChooseForSession", sessionId, 1), "Facade should accept choices for the active session.");
            AssertEqual(1, GetProperty<int>(facade, "LastCommittedChoiceIndex"), "Facade should record the committed choice index.");
            AssertEqual("event", GetProperty<string>(facade, "LastActionId"), "Facade should surface choice effect action ids.");
            AssertEqual("open_shop_reimu_seeds", GetDictionaryValue<string>(GetProperty<object>(facade, "LastActionPayload"), "value"), "Facade should surface action payload value.");
            AssertEqual(false, GetProperty<bool>(facade, "IsActive"), "One-line Reimu choice should finish the facade runtime.");
        }

        private static void TestRuneDialogueControllerDisplaysViewModel()
        {
            object database = LoadDialogueDatabase();
            Dictionary<string, object> context = new Dictionary<string, object> { ["bond_level"] = 5 };
            object lines = Invoke(database, "GetDialogue", "marisa", "question", context);

            object facade = Activator.CreateInstance(RequiredType(DialogueRuntimeFacadeTypeName));
            int sessionId = Invoke<int>(facade, "StartLines", "marisa", lines);
            object viewModel = Invoke(facade, "GetViewModel");

            GameObject host = new GameObject("DialogueSmokeTestHost");
            object controller = host.AddComponent(RequiredType(RuneDialogueControllerTypeName));
            Invoke(controller, "ShowViewModel", viewModel);

            AssertEqual(true, GetProperty<bool>(controller, "IsVisible"), "Rune controller should become visible for active dialogue.");
            AssertEqual("雾雨魔理沙", GetProperty<string>(controller, "SpeakerText"), "Rune controller should display speaker.");
            AssertContains(GetProperty<string>(controller, "FullText"), "努力", "Rune controller should display dialogue text.");
            AssertEqual(3, GetProperty<int>(controller, "ChoiceCount"), "Rune controller should display choices.");
            AssertEqual(sessionId, GetProperty<int>(controller, "SessionId"), "Rune controller should preserve session id.");
            AssertEqual(true, GetProperty<bool>(controller, "IsTyping"), "Rune controller should start with typewriter active.");

            Invoke(controller, "CompleteTypewriter");
            AssertEqual(false, GetProperty<bool>(controller, "IsTyping"), "Completing typewriter should reveal the whole line.");
            AssertEqual(false, Invoke<bool>(controller, "ConfirmChoice", -1), "Invalid choice should be ignored.");

            UnityEngine.Object.DestroyImmediate(host);
        }

        private static void TestRuntimeScenesContainRuneDialogueController()
        {
            Type runeControllerType = RequiredType(RuneDialogueControllerTypeName);

            EditorSceneManager.OpenScene(BambooHomeScenePath);
            AssertEqual(1, CountComponents(runeControllerType), "Bamboo Home should host the Rune dialogue controller.");

            EditorSceneManager.OpenScene(HumanVillageScenePath);
            AssertEqual(1, CountComponents(runeControllerType), "Human Village should host the Rune dialogue controller.");
        }

        private static object LoadDialogueDatabase()
        {
            object database = Activator.CreateInstance(RequiredType(DialogueDatabaseTypeName));
            AssertEqual(true, Invoke<bool>(database, "LoadFromPath", DialogueDataPath), "Dialogue database should load migrated Godot _npc_*.json files.");
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

        private static object First(object enumerable)
        {
            return Nth(enumerable, 0);
        }

        private static object Nth(object enumerable, int requestedIndex)
        {
            int index = 0;
            foreach (object item in (IEnumerable)enumerable)
            {
                if (index == requestedIndex)
                {
                    return item;
                }

                index++;
            }

            throw new Exception($"Enumerable did not contain index {requestedIndex}.");
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

        private static T GetDictionaryValue<T>(object dictionary, string key)
        {
            object value = GetDictionaryObjectValue(dictionary, key);
            return (T)Convert.ChangeType(value, typeof(T));
        }

        private static object GetDictionaryObjectValue(object dictionary, string key)
        {
            if (dictionary is IDictionary nonGenericDictionary)
            {
                foreach (DictionaryEntry entry in nonGenericDictionary)
                {
                    if ((string)entry.Key == key)
                    {
                        return entry.Value;
                    }
                }
            }
            else if (dictionary is IEnumerable enumerable)
            {
                foreach (object item in enumerable)
                {
                    PropertyInfo keyProperty = item.GetType().GetProperty("Key");
                    PropertyInfo valueProperty = item.GetType().GetProperty("Value");
                    if (keyProperty == null || valueProperty == null)
                    {
                        continue;
                    }

                    if ((string)keyProperty.GetValue(item) == key)
                    {
                        return valueProperty.GetValue(item);
                    }
                }
            }

            throw new Exception($"Dictionary did not contain key {key}.");
        }

        private static void AssertContains(string actual, string expectedSubstring, string message)
        {
            if (actual == null || !actual.Contains(expectedSubstring))
            {
                throw new Exception($"{message} Expected substring: {expectedSubstring}. Actual: {actual}.");
            }
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
