using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    public static class CookingQuestSmokeTests
    {
        private const string CookingDatabaseTypeName = "TouhouMigration.Runtime.Cooking.CookingDatabase, Assembly-CSharp";
        private const string ItemDatabaseTypeName = "TouhouMigration.Runtime.Inventory.ItemDatabase, Assembly-CSharp";
        private const string InventoryServiceTypeName = "TouhouMigration.Runtime.Inventory.InventoryService, Assembly-CSharp";
        private const string PlayerProgressTypeName = "TouhouMigration.Runtime.Player.MigrationPlayerProgressService, Assembly-CSharp";
        private const string QuestDatabaseTypeName = "TouhouMigration.Runtime.Quest.QuestDatabase, Assembly-CSharp";
        private const string QuestRewardLedgerTypeName = "TouhouMigration.Runtime.Quest.QuestRewardLedger, Assembly-CSharp";
        private const string QuestRewardSinkTypeName = "TouhouMigration.Runtime.Quest.QuestRewardSink, Assembly-CSharp";
        private const string QuestDeliveryServiceTypeName = "TouhouMigration.Runtime.Social.QuestDeliveryService, Assembly-CSharp";
        private const string CookingDataPath = "Assets/TouhouMigration/Data/Cooking/cooking_profiles.json";
        private const string ItemDataPath = "Assets/TouhouMigration/Data/Items/items.json";
        private const string QuestDataPath = "Assets/TouhouMigration/Data/Quests/quests.json";

        [MenuItem("Touhou Migration/Tests/Run Cooking Quest Smoke Tests")]
        public static void RunAll()
        {
            TestCookingDatabaseLoadsGodotDishProfilesAndClassifiers();
            TestQuestCraftDeliveryAndRewardsUseCookingAndRealServices();
            Debug.Log("Cooking quest smoke tests passed.");
        }

        private static void TestCookingDatabaseLoadsGodotDishProfilesAndClassifiers()
        {
            object cooking = LoadCookingDatabase();

            AssertEqual(40, GetProperty<int>(cooking, "DishCount"), "Cooking database should load all Godot dish combat profiles.");
            AssertEqual(true, Invoke<bool>(cooking, "HasDishCombatProfile", "spicy_beast_skewer"), "Attack dish should exist.");
            AssertEqual("drink", Invoke<string>(cooking, "GetDishTier", "green_tea"), "Green tea should be classified as a drink.");
            AssertEqual("meal", Invoke<string>(cooking, "GetDishTier", "phoenix_roast_chicken"), "Phoenix roast chicken should be a meal.");
            AssertEqual("feast", Invoke<string>(cooking, "GetDishTier", "mokou_yakitori"), "Mokou yakitori should be a feast.");
            AssertEqual("atk", Invoke<string>(cooking, "GetDishMainStat", "spicy_beast_skewer"), "Spicy beast skewer should be an attack-main dish.");
            AssertEqual(5, Invoke<int>(cooking, "GetDishStat", "spicy_beast_skewer", "atk"), "Spicy beast skewer should preserve atk=5.");

            AssertEqual(true, Invoke<bool>(cooking, "DishMatchesTier", "reishi_tea", "drink"), "Reishi tea should match drink tier.");
            AssertEqual(false, Invoke<bool>(cooking, "DishMatchesTier", "green_tea", "meal"), "Green tea should not match meal tier.");
            AssertEqual(true, Invoke<bool>(cooking, "DishMatchesStatRequirement", "spicy_beast_skewer", "atk", 5), "Attack quest dish should satisfy atk>=5.");
            AssertEqual(false, Invoke<bool>(cooking, "DishMatchesStatRequirement", "grilled_fish", "atk", 5), "Low attack snack should not satisfy atk>=5.");

            AssertEqual(true, Invoke<bool>(cooking, "IsSymbolicItemMatch", "spicy_beast_skewer", "atk_5_plus_any"), "Symbolic atk selector should match atk>=5 dishes.");
            AssertEqual(true, Invoke<bool>(cooking, "IsSymbolicItemMatch", "reishi_tea", "drink_any"), "Symbolic drink selector should match drinks.");
            AssertEqual(true, Invoke<bool>(cooking, "IsSymbolicItemMatch", "miso_soup", "meal_any"), "Symbolic meal selector should match meals.");
            AssertEqual(true, Invoke<bool>(cooking, "IsSymbolicItemMatch", "mokou_yakitori", "feast_any"), "Symbolic feast selector should match feasts.");
        }

        private static void TestQuestCraftDeliveryAndRewardsUseCookingAndRealServices()
        {
            object cooking = LoadCookingDatabase();
            object items = LoadItemDatabase();
            object inventory = Activator.CreateInstance(RequiredType(InventoryServiceTypeName), items, 48);
            object progress = Activator.CreateInstance(RequiredType(PlayerProgressTypeName));
            object rewardSink = Activator.CreateInstance(RequiredType(QuestRewardSinkTypeName), inventory, progress);
            object ledger = Activator.CreateInstance(RequiredType(QuestRewardLedgerTypeName));
            object quests = Activator.CreateInstance(RequiredType(QuestDeliveryServiceTypeName), LoadQuestDatabase(), ledger, cooking, rewardSink);

            Invoke(quests, "MarkQuestCompleted", "side_004");
            AssertEqual(true, Invoke<bool>(quests, "StartQuest", "side_005"), "side_005 should start once side_004 is completed.");
            Invoke(quests, "NotifyCraftCompleted", "spicy_beast_skewer", 1);
            AssertEqual(1, ProgressAt(quests, "side_005", 0), "craft_stat objective should progress from CookingDatabase atk>=5 classifier.");
            Invoke(quests, "NotifyDelivery", "spicy_beast_skewer", 1, "marisa");
            AssertEqual(true, Invoke<bool>(quests, "IsQuestCompleted", "side_005"), "atk_5_plus_any delivery should complete side_005 without manual delivery tags.");
            AssertEqual(260, GetProperty<int>(ledger, "Exp"), "Ledger should record side_005 exp.");
            AssertEqual(240, GetProperty<int>(ledger, "Coins"), "Ledger should record side_005 coins.");
            AssertEqual(260, GetProperty<int>(progress, "Experience"), "Reward sink should grant quest exp to player progress.");
            AssertEqual(240, GetProperty<int>(progress, "Coins"), "Reward sink should grant quest coins to player progress.");
            AssertEqual(3, Invoke<int>(inventory, "GetItemCount", "seed_fire_eggplant"), "Reward sink should add quest reward seeds to inventory.");
            AssertEqual(10, Invoke<int>(inventory, "GetItemCount", "spice"), "Reward sink should add quest reward materials to inventory.");

            AssertEqual(true, Invoke<bool>(quests, "StartQuest", "side_006"), "side_006 should start after side_005 completion.");
            Invoke(quests, "NotifyCraftCompleted", "green_tea", 1);
            Invoke(quests, "NotifyCraftCompleted", "reishi_tea", 1);
            Invoke(quests, "NotifyCraftCompleted", "bamboo_leaf_wine", 1);
            AssertEqual(3, ProgressAt(quests, "side_006", 0), "craft_tier drink objective should use CookingDatabase tier classifier.");
            Invoke(quests, "NotifyDelivery", "green_tea", 1, "sakuya");
            Invoke(quests, "NotifyDelivery", "reishi_tea", 1, "sakuya");
            Invoke(quests, "NotifyDelivery", "bamboo_leaf_wine", 1, "sakuya");
            AssertEqual(true, Invoke<bool>(quests, "IsQuestCompleted", "side_006"), "drink_any variety delivery should complete without manual delivery tags.");
            AssertEqual(540, GetProperty<int>(progress, "Experience"), "Second quest completion should accumulate player exp.");
            AssertEqual(500, GetProperty<int>(progress, "Coins"), "Second quest completion should accumulate player coins.");
            AssertEqual(2, Invoke<int>(inventory, "GetItemCount", "reishi_tea"), "side_006 reward drink should be added to real inventory.");
            AssertEqual(1, Invoke<int>(inventory, "GetItemCount", "moon_fish"), "side_006 reward fish should be added to real inventory.");
        }

        private static object LoadCookingDatabase()
        {
            object database = Activator.CreateInstance(RequiredType(CookingDatabaseTypeName));
            AssertEqual(true, Invoke<bool>(database, "LoadFromPath", CookingDataPath), "Cooking database should load migrated Godot cooking profile JSON.");
            return database;
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

        private static void AssertEqual<T>(T expected, T actual, string message)
        {
            if (!Equals(expected, actual))
            {
                throw new Exception($"{message} Expected: {expected}. Actual: {actual}.");
            }
        }
    }
}
