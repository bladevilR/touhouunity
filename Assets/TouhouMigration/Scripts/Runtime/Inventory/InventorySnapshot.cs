using System;
using System.Collections.Generic;

namespace TouhouMigration.Runtime.Inventory
{
    [Serializable]
    public sealed class InventorySnapshot
    {
        public int schema_version = 1;
        public List<InventorySlotData> slots = new List<InventorySlotData>();
        public string equipped_weapon = string.Empty;
        public string equipped_armor = string.Empty;
        public string equipped_accessory = string.Empty;

        public IReadOnlyList<InventorySlotData> Slots => slots;
        public string EquippedWeapon
        {
            get => equipped_weapon;
            set => equipped_weapon = value ?? string.Empty;
        }

        public string EquippedArmor
        {
            get => equipped_armor;
            set => equipped_armor = value ?? string.Empty;
        }

        public string EquippedAccessory
        {
            get => equipped_accessory;
            set => equipped_accessory = value ?? string.Empty;
        }
    }

    [Serializable]
    public sealed class InventorySlotData
    {
        public string item_id = string.Empty;
        public int amount;
        public int quality;

        public string ItemId => item_id;
        public int Amount => amount;
        public int Quality => quality;

        public bool IsEmpty => string.IsNullOrWhiteSpace(item_id) || amount <= 0;
    }
}
