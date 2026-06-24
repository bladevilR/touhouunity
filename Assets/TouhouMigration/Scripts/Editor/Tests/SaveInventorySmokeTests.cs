using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    public static class SaveInventorySmokeTests
    {
        private const string ItemDatabaseTypeName = "TouhouMigration.Runtime.Inventory.ItemDatabase, Assembly-CSharp";
        private const string InventoryServiceTypeName = "TouhouMigration.Runtime.Inventory.InventoryService, Assembly-CSharp";
        private const string CookingSnapshotTypeName = "TouhouMigration.Runtime.Cooking.CookingRuntimeSnapshot, Assembly-CSharp";
        private const string SaveDataTypeName = "TouhouMigration.Runtime.Save.MigrationSaveData, Assembly-CSharp";
        private const string SaveServiceTypeName = "TouhouMigration.Runtime.Save.MigrationSaveService, Assembly-CSharp";
        private const string ItemDataPath = "Assets/TouhouMigration/Data/Items/items.json";

        [MenuItem("Touhou Migration/Tests/Run Save Inventory Smoke Tests")]
        public static void RunAll()
        {
            TestItemDatabaseLoadsGodotItems();
            TestInventoryStacksAndSerializes();
            TestSaveServiceRoundTripsSlotData();
            Debug.Log("Save inventory smoke tests passed.");
        }

        private static void TestItemDatabaseLoadsGodotItems()
        {
            object database = LoadItemDatabase();

            AssertEqual(13, GetProperty<int>(database, "CategoryCount"), "Item category count should match Godot items.json.");
            AssertEqual(200, GetProperty<int>(database, "ItemCount"), "Item count should match Godot items.json.");
            AssertEqual(true, Invoke<bool>(database, "HasItem", "seed_apple"), "Apple seed should exist.");
            AssertEqual(true, Invoke<bool>(database, "HasItem", "health_potion_small"), "Small health potion should exist.");
            AssertEqual(true, Invoke<bool>(database, "HasItem", "spirit_crystal"), "Spirit crystal should exist.");

            object appleSeed = Invoke(database, "GetItem", "seed_apple");
            AssertEqual("苹果种子", GetProperty<string>(appleSeed, "Name"), "Apple seed should preserve Chinese display name.");
            AssertEqual("seed", GetProperty<string>(appleSeed, "ItemType"), "Seeds category should normalize to seed item type.");
            AssertEqual(99, GetProperty<int>(appleSeed, "MaxStack"), "Seed default max stack should be 99.");

            object sword = Invoke(database, "GetItem", "sword_basic");
            AssertEqual("equipment", GetProperty<string>(sword, "ItemType"), "Equipment category should normalize to equipment item type.");
            AssertEqual(1, GetProperty<int>(sword, "MaxStack"), "Equipment default max stack should be 1.");

            object spiritCrystal = Invoke(database, "GetItem", "spirit_crystal");
            AssertEqual(999, GetProperty<int>(spiritCrystal, "MaxStack"), "Explicit max stack should be preserved.");
        }

        private static void TestInventoryStacksAndSerializes()
        {
            object database = LoadItemDatabase();
            object inventory = Activator.CreateInstance(RequiredType(InventoryServiceTypeName), database, 48);

            AssertEqual(true, Invoke<bool>(inventory, "AddItem", "seed_apple", 120), "Adding stackable seeds should succeed.");
            AssertEqual(120, Invoke<int>(inventory, "GetItemCount", "seed_apple"), "Seed count should reflect stacked amount.");
            AssertEqual(2, GetProperty<int>(inventory, "UsedSlots"), "120 seeds should occupy 2 slots with max stack 99.");

            AssertEqual(true, Invoke<bool>(inventory, "RemoveItem", "seed_apple", 25), "Removing seeds should succeed.");
            AssertEqual(95, Invoke<int>(inventory, "GetItemCount", "seed_apple"), "Seed count should decrease after removal.");
            AssertEqual(2, GetProperty<int>(inventory, "UsedSlots"), "Removing 25 from 120 seeds should leave two non-empty stacks.");

            AssertEqual(true, Invoke<bool>(inventory, "AddItem", "sword_basic", 2), "Adding equipment should use one slot per item.");
            AssertEqual(2, Invoke<int>(inventory, "GetItemCount", "sword_basic"), "Equipment count should be tracked.");
            AssertEqual(4, GetProperty<int>(inventory, "UsedSlots"), "Two swords plus two seed stacks should occupy 4 slots.");

            object snapshot = Invoke(inventory, "CreateSnapshot");
            AssertEqual(48, CountEnumerable(GetProperty<object>(snapshot, "Slots")), "Inventory snapshot should preserve 48 slots.");

            object restored = Activator.CreateInstance(RequiredType(InventoryServiceTypeName), database, 48);
            Invoke(restored, "LoadSnapshot", snapshot);
            AssertEqual(95, Invoke<int>(restored, "GetItemCount", "seed_apple"), "Restored inventory should preserve seed count.");
            AssertEqual(2, Invoke<int>(restored, "GetItemCount", "sword_basic"), "Restored inventory should preserve equipment count.");
        }

        private static void TestSaveServiceRoundTripsSlotData()
        {
            object database = LoadItemDatabase();
            object inventory = Activator.CreateInstance(RequiredType(InventoryServiceTypeName), database, 48);
            Invoke(inventory, "AddItem", "seed_carrot", 12);
            Invoke(inventory, "AddItem", "health_potion_small", 3);
            Invoke(inventory, "AddItem", "grilled_fish", 1, 2);

            object cookingSnapshot = Activator.CreateInstance(RequiredType(CookingSnapshotTypeName));
            SetProperty(cookingSnapshot, "CookingLevel", 3);
            SetProperty(cookingSnapshot, "CookingExperience", 42);
            SetProperty(cookingSnapshot, "CookwareLevel", 2);
            Invoke(cookingSnapshot, "AddUnlockedRecipe", "spicy_beast_skewer");

            Type saveDataType = RequiredType(SaveDataTypeName);
            object saveData = InvokeStatic(saveDataType, "CreateDefault");
            SetProperty(saveData, "PlayerName", "藤原妹红");
            SetProperty(saveData, "Level", 3);
            SetProperty(saveData, "Coins", 250);
            SetProperty(saveData, "CurrentScene", "bamboo_home");
            SetProperty(saveData, "Inventory", Invoke(inventory, "CreateSnapshot"));
            SetProperty(saveData, "Cooking", cookingSnapshot);

            string saveRoot = Path.Combine(Path.GetTempPath(), "touhou_migration_save_inventory_tests");
            if (Directory.Exists(saveRoot))
            {
                Directory.Delete(saveRoot, true);
            }

            object saveService = Activator.CreateInstance(RequiredType(SaveServiceTypeName), saveRoot);
            AssertEqual(true, Invoke<bool>(saveService, "SaveSlot", 1, saveData), "Save service should write slot 1.");
            AssertEqual(true, Invoke<bool>(saveService, "HasSave", 1), "Save service should report slot 1 exists.");

            object info = Invoke(saveService, "GetSaveInfo", 1);
            AssertEqual("藤原妹红", GetProperty<string>(info, "PlayerName"), "Save info should expose player name.");
            AssertEqual("bamboo_home", GetProperty<string>(info, "CurrentScene"), "Save info should expose current scene.");

            object loaded = Invoke(saveService, "LoadSlot", 1);
            AssertEqual(3, GetProperty<int>(loaded, "SaveSchema"), "Save schema should match Godot SaveSchema.CURRENT_SAVE_SCHEMA.");
            AssertEqual("3.0.0", GetProperty<string>(loaded, "GameVersion"), "Game version should match Godot SaveSchema.CURRENT_GAME_VERSION.");
            AssertEqual(250, GetProperty<int>(loaded, "Coins"), "Loaded save should preserve coins.");

            object restoredInventory = Activator.CreateInstance(RequiredType(InventoryServiceTypeName), database, 48);
            Invoke(restoredInventory, "LoadSnapshot", GetProperty<object>(loaded, "Inventory"));
            AssertEqual(12, Invoke<int>(restoredInventory, "GetItemCount", "seed_carrot"), "Loaded save should preserve carrot seeds.");
            AssertEqual(3, Invoke<int>(restoredInventory, "GetItemCount", "health_potion_small"), "Loaded save should preserve potions.");
            AssertEqual(1, Invoke<int>(restoredInventory, "GetItemCount", "grilled_fish", 2), "Loaded save should preserve cooked dish quality stack.");

            object loadedCooking = GetProperty<object>(loaded, "Cooking");
            AssertEqual(3, GetProperty<int>(loadedCooking, "CookingLevel"), "Loaded save should preserve cooking level.");
            AssertEqual(42, GetProperty<int>(loadedCooking, "CookingExperience"), "Loaded save should preserve cooking exp.");
            AssertEqual(2, GetProperty<int>(loadedCooking, "CookwareLevel"), "Loaded save should preserve cookware level.");
            AssertEqual(true, Invoke<bool>(loadedCooking, "HasUnlockedRecipe", "spicy_beast_skewer"), "Loaded save should preserve unlocked recipes.");
        }

        private static object LoadItemDatabase()
        {
            object database = Activator.CreateInstance(RequiredType(ItemDatabaseTypeName));
            AssertEqual(true, Invoke<bool>(database, "LoadFromPath", ItemDataPath), "Item database should load Godot items.json from Unity data directory.");
            return database;
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
