namespace TouhouMigration.Runtime.Social
{
    public sealed class GiftReactionResult
    {
        public string NpcId { get; set; } = string.Empty;
        public string GiftId { get; set; } = string.Empty;
        public string ReactionId { get; set; } = "NEUTRAL";
        public int BondChange { get; set; }
        public string Dialogue { get; set; } = "谢谢。";
        public string SpecialEvent { get; set; } = string.Empty;
    }
}
