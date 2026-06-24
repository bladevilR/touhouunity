using System;

namespace TouhouMigration.Runtime.Cooking
{
    [Serializable]
    public sealed class CookingIngredientRequirement
    {
        public string Id { get; set; } = string.Empty;
        public int Quantity { get; set; }
    }
}
