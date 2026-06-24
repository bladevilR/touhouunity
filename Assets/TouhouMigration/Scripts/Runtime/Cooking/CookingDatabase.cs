using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TouhouMigration.Runtime.Serialization;

namespace TouhouMigration.Runtime.Cooking
{
    public sealed class CookingDatabase
    {
        private static readonly Dictionary<int, float> CookwareQualityBonus = new Dictionary<int, float>
        {
            { 1, 0f },
            { 2, 0.10f },
            { 3, 0.20f },
            { 4, 0.30f }
        };

        private static readonly Dictionary<string, int> CookwareLevelByTier = new Dictionary<string, int>
        {
            { "snack", 1 },
            { "drink", 1 },
            { "meal", 2 },
            { "feast", 3 }
        };

        private static readonly Dictionary<int, string> CookwareNameByLevel = new Dictionary<int, string>
        {
            { 1, "石灶" },
            { 2, "铁灶" },
            { 3, "灵火灶" },
            { 4, "凤凰灶" }
        };

        private static readonly int[] LevelExp = { 0, 50, 150, 350, 700, 1200, 1900, 2800, 3800, 5000 };

        private readonly Dictionary<string, CookingDishProfile> dishProfiles = new Dictionary<string, CookingDishProfile>();
        private readonly Dictionary<string, CookingRecipe> recipes = new Dictionary<string, CookingRecipe>();
        private readonly List<string> errors = new List<string>();

        public int DishCount => dishProfiles.Count;
        public int RecipeCount => recipes.Count;
        public IReadOnlyList<string> Errors => errors;

        public bool LoadFromPath(string filePath)
        {
            dishProfiles.Clear();
            errors.Clear();

            string resolvedPath = ResolvePath(filePath);
            if (!File.Exists(resolvedPath))
            {
                errors.Add($"Cooking profile file does not exist: {filePath}");
                return false;
            }

            try
            {
                object parsed = MigrationJson.Parse(File.ReadAllText(resolvedPath));
                if (parsed is not Dictionary<string, object> root)
                {
                    errors.Add("Cooking profile root is not an object.");
                    return false;
                }

                object rawProfiles = root.TryGetValue("dish_profiles", out object dishProfilesValue)
                    ? dishProfilesValue
                    : null;
                if (rawProfiles is not IList profileList)
                {
                    errors.Add("Cooking profile data has no dish_profiles list.");
                    return false;
                }

                foreach (object rawProfile in profileList)
                {
                    if (rawProfile is not Dictionary<string, object> dictionary)
                    {
                        continue;
                    }

                    CookingDishProfile profile = ParseProfile(dictionary);
                    if (!string.IsNullOrWhiteSpace(profile.Id))
                    {
                        dishProfiles[profile.Id] = profile;
                    }
                }
            }
            catch (Exception exception)
            {
                errors.Add(exception.Message);
            }

            return dishProfiles.Count > 0 && errors.Count == 0;
        }

        public bool LoadRecipesFromPath(string filePath)
        {
            recipes.Clear();

            string resolvedPath = ResolvePath(filePath);
            if (!File.Exists(resolvedPath))
            {
                errors.Add($"Cooking recipe file does not exist: {filePath}");
                return false;
            }

            try
            {
                object parsed = MigrationJson.Parse(File.ReadAllText(resolvedPath));
                if (parsed is not Dictionary<string, object> root)
                {
                    errors.Add("Cooking recipe root is not an object.");
                    return false;
                }

                object rawRecipes = root.TryGetValue("recipes", out object recipesValue)
                    ? recipesValue
                    : null;
                if (rawRecipes is not IList recipeList)
                {
                    errors.Add("Cooking recipe data has no recipes list.");
                    return false;
                }

                foreach (object rawRecipe in recipeList)
                {
                    if (rawRecipe is not Dictionary<string, object> dictionary)
                    {
                        continue;
                    }

                    CookingRecipe recipe = ParseRecipe(dictionary);
                    if (!string.IsNullOrWhiteSpace(recipe.Id))
                    {
                        recipes[recipe.Id] = recipe;
                    }
                }
            }
            catch (Exception exception)
            {
                errors.Add(exception.Message);
            }

            return recipes.Count > 0;
        }

        public bool HasDishCombatProfile(string dishId)
        {
            return dishProfiles.ContainsKey(NormalizeId(dishId));
        }

        public CookingDishProfile GetDishProfile(string dishId)
        {
            return dishProfiles.TryGetValue(NormalizeId(dishId), out CookingDishProfile profile)
                ? profile
                : null;
        }

        public IReadOnlyDictionary<string, CookingDishProfile> GetAllDishProfiles()
        {
            return dishProfiles;
        }

