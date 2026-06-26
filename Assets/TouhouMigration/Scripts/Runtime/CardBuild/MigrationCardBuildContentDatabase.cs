using System.Collections.Generic;
using System.IO;
using TouhouMigration.Runtime.Serialization;

namespace TouhouMigration.Runtime.CardBuild
{
    // Loads the cardbuild content (relics.json + upgrades.json) into queryable definitions that feed
    // MigrationCardProgression (Godot CardBuildDatabase _index_relics / _index_upgrades). Relic effect
    // blocks reuse the card parser's ParseBlock. UnityEngine-free + unit-testable.
    public sealed class MigrationCardBuildContentDatabase
    {
        private readonly Dictionary<string, MigrationRelic> relics = new Dictionary<string, MigrationRelic>();
        private readonly Dictionary<string, MigrationCardUpgrade> upgrades = new Dictionary<string, MigrationCardUpgrade>();
        private readonly List<string> errors = new List<string>();

        public IReadOnlyList<string> Errors => errors;
        public int RelicCount => relics.Count;
        public int UpgradeCount => upgrades.Count;

        public bool LoadFromPaths(string relicsJsonPath, string upgradesJsonPath)
        {
            relics.Clear();
            upgrades.Clear();
            errors.Clear();

            LoadRelics(relicsJsonPath);
            LoadUpgrades(upgradesJsonPath);
            return errors.Count == 0 && relics.Count > 0 && upgrades.Count > 0;
        }

        public MigrationRelic GetRelic(string relicId) =>
            relicId != null && relics.TryGetValue(relicId, out MigrationRelic relic) ? relic : null;

        public MigrationCardUpgrade GetUpgrade(string upgradeId) =>
            upgradeId != null && upgrades.TryGetValue(upgradeId, out MigrationCardUpgrade upgrade) ? upgrade : null;

        private void LoadRelics(string path)
        {
            foreach (Dictionary<string, object> entry in ReadArray(path, "relics"))
            {
                string id = GetString(entry, "id");
                if (string.IsNullOrEmpty(id))
                {
                    continue;
                }

                MigrationRelic relic = new MigrationRelic();
                if (entry.TryGetValue("effect_blocks", out object blocksObj) && blocksObj is List<object> blockList)
                {
                    foreach (object block in blockList)
                    {
                        if (block is Dictionary<string, object> blockData)
                        {
                            relic.EffectBlocks.Add(MigrationCardEffectBlockParser.ParseBlock(blockData));
                        }
                    }
                }

                relics[id] = relic;
            }
        }

        private void LoadUpgrades(string path)
        {
            foreach (Dictionary<string, object> entry in ReadArray(path, "upgrades"))
            {
                string id = GetString(entry, "id");
                if (string.IsNullOrEmpty(id))
                {
                    continue;
                }

                MigrationCardUpgrade upgrade = new MigrationCardUpgrade
                {
                    TargetCardId = GetString(entry, "target_card_id"),
                    Operation = GetString(entry, "operation"),
                    Cooldown = GetDoubleOrNull(entry, "cooldown"),
                    Charges = GetIntOrNull(entry, "charges"),
                    ActivationMode = entry.ContainsKey("activation_mode") ? GetString(entry, "activation_mode") : null,
                };

                if (entry.TryGetValue("answer_tags", out object tagsObj) && tagsObj is List<object> tagList)
                {
                    foreach (object tag in tagList)
                    {
                        upgrade.AnswerTags.Add(System.Convert.ToString(tag));
                    }
                }

                upgrades[id] = upgrade;
            }
        }

        private IEnumerable<Dictionary<string, object>> ReadArray(string path, string key)
        {
            List<Dictionary<string, object>> rows = new List<Dictionary<string, object>>();
            string resolved = path != null && File.Exists(path) ? path : null;
            if (resolved == null)
            {
                errors.Add($"missing cardbuild content file: {path}");
                return rows;
            }

            object parsed;
            try
            {
                parsed = MigrationJson.Parse(File.ReadAllText(resolved));
            }
            catch (System.Exception e)
            {
                errors.Add($"failed to parse {path}: {e.Message}");
                return rows;
            }

            if (parsed is Dictionary<string, object> root && root.TryGetValue(key, out object arr) && arr is List<object> list)
            {
                foreach (object item in list)
                {
                    if (item is Dictionary<string, object> row)
                    {
                        rows.Add(row);
                    }
                }
            }
            else
            {
                errors.Add($"{path} missing '{key}' array");
            }

            return rows;
        }

        private static string GetString(Dictionary<string, object> data, string key) =>
            data.TryGetValue(key, out object value) && value != null ? System.Convert.ToString(value) : string.Empty;

        private static double? GetDoubleOrNull(Dictionary<string, object> data, string key) =>
            data.TryGetValue(key, out object value) && value != null
                ? System.Convert.ToDouble(value)
                : (double?)null;

        private static int? GetIntOrNull(Dictionary<string, object> data, string key) =>
            data.TryGetValue(key, out object value) && value != null
                ? (int)System.Convert.ToDouble(value)
                : (int?)null;
    }
}
