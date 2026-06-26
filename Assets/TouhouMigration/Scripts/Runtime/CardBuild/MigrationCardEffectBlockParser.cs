using System;
using System.Collections.Generic;
using System.IO;
using TouhouMigration.Runtime.Serialization;

namespace TouhouMigration.Runtime.CardBuild
{
    // Parses cards.json effect_blocks into MigrationCardEffectBlock lists per card id. Uses the dict-based
    // MigrationJson (not JsonUtility) so absent fields stay null — letting the executor apply each type's
    // own default (e.g. an absent amount = spend-all). Keyed by card_id; the engine runs the lists.
    public sealed class MigrationCardEffectBlockParser
    {
        private readonly Dictionary<string, List<MigrationCardEffectBlock>> blocksByCard =
            new Dictionary<string, List<MigrationCardEffectBlock>>();
        private readonly List<string> errors = new List<string>();

        public IReadOnlyList<string> Errors => errors;
        public int CardCount => blocksByCard.Count;
        public IReadOnlyCollection<string> CardIds => blocksByCard.Keys;

        public bool LoadFromPath(string cardsJsonPath)
        {
            blocksByCard.Clear();
            errors.Clear();

            string path = ResolvePath(cardsJsonPath);
            if (!File.Exists(path))
            {
                errors.Add($"missing cards data file: {cardsJsonPath}");
                return false;
            }

            object parsed;
            try
            {
                parsed = MigrationJson.Parse(File.ReadAllText(path));
            }
            catch (Exception exception)
            {
                errors.Add($"invalid cards json: {exception.Message}");
                return false;
            }

            if (parsed is not Dictionary<string, object> root
                || !root.TryGetValue("cards", out object cardsObject)
                || cardsObject is not List<object> cardList)
            {
                errors.Add("cards json must contain a 'cards' array");
                return false;
            }

            foreach (object entry in cardList)
            {
                if (entry is not Dictionary<string, object> card)
                {
                    continue;
                }

                string cardId = GetString(card, "card_id");
                if (string.IsNullOrEmpty(cardId)
                    || !card.TryGetValue("effect_blocks", out object blocksObject)
                    || blocksObject is not List<object> blockList)
                {
                    continue;
                }

                List<MigrationCardEffectBlock> blocks = new List<MigrationCardEffectBlock>();
                foreach (object blockEntry in blockList)
                {
                    if (blockEntry is Dictionary<string, object> blockData)
                    {
                        blocks.Add(ParseBlock(blockData));
                    }
                }

                blocksByCard[cardId] = blocks;
            }

            return blocksByCard.Count > 0 && errors.Count == 0;
        }

        public IReadOnlyList<MigrationCardEffectBlock> GetEffectBlocks(string cardId)
        {
            return cardId != null && blocksByCard.TryGetValue(cardId, out List<MigrationCardEffectBlock> blocks)
                ? blocks
                : Array.Empty<MigrationCardEffectBlock>();
        }

        // Build one effect block from its JSON dict (shared with the relic loader in the content database).
        public static MigrationCardEffectBlock ParseBlock(Dictionary<string, object> data)
        {
            return new MigrationCardEffectBlock
            {
                Type = GetString(data, "type"),
                Resource = GetString(data, "resource"),
                Status = GetString(data, "status"),
                Target = GetString(data, "target"),
                ClauseId = data.ContainsKey("clause_id") ? GetString(data, "clause_id") : GetString(data, "clause_tag"),
                Id = data.ContainsKey("id") ? GetString(data, "id") : GetString(data, "terminal_id"),
                Family = GetString(data, "family"),
                Modifier = GetString(data, "modifier"),
                Amount = GetIntOrNull(data, "amount"),
                Flame = GetIntOrNull(data, "flame"),
                BaseDamage = GetDoubleOrNull(data, "base_damage"),
                EnergyCost = GetDoubleOrNull(data, "energy_cost"),
                TriggerCoefficient = GetDoubleOrNull(data, "trigger_coefficient"),
                ChargeDodgeRetain = GetDoubleOrNull(data, "charge_dodge_retain"),
                ChargeSpeedMultiplier = GetDoubleOrNull(data, "charge_speed_multiplier"),
                TerminalDamageBonus = GetDoubleOrNull(data, "terminal_damage_bonus"),
            };
        }

        private static string GetString(Dictionary<string, object> data, string key)
        {
            return data.TryGetValue(key, out object value) && value != null ? value.ToString() : null;
        }

        private static int? GetIntOrNull(Dictionary<string, object> data, string key)
        {
            if (!data.TryGetValue(key, out object value) || value == null)
            {
                return null;
            }

            return value switch
            {
                long longValue => (int)longValue,
                double doubleValue => (int)doubleValue,
                int intValue => intValue,
                _ => int.TryParse(value.ToString(), out int parsed) ? parsed : (int?)null
            };
        }

        private static double? GetDoubleOrNull(Dictionary<string, object> data, string key)
        {
            if (!data.TryGetValue(key, out object value) || value == null)
            {
                return null;
            }

            return value switch
            {
                double doubleValue => doubleValue,
                long longValue => longValue,
                int intValue => intValue,
                _ => double.TryParse(value.ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double parsed) ? parsed : (double?)null
            };
        }

        private static string ResolvePath(string assetPath)
        {
            return Path.IsPathRooted(assetPath)
                ? assetPath
                : Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), assetPath));
        }
    }
}
