using System;
using System.Collections;
using System.Reflection;
using TouhouMigration.Runtime.Dialogue;
using TouhouMigration.Runtime.Inventory;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    public static class SocialLoopSmokeTests
    {
        private const string GiftDatabaseTypeName = "TouhouMigration.Runtime.Social.GiftDatabase, Assembly-CSharp";
        private const string GiftInteractionServiceTypeName = "TouhouMigration.Runtime.Social.GiftInteractionService, Assembly-CSharp";
        private const string SocialBondServiceTypeName = "TouhouMigration.Runtime.Social.SocialBondService, Assembly-CSharp";
        private const string QuestDeliveryServiceTypeName = "TouhouMigration.Runtime.Social.QuestDeliveryService, Assembly-CSharp";
        private const string GiftSelectionControllerTypeName = "TouhouMigration.Runtime.UI.MigrationGiftSelectionController, Assembly-CSharp";
        private const string GlobalUiTypeName = "TouhouMigration.Runtime.UI.MigrationGlobalUiController, Assembly-CSharp";
        private const string GiftDataPath = "Assets/TouhouMigration/Data/Social/gifts.json";
        private const string ItemDataPath = "Assets/TouhouMigration/Data/Items/items.json";
        private const string DialogueDataPath = "Assets/TouhouMigration/Data/Dialogue";
        private const string BambooHomeScenePath = "Assets/TouhouMigration/Scenes/BambooHomeVerticalSlice.unity";
        private const string HumanVillageScenePath = "Assets/TouhouMigration/Scenes/HumanVillageVerticalSlice.unity";

        [MenuItem("Touhou Migration/Tests/Run Social Loop Smoke Tests")]
        public static void RunAll()
        {
            TestGiftSelectionListsGiftableInventoryAndDeliversThroughAdapters();
            TestRuntimeScenesContainGiftSelectionController();
            TestGiftSelectionBlocksGameplayInput();
            Debug.Log("Social loop smoke tests passed.");
        }

        private static void TestGiftSelectionListsGiftableInventoryAndDeliversThroughAdapters()
        {
            object giftDatabase = LoadGiftDatabase();
            object itemDatabase = Activator.CreateInstance(typeof(ItemDatabase));
            AssertEqual(true, Invoke<bool>(itemDatabase, "LoadFromPath", ItemDataPath), "Item database should load for gift-selection test.");

            object inventory = Activator.CreateInstance(typeof(InventoryService), itemDatabase, 48);
            AssertEqual(true, Invoke<bool>(inventory, "AddItem", "magic_crystal", 2), "Inventory should accept giftable magic crystal.");
            AssertEqual(true, Invoke<bool>(inventory, "AddItem", "green_tea", 1), "Inventory should accept giftable green tea.");
            AssertEqual(true, Invoke<bool>(inventory, "AddItem", "seed_apple", 3), "Inventory should accept non-gift seed item.");

            DialogueDatabase dialogueDatabase = new DialogueDatabase();
            AssertEqual(true, dialogueDatabase.LoadFromPath(DialogueDataPath), "Dialogue database should load for gift-selection speaker names.");
            DialogueRuntimeFacade facade = new DialogueRuntimeFacade();

            object bondService = Activator.CreateInstance(RequiredType(SocialBondServiceTypeName));
            object questService = Activator.CreateInstance(RequiredType(QuestDeliveryServiceTypeName));
            object giftService = Activator.CreateInstance(
                RequiredType(GiftInteractionServiceTypeName),
                giftDatabase,
                inventory,
                dialogueDatabase,
                facade,
                bondService,
                questService);

            Type controllerType = RequiredType(GiftSelectionControllerTypeName);
            GameObject ui = new GameObject("GiftSelectionSmokeController");
            Component controller = ui.AddComponent(controllerType);
            Invoke(controller, "Bind", giftDatabase, giftService, inventory, itemDatabase);
            Invoke(controller, "OpenForNpc", "marisa", "雾雨魔理沙");

            AssertEqual(true, GetProperty<bool>(controller, "IsOpen"), "Gift selection should open for an NPC.");
            AssertEqual("marisa", GetProperty<string>(controller, "NpcId"), "Gift selection should remember target NPC id.");
            AssertEqual("雾雨魔理沙", GetProperty<string>(controller, "DisplayName"), "Gift selection should remember target display name.");
            AssertEqual(2, GetProperty<int>(controller, "OptionCount"), "Gift selection should list only inventory items that are valid gifts.");

            object options = Invoke(controller, "GetGiftOptions");
            object firstOption = First(options);
            AssertEqual("magic_crystal", GetProperty<string>(firstOption, "GiftId"), "Recommended gifts should sort strongest reactions first.");
            AssertEqual("魔法水晶", GetProperty<string>(firstOption, "DisplayName"), "Gift option should use the migrated gift display name.");
            AssertEqual(2, GetProperty<int>(firstOption, "Amount"), "Gift option should expose inventory amount.");
            AssertEqual("LOVE", GetProperty<string>(firstOption, "ReactionId"), "Gift option should preview the NPC reaction.");
            AssertEqual(75, GetProperty<int>(firstOption, "BondChange"), "Gift option should preview bond delta.");

            object result = Invoke(controller, "SelectGift", "magic_crystal");
            AssertEqual(true, GetProperty<bool>(result, "Success"), "Selecting a valid gift should deliver it.");
            AssertEqual(false, GetProperty<bool>(controller, "IsOpen"), "Gift selection should close after successful delivery.");
            AssertEqual(1, Invoke<int>(inventory, "GetItemCount", "magic_crystal"), "Selected gift should be removed from inventory once.");
            AssertEqual(75, Invoke<int>(bondService, "GetBondPoints", "marisa"), "Gift delivery should apply bond delta.");
            AssertEqual("gift_positive", GetProperty<string>(bondService, "LastSource"), "Loved gifts should use a positive gift bond source.");
            AssertEqual(1, GetProperty<int>(questService, "DeliveryEventCount"), "Gift delivery should notify quest delivery once.");
            AssertEqual("magic_crystal", GetProperty<string>(questService, "LastItemId"), "Quest delivery should record delivered item id.");
            AssertEqual("marisa", GetProperty<string>(questService, "LastNpcId"), "Quest delivery should record target NPC id.");
            AssertEqual(true, facade.IsActive, "Successful gift delivery should start reaction dialogue.");

            UnityEngine.Object.DestroyImmediate(ui);
        }

        private static void TestRuntimeScenesContainGiftSelectionController()
        {
            Type controllerType = RequiredType(GiftSelectionControllerTypeName);

            EditorSceneManager.OpenScene(BambooHomeScenePath);
            AssertEqual(1, CountComponents(controllerType), "Bamboo Home should mount one gift-selection controller with global UI.");

            EditorSceneManager.OpenScene(HumanVillageScenePath);
            AssertEqual(1, CountComponents(controllerType), "Human Village should mount one gift-selection controller with global UI.");
        }

        private static void TestGiftSelectionBlocksGameplayInput()
        {
            Type globalUiType = RequiredType(GlobalUiTypeName);
            Type giftSelectionType = RequiredType(GiftSelectionControllerTypeName);
            EditorSceneManager.OpenScene(HumanVillageScenePath);

            Component globalUi = FindSingleComponent(globalUiType);
            Component giftSelection = FindSingleComponent(giftSelectionType);

            AssertEqual(false, GetProperty<bool>(globalUi, "BlocksGameplayInput"), "Global UI should not block gameplay input while every overlay is closed.");
            Invoke(giftSelection, "OpenForNpc", "marisa", "雾雨魔理沙");
            AssertEqual(true, GetProperty<bool>(globalUi, "BlocksGameplayInput"), "Open gift selection should block gameplay input pollers.");
            Invoke(giftSelection, "Close");
            AssertEqual(false, GetProperty<bool>(globalUi, "BlocksGameplayInput"), "Closing gift selection should release gameplay input.");
        }

        private static object LoadGiftDatabase()
        {
            object database = Activator.CreateInstance(RequiredType(GiftDatabaseTypeName));
            AssertEqual(true, Invoke<bool>(database, "LoadFromPath", GiftDataPath), "Gift database should load migrated Godot gifts.json.");
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
            IEnumerator enumerator = ((IEnumerable)enumerable).GetEnumerator();
            if (!enumerator.MoveNext())
            {
                throw new Exception("Expected at least one item.");
            }

            return enumerator.Current;
        }

        private static Component FindSingleComponent(Type componentType)
        {
            Component found = null;
            foreach (GameObject gameObject in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include))
            {
                Component component = gameObject.GetComponent(componentType);
                if (component == null)
                {
                    continue;
                }

                if (found != null)
                {
                    throw new Exception($"Expected one {componentType.FullName}, found more than one.");
                }

                found = component;
            }

            if (found == null)
            {
                throw new Exception($"Expected one {componentType.FullName}, found none.");
            }

            return found;
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