        public bool IsDishDrink(string dishId)
        {
            return GetDishTier(dishId) == "drink";
        }

        public string GetDishTier(string dishId)
        {
            return dishProfiles.TryGetValue(NormalizeId(dishId), out CookingDishProfile profile)
                ? profile.Tier
                : "snack";
        }

        public string GetDishMainStat(string dishId)
        {
            return dishProfiles.TryGetValue(NormalizeId(dishId), out CookingDishProfile profile)
                ? profile.MainStat
                : string.Empty;
        }

        public int GetDishStat(string dishId, string stat)
        {
            if (!dishProfiles.TryGetValue(NormalizeId(dishId), out CookingDishProfile profile))
            {
                return 0;
            }

            return profile.Stats.TryGetValue(NormalizeId(stat), out int value) ? value : 0;
        }

        public float GetDishBuffDuration(string dishId)
        {
            return dishProfiles.TryGetValue(NormalizeId(dishId), out CookingDishProfile profile)
                ? profile.BuffDuration
                : 0f;
        }

        public IReadOnlyList<string> GetDishSpecialEffects(string dishId)
        {
            return dishProfiles.TryGetValue(NormalizeId(dishId), out CookingDishProfile profile)
                ? profile.SpecialEffects
                : Array.Empty<string>();
        }

        public IReadOnlyList<string> GetDishDrinkEffects(string dishId)
        {
            return dishProfiles.TryGetValue(NormalizeId(dishId), out CookingDishProfile profile)
                ? profile.DrinkEffects
                : Array.Empty<string>();
        }

        public bool DishMatchesTier(string dishId, string tier)
        {
            string normalizedTier = NormalizeId(tier);
            return !string.IsNullOrEmpty(normalizedTier) && GetDishTier(dishId) == normalizedTier;
        }

        public bool DishMatchesStatRequirement(string dishId, string stat, int requiredStat)
        {
            string normalizedStat = NormalizeId(stat);
            if (string.IsNullOrEmpty(normalizedStat) || requiredStat <= 0 || !HasDishCombatProfile(dishId))
            {
                return false;
            }

            if (normalizedStat == "atk" && GetDishMainStat(dishId) != "atk")
            {
                return false;
            }

            return GetDishStat(dishId, normalizedStat) >= requiredStat;
        }

        public bool IsSymbolicItemMatch(string itemId, string symbolicItemId)
        {
            return NormalizeId(symbolicItemId) switch
            {
                "meal_any" => DishMatchesTier(itemId, "meal"),
                "drink_any" => DishMatchesTier(itemId, "drink"),
                "feast_any" => DishMatchesTier(itemId, "feast"),
                "atk_5_plus_any" => DishMatchesStatRequirement(itemId, "atk", 5),
                _ => false
            };
        }

        public CookingRecipe GetRecipe(string recipeId)
        {
            return recipes.TryGetValue(NormalizeId(recipeId), out CookingRecipe recipe) ? recipe : null;
        }

        public IReadOnlyDictionary<string, CookingRecipe> GetAllRecipes()
        {
            return recipes;
        }

        public string GetRecipeName(string recipeId)
        {
            return recipes.TryGetValue(NormalizeId(recipeId), out CookingRecipe recipe) ? recipe.Name : "未知食谱";
        }

        public string GetRecipeTier(string recipeId)
        {
            CookingRecipe recipe = GetRecipe(recipeId);
            return recipe == null || string.IsNullOrEmpty(recipe.ResultId) ? "snack" : GetDishTier(recipe.ResultId);
        }

        public bool CanCookWithCookware(string recipeId, int cookwareLevel)
        {
            if (!recipes.ContainsKey(NormalizeId(recipeId)))
            {
                return false;
            }

            return cookwareLevel >= GetRequiredCookwareLevelForTier(GetRecipeTier(recipeId));
        }

        public int GetRequiredCookwareLevelForTier(string tier)
        {
            return CookwareLevelByTier.TryGetValue(NormalizeId(tier), out int level) ? level : 1;
        }

        public string GetCookwareName(int level)
        {
            return CookwareNameByLevel.TryGetValue(level, out string name) ? name : "石灶";
        }

        public int CalculateDishQuality(int cookwareLevel)
        {
            return CalculateDishQuality(cookwareLevel, UnityEngine.Random.value);
        }

        public int CalculateDishQuality(int cookwareLevel, float roll)
        {
            float bonus = CookwareQualityBonus.TryGetValue(Math.Max(1, Math.Min(4, cookwareLevel)), out float value)
                ? value
                : 0f;
            float score = Math.Max(0f, Math.Min(1f, roll)) + bonus;
            if (score >= 0.95f)
            {
                return 3;
            }

            if (score >= 0.75f)
            {
                return 2;
            }

            return score >= 0.45f ? 1 : 0;
        }

