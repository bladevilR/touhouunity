namespace TouhouMigration.Runtime.Farming
{
    // Outcome of a farm-plot harvest: the produce item id + amount on success, or a failure reason
    // (no_plot / not_ready / unknown_crop).
    public sealed class MigrationHarvestResult
    {
        public bool Success { get; }
        public string ItemId { get; }
        public int Amount { get; }
        public string FailureReason { get; }

        private MigrationHarvestResult(bool success, string itemId, int amount, string failureReason)
        {
            Success = success;
            ItemId = itemId ?? string.Empty;
            Amount = amount;
            FailureReason = failureReason ?? string.Empty;
        }

        public static MigrationHarvestResult Ok(string itemId, int amount)
        {
            return new MigrationHarvestResult(true, itemId, amount, string.Empty);
        }

        public static MigrationHarvestResult Fail(string reason)
        {
            return new MigrationHarvestResult(false, string.Empty, 0, reason);
        }
    }
}
