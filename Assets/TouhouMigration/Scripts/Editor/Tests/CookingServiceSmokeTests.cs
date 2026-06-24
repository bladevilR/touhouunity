using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    public static class CookingServiceSmokeTests
    {
        private const string CookingDatabaseTypeName = "TouhouMigration.Runtime.Cooking.CookingDatabase, Assembly-CSharp";
        private const string CookingServiceTypeName = "TouhouMigration.Runtime.Cooking.CookingService, Assembly-CSharp";
        private const string ItemDatabaseTypeName = "TouhouMigration.Runtime.Inventory.ItemDatabase, Assembly-CSharp";
        private const string InventoryServiceTypeName = "TouhouMigration.Runtime.Inventory.InventoryService, Assembly-CSharp";
        private const string PlayerProgressTypeName = "TouhouMigration.Runtime.Player.MigrationPlayerProgressService, Assembly-CSharp";
        private const string QuestDatabaseTypeName = "TouhouMigration.Runtime.Quest.QuestDatabase, Assembly-CSharp";
        private const string QuestRewardLedgerTypeName = "TouhouMigration.Runtime.Quest.QuestRewardLedger, Assembly-CSharp";
        private const string QuestRewardSinkTypeName = "TouhouMigration.Runtime.Quest.QuestRewardSink, Assembly-CSharp";
        private const string QuestDeliveryServiceTypeName = "TouhouMigration.Runtime.Social.QuestDeliveryService, Assembly-CSharp";
        private const string CookingProfilesPath = "Assets/TouhouMigration/Data/Cooking/cooking_profiles.json";
        private const string CookingRecipesPath = "Assets/TouhouMigration/Data/Cooking/cooking_recipes.json";
        private const string ItemDataPath = "Assets/TouhouMigration/Data/Items/items.json";
        private const string QuestDataPath = "Assets/TouhouMigration/Data/Quests/quests.json";

        [MenuItem("Touhou Migration/Tests/Run Cooking Service Smoke Tests")]
        public static void RunAll()
        {
            TestRecipeDatabaseLoadsGodotRecipesAndCookwareRules();
            TestCookingServiceConsumesIngredientsProducesQualityDishAndQuestProgress();
            Debug.Log("Cooking service smoke tests passed.");
        }

        private static void TestRecipeDatabaseLoadsGodotRecipesAndCookwareRules()
        {
            object cooking = LoadCookingDatabase();

            AssertEqual(40, GetProperty<int>(cooking, "RecipeCount"), "Cooking database should load all Godot recipes.");
            object grilledFish = Invoke(cooking, "GetRecipe", "grilled_fish");
            AssertEqual("grilled_fish", GetProperty<string>(grilledFish, "Id"), "Recipe id should be normalized.");
            AssertEqual("烤鱼", GetProperty<string>(grilledFish, "Name"), "Recipe name should preserve Godot text.");
            AssertEqual("grilled_fish", GetProperty<string>(grilledFish, "ResultId"), "Recipe result id should load.");
            AssertEqual(1, GetProperty<int>(grilledFish, "ResultQuantity"), "Recipe result quantity should load.");
            AssertEqual(2, GetProperty<int>(Invoke(cooking, "GetRecipe", "onigiri"), "ResultQuantity"), "Onigiri should produce two rice balls.");
            AssertEqual("meal", Invoke<string>(cooking, "GetRecipeTier", "phoenix_roast_chicken"), "Meal recipe tier should come from its result profile.");
            AssertEqual(2, Invoke<int>(cooking, "GetRequiredCookwareLevelForTier", "meal"), "Meal recipes should require iron cookware.");
            AssertEqual(false, Invoke<bool>(cooking, "CanCookWithCookware", "phoenix_roast_chicken", 1), "Cookware level 1 should not cook meals.");
            AssertEqual(true, Invoke<bool>(cooking, "CanCookWithCookware", "phoenix_roast_chicken", 2), "Cookware level 2 should cook meals.");
        }

        private static void TestCookingServiceConsumesIngredientsProducesQualityDishAndQuestProgress()
        {
            object cooking = LoadCookingDatabase();
            object items = LoadItemDatabase();
            object inventory = Activator.CreateInstance(RequiredType(InventoryServiceTypeName), items, 48);
            object progress = Activator.CreateInstance(RequiredType(PlayerProgressTypeName));
            object rewardSink = Activator.CreateInstance(RequiredType(QuestRewardSinkTypeName), inventory, progress);
            object ledger = Activator.CreateInstance(RequiredType(QuestRewardLedgerTypeName));
            object quests = Activator.CreateInstance(RequiredType(QuestDeliveryServiceTypeName), LoadQuestDatabase(), ledger, cooking, rewardSink);
            object cookingService = Activator.CreateInstance(RequiredType(CookingServiceTypeName), cooking, inventory, items, quests);

            Invoke(inventory, "AddItem", "crucian_carp", 1);
            Invoke(inventory, "AddItem", "salt", 1);
            AssertEqual(true, Invoke<bool>(cookingService, "CanCook", "grilled_fish"), "Default unlocked grilled fish should be cookable with any fish and salt.");
            object fishResult = Invoke(cookingService, "Cook", "grilled_fish", 0.76f);
            AssertEqual(true, GetProperty<bool>(fishResult, "Success"), "Cooking should succeed.");
            AssertEqual("grilled_fish", GetProperty<string>(fishResult, "ResultItemId"), "Cooking result id should match recipe result.");
            AssertEqual(2, GetProperty<int>(fishResult, "Quality"), "Quality roll plus cookware bonus should produce rare quality.");
            AssertEqual(0, Invoke<int>(inventory, "GetItemCount", "crucian_carp"), "Any-fish ingredient should be consumed from real inventory.");
            AssertEqual(0, Invoke<int>(inventory, "GetItemCount", "salt"), "Regular ingredient should be consumed.");
            AssertEqual(1, Invoke<int>(inventory, "GetItemCount", "grilled_fish"), "Cooked dish should be added to inventory.");
            AssertEqual(1, Invoke<int>(inventory, "GetItemCount", "grilled_fish", 2), "Cooked dish should keep its quality-specific stack identity.");
            AssertEqual(10, GetProperty<int>(cookingService, "CookingExperience"), "Cooking exp should increase by recipe exp_gain.");

            Invoke(quests, "MarkQuestCompleted", "side_004");
            AssertEqual(true, Invoke<bool>(quests, "StartQuest", "side_005"), "side_005 should start once side_004 is completed.");
            Invoke(cookingService, "SetCookwareLevel", 2);
            Invoke(cookingService, "UnlockRecipe", "spicy_beast_skewer");
            Invoke(inventory, "AddItem", "beast_meat", 1);
            Invoke(inventory, "AddItem", "chili", 2);
            object skewerResult = Invoke(cookingService, "Cook", "spicy_beast_skewer", 0.80f);
            AssertEqual(true, GetProperty<bool>(skewerResult, "Success"), "Unlocked meal recipe should cook with level 2 cookware.");
            AssertEqual(1, ProgressAt(quests, "side_005", 0), "Cooking service should notify craft completion for quest craft_stat objectives.");
            AssertEqual(30, GetProperty<int>(cookingService, "CookingExperience"), "Cooking exp should accumulate.");
        }

        private static object LoadCookingDatabase()
        {
            object database = Activator.CreateInstance(RequiredType(CookingDatabaseTypeName));
            AssertEqual(true, Invoke<bool>(database, "LoadFromPath", CookingProfilesPath), "Cooking profiles should load.");
            AssertEqual(true, Invoke<bool>(database, "LoadRecipesFromPath", CookingRecipesPath), "Cooking recipes should load.");
            return database;
        }

        private static object LoadItemDatabase()
        {
            object database = Activator.CreateInstance(RequiredType(ItemDatabaseTypeName));
            AssertEqual(true, Invoke<bool>(database, "LoadFromPath", ItemDataPath), "Item database should load.");
            return database;
        }

        private static object LoadQuestDatabase()
        {
            object database = Activator.CreateInstance(RequiredType(QuestDatabaseTypeName));
            AssertEqual(true, Invoke<bool>(database, "LoadFromPath", QuestDataPath), "Quest database should load.");
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
