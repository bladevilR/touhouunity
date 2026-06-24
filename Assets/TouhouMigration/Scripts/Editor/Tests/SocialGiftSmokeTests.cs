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
    public static class SocialGiftSmokeTests
    {
        private const string GiftDatabaseTypeName = "TouhouMigration.Runtime.Social.GiftDatabase, Assembly-CSharp";
        private const string GiftInteractionServiceTypeName = "TouhouMigration.Runtime.Social.GiftInteractionService, Assembly-CSharp";
        private const string NpcInteractorTypeName = "TouhouMigration.Runtime.Social.MigrationNpcInteractor, Assembly-CSharp";
        private const string GiftDataPath = "Assets/TouhouMigration/Data/Social/gifts.json";
        private const string ItemDataPath = "Assets/TouhouMigration/Data/Items/items.json";
        private const string DialogueDataPath = "Assets/TouhouMigration/Data/Dialogue";
        private const string HumanVillageScenePath = "Assets/TouhouMigration/Scenes/HumanVillageVerticalSlice.unity";

        [MenuItem("Touhou Migration/Tests/Run Social Gift Smoke Tests")]
        public static void RunAll()
        {
            TestGiftDatabaseLoadsPreferencesAndReactions();
            TestGiftInteractionRemovesInventoryAndStartsReactionDialogue();
            TestHumanVillageContainsConfiguredNpcInteractors();
            Debug.Log("Social gift smoke tests passed.");
        }

        private static void TestGiftDatabaseLoadsPreferencesAndReactions()
        {
            object giftDatabase = LoadGiftDatabase();
            AssertEqual(30, GetProperty<int>(giftDatabase, "GiftCount"), "Gift count should match Godot gifts.json.");
            AssertEqual(34, GetProperty<int>(giftDatabase, "PreferenceCount"), "NPC preference count should match Godot gifts.json.");
            AssertEqual(2f, GetProperty<float>(giftDatabase, "BirthdayGiftMultiplier"), "Birthday gift multiplier should migrate.");
            AssertEqual(true, Invoke<bool>(giftDatabase, "HasGift", "magic_crystal"), "Magic crystal gift should exist.");
            AssertEqual(true, Invoke<bool>(giftDatabase, "HasPreference", "marisa"), "Marisa gift preferences should exist.");

            object magicCrystal = Invoke(giftDatabase, "GetGift", "magic_crystal");
            AssertEqual("魔法水晶", GetProperty<string>(magicCrystal, "Name"), "Gift name should preserve Chinese display text.");
            AssertEqual("MAGIC_ITEM", GetProperty<string>(magicCrystal, "Category"), "Gift category should preserve source category id.");

            object marisaReaction = Invoke(giftDatabase, "GetReaction", "marisa", "magic_crystal");
            AssertEqual("LOVE", GetProperty<string>(marisaReaction, "ReactionId"), "Explicit loved gift should resolve to LOVE.");
            AssertEqual(75, GetProperty<int>(marisaReaction, "BondChange"), "Marisa MAGIC_ITEM category bonus should multiply love bond.");
            AssertContains(GetProperty<string>(marisaReaction, "Dialogue"), "太棒", "Loved gift should expose reaction dialogue.");

            object reimuSpecial = Invoke(giftDatabase, "GetReaction", "reimu", "spell_card");
            AssertEqual("SPECIAL", GetProperty<string>(reimuSpecial, "ReactionId"), "Special gift should resolve before normal scoring.");
            AssertEqual("shrine_maiden_reaction", GetProperty<string>(reimuSpecial, "SpecialEvent"), "Special gift should expose event id.");
            AssertEqual(30, GetProperty<int>(reimuSpecial, "BondChange"), "Special gift base bond should match Godot GiftDatabase.");

            object keineHate = Invoke(giftDatabase, "GetReaction", "keine", "spider_lily");
            AssertEqual("HATE", GetProperty<string>(keineHate, "ReactionId"), "Explicit hated gift should resolve to HATE.");
            AssertEqual(-15, GetProperty<int>(keineHate, "BondChange"), "Hated gift bond should match Godot GiftDatabase.");
        }

        private static void TestGiftInteractionRemovesInventoryAndStartsReactionDialogue()
        {
            object giftDatabase = LoadGiftDatabase();
            object itemDatabase = Activator.CreateInstance(typeof(ItemDatabase));
            AssertEqual(true, Invoke<bool>(itemDatabase, "LoadFromPath", ItemDataPath), "Item database should load for gift inventory test.");
            object inventory = Activator.CreateInstance(typeof(InventoryService), itemDatabase, 48);
            AssertEqual(true, Invoke<bool>(inventory, "AddItem", "magic_crystal", 2), "Inventory should accept giftable magic crystal.");

            DialogueDatabase dialogueDatabase = new DialogueDatabase();
            AssertEqual(true, dialogueDatabase.LoadFromPath(DialogueDataPath), "Dialogue database should load for reaction speaker names.");
            DialogueRuntimeFacade facade = new DialogueRuntimeFacade();

            object service = Activator.CreateInstance(RequiredType(GiftInteractionServiceTypeName), giftDatabase, inventory, dialogueDatabase, facade);
            object result = Invoke(service, "GiveGift", "marisa", "magic_crystal");

            AssertEqual(true, GetProperty<bool>(result, "Success"), "Gift interaction should succeed when inventory contains the gift.");
            AssertEqual("LOVE", GetProperty<string>(result, "ReactionId"), "Gift interaction should return reaction id.");
            AssertEqual(75, GetProperty<int>(result, "BondChange"), "Gift interaction should return bond delta.");
            AssertEqual(1, Invoke<int>(inventory, "GetItemCount", "magic_crystal"), "Gift interaction should remove one item from inventory.");
            AssertEqual(true, facade.IsActive, "Gift interaction should start a reaction dialogue.");
            DialogueViewModel viewModel = facade.GetViewModel();
            AssertEqual("marisa", viewModel.NpcId, "Reaction dialogue should be scoped to NPC id.");
            AssertEqual("雾雨魔理沙", viewModel.Speaker, "Reaction dialogue should use migrated dialogue display name.");
            AssertContains(viewModel.Text, "太棒", "Reaction dialogue should use gift reaction text.");

            object missing = Invoke(service, "GiveGift", "marisa", "magic_crystal", 9);
            AssertEqual(false, GetProperty<bool>(missing, "Success"), "Gift interaction should fail without enough inventory.");
        }

        private static void TestHumanVillageContainsConfiguredNpcInteractors()
        {
            Type interactorType = RequiredType(NpcInteractorTypeName);
            EditorSceneManager.OpenScene(HumanVillageScenePath);

            int count = 0;
            bool foundMarisa = false;
            bool foundReimu = false;
            bool foundKeine = false;
            foreach (GameObject gameObject in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include))
            {
                Component interactor = gameObject.GetComponent(interactorType);
                if (interactor == null)
                {
                    continue;
                }

                count++;
                string npcId = GetProperty<string>(interactor, "NpcId");
                string displayName = GetProperty<string>(interactor, "DisplayName");
                string preferredGift = GetProperty<string>(interactor, "PreferredGiftId");
                AssertEqual(false, string.IsNullOrWhiteSpace(displayName), "NPC interactor should expose a display name.");
                AssertEqual(false, string.IsNullOrWhiteSpace(preferredGift), "NPC interactor should expose a preferred gift id.");

                foundMarisa |= npcId == "marisa" && preferredGift == "magic_crystal";
                foundReimu |= npcId == "reimu" && preferredGift == "green_tea";
                foundKeine |= npcId == "keine" && preferredGift == "history_book";
            }

            AssertEqual(true, count >= 3, "Human Village should contain at least three configured NPC interactors.");
            AssertEqual(true, foundMarisa, "Human Village should contain Marisa NPC marker.");
            AssertEqual(true, foundReimu, "Human Village should contain Reimu NPC marker.");
            AssertEqual(true, foundKeine, "Human Village should contain Keine NPC marker.");
        }

        private static object LoadGiftDatabase()
        {
            object database = Activator.CreateInstance(RequiredType(GiftDatabaseTypeName));
            AssertEqual(true, Invoke<bool>(database, "LoadFromPath", GiftDataPath), "Gift database should load migrated Godot gifts.json.");
            return database;
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
