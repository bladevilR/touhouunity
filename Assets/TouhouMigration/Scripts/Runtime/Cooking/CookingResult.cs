using System;

namespace TouhouMigration.Runtime.Cooking
{
    [Serializable]
    public sealed class CookingResult
    {
        public bool Success { get; set; }
        public string FailureReason { get; set; } = string.Empty;
        public string RecipeId { get; set; } = string.Empty;
        public string ResultItemId { get; set; } = string.Empty;
        public int ResultQuantity { get; set; }
        public int Quality { get; set; }
        public int ExpGained { get; set; }
    }
}
