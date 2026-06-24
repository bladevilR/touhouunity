namespace TouhouMigration.Runtime.Social
{
    public sealed class GiftDeliveryResult
    {
        public bool Success { get; set; }
        public string NpcId { get; set; } = string.Empty;
        public string GiftId { get; set; } = string.Empty;
        public string ReactionId { get; set; } = "NEUTRAL";
        public int BondChange { get; set; }
        public string Dialogue { get; set; } = string.Empty;
        public string SpecialEvent { get; set; } = string.Empty;
        public int RemainingAmount { get; set; }
        public string FailureReason { get; set; } = string.Empty;
    }
}
