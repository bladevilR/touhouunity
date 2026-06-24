using System;
using System.Collections.Generic;

namespace TouhouMigration.Runtime.Cooking
{
    [Serializable]
    public sealed class CookingRecipe
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Difficulty { get; set; } = string.Empty;
        public List<CookingIngredientRequirement> Ingredients { get; } = new List<CookingIngredientRequirement>();
        public string ResultId { get; set; } = string.Empty;
        public int ResultQuantity { get; set; } = 1;
        public float CookingTime { get; set; }
        public int ExpGain { get; set; }
        public string Description { get; set; } = string.Empty;
        public string UnlockCondition { get; set; } = string.Empty;
    }
}
