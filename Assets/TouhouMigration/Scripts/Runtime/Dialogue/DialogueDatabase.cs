using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TouhouMigration.Runtime.Serialization;

namespace TouhouMigration.Runtime.Dialogue
{
    public sealed class DialogueDatabase
    {
        private readonly Dictionary<string, NpcDialogueData> npcs = new Dictionary<string, NpcDialogueData>();
        private readonly List<string> errors = new List<string>();

        public int NpcCount => npcs.Count;
        public IReadOnlyList<string> Errors => errors;

        public bool LoadFromPath(string directoryPath)
        {
            npcs.Clear();
            errors.Clear();

            if (!Directory.Exists(directoryPath))
            {
                errors.Add($"Dialogue directory does not exist: {directoryPath}");
                return false;
            }

            foreach (string filePath in Directory.GetFiles(directoryPath, "_npc_*.json"))
            {
                try
                {
                    object parsed = MigrationJson.Parse(File.ReadAllText(filePath));
                    if (parsed is not Dictionary<string, object> root)
                    {
                        errors.Add($"Dialogue file root is not an object: {filePath}");
                        continue;
                    }

                    NpcDialogueData npc = NormalizeNpc(root, filePath);
                    if (!string.IsNullOrWhiteSpace(npc.Id))
                    {
                        npcs[npc.Id] = npc;
                    }
                }
                catch (Exception exception)
                {
                    errors.Add($"{filePath}: {exception.Message}");
                }
            }

            return npcs.Count > 0 && errors.Count == 0;
        }

        public bool HasNpc(string npcId)
        {
            return npcs.ContainsKey(NormalizeId(npcId));
        }

        public string GetNpcName(string npcId)
        {
            return npcs.TryGetValue(NormalizeId(npcId), out NpcDialogueData npc) ? npc.Name : string.Empty;
        }

        public List<DialogueLine> GetDialogue(string npcId, string dialogueType, Dictionary<string, object> context)
        {
            string normalizedNpcId = NormalizeId(npcId);
            if (!npcs.TryGetValue(normalizedNpcId, out NpcDialogueData npc))
            {
                return BuildFallback(normalizedNpcId);
            }

            if (!npc.Topics.TryGetValue(dialogueType ?? string.Empty, out object topic))
            {
                return new List<DialogueLine>();
            }

            List<DialogueEntry> entries = ResolveEntries(topic, context ?? new Dictionary<string, object>());
            foreach (DialogueEntry entry in entries)
            {
                if (ConditionsPass(entry.Conditions, context ?? new Dictionary<string, object>()))
                {
                    return BuildRuntimeLines(entry);
                }
            }

            return new List<DialogueLine>();
        }

        private static NpcDialogueData NormalizeNpc(Dictionary<string, object> root, string filePath)
        {
            string id = GetString(root, "id");
            if (string.IsNullOrWhiteSpace(id))
            {
                id = Path.GetFileNameWithoutExtension(filePath).Replace("_npc_", string.Empty);
            }

            NpcDialogueData npc = new NpcDialogueData
            {
                Id = NormalizeId(id),
                Name = GetString(root, "name")
            };

            foreach (KeyValuePair<string, object> pair in root)
            {
                if (pair.Key == "id" || pair.Key == "name")
                {
                    continue;
                }

                object normalized = NormalizeTopicValue(pair.Value);
                if (normalized != null)
                {
                    npc.Topics[pair.Key] = normalized;
                }
            }

            return npc;
        }

        private static object NormalizeTopicValue(object value)
        {
            if (value is List<object> array)
            {
                return NormalizeEntryArray(array);
            }

            if (value is Dictionary<string, object> dictionary)
            {
                Dictionary<string, object> grouped = new Dictionary<string, object>();
                foreach (KeyValuePair<string, object> pair in dictionary)
                {
                    object normalized = NormalizeTopicValue(pair.Value);
                    if (normalized != null)
                    {
                        grouped[pair.Key] = normalized;
                    }
                }

                return grouped;
            }

            return null;
        }

        private static List<DialogueEntry> NormalizeEntryArray(List<object> entries)
        {
            List<DialogueEntry> normalized = new List<DialogueEntry>();
            foreach (object entry in entries)
            {
                if (entry is Dictionary<string, object> dictionary)
                {
                    normalized.Add(NormalizeEntry(dictionary));
                }
            }

            return normalized;
        }

