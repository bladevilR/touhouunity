using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    public static class QuestJournalSmokeTests
    {
        private const string QuestDatabaseTypeName = "TouhouMigration.Runtime.Quest.QuestDatabase, Assembly-CSharp";
        private const string QuestRewardLedgerTypeName = "TouhouMigration.Runtime.Quest.QuestRewardLedger, Assembly-CSharp";
        private const string QuestDeliveryServiceTypeName = "TouhouMigration.Runtime.Social.QuestDeliveryService, Assembly-CSharp";
        private const string SocialBondServiceTypeName = "TouhouMigration.Runtime.Social.SocialBondService, Assembly-CSharp";
        private const string DialogueEffectRouterTypeName = "TouhouMigration.Runtime.Dialogue.DialogueEffectRouter, Assembly-CSharp";
        private const string UnifiedMenuTypeName = "TouhouMigration.Runtime.UI.MigrationUnifiedMenuController, Assembly-CSharp";
        private const string QuestDataPath = "Assets/TouhouMigration/Data/Quests/quests.json";

        [MenuItem("Touhou Migration/Tests/Run Quest Journal Smoke Tests")]
        public static void RunAll()
        {
            TestQuestRewardsCountersUnlocksAndJournalEntries();
            TestDialogueEffectsRouteToBondQuestCounterAndUnlock();
            TestUnifiedMenuBindsQuestJournalServices();
            Debug.Log("Quest journal smoke tests passed.");
        }

        private static void TestQuestRewardsCountersUnlocksAndJournalEntries()
        {
            object database = LoadQuestDatabase();
            object ledger = Activator.CreateInstance(RequiredType(QuestRewardLedgerTypeName));
            object quests = Activator.CreateInstance(RequiredType(QuestDeliveryServiceTypeName), database, ledger);

            Invoke(quests, "MarkQuestCompleted", "side_005");
            AssertEqual(true, Invoke<bool>(quests, "StartQuest", "side_006"), "side_006 should start once side_005 is completed.");
            Invoke(quests, "UpdateQuestProgress", "side_006", 0, 3);
            Invoke(quests, "RegisterDeliveryTag", "green_tea", "drink_any");
            Invoke(quests, "RegisterDeliveryTag", "moon_sake", "drink_any");
            Invoke(quests, "RegisterDeliveryTag", "red_tea", "drink_any");
            Invoke(quests, "NotifyDelivery", "green_tea", 1, "sakuya");
            Invoke(quests, "NotifyDelivery", "moon_sake", 1, "sakuya");
            Invoke(quests, "NotifyDelivery", "red_tea", 1, "sakuya");

            AssertEqual(true, Invoke<bool>(quests, "IsQuestCompleted", "side_006"), "Completing side_006 should move it to completed quests.");
            AssertEqual(280, GetProperty<int>(ledger, "Exp"), "Quest reward ledger should receive side_006 exp.");
            AssertEqual(260, GetProperty<int>(ledger, "Coins"), "Quest reward ledger should receive side_006 coins.");
            AssertEqual(2, Invoke<int>(ledger, "GetItemCount", "reishi_tea"), "Quest reward ledger should receive item rewards.");
            AssertEqual(1, Invoke<int>(ledger, "GetItemCount", "moon_fish"), "Quest reward ledger should receive all item rewards.");

            Invoke(quests, "IncrementCounter", "tea_party", 2);
            AssertEqual(2, Invoke<int>(quests, "GetCounter", "tea_party"), "Quest counters should preserve arbitrary Godot counter ids.");
            Invoke(quests, "UnlockNpc", "sakuya");
            AssertEqual(true, Invoke<bool>(quests, "IsNpcUnlocked", "sakuya"), "Quest service should preserve unlocked NPC state.");

            AssertEqual(true, Invoke<bool>(quests, "StartQuest", "side_007"), "side_007 should start once side_006 is completed.");

            object completedEntries = Invoke(quests, "GetJournalEntries", "completed");
            object completedSide006 = FindEntry(completedEntries, "side_006");
            AssertEqual("咲夜的茶会", GetProperty<string>(completedSide006, "Title"), "Journal entry title should preserve migrated quest text.");
            AssertEqual("completed", GetProperty<string>(completedSide006, "Status"), "Completed journal entries should expose status.");
            AssertEqual("side", GetProperty<string>(completedSide006, "Type"), "Journal entries should expose quest type.");
            AssertEqual(2, GetProperty<int>(completedSide006, "ObjectiveCount"), "Completed entry should expose objective count.");
            AssertEqual(2, GetProperty<int>(completedSide006, "CompletedObjectiveCount"), "Completed entry should mark all objectives complete.");

            object activeEntries = Invoke(quests, "GetJournalEntries", "active");
            object activeSide007 = FindEntry(activeEntries, "side_007");
            AssertEqual("迷途之宴", GetProperty<string>(activeSide007, "Title"), "Active journal entries should include newly started prerequisite quests.");
            AssertContains(GetProperty<string>(activeSide007, "ProgressText"), "0/1", "Active journal progress should include objective progress.");
            AssertContains(GetProperty<string>(activeSide007, "RewardText"), "spirit_crystal x3", "Journal reward text should include item rewards.");

            object snapshot = Invoke(quests, "CreateSnapshot");
            object restoredLedger = Activator.CreateInstance(RequiredType(QuestRewardLedgerTypeName));
            object restoredQuests = Activator.CreateInstance(RequiredType(QuestDeliveryServiceTypeName), database, restoredLedger);
            Invoke(restoredQuests, "LoadSnapshot", snapshot);
            AssertEqual(true, Invoke<bool>(restoredQuests, "IsQuestCompleted", "side_006"), "Quest snapshot should restore completed quests.");
            AssertEqual(true, Invoke<bool>(restoredQuests, "IsNpcUnlocked", "sakuya"), "Quest snapshot should restore unlocked NPCs.");
            AssertEqual(2, Invoke<int>(restoredQuests, "GetCounter", "tea_party"), "Quest snapshot should restore counters.");
            AssertEqual(280, GetProperty<int>(restoredLedger, "Exp"), "Quest snapshot should restore reward ledger exp.");
            AssertEqual(1, Invoke<int>(restoredLedger, "GetItemCount", "moon_fish"), "Quest snapshot should restore reward ledger items.");
        }

        private static void TestDialogueEffectsRouteToBondQuestCounterAndUnlock()
        {
            object database = LoadQuestDatabase();
            object ledger = Activator.CreateInstance(RequiredType(QuestRewardLedgerTypeName));
            object quests = Activator.CreateInstance(RequiredType(QuestDeliveryServiceTypeName), database, ledger);
            object bonds = Activator.CreateInstance(RequiredType(SocialBondServiceTypeName));
            object router = Activator.CreateInstance(RequiredType(DialogueEffectRouterTypeName), bonds, quests);

            Dictionary<string, object> effects = new Dictionary<string, object>
            {
                ["bond"] = 15,
                ["quest"] = "side_001",
                ["counter"] = "dialogue_counter",
                ["unlock_npc"] = "sakuya"
            };

            AssertEqual(true, Invoke<bool>(router, "Apply", "marisa", effects), "Dialogue effect router should report handled effects.");
            AssertEqual(25, Invoke<int>(bonds, "GetBondPoints", "marisa"), "Dialogue bond effects should use the Godot dialogue source plus bonus value.");
            AssertEqual(true, Invoke<bool>(quests, "IsQuestActive", "side_001"), "Dialogue quest effects should start quests.");
            AssertEqual(1, Invoke<int>(quests, "GetCounter", "dialogue_counter"), "Dialogue counter effects should increment counters.");
            AssertEqual(true, Invoke<bool>(quests, "IsNpcUnlocked", "sakuya"), "Dialogue unlock effects should unlock NPCs.");
        }

        private static void TestUnifiedMenuBindsQuestJournalServices()
        {
            object database = LoadQuestDatabase();
            object ledger = Activator.CreateInstance(RequiredType(QuestRewardLedgerTypeName));
            object quests = Activator.CreateInstance(RequiredType(QuestDeliveryServiceTypeName), database, ledger);
            object bonds = Activator.CreateInstance(RequiredType(SocialBondServiceTypeName));
            Invoke(quests, "StartQuest", "side_001");

            GameObject host = new GameObject("QuestJournalSmokeMenu");
            try
            {
                object menu = host.AddComponent(RequiredType(UnifiedMenuTypeName));
                Invoke(menu, "Bind", null, null, null, null, database, quests, bonds);

                AssertEqual(1, GetProperty<int>(menu, "QuestJournalEntryCount"), "Unified menu should expose bound quest journal entries.");
                object entries = Invoke(menu, "GetQuestJournalEntries", "active");
                object side001 = FindEntry(entries, "side_001");
                AssertEqual("收集竹子", GetProperty<string>(side001, "Title"), "Unified menu should expose quest journal data from the service.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        private static object LoadQuestDatabase()
        {
            object database = Activator.CreateInstance(RequiredType(QuestDatabaseTypeName));
            AssertEqual(true, Invoke<bool>(database, "LoadFromPath", QuestDataPath), "Quest database should load migrated Godot QuestData JSON.");
            return database;
        }

        private static object FindEntry(object entries, string questId)
        {
            foreach (object entry in (IEnumerable)entries)
            {
                if (GetProperty<string>(entry, "QuestId") == questId)
                {
                    return entry;
                }
            }

            throw new Exception($"Missing journal entry for quest {questId}.");
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

        private static void AssertContains(string actual, string expectedFragment, string message)
        {
            if (actual == null || !actual.Contains(expectedFragment, StringComparison.Ordinal))
            {
                throw new Exception($"{message} Expected fragment: {expectedFragment}. Actual: {actual}.");
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
