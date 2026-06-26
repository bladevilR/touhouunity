using System.Collections.Generic;
using System.IO;
using TouhouMigration.Runtime.Serialization;

namespace TouhouMigration.Runtime.CardBuild
{
    // A cardbuild resource definition (resources.json).
    public sealed class MigrationCardResourceDef
    {
        public string Id = string.Empty;
        public string DisplayName = string.Empty;
        public string Archetype = string.Empty;
        public string Storage = string.Empty;
        public string Decay = string.Empty;
    }

    // A cardbuild status definition (statuses.json).
    public sealed class MigrationCardStatusDef
    {
        public string Id = string.Empty;
        public string DisplayName = string.Empty;
        public string Polarity = string.Empty;
        public string StackPolicy = string.Empty;
    }

    // A boss puzzle rule (boss_rules.json): which bosses pose it + which answer families counter it.
    public sealed class MigrationCardBossRule
    {
        public string Id = string.Empty;
        public string DisplayName = string.Empty;
        public List<string> CandidateBosses = new List<string>();
        public List<string> AnswerFamilies = new List<string>();
        public string Pressure = string.Empty;
    }

    // Loads the cardbuild content (relics.json + upgrades.json) into queryable definitions that feed
    // MigrationCardProgression (Godot CardBuildDatabase _index_relics / _index_upgrades). Relic effect
    // blocks reuse the card parser's ParseBlock. UnityEngine-free + unit-testable.
    public sealed class MigrationCardBuildContentDatabase
    {
        private readonly Dictionary<string, MigrationRelic> relics = new Dictionary<string, MigrationRelic>();
        private readonly Dictionary<string, MigrationCardUpgrade> upgrades = new Dictionary<string, MigrationCardUpgrade>();
        private readonly Dictionary<string, MigrationCardResourceDef> resources = new Dictionary<string, MigrationCardResourceDef>();
        private readonly Dictionary<string, MigrationCardStatusDef> statuses = new Dictionary<string, MigrationCardStatusDef>();
        private readonly Dictionary<string, MigrationCardBossRule> bossRules = new Dictionary<string, MigrationCardBossRule>();
        private readonly List<string> errors = new List<string>();

        public IReadOnlyList<string> Errors => errors;
        public int RelicCount => relics.Count;
        public int UpgradeCount => upgrades.Count;
        public int ResourceCount => resources.Count;
        public int StatusCount => statuses.Count;
        public int BossRuleCount => bossRules.Count;

        // Load the boss puzzle rules (Godot CardBuildDatabase _index_boss_rules).
        public bool LoadBossRules(string bossRulesJsonPath)
        {
            bossRules.Clear();
            foreach (Dictionary<string, object> entry in ReadArray(bossRulesJsonPath, "boss_rules"))
            {
                string id = GetString(entry, "id");
                if (string.IsNullOrEmpty(id))
                {
                    continue;
                }

                MigrationCardBossRule rule = new MigrationCardBossRule
                {
                    Id = id,
                    DisplayName = entry.ContainsKey("display_name_en") ? GetString(entry, "display_name_en") : GetString(entry, "display_name_zh"),
                    Pressure = GetString(entry, "pressure"),
                };
                rule.CandidateBosses.AddRange(GetStringList(entry, "candidate_bosses"));
                rule.AnswerFamilies.AddRange(GetStringList(entry, "answer_families"));
                bossRules[id] = rule;
            }

            return bossRules.Count > 0;
        }

        public MigrationCardBossRule GetBossRule(string ruleId) =>
            ruleId != null && bossRules.TryGetValue(ruleId, out MigrationCardBossRule rule) ? rule : null;

        private static IEnumerable<string> GetStringList(Dictionary<string, object> data, string key)
        {
            List<string> values = new List<string>();
            if (data.TryGetValue(key, out object obj) && obj is List<object> list)
            {
                foreach (object item in list)
                {
                    values.Add(System.Convert.ToString(item));
                }
            }

            return values;
        }

        // Load the resource + status definition tables (Godot CardBuildDatabase _index_resources/_statuses).
        public bool LoadDefinitions(string resourcesJsonPath, string statusesJsonPath)
        {
            resources.Clear();
            statuses.Clear();

            foreach (Dictionary<string, object> entry in ReadArray(resourcesJsonPath, "resources"))
            {
                string id = GetString(entry, "id");
                if (!string.IsNullOrEmpty(id))
                {
                    resources[id] = new MigrationCardResourceDef
                    {
                        Id = id,
                        DisplayName = GetString(entry, "display_name_en"),
                        Archetype = GetString(entry, "archetype"),
                        Storage = GetString(entry, "storage"),
                        Decay = GetString(entry, "decay"),
                    };
                }
            }

            foreach (Dictionary<string, object> entry in ReadArray(statusesJsonPath, "statuses"))
            {
                string id = GetString(entry, "id");
                if (!string.IsNullOrEmpty(id))
                {
                    statuses[id] = new MigrationCardStatusDef
                    {
                        Id = id,
                        DisplayName = GetString(entry, "display_name_en"),
                        Polarity = GetString(entry, "polarity"),
                        StackPolicy = GetString(entry, "stack_policy"),
                    };
                }
            }

            return resources.Count > 0 && statuses.Count > 0;
        }

        public MigrationCardResourceDef GetResource(string resourceId) =>
            resourceId != null && resources.TryGetValue(resourceId, out MigrationCardResourceDef def) ? def : null;

        public MigrationCardStatusDef GetStatus(string statusId) =>
            statusId != null && statuses.TryGetValue(statusId, out MigrationCardStatusDef def) ? def : null;

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