        private static DialogueEntry NormalizeEntry(Dictionary<string, object> entry)
        {
            DialogueEntry normalized = new DialogueEntry();

            if (entry.TryGetValue("lines", out object explicitLines))
            {
                normalized.Lines = NormalizeLines(explicitLines);
                normalized.Conditions = ToStringObjectDictionary(entry.TryGetValue("conditions", out object conditions) ? conditions : null);
                normalized.Effects = ToStringObjectDictionary(entry.TryGetValue("effects", out object effects) ? effects : null);
                normalized.Choices = NormalizeChoices(entry.TryGetValue("choices", out object choices) ? choices : null);
                return normalized;
            }

            normalized.Lines = NormalizeLines(entry.TryGetValue("l", out object compactLines) ? compactLines : null);
            normalized.Conditions = ParseCompactPairs(GetString(entry, "c"));
            normalized.Effects = ParseEffectPairs(entry.TryGetValue("fx", out object compactEffects) ? compactEffects : null);
            normalized.Choices = ParseChoiceList(entry.TryGetValue("ch", out object compactChoices) ? compactChoices : null);
            return normalized;
        }

        private static List<DialogueLine> NormalizeLines(object lines)
        {
            List<DialogueLine> normalized = new List<DialogueLine>();
            if (lines is not IEnumerable enumerable || lines is string)
            {
                return normalized;
            }

            foreach (object line in enumerable)
            {
                if (line is Dictionary<string, object> dictionary)
                {
                    normalized.Add(new DialogueLine
                    {
                        Speaker = GetString(dictionary, "speaker"),
                        Text = GetString(dictionary, "text"),
                        Expression = string.IsNullOrWhiteSpace(GetString(dictionary, "expression")) ? "neutral" : GetString(dictionary, "expression"),
                        Choices = NormalizeChoices(dictionary.TryGetValue("choices", out object choices) ? choices : null)
                    });
                }
                else if (line is List<object> compact)
                {
                    normalized.Add(new DialogueLine
                    {
                        Speaker = compact.Count > 0 ? Convert.ToString(compact[0]) ?? string.Empty : string.Empty,
                        Text = compact.Count > 1 ? Convert.ToString(compact[1]) ?? string.Empty : string.Empty,
                        Expression = compact.Count > 2 && !string.IsNullOrWhiteSpace(Convert.ToString(compact[2]))
                            ? Convert.ToString(compact[2])
                            : "neutral"
                    });
                }
            }

            return normalized;
        }

        private static List<DialogueChoice> NormalizeChoices(object choices)
        {
            List<DialogueChoice> normalized = new List<DialogueChoice>();
            if (choices is not IEnumerable enumerable || choices is string)
            {
                return normalized;
            }

            foreach (object choice in enumerable)
            {
                if (choice is Dictionary<string, object> dictionary)
                {
                    DialogueChoice normalizedChoice = new DialogueChoice
                    {
                        Text = GetString(dictionary, "text"),
                        Effects = ToStringObjectDictionary(dictionary.TryGetValue("effects", out object effects) ? effects : null)
                    };
                    if (dictionary.TryGetValue("next", out object next) && TryToInt(next, out int nextIndex))
                    {
                        normalizedChoice.HasNextIndex = true;
                        normalizedChoice.NextIndex = nextIndex;
                    }

                    normalized.Add(normalizedChoice);
                }
            }

            return normalized;
        }

        private static List<DialogueChoice> ParseChoiceList(object choices)
        {
            List<DialogueChoice> parsed = new List<DialogueChoice>();
            if (choices is not IEnumerable enumerable || choices is string)
            {
                return parsed;
            }

            foreach (object choice in enumerable)
            {
                string text = Convert.ToString(choice) ?? string.Empty;
                string[] parts = text.Split(new[] { '>' }, 2);
                parsed.Add(new DialogueChoice
                {
                    Text = parts[0],
                    Effects = ParseCompactPairs(parts.Length > 1 ? parts[1] : string.Empty)
                });
            }

            return parsed;
        }

