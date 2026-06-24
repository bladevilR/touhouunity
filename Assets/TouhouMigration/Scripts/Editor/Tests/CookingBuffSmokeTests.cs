using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    public static class CookingBuffSmokeTests
    {
        private const string CookingDatabaseTypeName = "TouhouMigration.Runtime.Cooking.CookingDatabase, Assembly-CSharp";
        private const string CookingBuffServiceTypeName = "TouhouMigration.Runtime.Cooking.CookingBuffService, Assembly-CSharp";
        private const string ItemUseServiceTypeName = "TouhouMigration.Runtime.Inventory.ItemUseService, Assembly-CSharp";
        private const string ItemDatabaseTypeName = "TouhouMigration.Runtime.Inventory.ItemDatabase, Assembly-CSharp";
        private const string InventoryServiceTypeName = "TouhouMigration.Runtime.Inventory.InventoryService, Assembly-CSharp";
        private const string SaveDataTypeName = "TouhouMigration.Runtime.Save.MigrationSaveData, Assembly-CSharp";
        private const string SaveServiceTypeName = "TouhouMigration.Runtime.Save.MigrationSaveService, Assembly-CSharp";
        private const string CookingProfilesPath = "Assets/TouhouMigration/Data/Cooking/cooking_profiles.json";
        private const string ItemDataPath = "Assets/TouhouMigration/Data/Items/items.json";

        [MenuItem("Touhou Migration/Tests/Run Cooking Buff Smoke Tests")]
        public static void RunAll()
        {
            TestItemDatabasePreservesUsableEffects();
            TestQualityDishUseAppliesCookingBuffAndConsumesMatchingStack();
            TestDrinkUseAppliesDrinkSlotAndExpires();
            TestCookingBuffSnapshotRoundTripsThroughSaveService();
            Debug.Log("Cooking buff smoke tests passed.");
        }

        private static void TestItemDatabasePreservesUsableEffects()
        {
            object database = LoadItemDatabase();
            object potion = Invoke(database, "GetItem", "health_potion_small");
            object speedBoost = Invoke(database, "GetItem", "speed_boost");
            object grilledFish = Invoke(database, "GetItem", "grilled_fish");

            AssertEqual(50, Invoke<int>(potion, "GetEffectInt", "heal_hp"), "Potion heal amount should be loaded from effects.");
            AssertEqual("speed_boost", Invoke<string>(speedBoost, "GetEffectString", "buff"), "Buff consumable id should be loaded from effects.");
            AssertEqual(80, Invoke<int>(grilledFish, "GetEffectInt", "heal_hp"), "Dish top-level heal_hp should be exposed as a usable effect.");
        }

        private static void TestQualityDishUseAppliesCookingBuffAndConsumesMatchingStack()
        {
            object cooking = LoadCookingDatabase();
            object items = LoadItemDatabase();
            object inventory = Activator.CreateInstance(RequiredType(InventoryServiceTypeName), items, 48);
            object buffService = Activator.CreateInstance(RequiredType(CookingBuffServiceTypeName), cooking);
            object itemUseService = Activator.CreateInstance(RequiredType(ItemUseServiceTypeName), inventory, items, buffService);

            Invoke(inventory, "AddItem", "spicy_beast_skewer", 1, 0);
            Invoke(inventory, "AddItem", "spicy_beast_skewer", 1, 2);

            object result = Invoke(itemUseService, "UseItem", "spicy_beast_skewer", 2);
            AssertEqual(true, GetProperty<bool>(result, "Success"), "Using a quality dish should succeed.");
            AssertEqual(true, GetProperty<bool>(result, "AppliedCookingBuff"), "Dish use should apply cooking buff service.");
            AssertEqual(0, Invoke<int>(inventory, "GetItemCount", "spicy_beast_skewer", 2), "Quality 2 dish stack should be consumed.");
            AssertEqual(1, Invoke<int>(inventory, "GetItemCount", "spicy_beast_skewer", 0), "Quality 0 dish stack should remain untouched.");
            AssertEqual(8, Invoke<int>(buffService, "GetStatValue", "atk"), "Quality 2 spicy skewer should floor 5 atk * 1.6 to 8.");
            AssertEqual(1, Invoke<int>(buffService, "GetStatValue", "spd"), "Quality 2 spicy skewer should floor 1 spd * 1.6 to 1.");
            AssertEqual(true, Invoke<bool>(buffService, "IsThresholdActive", "atk", 6), "Atk 6 threshold should unlock.");
            AssertApproximately(1.32f, Invoke<float>(buffService, "GetDamageMultiplier"), 0.001f, "Atk 8 should map to Godot damage multiplier.");
        }

        private static void TestDrinkUseAppliesDrinkSlotAndExpires()
        {
            object cooking = LoadCookingDatabase();
            object items = LoadItemDatabase();
            object inventory = Activator.CreateInstance(RequiredType(InventoryServiceTypeName), items, 48);
            object buffService = Activator.CreateInstance(RequiredType(CookingBuffServiceTypeName), cooking);
            object itemUseService = Activator.CreateInstance(RequiredType(ItemUseServiceTypeName), inventory, items, buffService);

            Invoke(inventory, "AddItem", "green_tea", 1);
            object result = Invoke(itemUseService, "UseItem", "green_tea", 0);
            AssertEqual(true, GetProperty<bool>(result, "Success"), "Using a drink should succeed.");
            AssertEqual(true, GetProperty<bool>(result, "AppliedCookingBuff"), "Drink use should apply cooking buff service.");
            AssertEqual(true, Invoke<bool>(buffService, "HasActiveDrink"), "Green tea should occupy the drink slot.");
            AssertEqual(1, Invoke<int>(buffService, "GetStatValue", "spi"), "Green tea should add 1 spi.");
            AssertEqual(true, Invoke<bool>(buffService, "HasDrinkEffect", "drink_focus"), "Green tea special effect should be treated as a drink effect.");
            AssertApproximately(1.219f, Invoke<float>(buffService, "GetSpiritChargeMultiplier"), 0.001f, "Green tea should apply spi and drink focus multipliers.");

            Invoke(buffService, "Tick", 540.1f);
            AssertEqual(false, Invoke<bool>(buffService, "HasActiveDrink"), "Green tea should expire after its duration.");
            AssertEqual(0, Invoke<int>(buffService, "GetStatValue", "spi"), "Expired drink should no longer contribute stats.");
        }

        private static void TestCookingBuffSnapshotRoundTripsThroughSaveService()
        {
            object cooking = LoadCookingDatabase();
            object items = LoadItemDatabase();
            object inventory = Activator.CreateInstance(RequiredType(InventoryServiceTypeName), items, 48);
            object buffService = Activator.CreateInstance(RequiredType(CookingBuffServiceTypeName), cooking);
            object itemUseService = Activator.CreateInstance(RequiredType(ItemUseServiceTypeName), inventory, items, buffService);

            Invoke(inventory, "AddItem", "spicy_beast_skewer", 1, 2);
            Invoke(inventory, "AddItem", "green_tea", 1);
            AssertEqual(true, GetProperty<bool>(Invoke(itemUseService, "UseItem", "spicy_beast_skewer", 2), "Success"), "Dish use should succeed before save.");
            AssertEqual(true, GetProperty<bool>(Invoke(itemUseService, "UseItem", "green_tea", 0), "Success"), "Drink use should succeed before save.");

            Type saveDataType = RequiredType(SaveDataTypeName);
            object saveData = InvokeStatic(saveDataType, "CreateDefault");
            SetProperty(saveData, "CookingBuffs", Invoke(buffService, "CreateSnapshot"));

            string saveRoot = Path.Combine(Path.GetTempPath(), "touhou_migration_cooking_buff_tests");
            if (Directory.Exists(saveRoot))
            {
                Directory.Delete(saveRoot, true);
            }

            object saveService = Activator.CreateInstance(RequiredType(SaveServiceTypeName), saveRoot);
            AssertEqual(true, Invoke<bool>(saveService, "SaveSlot", 2, saveData), "Save service should write cooking buff slot.");

            object loaded = Invoke(saveService, "LoadSlot", 2);
            object loadedBuffSnapshot = GetProperty<object>(loaded, "CookingBuffs");
            object restoredBuffService = Activator.CreateInstance(RequiredType(CookingBuffServiceTypeName), cooking);
            Invoke(restoredBuffService, "LoadSnapshot", loadedBuffSnapshot);

            AssertEqual(8, Invoke<int>(restoredBuffService, "GetStatValue", "atk"), "Loaded buff snapshot should preserve quality-scaled atk.");
            AssertEqual(2, FirstOccupiedSlotQuality(restoredBuffService), "Loaded buff snapshot should preserve dish quality.");
            AssertEqual(true, Invoke<bool>(restoredBuffService, "HasActiveDrink"), "Loaded buff snapshot should preserve active drink.");
            AssertEqual(true, Invoke<bool>(restoredBuffService, "IsThresholdActive", "atk", 6), "Loaded buff snapshot should preserve unlocked thresholds.");
        }

        private static object LoadCookingDatabase()
        {
            object database = Activator.CreateInstance(RequiredType(CookingDatabaseTypeName));
            AssertEqual(true, Invoke<bool>(database, "LoadFromPath", CookingProfilesPath), "Cooking profiles should load.");
            return database;
        }

        private static object LoadItemDatabase()
        {
            object database = Activator.CreateInstance(RequiredType(ItemDatabaseTypeName));
            AssertEqual(true, Invoke<bool>(database, "LoadFromPath", ItemDataPath), "Item database should load.");
            return database;
        }

        private static int FirstOccupiedSlotQuality(object buffService)
        {
            object slots = Invoke(buffService, "GetBuffSlots");
            foreach (object slot in (IEnumerable)slots)
            {
                if (!string.IsNullOrWhiteSpace(GetProperty<string>(slot, "DishId")))
                {
                    return GetProperty<int>(slot, "Quality");
                }
            }

            throw new Exception("Expected an occupied buff slot.");
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

        private static void AssertApproximately(float expected, float actual, float tolerance, string message)
        {
            if (Mathf.Abs(expected - actual) > tolerance)
            {
                throw new Exception($"{message} Expected: {expected}. Actual: {actual}.");
            }
        }
    }
}
