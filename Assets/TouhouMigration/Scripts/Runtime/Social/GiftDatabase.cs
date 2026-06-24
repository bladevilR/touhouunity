using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TouhouMigration.Runtime.Serialization;

namespace TouhouMigration.Runtime.Social
{
    public sealed class GiftDatabase
    {
        private readonly Dictionary<string, GiftDefinition> gifts = new Dictionary<string, GiftDefinition>();
        private readonly Dictionary<string, GiftPreference> preferences = new Dictionary<string, GiftPreference>();
        private readonly List<string> errors = new List<string>();

        public int GiftCount => gifts.Count;
        public int PreferenceCount => preferences.Count;
        public float BirthdayGiftMultiplier { get; private set; } = 2f;
        public IReadOnlyList<string> Errors => errors;

        public bool LoadFromPath(string filePath)
        {
            gifts.Clear();
            preferences.Clear();
            errors.Clear();
            BirthdayGiftMultiplier = 2f;

            if (!File.Exists(filePath))
            {
                errors.Add($"Gift data file does not exist: {filePath}");
                return false;
            }

            try
            {
                object parsed = MigrationJson.Parse(File.ReadAllText(filePath));
                if (parsed is not Dictionary<string, object> root)
                {
                    errors.Add("Gift data root is not an object.");
                    return false;
                }

                LoadGiftDefinitions(root.TryGetValue("gifts", out object giftRoot) ? giftRoot : null);
                LoadPreferences(root.TryGetValue("npc_preferences", out object preferenceRoot) ? preferenceRoot : null);
                BirthdayGiftMultiplier = ToFloat(root.TryGetValue("birthday_gift_multiplier", out object multiplier) ? multiplier : 2f, 2f);
            }
            catch (Exception exception)
            {
                errors.Add(exception.Message);
            }

            return gifts.Count > 0 && preferences.Count > 0 && errors.Count == 0;
        }

        public bool HasGift(string giftId)
        {
            return gifts.ContainsKey(NormalizeId(giftId));
        }

        public bool HasPreference(string npcId)
        {
            return preferences.ContainsKey(NormalizeId(npcId));
        }

        public GiftDefinition GetGift(string giftId)
        {
            return gifts.TryGetValue(NormalizeId(giftId), out GiftDefinition gift) ? gift : null;
        }

        public GiftReactionResult GetReaction(string npcId, string giftId)
        {
            string normalizedNpcId = NormalizeId(npcId);
            string normalizedGiftId = NormalizeId(giftId);
            GiftPreference preference = preferences.TryGetValue(normalizedNpcId, out GiftPreference foundPreference)
                ? foundPreference
                : new GiftPreference();

            GiftDefinition gift = GetGift(normalizedGiftId);
            string reactionId = ResolveReactionId(preference, normalizedGiftId, gift);
            string specialEvent = reactionId == "SPECIAL" && preference.Special.TryGetValue(normalizedGiftId, out string eventId)
                ? eventId
                : string.Empty;
            int bondChange = CalculateBondChange(preference, gift, reactionId);
            string dialogue = ResolveDialogue(preference, reactionId);

            return new GiftReactionResult
            {
                NpcId = normalizedNpcId,
                GiftId = normalizedGiftId,
                ReactionId = reactionId,
                BondChange = bondChange,
                Dialogue = dialogue,
                SpecialEvent = specialEvent
            };
        }

        public int CalculateBondChange(string npcId, string giftId)
        {
            return GetReaction(npcId, giftId).BondChange;
        }

        public List<string> GetRecommendedGiftIds(string npcId, IEnumerable<string> availableGiftIds)
        {
            List<(string GiftId, int Score)> scored = new List<(string, int)>();
            foreach (string giftId in availableGiftIds)
            {
                GiftReactionResult reaction = GetReaction(npcId, giftId);
                scored.Add((giftId, ReactionScore(reaction.ReactionId)));
            }

            scored.Sort((left, right) => right.Score.CompareTo(left.Score));
            List<string> result = new List<string>();
            foreach ((string giftId, _) in scored)
            {
                result.Add(giftId);
            }

            return result;
        }

