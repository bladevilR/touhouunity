using System;
using TouhouMigration.Runtime.Fishing;
using TouhouMigration.Runtime.Inventory;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    // Covers MigrationFishingSession: the playable cast -> reel -> land flow composing the catch-bar
    // minigame (lands the fish) with the fishing service (rolls which fish + grants it).
    public static class FishingSessionSmokeTests
    {
        private const string ItemDataPath = "Assets/TouhouMigration/Data/Items/items.json";

        [MenuItem("Touhou Migration/Tests/Run Fishing Session Smoke Tests")]
        public static void RunAll()
        {
            TestSuccessfulReelLandsAndGrantsFish();
            TestFishEscapingFailsTheCast();
            Debug.Log("Fishing session smoke tests passed.");
        }

        private static (MigrationFishingService, InventoryService, string) BuildService()
        {
            ItemDatabase items = new ItemDatabase();
            AssertEqual(true, items.LoadFromPath(ItemDataPath), "Item database loads.");
            string itemId = null;
            foreach (var pair in items.GetAllItems())
            {
                itemId = pair.Key;
                break;
            }

            InventoryService inventory = new InventoryService(items);
            MigrationFishingService service = new MigrationFishingService(inventory);
            service.RegisterFish(new MigrationFishDefinition("carp", MigrationFishRarity.Common, itemId));
            return (service, inventory, itemId);
        }

        private static void TestSuccessfulReelLandsAndGrantsFish()
        {
            (MigrationFishingService service, InventoryService inventory, string itemId) = BuildService();
            MigrationFishingSession session = new MigrationFishingSession(service, fishingLevel: 0, nextInt: max => 0);

            session.CastLine(0.2);
            AssertEqual(true, session.IsReeling, "Casting the line starts reeling.");

            // Keep the fish in the box (fish at the box position) until it's landed.
            for (int i = 0; i < 40 && session.IsReeling; i++)
            {
                session.Reel(0.1, lifting: true, fishPosition: session.Minigame.BoxPosition);
            }

            AssertEqual(false, session.IsReeling, "Landing the fish ends the reel.");
            AssertEqual(true, session.LandedCatch != null && session.LandedCatch.Success, "A fish is landed.");
            AssertEqual("carp", session.LandedCatch.FishId, "The carp is caught.");
            AssertEqual(1, inventory.GetItemCount(itemId), "The caught fish is granted to the inventory.");
        }

        private static void TestFishEscapingFailsTheCast()
        {
            (MigrationFishingService service, InventoryService inventory, string itemId) = BuildService();
            MigrationFishingSession session = new MigrationFishingSession(service, fishingLevel: 0, nextInt: max => 0);

            session.CastLine(0.2);
            // Box falls; fish sits at the top -> never in box -> the line fails.
            for (int i = 0; i < 40 && session.IsReeling; i++)
            {
                session.Reel(0.1, lifting: false, fishPosition: 1.0);
            }

            AssertEqual(true, session.GotAway, "An escaping fish fails the cast.");
            AssertEqual(true, session.LandedCatch == null, "No fish is landed when it escapes.");
            AssertEqual(0, inventory.GetItemCount(itemId), "No fish is granted on a failed cast.");
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
