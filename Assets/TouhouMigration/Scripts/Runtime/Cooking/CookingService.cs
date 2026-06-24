using System;
using System.Collections.Generic;
using TouhouMigration.Runtime.Inventory;
using TouhouMigration.Runtime.Social;

namespace TouhouMigration.Runtime.Cooking
{
    public sealed class CookingService
    {
        private static readonly string[] DefaultRecipes =
        {
            "onigiri",
            "grilled_fish",
            "miso_soup",
            "dango",
            "green_tea",
            "herb_salad",
            "mokou_yakitori"
        };

        private readonly CookingDatabase cookingDatabase;
        private readonly InventoryService inventoryService;
        private readonly ItemDatabase itemDatabase;
        private readonly QuestDeliveryService questDeliveryService;
        private readonly HashSet<string> unlockedRecipes = new HashSet<string>();

        public CookingService(
            CookingDatabase cookingDatabase,
            InventoryService inventoryService,
            ItemDatabase itemDatabase,
            QuestDeliveryService questDeliveryService = null)
        {
            this.cookingDatabase = cookingDatabase;
            this.inventoryService = inventoryService;
            this.itemDatabase = itemDatabase;
            this.questDeliveryService = questDeliveryService;

            foreach (string recipeId in DefaultRecipes)
            {
                unlockedRecipes.Add(recipeId);
            }
        }

        public int CookingLevel { get; private set; } = 1;
        public int CookingExperience { get; private set; }
        public int CookwareLevel { get; private set; } = 1;

        public bool CanCook(string recipeId)
        {
            string normalizedRecipeId = NormalizeId(recipeId);
            CookingRecipe recipe = cookingDatabase?.GetRecipe(normalizedRecipeId);
            if (recipe == null || !unlockedRecipes.Contains(normalizedRecipeId))
            {
                return false;
            }

            if (!cookingDatabase.CanCookWithCookware(normalizedRecipeId, CookwareLevel))
            {
                return false;
            }

            foreach (CookingIngredientRequirement ingredient in recipe.Ingredients)
            {
                if (CountIngredient(ingredient.Id) < ingredient.Quantity)
                {
                    return false;
                }
            }

            return true;
        }

        public CookingResult Cook(string recipeId)
        {
            return Cook(recipeId, UnityEngine.Random.value);
        }

        public CookingResult Cook(string recipeId, float qualityRoll)
        {
            string normalizedRecipeId = NormalizeId(recipeId);
            CookingRecipe recipe = cookingDatabase?.GetRecipe(normalizedRecipeId);
            if (recipe == null)
            {
                return Failed(normalizedRecipeId, "invalid_recipe");
            }

            if (!unlockedRecipes.Contains(normalizedRecipeId))
            {
                return Failed(normalizedRecipeId, "recipe_locked");
            }

            if (!cookingDatabase.CanCookWithCookware(normalizedRecipeId, CookwareLevel))
            {
                return Failed(normalizedRecipeId, "cookware_level_low");
            }

            foreach (CookingIngredientRequirement ingredient in recipe.Ingredients)
            {
                if (CountIngredient(ingredient.Id) < ingredient.Quantity)
                {
                    return Failed(normalizedRecipeId, "missing_ingredients");
                }
            }

            foreach (CookingIngredientRequirement ingredient in recipe.Ingredients)
            {
                ConsumeIngredient(ingredient.Id, ingredient.Quantity);
            }

            int quality = cookingDatabase.CalculateDishQuality(CookwareLevel, qualityRoll);
            bool added = inventoryService != null &&
                inventoryService.AddItem(recipe.ResultId, recipe.ResultQuantity, quality);
            if (!added)
            {
                return Failed(normalizedRecipeId, "result_inventory_full");
            }

            questDeliveryService?.NotifyCraftCompleted(recipe.ResultId, recipe.ResultQuantity);
            AddExperience(recipe.ExpGain);

            return new CookingResult
            {
                Success = true,
                RecipeId = normalizedRecipeId,
                ResultItemId = recipe.ResultId,
                ResultQuantity = recipe.ResultQuantity,
                Quality = quality,
                ExpGained = recipe.ExpGain
            };
        }

        public bool UnlockRecipe(string recipeId)
        {
            string normalizedRecipeId = NormalizeId(recipeId);
            if (string.IsNullOrEmpty(normalizedRecipeId) || cookingDatabase?.GetRecipe(normalizedRecipeId) == null)
            {
                return false;
            }

            return unlockedRecipes.Add(normalizedRecipeId);
        }

        public IReadOnlyCollection<string> GetUnlockedRecipes()
        {
            return unlockedRecipes;
        }

        public CookingRuntimeSnapshot CreateSnapshot()
        {
            CookingRuntimeSnapshot snapshot = new CookingRuntimeSnapshot
            {
                CookingLevel = CookingLevel,
                CookingExperience = CookingExperience,
                CookwareLevel = CookwareLevel
            };

            foreach (string recipeId in unlockedRecipes)
            {
                snapshot.AddUnlockedRecipe(recipeId);
            }

            return snapshot;
        }

