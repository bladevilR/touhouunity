using System;
using TouhouMigration.Runtime.Home;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    // Covers MigrationHomeStorage: the bamboo home's storage box (Godot HomeInteractionSystem store_item /
    // retrieve_item / get_stored_amount), a capacity-limited item store.
    public static class MigrationHomeStorageSmokeTests
    {
        [MenuItem("Touhou Migration/Tests/Run Migration Home Storage Smoke Tests")]
        public static void RunAll()
        {
            TestStoreAndRetrieveItems();
            TestRetrieveMissingOrInsufficientFails();
            TestRetrieveToZeroRemovesEntry();
            TestStoreRejectedWhenFull();
            TestTotalStoredItemsSums();
            Debug.Log("Migration home storage smoke tests passed.");
        }

        private static void TestStoreAndRetrieveItems()
        {
            MigrationHomeStorage storage = new MigrationHomeStorage();
            AssertEqual(true, storage.StoreItem("apple", 3), "Storing items into the box should succeed.");
            AssertEqual(3, storage.GetStoredAmount("apple"), "The box reports the stored amount.");
            AssertEqual(true, storage.StoreItem("apple", 2), "Storing more of the same item should succeed.");
            AssertEqual(5, storage.GetStoredAmount("apple"), "Storing the same item accumulates the amount.");
            AssertEqual(true, storage.RetrieveItem("apple", 2), "Retrieving available items should succeed.");
            AssertEqual(3, storage.GetStoredAmount("apple"), "Retrieving reduces the stored amount.");
        }

        private static void TestRetrieveMissingOrInsufficientFails()
        {
            MigrationHomeStorage storage = new MigrationHomeStorage();
            AssertEqual(false, storage.RetrieveItem("apple", 1), "Retrieving an item not in the box should fail.");
            storage.StoreItem("apple", 1);
            AssertEqual(false, storage.RetrieveItem("apple", 5), "Retrieving more than stored should fail.");
            AssertEqual(1, storage.GetStoredAmount("apple"), "A failed retrieve does not change the stored amount.");
        }

        private static void TestRetrieveToZeroRemovesEntry()
        {
            MigrationHomeStorage storage = new MigrationHomeStorage();
            storage.StoreItem("apple", 2);
            AssertEqual(true, storage.RetrieveItem("apple", 2), "Retrieving the full stack should succeed.");
            AssertEqual(0, storage.GetStoredAmount("apple"), "An emptied item reports zero.");
            AssertEqual(0, storage.GetAllStoredItems().Count, "An emptied item is removed from the box.");
        }

        private static void TestStoreRejectedWhenFull()
        {
            MigrationHomeStorage storage = new MigrationHomeStorage();
            AssertEqual(true, storage.StoreItem("wood", MigrationHomeStorage.MaxStorageSlots), "Filling the box to capacity should succeed.");
            AssertEqual(MigrationHomeStorage.MaxStorageSlots, storage.TotalStoredItems, "The box is now at capacity.");
            AssertEqual(false, storage.StoreItem("stone", 1), "Storing into a full box should fail.");
            AssertEqual(0, storage.GetStoredAmount("stone"), "A rejected store does not add the item.");
        }

        private static void TestTotalStoredItemsSums()
        {
            MigrationHomeStorage storage = new MigrationHomeStorage();
            storage.StoreItem("apple", 3);
            storage.StoreItem("bread", 4);
            AssertEqual(7, storage.TotalStoredItems, "Total stored items sums every item amount.");
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
