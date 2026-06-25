namespace TouhouMigration.Runtime.Economy
{
    // Outcome of a shop buy/sell. CoinDelta is negative when the player spent coins (buy) and
    // positive when the player earned coins (sell); 0 on failure.
    public sealed class ShopTransactionResult
    {
        public bool Success { get; }
        public string ItemId { get; }
        public int Quantity { get; }
        public int CoinDelta { get; }
        public string FailureReason { get; }

        private ShopTransactionResult(bool success, string itemId, int quantity, int coinDelta, string failureReason)
        {
            Success = success;
            ItemId = itemId ?? string.Empty;
            Quantity = quantity;
            CoinDelta = coinDelta;
            FailureReason = failureReason ?? string.Empty;
        }

        public static ShopTransactionResult Ok(string itemId, int quantity, int coinDelta)
        {
            return new ShopTransactionResult(true, itemId, quantity, coinDelta, string.Empty);
        }

        public static ShopTransactionResult Fail(string itemId, int quantity, string reason)
        {
            return new ShopTransactionResult(false, itemId, quantity, 0, reason);
        }
    }
}
