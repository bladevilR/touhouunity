using System;
using System.Collections.Generic;
using System.Linq;

namespace TouhouMigration.Runtime.Inventory
{
    public sealed class InventoryService
    {
        public const int DefaultMaxSlots = 48;

        private readonly ItemDatabase itemDatabase;
        private readonly InventorySlotData[] slots;

        public InventoryService(ItemDatabase itemDatabase, int maxSlots = DefaultMaxSlots)
        {
            this.itemDatabase = itemDatabase;
            slots = new InventorySlotData[Math.Max(0, maxSlots)];
        }

        public int UsedSlots => slots.Count(slot => slot != null && !slot.IsEmpty);

        public bool AddItem(string itemId, int amount)
        {
            return AddItem(itemId, amount, 0);
        }

        public bool AddItem(string itemId, int amount, int quality)
        {
            if (amount <= 0 || itemDatabase == null || !itemDatabase.HasItem(itemId))
            {
                return false;
            }

            ItemDefinition item = itemDatabase.GetItem(itemId);
            int remaining = amount;
            int normalizedQuality = Math.Max(0, quality);

            for (int index = 0; index < slots.Length && remaining > 0; index++)
            {
                InventorySlotData slot = slots[index];
                if (slot == null || slot.IsEmpty || slot.item_id != itemId || slot.quality != normalizedQuality)
                {
                    continue;
                }

                int space = Math.Max(0, item.MaxStack - slot.amount);
                if (space <= 0)
                {
                    continue;
                }

                int toAdd = Math.Min(space, remaining);
                slot.amount += toAdd;
                remaining -= toAdd;
            }

            for (int index = 0; index < slots.Length && remaining > 0; index++)
            {
                if (slots[index] != null && !slots[index].IsEmpty)
                {
                    continue;
                }

                int toAdd = Math.Min(item.MaxStack, remaining);
                slots[index] = new InventorySlotData
                {
                    item_id = itemId,
                    amount = toAdd,
                    quality = normalizedQuality
                };
                remaining -= toAdd;
            }

            return remaining == 0;
        }

        public bool RemoveItem(string itemId, int amount)
        {
            return RemoveItemInternal(itemId, amount, null);
        }

        public bool RemoveItem(string itemId, int amount, int quality)
        {
            return RemoveItemInternal(itemId, amount, quality);
        }

        private bool RemoveItemInternal(string itemId, int amount, int? quality)
        {
            if (amount <= 0 || GetItemCount(itemId) < amount)
            {
                return false;
            }

            if (quality.HasValue && GetItemCount(itemId, quality.Value) < amount)
            {
                return false;
            }

            int remaining = amount;
            for (int index = 0; index < slots.Length && remaining > 0; index++)
            {
                InventorySlotData slot = slots[index];
                if (slot == null || slot.IsEmpty || slot.item_id != itemId ||
                    quality.HasValue && slot.quality != Math.Max(0, quality.Value))
                {
                    continue;
                }

                int toRemove = Math.Min(slot.amount, remaining);
                slot.amount -= toRemove;
                remaining -= toRemove;
                if (slot.amount <= 0)
                {
                    slots[index] = null;
                }
            }

            CompactSlots();
            return true;
        }

        public int GetItemCount(string itemId)
        {
            int total = 0;
            foreach (InventorySlotData slot in slots)
            {
                if (slot != null && !slot.IsEmpty && slot.item_id == itemId)
                {
                    total += slot.amount;
                }
            }

            return total;
        }

        public int GetItemCount(string itemId, int quality)
        {
            int total = 0;
            int normalizedQuality = Math.Max(0, quality);
            foreach (InventorySlotData slot in slots)
            {
                if (slot != null && !slot.IsEmpty && slot.item_id == itemId && slot.quality == normalizedQuality)
                {
                    total += slot.amount;
                }
            }

            return total;
        }

        public InventorySnapshot CreateSnapshot()
        {
            InventorySnapshot snapshot = new InventorySnapshot();
            foreach (InventorySlotData slot in slots)
            {
                snapshot.slots.Add(slot == null || slot.IsEmpty
                    ? null
                    : new InventorySlotData
                    {
                        item_id = slot.item_id,
                        amount = slot.amount,
                        quality = slot.quality
                    });
            }

            return snapshot;
        }

        public void LoadSnapshot(InventorySnapshot snapshot)
        {
            Array.Clear(slots, 0, slots.Length);
            if (snapshot == null)
            {
                return;
            }

            int limit = Math.Min(slots.Length, snapshot.slots?.Count ?? 0);
            for (int index = 0; index < limit; index++)
            {
                InventorySlotData slot = snapshot.slots[index];
                if (slot == null || slot.IsEmpty)
                {
                    slots[index] = null;
                    continue;
                }

                slots[index] = new InventorySlotData
                {
                    item_id = slot.item_id,
                    amount = Math.Max(0, slot.amount),
                    quality = slot.quality
                };
            }

            CompactSlots();
        }

        public IReadOnlyDictionary<string, int> GetAllItems()
        {
            Dictionary<string, int> result = new Dictionary<string, int>();
            foreach (InventorySlotData slot in slots)
            {
                if (slot == null || slot.IsEmpty)
                {
                    continue;
                }

                result[slot.item_id] = result.TryGetValue(slot.item_id, out int amount)
                    ? amount + slot.amount
                    : slot.amount;
            }

            return result;
        }

        public IReadOnlyList<InventorySlotData> GetOccupiedSlots()
        {
            List<InventorySlotData> result = new List<InventorySlotData>();
            foreach (InventorySlotData slot in slots)
            {
                if (slot == null || slot.IsEmpty)
                {
                    continue;
                }

                result.Add(new InventorySlotData
                {
                    item_id = slot.item_id,
                    amount = slot.amount,
                    quality = slot.quality
                });
            }

            return result;
        }

        private void CompactSlots()
        {
            List<InventorySlotData> occupied = slots.Where(slot => slot != null && !slot.IsEmpty).ToList();
            Array.Clear(slots, 0, slots.Length);
            for (int index = 0; index < occupied.Count && index < slots.Length; index++)
            {
                slots[index] = occupied[index];
            }
        }
    }
}