        private static Dictionary<string, object> ParseEffectPairs(object effects)
        {
            Dictionary<string, object> parsed = new Dictionary<string, object>();
            if (effects is not IEnumerable enumerable || effects is string)
            {
                return parsed;
            }

            foreach (object effect in enumerable)
            {
                if (effect is List<object> pair && pair.Count >= 2)
                {
                    parsed[Convert.ToString(pair[0]) ?? string.Empty] = pair[1];
                }
            }

            return parsed;
        }

        private static Dictionary<string, object> ParseCompactPairs(string text)
        {
            Dictionary<string, object> parsed = new Dictionary<string, object>();
            if (string.IsNullOrWhiteSpace(text))
            {
                return parsed;
            }

            foreach (string rawPair in text.Split(','))
            {
                string pair = rawPair.Trim();
                if (pair.Length == 0)
                {
                    continue;
                }

                string[] parts = pair.Split(new[] { ':' }, 2);
                if (parts.Length == 2)
                {
                    parsed[parts[0].Trim()] = ParseCompactValue(parts[1].Trim());
                }
            }

            return parsed;
        }

        private static object ParseCompactValue(string raw)
        {
            if (bool.TryParse(raw, out bool boolValue))
            {
                return boolValue;
            }

            if (long.TryParse(raw, out long longValue))
            {
                return longValue;
            }

            if (double.TryParse(raw, out double doubleValue))
            {
                return doubleValue;
            }

            return raw;
        }

        private static List<DialogueEntry> ResolveEntries(object topic, Dictionary<string, object> context)
        {
            if (topic is List<DialogueEntry> entries)
            {
                return entries;
            }

            if (topic is Dictionary<string, object> grouped)
            {
                string key = ContextKey(context);
                if (grouped.TryGetValue(key, out object keyed))
                {
                    return ResolveEntries(keyed, context);
                }

                if (grouped.TryGetValue("any", out object any))
                {
                    return ResolveEntries(any, context);
                }
            }

            return new List<DialogueEntry>();
        }

        private static string ContextKey(Dictionary<string, object> context)
        {
            if (context.TryGetValue("time_of_day", out object timeOfDay))
            {
                return Convert.ToString(timeOfDay) ?? "any";
            }

            if (context.TryGetValue("weather", out object weather))
            {
                return Convert.ToString(weather) ?? "any";
            }

            return "any";
        }

        private static bool ConditionsPass(Dictionary<string, object> conditions, Dictionary<string, object> context)
        {
            foreach (KeyValuePair<string, object> condition in conditions)
            {
                switch (condition.Key)
                {
                    case "bond_min":
                        if (Number(context, "bond_level") < ToDouble(condition.Value)) return false;
                        break;
                    case "bond_max":
                        if (Number(context, "bond_level") > ToDouble(condition.Value)) return false;
                        break;
                    case "humanity_min":
                        if (Number(context, "humanity", 100) < ToDouble(condition.Value)) return false;
                        break;
                    case "humanity_max":
                        if (Number(context, "humanity", 100) > ToDouble(condition.Value)) return false;
                        break;
                    case "weather":
                        if (Text(context, "weather") != Convert.ToString(condition.Value)) return false;
                        break;
                    case "weather_not":
                        if (Text(context, "weather") == Convert.ToString(condition.Value)) return false;
                        break;
                    case "is_full_moon":
                        if (Bool(context, "is_full_moon") != ToBool(condition.Value)) return false;
                        break;
                    case "trigger":
                        if (Text(context, "trigger") != Convert.ToString(condition.Value)) return false;
                        break;
                    case "first_meeting":
                        if ((Number(context, "times_met") == 0) != ToBool(condition.Value)) return false;
                        break;
                    case "times_met":
                        if ((int)Number(context, "times_met") != (int)ToDouble(condition.Value)) return false;
                        break;
                    case "times_met_min":
                        if (Number(context, "times_met") < ToDouble(condition.Value)) return false;
                        break;
                    case "talk_count_min":
                        if (Number(context, "talk_count") < ToDouble(condition.Value)) return false;
                        break;
                    case "talk_count_max":
                        if (Number(context, "talk_count") > ToDouble(condition.Value)) return false;
                        break;
                    case "event_not_seen":
                        if (ContainsValue(context, "seen_events", condition.Value)) return false;
                        break;
                    case "event":
                        if (!ContainsValue(context, "seen_events", condition.Value)) return false;
                        break;
                    case "quest_not_started":
                        if (ContainsValue(context, "started_quests", condition.Value)) return false;
                        break;
                    case "quest_active":
                        if (!ContainsValue(context, "active_quests", condition.Value)) return false;
                        break;
                    case "quest_completed":
                        if (!ContainsValue(context, "completed_quests", condition.Value)) return false;
                        break;
                }
            }

            return true;
        }

