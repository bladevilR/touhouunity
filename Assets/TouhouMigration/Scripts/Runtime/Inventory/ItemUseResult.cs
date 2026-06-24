namespace TouhouMigration.Runtime.Inventory
{
    public sealed class ItemUseResult
    {
        public bool Success { get; set; }
        public string ItemId { get; set; } = string.Empty;
        public string ItemType { get; set; } = string.Empty;
        public string FailureReason { get; set; } = string.Empty;
        public int Quality { get; set; }
        public int HealAmount { get; set; }
        public bool AppliedCookingBuff { get; set; }
        public string AppliedStatusEffect { get; set; } = string.Empty;
    }
}
