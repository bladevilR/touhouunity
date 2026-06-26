using System;
using System.IO;
using UnityEngine;

namespace TouhouMigration.Runtime.CardBuild
{
    // Writes/reads the per-character card-run store to/from a JSON file (Godot CardBuildRunStore file IO).
    // The store + its snapshots are JsonUtility-safe; this is the disk call site. Both calls are exception-
    // safe (return false on IO/parse failure).
    public static class MigrationCardBuildRunSaveService
    {
        public static bool SaveToFile(MigrationCardBuildRunStore store, string path)
        {
            if (store == null || string.IsNullOrEmpty(path))
            {
                return false;
            }

            try
            {
                string json = JsonUtility.ToJson(store.CreateFileSnapshot(), true);
                File.WriteAllText(path, json);
                return true;
            }
            catch (IOException)
            {
                return false;
            }
        }

        public static bool LoadFromFile(MigrationCardBuildRunStore store, string path)
        {
            if (store == null || string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                return false;
            }

            try
            {
                CardBuildRunStoreFile file = JsonUtility.FromJson<CardBuildRunStoreFile>(File.ReadAllText(path));
                store.LoadFileSnapshot(file);
                return true;
            }
            catch (IOException)
            {
                return false;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }
    }
}
