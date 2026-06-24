using System;
using System.Collections.Generic;

namespace TouhouMigration.Runtime.Cooking
{
    [Serializable]
    public sealed class CookingRuntimeSnapshot
    {
        public int cooking_level = 1;
        public int cooking_exp;
        public int cookware_level = 1;
        public List<string> unlocked_recipes = new List<string>();

        public int CookingLevel
        {
            get => cooking_level;
            set => cooking_level = Math.Max(1, value);
        }

        public int CookingExperience
        {
            get => cooking_exp;
            set => cooking_exp = Math.Max(0, value);
        }

        public int CookwareLevel
        {
            get => cookware_level;
            set => cookware_level = Math.Max(1, Math.Min(4, value));
        }

        public IReadOnlyList<string> UnlockedRecipes => unlocked_recipes;

        public void AddUnlockedRecipe(string recipeId)
        {
            string normalizedRecipeId = NormalizeId(recipeId);
            if (!string.IsNullOrEmpty(normalizedRecipeId) && !unlocked_recipes.Contains(normalizedRecipeId))
            {
                unlocked_recipes.Add(normalizedRecipeId);
            }
        }

        public bool HasUnlockedRecipe(string recipeId)
        {
            return unlocked_recipes.Contains(NormalizeId(recipeId));
        }

        private static string NormalizeId(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToLowerInvariant();
        }
    }
}
