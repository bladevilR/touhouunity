using System;
using System.IO;
using TouhouMigration.Runtime.Cooking;
using TouhouMigration.Runtime.Social;
using UnityEngine;

namespace TouhouMigration.Runtime.Save
{
    public sealed class MigrationSaveService
    {
        private const string SaveFilePrefix = "save_slot_";
        private const string SaveFileExtension = ".json";

        private readonly string saveDirectory;

        public MigrationSaveService(string saveDirectory)
        {
            this.saveDirectory = string.IsNullOrWhiteSpace(saveDirectory)
                ? Path.Combine(Application.persistentDataPath, "saves")
                : saveDirectory;
        }

        public bool SaveSlot(int slot, MigrationSaveData data)
        {
            if (slot < 0 || data == null)
            {
                return false;
            }

            Directory.CreateDirectory(saveDirectory);
            data.save_schema = 3;
            data.version = "3.0.0";
            data.timestamp = string.IsNullOrWhiteSpace(data.timestamp) ? DateTime.Now.ToString("s") : data.timestamp;
            File.WriteAllText(GetSavePath(slot), JsonUtility.ToJson(data, true));
            return true;
        }

        public MigrationSaveData LoadSlot(int slot)
        {
            string path = GetSavePath(slot);
            if (!File.Exists(path))
            {
                return null;
            }

            MigrationSaveData data = JsonUtility.FromJson<MigrationSaveData>(File.ReadAllText(path));
            return Normalize(data);
        }

        public bool HasSave(int slot)
        {
            return File.Exists(GetSavePath(slot));
        }

        public bool DeleteSave(int slot)
        {
            string path = GetSavePath(slot);
            if (!File.Exists(path))
            {
                return false;
            }

            File.Delete(path);
            return true;
        }

        public MigrationSaveInfo GetSaveInfo(int slot)
        {
            MigrationSaveData data = LoadSlot(slot);
            return data == null ? null : new MigrationSaveInfo(slot, data);
        }

        private string GetSavePath(int slot)
        {
            return Path.Combine(saveDirectory, $"{SaveFilePrefix}{slot}{SaveFileExtension}");
        }

        private static MigrationSaveData Normalize(MigrationSaveData data)
        {
            if (data == null)
            {
                return null;
            }

            data.save_schema = 3;
            data.version = string.IsNullOrWhiteSpace(data.version) ? "3.0.0" : data.version;
            data.timestamp ??= string.Empty;
            data.player_name = string.IsNullOrWhiteSpace(data.player_name) ? "藤原妹红" : data.player_name;
            data.level = Math.Max(1, data.level);
            data.max_hp = Math.Max(1, data.max_hp);
            data.current_hp = Math.Max(0, data.current_hp);
            data.current_scene = string.IsNullOrWhiteSpace(data.current_scene) ? "town" : data.current_scene;
            data.position ??= new MigrationSavePosition();
            data.inventory ??= new Inventory.InventorySnapshot();
            data.cooking ??= new CookingRuntimeSnapshot();
            data.cooking_buffs ??= new CookingBuffRuntimeSnapshot();
            data.social_bonds ??= new SocialBondSnapshot();
            data.quests ??= new QuestRuntimeSnapshot();
            return data;
        }
    }
}
