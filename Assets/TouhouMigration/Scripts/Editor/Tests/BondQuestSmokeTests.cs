using System;
using System.Collections;
using System.Reflection;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    public static class BondQuestSmokeTests
    {
        private const string SocialBondServiceTypeName = "TouhouMigration.Runtime.Social.SocialBondService, Assembly-CSharp";
        private const string QuestDatabaseTypeName = "TouhouMigration.Runtime.Quest.QuestDatabase, Assembly-CSharp";
        private const string QuestDeliveryServiceTypeName = "TouhouMigration.Runtime.Social.QuestDeliveryService, Assembly-CSharp";
        private const string SaveDataTypeName = "TouhouMigration.Runtime.Save.MigrationSaveData, Assembly-CSharp";
        private const string SaveServiceTypeName = "TouhouMigration.Runtime.Save.MigrationSaveService, Assembly-CSharp";
        private const string QuestDataPath = "Assets/TouhouMigration/Data/Quests/quests.json";

        [MenuItem("Touhou Migration/Tests/Run Bond Quest Smoke Tests")]
        public static void RunAll()
        {
            TestQuestDatabaseLoadsGodotQuestData();
            TestBondSystemLevelsSourcesDailyAndSnapshot();
            TestQuestDeliveryMatchesObjectivesVarietyAndCompletion();
            TestSaveServiceRoundTripsBondAndQuestSnapshots();
            Debug.Log("Bond quest smoke tests passed.");
        }

        private static void TestQuestDatabaseLoadsGodotQuestData()
        {
            object database = LoadQuestDatabase();
            AssertEqual(15, GetProperty<int>(database, "QuestCount"), "Quest database should contain all Godot QuestData definitions.");
            AssertEqual(5, GetProperty<int>(database, "MainQuestCount"), "Main quest count should match Godot QuestData.");
            AssertEqual(7, GetProperty<int>(database, "SideQuestCount"), "Side quest count should match Godot QuestData.");
            AssertEqual(3, GetProperty<int>(database, "DailyQuestCount"), "Daily quest count should match Godot QuestData.");
            AssertEqual(true, Invoke<bool>(database, "HasQuest", "main_005"), "main_005 should exist.");

            object main005 = Invoke(database, "GetQuest", "main_005");
            AssertEqual("留白的旧稿", GetProperty<string>(main005, "Title"), "Quest title should preserve Chinese text.");
            AssertEqual("main_004", FirstString(GetProperty<object>(main005, "Prerequisites")), "Prerequisites should be preserved.");

            object side006 = Invoke(database, "GetQuest", "side_006");
            object objectives = GetProperty<object>(side006, "Objectives");
            AssertEqual(2, CountEnumerable(objectives), "side_006 should preserve both objectives.");
            object deliveryObjective = ItemAt(objectives, 1);
            AssertEqual("deliver_variety", GetProperty<string>(deliveryObjective, "Type"), "side_006 second objective should be deliver_variety.");
            AssertEqual("drink_any", GetProperty<string>(deliveryObjective, "ItemId"), "side_006 delivery objective should target drink_any.");
            AssertEqual("sakuya", GetProperty<string>(deliveryObjective, "NpcId"), "side_006 delivery objective should target Sakuya.");
            AssertEqual(3, GetProperty<int>(deliveryObjective, "UniqueRequired"), "side_006 should require three unique drinks.");
        }

        private static void TestBondSystemLevelsSourcesDailyAndSnapshot()
        {
            object bonds = Activator.CreateInstance(RequiredType(SocialBondServiceTypeName));

            Invoke(bonds, "AddBondPoints", "reimu", "dialogue");
            AssertEqual(10, Invoke<int>(bonds, "GetBondPoints", "reimu"), "Dialogue source should add Godot source-table points.");
            AssertEqual(1, Invoke<int>(bonds, "GetBondLevel", "reimu"), "Any positive interaction should raise Lv.0 to Lv.1.");
            AssertEqual(100, Invoke<int>(bonds, "GetPointsForNextLevel", "reimu"), "Lv.1 should target the 100 point threshold.");

            Invoke(bonds, "AddBondPoints", "reimu", "quest_help", 70);
            AssertEqual(110, Invoke<int>(bonds, "GetBondPoints", "reimu"), "Source base plus bonus should be applied.");
            AssertEqual(2, Invoke<int>(bonds, "GetBondLevel", "reimu"), "110 points should reach Lv.2.");
            AssertEqual(250, Invoke<int>(bonds, "GetPointsForNextLevel", "reimu"), "Lv.2 should target the 250 point threshold.");
            AssertEqual("quest_help", GetProperty<string>(bonds, "LastSource"), "Last source should preserve the Godot source id.");
            AssertEqual(100, GetProperty<int>(bonds, "LastDelta"), "Last delta should include source base plus bonus.");

            AssertEqual(true, Invoke<bool>(bonds, "TryDailyInteraction", "reimu"), "First daily interaction should grant points.");
            AssertEqual(false, Invoke<bool>(bonds, "TryDailyInteraction", "reimu"), "Duplicate daily interaction should be ignored.");
            AssertEqual(115, Invoke<int>(bonds, "GetBondPoints", "reimu"), "Daily interaction should add 5 points once.");

            Invoke(bonds, "SetBondLevel", "marisa", 3);
            AssertEqual(3, Invoke<int>(bonds, "GetBondLevel", "marisa"), "SetBondLevel should set the requested level.");
            AssertEqual(250, Invoke<int>(bonds, "GetBondPoints", "marisa"), "Level 3 starts at the Lv.2 threshold, matching Godot.");

            object snapshot = Invoke(bonds, "CreateSnapshot");
            object restored = Activator.CreateInstance(RequiredType(SocialBondServiceTypeName));
            Invoke(restored, "LoadSnapshot", snapshot);
            AssertEqual(115, Invoke<int>(restored, "GetBondPoints", "reimu"), "Bond snapshot should preserve points.");
            AssertEqual(2, Invoke<int>(restored, "GetBondLevel", "reimu"), "Bond snapshot should preserve level.");
            AssertEqual(false, Invoke<bool>(restored, "TryDailyInteraction", "reimu"), "Bond snapshot should preserve daily interaction list.");
        }

        private static void TestQuestDeliveryMatchesObjectivesVarietyAndCompletion()
        {
            object database = LoadQuestDatabase();
            object quests = Activator.CreateInstance(RequiredType(QuestDeliveryServiceTypeName), database);

            Invoke(quests, "MarkQuestCompleted", "side_005");
            AssertEqual(true, Invoke<bool>(quests, "StartQuest", "side_006"), "side_006 should start once prerequisite side_005 is completed.");
            AssertEqual(true, Invoke<bool>(quests, "IsQuestActive", "side_006"), "Started quest should be active.");

            Invoke(quests, "UpdateQuestProgress", "side_006", 0, 3);
            AssertEqual(3, ProgressAt(quests, "side_006", 0), "Craft objective should clamp to required amount.");

            Invoke(quests, "RegisterDeliveryTag", "green_tea", "drink_any");
            Invoke(quests, "RegisterDeliveryTag", "moon_sake", "drink_any");
            Invoke(quests, "RegisterDeliveryTag", "red_tea", "drink_any");
            Invoke(quests, "NotifyDelivery", "green_tea", 1, "sakuya");
            Invoke(quests, "NotifyDelivery", "green_tea", 1, "sakuya");
            AssertEqual(1, ProgressAt(quests, "side_006", 1), "deliver_variety should ignore duplicate item ids.");
            Invoke(quests, "NotifyDelivery", "moon_sake", 1, "sakuya");
            AssertEqual(2, ProgressAt(quests, "side_006", 1), "Second unique drink should advance variety progress.");
            Invoke(quests, "NotifyDelivery", "red_tea", 1, "reimu");
            AssertEqual(2, ProgressAt(quests, "side_006", 1), "Wrong NPC should not advance delivery progress.");
            Invoke(quests, "NotifyDelivery", "red_tea", 1, "sakuya");

            AssertEqual(true, Invoke<bool>(quests, "IsQuestCompleted", "side_006"), "Completing all objectives should mark quest complete.");
            AssertEqual(false, Invoke<bool>(quests, "IsQuestActive", "side_006"), "Completed quest should leave active quest list.");
            AssertEqual("side_006", GetProperty<string>(quests, "LastCompletedQuestId"), "Quest service should record the completed quest id.");

            object snapshot = Invoke(quests, "CreateSnapshot");
            object restored = Activator.CreateInstance(RequiredType(QuestDeliveryServiceTypeName), database);
            Invoke(restored, "LoadSnapshot", snapshot);
            AssertEqual(true, Invoke<bool>(restored, "IsQuestCompleted", "side_006"), "Quest snapshot should preserve completed quests.");
        }

        private static void TestSaveServiceRoundTripsBondAndQuestSnapshots()
        {
            object database = LoadQuestDatabase();
            object bonds = Activator.CreateInstance(RequiredType(SocialBondServiceTypeName));
            Invoke(bonds, "AddBondPoints", "reimu", "dialogue");
            Invoke(bonds, "TryDailyInteraction", "reimu");

            object quests = Activator.CreateInstance(RequiredType(QuestDeliveryServiceTypeName), database);
            Invoke(quests, "MarkQuestCompleted", "side_005");
            Invoke(quests, "StartQuest", "side_006");
            Invoke(quests, "UpdateQuestProgress", "side_006", 0, 2);

            object saveData = InvokeStatic(RequiredType(SaveDataTypeName), "CreateDefault");
            SetProperty(saveData, "SocialBonds", Invoke(bonds, "CreateSnapshot"));
            SetProperty(saveData, "Quests", Invoke(quests, "CreateSnapshot"));

            string saveRoot = Path.Combine(Path.GetTempPath(), "touhou_migration_bond_quest_tests");
            if (Directory.Exists(saveRoot))
            {
                Directory.Delete(saveRoot, true);
            }

            object saveService = Activator.CreateInstance(RequiredType(SaveServiceTypeName), saveRoot);
            AssertEqual(true, Invoke<bool>(saveService, "SaveSlot", 2, saveData), "Save service should write bond/quest snapshot data.");

            object loaded = Invoke(saveService, "LoadSlot", 2);
            object restoredBonds = Activator.CreateInstance(RequiredType(SocialBondServiceTypeName));
            Invoke(restoredBonds, "LoadSnapshot", GetProperty<object>(loaded, "SocialBonds"));
            AssertEqual(15, Invoke<int>(restoredBonds, "GetBondPoints", "reimu"), "Loaded save should preserve bond points.");
            AssertEqual(false, Invoke<bool>(restoredBonds, "TryDailyInteraction", "reimu"), "Loaded save should preserve daily-interacted state.");

            object restoredQuests = Activator.CreateInstance(RequiredType(QuestDeliveryServiceTypeName), database);
            Invoke(restoredQuests, "LoadSnapshot", GetProperty<object>(loaded, "Quests"));
            AssertEqual(true, Invoke<bool>(restoredQuests, "IsQuestActive", "side_006"), "Loaded save should preserve active quest state.");
            AssertEqual(2, ProgressAt(restoredQuests, "side_006", 0), "Loaded save should preserve quest progress.");
        }

        private static object LoadQuestDatabase()
        {
            object database = Activator.CreateInstance(RequiredType(QuestDatabaseTypeName));
            AssertEqual(true, Invoke<bool>(database, "LoadFromPath", QuestDataPath), "Quest database should load migrated Godot QuestData JSON.");
            return database;
        }

        private static int ProgressAt(object questService, string questId, int index)
        {
            object progress = Invoke(questService, "GetQuestProgress", questId);
            return (int)ItemAt(progress, index);
        }

        private static string FirstString(object enumerable)
        {
            object value = ItemAt(enumerable, 0);
            return value == null ? string.Empty : Convert.ToString(value);
        }

        private static object ItemAt(object enumerable, int targetIndex)
        {
            int index = 0;
            foreach (object item in (IEnumerable)enumerable)
            {
                if (index == targetIndex)
                {
                    return item;
                }

                index++;
            }

            throw new Exception($"Expected item at index {targetIndex}.");
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

        private static object InvokeStatic(Type type, string methodName, params object[] args)
        {
            MethodInfo method = type.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public);
            if (method == null)
            {
                throw new Exception($"Missing static method {type.FullName}.{methodName}");
            }

            return method.Invoke(null, args);
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

        private static void SetProperty(object target, string propertyName, object value)
        {
            PropertyInfo property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            if (property == null)
            {
                throw new Exception($"Missing property {target.GetType().FullName}.{propertyName}");
            }

            property.SetValue(target, value);
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
