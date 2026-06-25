using System;
using System.Collections.Generic;

namespace TouhouMigration.Runtime.Home
{
    // The bamboo home's storage box (Godot HomeInteractionSystem storage_items): a capacity-limited item
    // store keyed by item id. Free of UnityEngine. The sleep/tea/meal/read home interactions are signal- and
    // scene-coupled (SignalBus / GameStateManager / FatigueSystem / TimeManager) and are deferred.
    public sealed class MigrationHomeStorage
    {
        public const int MaxStorageSlots = 200;

        private readonly Dictionary<string, int> items = new Dictionary<string, int>();

        // Total number of stored items across every id (Godot _get_total_stored_items).
        public int TotalStoredItems
        {
            get
            {
                int total = 0;
                foreach (int amount in items.Values)
                {
                    total += amount;
                }

                return total;
            }
        }

        // Store items (Godot store_item): rejected only when the box is already at or over capacity. Matching
        // Godot, the capacity check is on the current total and does not clamp the amount being stored.
        public bool StoreItem(string itemId, int amount)
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                return false;
            }

            if (TotalStoredItems >= MaxStorageSlots)
            {
                return false;
            }

            if (items.ContainsKey(itemId))
            {
                items[itemId] += amount;
            }
            else
            {
                items[itemId] = amount;
            }

            return true;
        }

        // Retrieve items (Godot retrieve_item): fails if the item is absent or there is not enough; the entry
        // is removed once it reaches zero.
        public bool RetrieveItem(string itemId, int amount)
        {
            if (!items.TryGetValue(itemId ?? string.Empty, out int stored) || stored < amount)
            {
                return false;
            }

            int remaining = stored - amount;
            if (remaining <= 0)
            {
                items.Remove(itemId);
            }
            else
            {
                items[itemId] = remaining;
            }

            return true;
        }

        public int GetStoredAmount(string itemId)
        {
            return items.TryGetValue(itemId ?? string.Empty, out int amount) ? amount : 0;
        }

        public IReadOnlyDictionary<string, int> GetAllStoredItems()
        {
            return new Dictionary<string, int>(items);
        }

        // Snapshot the stored items for save/load (parallel lists, JsonUtility-safe).
        public HomeStorageSnapshot CreateSnapshot()
        {
            HomeStorageSnapshot snapshot = new HomeStorageSnapshot();
            foreach (KeyValuePair<string, int> entry in items)
            {
                snapshot.itemIds.Add(entry.Key);
                snapshot.amounts.Add(entry.Value);
            }

            return snapshot;
        }

        public void LoadSnapshot(HomeStorageSnapshot snapshot)
        {
            items.Clear();
            if (snapshot?.itemIds == null || snapshot.amounts == null)
            {
                return;
            }

            int count = Math.Min(snapshot.itemIds.Count, snapshot.amounts.Count);
            for (int i = 0; i < count; i++)
            {
                string id = snapshot.itemIds[i];
                if (!string.IsNullOrWhiteSpace(id) && snapshot.amounts[i] > 0)
                {
                    items[id] = snapshot.amounts[i];
                }
            }
        }
    }

    // Persisted bamboo-home storage box contents.
    [Serializable]
    public sealed class HomeStorageSnapshot
    {
        public List<string> itemIds = new List<string>();
        public List<int> amounts = new List<int>();
    }
}
