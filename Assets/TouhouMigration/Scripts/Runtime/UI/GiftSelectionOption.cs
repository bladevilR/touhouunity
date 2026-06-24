namespace TouhouMigration.Runtime.UI
{
    public sealed class GiftSelectionOption
    {
        public string GiftId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int Amount { get; set; }
        public string ReactionId { get; set; } = "NEUTRAL";
        public int BondChange { get; set; }
    }
}
