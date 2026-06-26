using System;
using TouhouMigration.Runtime.Inventory;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    // Covers InventoryService slot operations (Godot InventoryRuntime get_slot / swap_slots / clear):
    // direct slot access, drag-to-reorder swapping, and clearing.
    public static class InventorySlotOpsSmokeTests
    {
        private const string ItemDataPath = "Assets/TouhouMigration/Data/Items/items.json";

        [MenuItem("Touhou Migration/Tests/Run Inventory Slot Ops Smoke Tests")]
        public static void RunAll()
        {
            TestSlotCountAndAccess();
            TestSwapSlots();
            TestSwapBoundsAreSafe();
            TestClear();
            Debug.Log("Inventory slot ops smoke tests passed.");
        }

        private static (ItemDatabase, InventoryService, string) Build()
        {
            ItemDatabase items = new ItemDatabase();
            AssertEqual(true, items.LoadFromPath(ItemDataPath), "Item database loads.");
            InventoryService inv = new InventoryService(items, 8);
            string itemId = null;
            foreach (var pair in items.GetAllItems())
            {
                itemId = pair.Key;
                break;
            }

            return (items, inv, itemId);
        }

        private static void TestSlotCountAndAccess()
        {
            (_, InventoryService inv, string itemId) = Build();
            AssertEqual(8, inv.SlotCount, "The inventory exposes its slot count.");
            AssertEqual(true, inv.GetSlot(0) == null || inv.GetSlot(0).IsEmpty, "An untouched slot is empty/null.");

            inv.AddItem(itemId, 1);
            InventorySlotData slot0 = inv.GetSlot(0);
            AssertEqual(true, slot0 != null && slot0.item_id == itemId, "The added item lands in slot 0.");
            AssertEqual(true, inv.GetSlot(99) == null, "An out-of-range slot index returns null.");
        }

        private static void TestSwapSlots()
        {
            (_, InventoryService inv, string itemId) = Build();
            inv.AddItem(itemId, 1); // slot 0
            AssertEqual(true, inv.GetSlot(0) != null && !inv.GetSlot(0).IsEmpty, "Slot 0 is filled before the swap.");
            AssertEqual(true, inv.GetSlot(3) == null || inv.GetSlot(3).IsEmpty, "Slot 3 is empty before the swap.");

            AssertEqual(true, inv.SwapSlots(0, 3), "Swapping two valid slots succeeds.");
            AssertEqual(true, inv.GetSlot(3) != null && inv.GetSlot(3).item_id == itemId, "The item moved to slot 3.");
            AssertEqual(true, inv.GetSlot(0) == null || inv.GetSlot(0).IsEmpty, "Slot 0 is now empty.");
        }

        private static void TestSwapBoundsAreSafe()
        {
            (_, InventoryService inv, _) = Build();
            AssertEqual(false, inv.SwapSlots(-1, 0), "A negative index swap fails.");
            AssertEqual(false, inv.SwapSlots(0, 99), "An out-of-range swap fails.");
        }

        private static void TestClear()
        {
            (_, InventoryService inv, string itemId) = Build();
            inv.AddItem(itemId, 3);
            AssertEqual(true, inv.UsedSlots > 0, "The inventory holds items before clearing.");

            inv.Clear();
            AssertEqual(0, inv.UsedSlots, "Clear empties every slot.");
            AssertEqual(0, inv.GetItemCount(itemId), "No items remain after clearing.");
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