        private static List<DialogueLine> BuildRuntimeLines(DialogueEntry entry)
        {
            List<DialogueLine> lines = new List<DialogueLine>();
            foreach (DialogueLine line in entry.Lines)
            {
                lines.Add(line.Clone());
            }

            if (entry.Choices.Count > 0 && lines.Count > 0)
            {
                lines[^1].Choices = new List<DialogueChoice>();
                foreach (DialogueChoice choice in entry.Choices)
                {
                    lines[^1].Choices.Add(choice.Clone());
                }
            }

            return lines;
        }

        private static List<DialogueLine> BuildFallback(string npcId)
        {
            return new List<DialogueLine>
            {
                new DialogueLine
                {
                    Speaker = string.IsNullOrWhiteSpace(npcId) ? "???" : npcId,
                    Text = "现在还没有合适的话题。",
                    Expression = "neutral"
                }
            };
        }

        private static Dictionary<string, object> ToStringObjectDictionary(object value)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();
            if (value is not Dictionary<string, object> dictionary)
            {
                return result;
            }

            foreach (KeyValuePair<string, object> pair in dictionary)
            {
                result[pair.Key] = pair.Value;
            }

            return result;
        }

        private static string GetString(Dictionary<string, object> dictionary, string key)
        {
            return dictionary.TryGetValue(key, out object value) ? Convert.ToString(value) ?? string.Empty : string.Empty;
        }

        private static string NormalizeId(string value)
        {
            return (value ?? string.Empty).Trim().ToLowerInvariant();
        }

        private static double Number(Dictionary<string, object> context, string key, double fallback = 0)
        {
            return context.TryGetValue(key, out object value) ? ToDouble(value) : fallback;
        }

        private static string Text(Dictionary<string, object> context, string key)
        {
            return context.TryGetValue(key, out object value) ? Convert.ToString(value) ?? string.Empty : string.Empty;
        }

        private static bool Bool(Dictionary<string, object> context, string key)
        {
            return context.TryGetValue(key, out object value) && ToBool(value);
        }

        private static double ToDouble(object value)
        {
            return value switch
            {
                int intValue => intValue,
                long longValue => longValue,
                float floatValue => floatValue,
                double doubleValue => doubleValue,
                _ => double.TryParse(Convert.ToString(value), out double parsed) ? parsed : 0
            };
        }

        private static bool ToBool(object value)
        {
            return value switch
            {
                bool boolValue => boolValue,
                _ => bool.TryParse(Convert.ToString(value), out bool parsed) && parsed
            };
        }

        private static bool TryToInt(object value, out int parsed)
        {
            parsed = 0;
            if (value is int intValue)
            {
                parsed = intValue;
                return true;
            }

            if (value is long longValue)
            {
                parsed = (int)longValue;
                return true;
            }

            return int.TryParse(Convert.ToString(value), out parsed);
        }

        private static bool ContainsValue(Dictionary<string, object> context, string key, object expected)
        {
            if (!context.TryGetValue(key, out object value) || value is string)
            {
                return false;
            }

            if (value is not IEnumerable enumerable)
            {
                return false;
            }

            string expectedText = Convert.ToString(expected) ?? string.Empty;
            foreach (object item in enumerable)
            {
                if ((Convert.ToString(item) ?? string.Empty) == expectedText)
                {
                    return true;
                }
            }

            return false;
        }

        private sealed class NpcDialogueData
        {
            public string Id { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public Dictionary<string, object> Topics { get; } = new Dictionary<string, object>();
        }

        private sealed class DialogueEntry
        {
            public List<DialogueLine> Lines { get; set; } = new List<DialogueLine>();
            public Dictionary<string, object> Conditions { get; set; } = new Dictionary<string, object>();
            public Dictionary<string, object> Effects { get; set; } = new Dictionary<string, object>();
            public List<DialogueChoice> Choices { get; set; } = new List<DialogueChoice>();
        }
    }
}
