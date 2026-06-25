namespace TouhouMigration.Runtime.Fishing
{
    // Outcome of a fishing cast: the caught fish (id / item / rarity) on success, or a failure reason
    // (no_fish).
    public sealed class MigrationFishCatchResult
    {
        public bool Success { get; }
        public string FishId { get; }
        public string ItemId { get; }
        public MigrationFishRarity Rarity { get; }
        public string FailureReason { get; }

        private MigrationFishCatchResult(bool success, string fishId, string itemId, MigrationFishRarity rarity, string failureReason)
        {
            Success = success;
            FishId = fishId ?? string.Empty;
            ItemId = itemId ?? string.Empty;
            Rarity = rarity;
            FailureReason = failureReason ?? string.Empty;
        }

        public static MigrationFishCatchResult Ok(string fishId, string itemId, MigrationFishRarity rarity)
        {
            return new MigrationFishCatchResult(true, fishId, itemId, rarity, string.Empty);
        }

        public static MigrationFishCatchResult Fail(string reason)
        {
            return new MigrationFishCatchResult(false, string.Empty, string.Empty, MigrationFishRarity.Common, reason);
        }
    }
}
