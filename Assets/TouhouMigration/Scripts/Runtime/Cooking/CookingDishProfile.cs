using System.Collections.Generic;

namespace TouhouMigration.Runtime.Cooking
{
    public sealed class CookingDishProfile
    {
        public string Id { get; set; } = string.Empty;
        public string Tier { get; set; } = string.Empty;
        public string MainStat { get; set; } = string.Empty;
        public Dictionary<string, int> Stats { get; } = new Dictionary<string, int>();
        public float BuffDuration { get; set; }
        public List<string> SpecialEffects { get; } = new List<string>();
        public List<string> DrinkEffects { get; } = new List<string>();
    }
}
