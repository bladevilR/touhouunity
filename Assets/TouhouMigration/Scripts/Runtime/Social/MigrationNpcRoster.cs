using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using TouhouMigration.Runtime.Serialization;

namespace TouhouMigration.Runtime.Social
{
    // Loads a location's NPC spawn roster (Godot human_village_final_npc_roster.json): each entry's id,
    // model, spawn flag/tier, and home/work/activity. Mirrors the ItemDatabase loader (reuses
    // MigrationJson). The Godot res:// model paths load as-is (Unity asset mapping is an E3 follow-up).
    public sealed class MigrationNpcRoster
    {
        private readonly Dictionary<string, MigrationNpcRosterEntry> entries = new Dictionary<string, MigrationNpcRosterEntry>();
        private readonly List<string> errors = new List<string>();

        public IReadOnlyList<string> Errors => errors;
        public int Count => entries.Count;

        public bool LoadFromPath(string assetPath)
        {
            entries.Clear();
            errors.Clear();

            string path = ResolvePath(assetPath);
            if (!File.Exists(path))
            {
                errors.Add($"missing roster data file: {assetPath}");
                return false;
            }

            object parsed;
            try
            {
                parsed = MigrationJson.Parse(File.ReadAllText(path));
            }
            catch (Exception exception)
            {
                errors.Add($"invalid roster json: {exception.Message}");
                return false;
            }

            if (parsed is not Dictionary<string, object> root)
            {
                errors.Add("roster json root must be an object");
                return false;
            }

            if (!root.TryGetValue("npcs", out object npcsObject) || npcsObject is not List<object> npcList)
            {
                errors.Add("roster json must contain an 'npcs' array");
                return false;
            }

            foreach (object entry in npcList)
            {
                if (entry is not Dictionary<string, object> data)
                {
                    continue;
                }

                string npcId = GetString(data, "id");
                if (string.IsNullOrWhiteSpace(npcId))
                {
                    continue;
                }

                entries[npcId] = new MigrationNpcRosterEntry(
                    npcId,
                    GetString(data, "display_name"),
                    GetString(data, "model_path"),
                    GetBool(data, "spawn_enabled", true),
                    GetString(data, "tier"),
                    GetFloat(data, "max_distance", 0f),
                    GetString(data, "home"),
                    GetString(data, "work_location"),
                    GetString(data, "activity"));
            }

            return entries.Count > 0 && errors.Count == 0;
        }

        public MigrationNpcRosterEntry GetEntry(string npcId)
        {
            return npcId != null && entries.TryGetValue(npcId, out MigrationNpcRosterEntry entry) ? entry : null;
        }

        public IReadOnlyDictionary<string, MigrationNpcRosterEntry> GetAllEntries()
        {
            return entries;
        }

        private static string GetString(Dictionary<string, object> data, string key)
        {
            return data.TryGetValue(key, out object value) && value != null ? value.ToString() : string.Empty;
        }

        private static bool GetBool(Dictionary<string, object> data, string key, bool fallback)
        {
            if (!data.TryGetValue(key, out object value) || value == null)
            {
                return fallback;
            }

            return value switch
            {
                bool boolValue => boolValue,
                long longValue => longValue != 0,
                double doubleValue => doubleValue != 0,
                _ => bool.TryParse(value.ToString(), out bool parsed) ? parsed : fallback
            };
        }

        private static float GetFloat(Dictionary<string, object> data, string key, float fallback)
        {
            if (!data.TryGetValue(key, out object value) || value == null)
            {
                return fallback;
            }

            return value switch
            {
                double doubleValue => (float)doubleValue,
                long longValue => longValue,
                int intValue => intValue,
                float floatValue => floatValue,
                _ => float.TryParse(value.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed) ? parsed : fallback
            };
        }

        private static string ResolvePath(string assetPath)
        {
            if (Path.IsPathRooted(assetPath))
            {
                return assetPath;
            }

            return Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), assetPath));
        }
    }
}