        private void LoadGiftDefinitions(object giftRoot)
        {
            if (giftRoot is not Dictionary<string, object> giftDictionary)
            {
                return;
            }

            foreach (KeyValuePair<string, object> pair in giftDictionary)
            {
                if (pair.Value is not Dictionary<string, object> rawGift)
                {
                    continue;
                }

                string id = NormalizeId(pair.Key);
                gifts[id] = new GiftDefinition
                {
                    Id = id,
                    Name = GetString(rawGift, "name"),
                    Category = string.IsNullOrWhiteSpace(GetString(rawGift, "category")) ? "JUNK" : GetString(rawGift, "category"),
                    Description = GetString(rawGift, "description"),
                    BaseValue = ToInt(rawGift.TryGetValue("base_value", out object baseValue) ? baseValue : 0),
                    Tags = ToStringList(rawGift.TryGetValue("tags", out object tags) ? tags : null)
                };
            }
        }

        private void LoadPreferences(object preferenceRoot)
        {
            if (preferenceRoot is not Dictionary<string, object> preferenceDictionary)
            {
                return;
            }

            foreach (KeyValuePair<string, object> pair in preferenceDictionary)
            {
                if (pair.Value is not Dictionary<string, object> rawPreference)
                {
                    continue;
                }

                GiftPreference preference = new GiftPreference
                {
                    Loves = ToStringSet(rawPreference.TryGetValue("loves", out object loves) ? loves : null),
                    Likes = ToStringSet(rawPreference.TryGetValue("likes", out object likes) ? likes : null),
                    Dislikes = ToStringSet(rawPreference.TryGetValue("dislikes", out object dislikes) ? dislikes : null),
                    Hates = ToStringSet(rawPreference.TryGetValue("hates", out object hates) ? hates : null),
                    LikedTags = ToStringSet(rawPreference.TryGetValue("liked_tags", out object likedTags) ? likedTags : null),
                    DislikedTags = ToStringSet(rawPreference.TryGetValue("disliked_tags", out object dislikedTags) ? dislikedTags : null),
                    Special = ToStringDictionary(rawPreference.TryGetValue("special", out object special) ? special : null),
                    CategoryBonus = ToFloatDictionary(rawPreference.TryGetValue("category_bonus", out object categoryBonus) ? categoryBonus : null),
                    Dialogues = ToDialogueDictionary(rawPreference.TryGetValue("dialogues", out object dialogues) ? dialogues : null)
                };
                preferences[NormalizeId(pair.Key)] = preference;
            }
        }

        private static string ResolveReactionId(GiftPreference preference, string giftId, GiftDefinition gift)
        {
            if (preference.Special.ContainsKey(giftId))
            {
                return "SPECIAL";
            }

            if (preference.Loves.Contains(giftId))
            {
                return "LOVE";
            }

            if (preference.Likes.Contains(giftId))
            {
                return "LIKE";
            }

            if (preference.Hates.Contains(giftId))
            {
                return "HATE";
            }

            if (preference.Dislikes.Contains(giftId))
            {
                return "DISLIKE";
            }

            if (gift == null || gift.Tags.Count == 0)
            {
                return "NEUTRAL";
            }

            int likeHits = 0;
            int dislikeHits = 0;
            foreach (string tag in gift.Tags)
            {
                if (preference.LikedTags.Contains(tag))
                {
                    likeHits++;
                }

                if (preference.DislikedTags.Contains(tag))
                {
                    dislikeHits++;
                }
            }

            int netScore = likeHits - dislikeHits;
            if (netScore >= 2)
            {
                return "LIKE";
            }

            if (netScore <= -1)
            {
                return "DISLIKE";
            }

            return "NEUTRAL";
        }

        private static int CalculateBondChange(GiftPreference preference, GiftDefinition gift, string reactionId)
        {
            int baseChange = reactionId switch
            {
                "LOVE" => 50,
                "LIKE" => 25,
                "NEUTRAL" => 10,
                "DISLIKE" => 0,
                "HATE" => -15,
                "SPECIAL" => 30,
                _ => 10
            };

            if (gift != null && preference.CategoryBonus.TryGetValue(gift.Category, out float multiplier))
            {
                baseChange = (int)(baseChange * multiplier);
            }

            return baseChange;
        }

