using System;
using System.Collections.Generic;
using TouhouMigration.Runtime.Dialogue;
using TouhouMigration.Runtime.Inventory;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    public static class DialogueInventoryEffectSmokeTests
    {
        private const string ItemDataPath = "Assets/TouhouMigration/Data/Items/items.json";

        [MenuItem("Touhou Migration/Tests/Run Dialogue Inventory Effect Smoke Tests")]
        public static void RunAll()
        {
            TestGiveAndTakeItemRouteToInventory();
            TestInventoryEffectsNoOpWithoutBoundInventory();
            Debug.Log("Dialogue inventory effect smoke tests passed.");
        }

        private static InventoryService BuildInventory()
        {
            ItemDatabase itemDatabase = new ItemDatabase();
            AssertEqual(true, itemDatabase.LoadFromPath(ItemDataPath), "Item database should load items.json.");
            return new InventoryService(itemDatabase);
        }

        private static void TestGiveAndTakeItemRouteToInventory()
        {
            InventoryService inventory = BuildInventory();
            DialogueEffectRouter router = new DialogueEffectRouter(null, null);
            router.BindInventory(inventory);

            bool gave = router.Apply("marisa", new Dictionary<string, object>
            {
                ["give_item"] = new Dictionary<string, object> { ["item_id"] = "health_potion_small", ["amount"] = 3 }
            });
            AssertEqual(true, gave, "give_item should route to the inventory service.");
            AssertEqual(3, inventory.GetItemCount("health_potion_small"), "Inventory should hold the granted items.");

            bool took = router.Apply("marisa", new Dictionary<string, object>
            {
                ["take_item"] = new Dictionary<string, object> { ["item_id"] = "health_potion_small", ["amount"] = 1 }
            });
            AssertEqual(true, took, "take_item should route to the inventory service.");
            AssertEqual(2, inventory.GetItemCount("health_potion_small"), "Inventory should drop the removed items.");
        }

        private static void TestInventoryEffectsNoOpWithoutBoundInventory()
        {
            DialogueEffectRouter router = new DialogueEffectRouter(null, null);
            bool handled = router.Apply("marisa", new Dictionary<string, object>
            {
                ["give_item"] = "health_potion_small"
            });
            AssertEqual(false, handled, "give_item is a no-op when no inventory is bound.");
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