        public void LoadSnapshot(CookingRuntimeSnapshot snapshot)
        {
            unlockedRecipes.Clear();
            foreach (string recipeId in DefaultRecipes)
            {
                unlockedRecipes.Add(recipeId);
            }

            if (snapshot == null)
            {
                CookingLevel = 1;
                CookingExperience = 0;
                CookwareLevel = 1;
                return;
            }

            CookingLevel = Math.Max(1, snapshot.CookingLevel);
            CookingExperience = Math.Max(0, snapshot.CookingExperience);
            CookwareLevel = Math.Max(1, Math.Min(4, snapshot.CookwareLevel));
            foreach (string recipeId in snapshot.UnlockedRecipes)
            {
                string normalizedRecipeId = NormalizeId(recipeId);
                if (!string.IsNullOrEmpty(normalizedRecipeId) && cookingDatabase?.GetRecipe(normalizedRecipeId) != null)
                {
                    unlockedRecipes.Add(normalizedRecipeId);
                }
            }
        }

        public void SetCookwareLevel(int cookwareLevel)
        {
            CookwareLevel = Math.Max(1, Math.Min(4, cookwareLevel));
        }

        public int CountIngredient(string ingredientId)
        {
            string normalizedIngredientId = NormalizeId(ingredientId);
            if (inventoryService == null || itemDatabase == null || string.IsNullOrEmpty(normalizedIngredientId))
            {
                return 0;
            }

            if (normalizedIngredientId == "fish_any")
            {
                return CountByPredicate(item => item != null && item.ItemType == "fish");
            }

            if (normalizedIngredientId == "meat_any")
            {
                return CountByPredicate(item =>
                    item != null &&
                    item.ItemType == "ingredient" &&
                    item.Id.EndsWith("_meat", StringComparison.Ordinal));
            }

            return inventoryService.GetItemCount(normalizedIngredientId);
        }

        private int CountByPredicate(Func<ItemDefinition, bool> predicate)
        {
            int total = 0;
            foreach (KeyValuePair<string, int> pair in inventoryService.GetAllItems())
            {
                ItemDefinition item = itemDatabase.GetItem(pair.Key);
                if (predicate(item))
                {
                    total += pair.Value;
                }
            }

            return total;
        }

        private void ConsumeIngredient(string ingredientId, int quantity)
        {
            string normalizedIngredientId = NormalizeId(ingredientId);
            if (quantity <= 0 || string.IsNullOrEmpty(normalizedIngredientId))
            {
                return;
            }

            if (normalizedIngredientId == "fish_any")
            {
                ConsumeByPredicate(quantity, item => item != null && item.ItemType == "fish");
                return;
            }

            if (normalizedIngredientId == "meat_any")
            {
                ConsumeByPredicate(
                    quantity,
                    item => item != null && item.ItemType == "ingredient" && item.Id.EndsWith("_meat", StringComparison.Ordinal));
                return;
            }

            inventoryService?.RemoveItem(normalizedIngredientId, quantity);
        }

        private void ConsumeByPredicate(int quantity, Func<ItemDefinition, bool> predicate)
        {
            int remaining = quantity;
            List<KeyValuePair<string, int>> inventoryItems = new List<KeyValuePair<string, int>>(inventoryService.GetAllItems());
            foreach (KeyValuePair<string, int> pair in inventoryItems)
            {
                if (remaining <= 0)
                {
                    break;
                }

                ItemDefinition item = itemDatabase.GetItem(pair.Key);
                if (!predicate(item))
                {
                    continue;
                }

                int take = Math.Min(pair.Value, remaining);
                if (take > 0 && inventoryService.RemoveItem(pair.Key, take))
                {
                    remaining -= take;
                }
            }
        }

        private void AddExperience(int amount)
        {
            if (amount <= 0 || cookingDatabase == null)
            {
                return;
            }

            CookingExperience += amount;
            int needed = cookingDatabase.GetExpForLevel(CookingLevel);
            while (CookingExperience >= needed && CookingLevel < 10)
            {
                CookingExperience -= needed;
                CookingLevel++;
                UnlockRecipesForLevel(CookingLevel);
                needed = cookingDatabase.GetExpForLevel(CookingLevel);
            }
        }

        private void UnlockRecipesForLevel(int level)
        {
            foreach (KeyValuePair<string, CookingRecipe> pair in cookingDatabase.GetAllRecipes())
            {
                const string prefix = "cooking_level_";
                if (!pair.Value.UnlockCondition.StartsWith(prefix, StringComparison.Ordinal))
                {
                    continue;
                }

                string levelText = pair.Value.UnlockCondition.Substring(prefix.Length);
                if (int.TryParse(levelText, out int requiredLevel) && level >= requiredLevel)
                {
                    unlockedRecipes.Add(pair.Key);
                }
            }
        }

        private static CookingResult Failed(string recipeId, string reason)
        {
            return new CookingResult
            {
                Success = false,
                RecipeId = recipeId,
                FailureReason = reason
            };
        }

        private static string NormalizeId(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToLowerInvariant();
        }
    }
}