        public string GetQualityName(int quality)
        {
            return quality switch
            {
                1 => "优良",
                2 => "极品",
                3 => "传说",
                _ => "普通"
            };
        }

        public float GetQualityMultiplier(int quality)
        {
            return quality switch
            {
                1 => 1.3f,
                2 => 1.6f,
                3 => 2.0f,
                _ => 1.0f
            };
        }

        public int GetExpForLevel(int level)
        {
            if (level >= 0 && level < LevelExp.Length)
            {
                return LevelExp[level];
            }

            return LevelExp[^1] + (level - LevelExp.Length + 1) * 700;
        }

        private static CookingDishProfile ParseProfile(Dictionary<string, object> rawProfile)
        {
            CookingDishProfile profile = new CookingDishProfile
            {
                Id = NormalizeId(GetString(rawProfile, "id")),
                Tier = NormalizeId(GetString(rawProfile, "tier", "snack")),
                MainStat = NormalizeId(GetString(rawProfile, "main_stat")),
                BuffDuration = ToFloat(rawProfile.TryGetValue("buff_duration", out object duration) ? duration : 0f)
            };

            if (rawProfile.TryGetValue("stats", out object rawStats) && rawStats is Dictionary<string, object> stats)
            {
                foreach (KeyValuePair<string, object> pair in stats)
                {
                    profile.Stats[NormalizeId(pair.Key)] = ToInt(pair.Value);
                }
            }

            profile.SpecialEffects.AddRange(ToStringList(rawProfile.TryGetValue("special_effects", out object specialEffects)
                ? specialEffects
                : null));
            profile.DrinkEffects.AddRange(ToStringList(rawProfile.TryGetValue("drink_effects", out object drinkEffects)
                ? drinkEffects
                : null));
            return profile;
        }

        private static CookingRecipe ParseRecipe(Dictionary<string, object> rawRecipe)
        {
            CookingRecipe recipe = new CookingRecipe
            {
                Id = NormalizeId(GetString(rawRecipe, "id")),
                Name = GetString(rawRecipe, "name"),
                Category = NormalizeId(GetString(rawRecipe, "category")),
                Difficulty = NormalizeId(GetString(rawRecipe, "difficulty")),
                ResultId = NormalizeId(GetString(rawRecipe, "result_id")),
                ResultQuantity = Math.Max(1, ToInt(rawRecipe.TryGetValue("result_qty", out object resultQty) ? resultQty : 1)),
                CookingTime = ToFloat(rawRecipe.TryGetValue("cooking_time", out object cookingTime) ? cookingTime : 0f),
                ExpGain = Math.Max(0, ToInt(rawRecipe.TryGetValue("exp_gain", out object expGain) ? expGain : 0)),
                Description = GetString(rawRecipe, "description"),
                UnlockCondition = NormalizeId(GetString(rawRecipe, "unlock_condition"))
            };

            if (rawRecipe.TryGetValue("ingredients", out object rawIngredients) && rawIngredients is IList ingredients)
            {
                foreach (object rawIngredient in ingredients)
                {
                    if (rawIngredient is not Dictionary<string, object> ingredient)
                    {
                        continue;
                    }

                    string id = NormalizeId(GetString(ingredient, "id"));
                    int quantity = Math.Max(0, ToInt(ingredient.TryGetValue("quantity", out object value) ? value : 0));
                    if (!string.IsNullOrEmpty(id) && quantity > 0)
                    {
                        recipe.Ingredients.Add(new CookingIngredientRequirement
                        {
                            Id = id,
                            Quantity = quantity
                        });
                    }
                }
            }

            return recipe;
        }

        private static List<string> ToStringList(object value)
        {
            List<string> result = new List<string>();
            if (value is not IList list)
            {
                return result;
            }

            foreach (object item in list)
            {
                string text = NormalizeId(Convert.ToString(item) ?? string.Empty);
                if (!string.IsNullOrEmpty(text))
                {
                    result.Add(text);
                }
            }

            return result;
        }

        private static string ResolvePath(string filePath)
        {
            return Path.IsPathRooted(filePath)
                ? filePath
                : Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), filePath));
        }

        private static string GetString(Dictionary<string, object> dictionary, string key, string fallback = "")
        {
            return dictionary.TryGetValue(key, out object value) ? Convert.ToString(value) ?? fallback : fallback;
        }

        private static int ToInt(object value)
        {
            try
            {
                return Convert.ToInt32(value);
            }
            catch
            {
                return 0;
            }
        }

        private static float ToFloat(object value)
        {
            try
            {
                return Convert.ToSingle(value);
            }
            catch
            {
                return 0f;
            }
        }

        private static string NormalizeId(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToLowerInvariant();
        }
    }
}
