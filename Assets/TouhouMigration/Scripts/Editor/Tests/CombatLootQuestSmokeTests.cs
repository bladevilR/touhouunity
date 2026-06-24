using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    public static class CombatLootQuestSmokeTests
    {
        private const string ItemDatabaseTypeName = "TouhouMigration.Runtime.Inventory.ItemDatabase, Assembly-CSharp";
        private const string InventoryServiceTypeName = "TouhouMigration.Runtime.Inventory.InventoryService, Assembly-CSharp";
        private const string QuestDatabaseTypeName = "TouhouMigration.Runtime.Quest.QuestDatabase, Assembly-CSharp";
        private const string QuestRewardLedgerTypeName = "TouhouMigration.Runtime.Quest.QuestRewardLedger, Assembly-CSharp";
        private const string QuestDeliveryServiceTypeName = "TouhouMigration.Runtime.Social.QuestDeliveryService, Assembly-CSharp";
        private const string CombatTargetBehaviourTypeName = "TouhouMigration.Runtime.Combat.MigrationCombatTargetBehaviour, Assembly-CSharp";
        private const string CombatLootDropHandlerTypeName = "TouhouMigration.Runtime.Combat.MigrationCombatLootDropHandler, Assembly-CSharp";
        private const string ItemDataPath = "Assets/TouhouMigration/Data/Items/items.json";
        private const string QuestDataPath = "Assets/TouhouMigration/Data/Quests/quests.json";
        private const string HumanVillageScenePath = "Assets/TouhouMigration/Scenes/HumanVillageVerticalSlice.unity";

        [MenuItem("Touhou Migration/Tests/Run Combat Loot Quest Smoke Tests")]
        public static void RunAll()
        {
            TestEnemyKilledProgressesActiveKillObjectives();
            TestDefeatLootHandlerGrantsInventoryAndQuestKillOnce();
            TestGodotLootTablesCoverEnemyClassificationAndDropFamilies();
            TestHumanVillageSceneContainsLootDropAdapter();
        }

        private static void TestEnemyKilledProgressesActiveKillObjectives()
        {
            object database = LoadQuestDatabase();
            object ledger = Activator.CreateInstance(RequiredType(QuestRewardLedgerTypeName));
            object quests = Activator.CreateInstance(RequiredType(QuestDeliveryServiceTypeName), database, ledger);

            Invoke(quests, "MarkQuestCompleted", "main_001");
            AssertEqual(true, Invoke<bool>(quests, "StartQuest", "main_002"), "main_002 should start after main_001 is completed.");

            Invoke(quests, "NotifyEnemyKilled");
            Invoke(quests, "NotifyEnemyKilled");

            AssertEqual(2, ProgressAt(quests, "main_002", 1), "Enemy kills should progress active Godot kill objectives.");
            AssertEqual(false, Invoke<bool>(quests, "IsQuestCompleted", "main_002"), "Kill progress alone should not complete mixed-objective quests.");
        }

        private static void TestDefeatLootHandlerGrantsInventoryAndQuestKillOnce()
        {
            object itemDatabase = LoadItemDatabase();
            object inventory = Activator.CreateInstance(RequiredType(InventoryServiceTypeName), itemDatabase, 48);
            object questDatabase = LoadQuestDatabase();
            object ledger = Activator.CreateInstance(RequiredType(QuestRewardLedgerTypeName));
            object quests = Activator.CreateInstance(RequiredType(QuestDeliveryServiceTypeName), questDatabase, ledger);
            Invoke(quests, "MarkQuestCompleted", "main_001");
            AssertEqual(true, Invoke<bool>(quests, "StartQuest", "main_002"), "main_002 should start for combat loot quest smoke.");

            Type targetBehaviourType = RequiredType(CombatTargetBehaviourTypeName);
            Type lootHandlerType = RequiredType(CombatLootDropHandlerTypeName);
            GameObject targetObject = new GameObject("CombatLootQuestSmoke_Target");
            try
            {
                object target = targetObject.AddComponent(targetBehaviourType);
                Invoke(target, "Initialize", 5f);
                object lootHandler = targetObject.AddComponent(lootHandlerType);
                Invoke(lootHandler, "BindTarget", target);
                Invoke(lootHandler, "BindServices", inventory, quests);
                Invoke(lootHandler, "ConfigureGuaranteedDrop", "fairy_meat", 2);

                Invoke(target, "ApplyDamage", 6f);

                AssertEqual(1, GetProperty<int>(lootHandler, "LootGrantCount"), "Loot drops should grant once on first defeat.");
                AssertEqual(2, Invoke<int>(inventory, "GetItemCount", "fairy_meat"), "Defeat loot should add migrated Godot item ids to inventory.");
                AssertEqual(1, ProgressAt(quests, "main_002", 1), "Defeat loot handler should notify quest kill objectives.");

                Invoke(target, "ApplyDamage", 6f);

                AssertEqual(1, GetProperty<int>(lootHandler, "LootGrantCount"), "Duplicate defeat should not grant duplicate loot.");
                AssertEqual(2, Invoke<int>(inventory, "GetItemCount", "fairy_meat"), "Duplicate defeat should not duplicate inventory drops.");
                AssertEqual(1, ProgressAt(quests, "main_002", 1), "Duplicate defeat should not duplicate quest kill progress.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(targetObject);
            }
        }

        private static void TestGodotLootTablesCoverEnemyClassificationAndDropFamilies()
        {
            object itemDatabase = LoadItemDatabase();
            object inventory = Activator.CreateInstance(RequiredType(InventoryServiceTypeName), itemDatabase, 48);

            Type targetBehaviourType = RequiredType(CombatTargetBehaviourTypeName);
            Type lootHandlerType = RequiredType(CombatLootDropHandlerTypeName);
            GameObject targetObject = new GameObject("CombatLootQuestSmoke_EliteLootTableTarget");
            try
            {
                object target = targetObject.AddComponent(targetBehaviourType);
                Invoke(target, "Initialize", 5f);
                object lootHandler = targetObject.AddComponent(lootHandlerType);
                Invoke(lootHandler, "BindTarget", target);
                Invoke(lootHandler, "BindServices", inventory, null);
                Invoke(lootHandler, "ConfigureGodotLootTables", "elite", "fire_enemy", true);

                Invoke(target, "ApplyDamage", 6f);

                AssertEqual(1, Invoke<int>(inventory, "GetItemCount", "youkai_beast_meat"), "Elite classification should grant the elite meat table item in forced-table mode.");
                AssertEqual(1, Invoke<int>(inventory, "GetItemCount", "element_crystal_fire"), "Fire enemy group should grant the fire crystal table item in forced-table mode.");
                AssertEqual(1, Invoke<int>(inventory, "GetItemCount", "seed_pumpkin"), "Forced common seed table should grant a common combat seed.");
                AssertEqual(1, Invoke<int>(inventory, "GetItemCount", "seed_fire_eggplant"), "Elite classification should grant a rare seed table item in forced-table mode.");
                AssertEqual(1, Invoke<int>(inventory, "GetItemCount", "dungeon_compost"), "Forced fertilizer table should grant dungeon compost.");
                AssertEqual(1, Invoke<int>(inventory, "GetItemCount", "spirit_soil"), "Elite classification should grant deep fertilizer in forced-table mode.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(targetObject);
            }

            object bossInventory = Activator.CreateInstance(RequiredType(InventoryServiceTypeName), itemDatabase, 48);
            GameObject bossObject = new GameObject("CombatLootQuestSmoke_BossLootTableTarget");
            try
            {
                object bossTarget = bossObject.AddComponent(targetBehaviourType);
                Invoke(bossTarget, "Initialize", 5f);
                object bossLootHandler = bossObject.AddComponent(lootHandlerType);
                Invoke(bossLootHandler, "BindTarget", bossTarget);
                Invoke(bossLootHandler, "BindServices", bossInventory, null);
                Invoke(bossLootHandler, "ConfigureGodotLootTables", "boss", "wind_enemy", true);

                Invoke(bossTarget, "ApplyDamage", 6f);

                AssertEqual(1, Invoke<int>(bossInventory, "GetItemCount", "youkai_beast_meat"), "Boss classification should grant boss meat table output in forced-table mode.");
                AssertEqual(1, Invoke<int>(bossInventory, "GetItemCount", "element_crystal_wind"), "Wind enemy group should grant the wind crystal table item in forced-table mode.");
                AssertEqual(1, Invoke<int>(bossInventory, "GetItemCount", "seed_shadow_root"), "Boss classification should grant boss seed table output in forced-table mode.");
                AssertEqual(1, Invoke<int>(bossInventory, "GetItemCount", "dungeon_compost"), "Boss forced-table drops should still include common fertilizer.");
                AssertEqual(1, Invoke<int>(bossInventory, "GetItemCount", "spirit_soil"), "Boss classification should grant deep fertilizer in forced-table mode.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(bossObject);
            }
        }

        private static void TestHumanVillageSceneContainsLootDropAdapter()
        {
            Type lootHandlerType = RequiredType(CombatLootDropHandlerTypeName);
            EditorSceneManager.OpenScene(HumanVillageScenePath);

            AssertEqual(true, CountComponents(lootHandlerType) >= 1, "Human Village combat target should mount a loot drop handler.");
        }

        private static object LoadItemDatabase()
        {
            object database = Activator.CreateInstance(RequiredType(ItemDatabaseTypeName));
            AssertEqual(true, Invoke<bool>(database, "LoadFromPath", ItemDataPath), "Item database should load Godot items JSON.");
            return database;
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
            int currentIndex = 0;
            foreach (object item in (System.Collections.IEnumerable)progress)
            {
                if (currentIndex == index)
                {
                    return (int)item;
                }

                currentIndex++;
            }

            throw new Exception($"Expected progress at index {index}.");
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

        private static void AssertEqual<T>(T expected, T actual, string message)
        {
            if (!Equals(expected, actual))
            {
                throw new Exception($"{message} Expected: {expected}. Actual: {actual}.");
            }
        }
    }
}
