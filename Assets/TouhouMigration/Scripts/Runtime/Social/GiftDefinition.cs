using System.Collections.Generic;

namespace TouhouMigration.Runtime.Social
{
    public sealed class GiftDefinition
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = "JUNK";
        public string Description { get; set; } = string.Empty;
        public int BaseValue { get; set; }
        public List<string> Tags { get; set; } = new List<string>();
    }
}