        private static string ResolveDialogue(GiftPreference preference, string reactionId)
        {
            if (preference.Dialogues.TryGetValue(reactionId, out List<string> lines) && lines.Count > 0)
            {
                return lines[0];
            }

            return "谢谢。";
        }

        private static int ReactionScore(string reactionId)
        {
            return reactionId switch
            {
                "LOVE" => 100,
                "SPECIAL" => 90,
                "LIKE" => 75,
                "NEUTRAL" => 50,
                "DISLIKE" => 25,
                "HATE" => 0,
                _ => 50
            };
        }

        private static HashSet<string> ToStringSet(object value)
        {
            return new HashSet<string>(ToStringList(value));
        }

        private static List<string> ToStringList(object value)
        {
            List<string> result = new List<string>();
            if (value is not IEnumerable enumerable || value is string)
            {
                return result;
            }

            foreach (object item in enumerable)
            {
                string text = NormalizeId(Convert.ToString(item));
                if (!string.IsNullOrWhiteSpace(text))
                {
                    result.Add(text);
                }
            }

            return result;
        }

        private static Dictionary<string, string> ToStringDictionary(object value)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            if (value is not Dictionary<string, object> dictionary)
            {
                return result;
            }

            foreach (KeyValuePair<string, object> pair in dictionary)
            {
                result[NormalizeId(pair.Key)] = Convert.ToString(pair.Value) ?? string.Empty;
            }

            return result;
        }

        private static Dictionary<string, float> ToFloatDictionary(object value)
        {
            Dictionary<string, float> result = new Dictionary<string, float>();
            if (value is not Dictionary<string, object> dictionary)
            {
                return result;
            }

            foreach (KeyValuePair<string, object> pair in dictionary)
            {
                result[pair.Key] = ToFloat(pair.Value, 1f);
            }

            return result;
        }

        private static Dictionary<string, List<string>> ToDialogueDictionary(object value)
        {
            Dictionary<string, List<string>> result = new Dictionary<string, List<string>>();
            if (value is not Dictionary<string, object> dictionary)
            {
                return result;
            }

            foreach (KeyValuePair<string, object> pair in dictionary)
            {
                result[pair.Key] = ToRawStringList(pair.Value);
            }

            return result;
        }

        private static List<string> ToRawStringList(object value)
        {
            List<string> result = new List<string>();
            if (value is not IEnumerable enumerable || value is string)
            {
                return result;
            }

            foreach (object item in enumerable)
            {
                result.Add(Convert.ToString(item) ?? string.Empty);
            }

            return result;
        }

        private static string GetString(Dictionary<string, object> dictionary, string key)
        {
            return dictionary.TryGetValue(key, out object value) ? Convert.ToString(value) ?? string.Empty : string.Empty;
        }

        private static int ToInt(object value)
        {
            return value switch
            {
                int intValue => intValue,
                long longValue => (int)longValue,
                float floatValue => (int)floatValue,
                double doubleValue => (int)doubleValue,
                _ => int.TryParse(Convert.ToString(value), out int parsed) ? parsed : 0
            };
        }

        private static float ToFloat(object value, float fallback)
        {
            return value switch
            {
                int intValue => intValue,
                long longValue => longValue,
                float floatValue => floatValue,
                double doubleValue => (float)doubleValue,
                _ => float.TryParse(Convert.ToString(value), out float parsed) ? parsed : fallback
            };
        }

        private static string NormalizeId(string value)
        {
            return (value ?? string.Empty).Trim().ToLowerInvariant();
        }

        private sealed class GiftPreference
        {
            public HashSet<string> Loves { get; set; } = new HashSet<string>();
            public HashSet<string> Likes { get; set; } = new HashSet<string>();
            public HashSet<string> Dislikes { get; set; } = new HashSet<string>();
            public HashSet<string> Hates { get; set; } = new HashSet<string>();
            public HashSet<string> LikedTags { get; set; } = new HashSet<string>();
            public HashSet<string> DislikedTags { get; set; } = new HashSet<string>();
            public Dictionary<string, string> Special { get; set; } = new Dictionary<string, string>();
            public Dictionary<string, float> CategoryBonus { get; set; } = new Dictionary<string, float>();
            public Dictionary<string, List<string>> Dialogues { get; set; } = new Dictionary<string, List<string>>();
        }
    }
}
