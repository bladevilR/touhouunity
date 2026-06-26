namespace TouhouMigration.Runtime.Economy
{
    // A runtime shop: gates buy/sell on the shop's open hours + catalog (per-shop catalog price for
    // buying, per-shop buy_rate for selling), composing MigrationShopService for the coins/inventory
    // mechanic (no duplication). Godot ShopManager intent.
    public sealed class MigrationShop
    {
        private readonly MigrationShopDefinition definition;
        private readonly MigrationShopService service;
        private readonly MigrationShopStock stock;

        public MigrationShop(MigrationShopDefinition definition, MigrationShopService service)
            : this(definition, service, null)
        {
        }

        // Optional stock ledger: when supplied, Buy is gated on remaining stock (decremented on success)
        // and Sell refunds stock for carried items. When null, the shop has unlimited stock.
        public MigrationShop(MigrationShopDefinition definition, MigrationShopService service, MigrationShopStock stock)
        {
            this.definition = definition;
            this.service = service;
            this.stock = stock;
        }

        public ShopTransactionResult Buy(string itemId, int quantity, int currentHour)
        {
            if (definition == null || service == null)
            {
                return ShopTransactionResult.Fail(itemId, quantity, "shop_unconfigured");
            }

            if (!definition.IsOpen(currentHour))
            {
                return ShopTransactionResult.Fail(itemId, quantity, "shop_closed");
            }

            if (!definition.SellsItem(itemId))
            {
                return ShopTransactionResult.Fail(itemId, quantity, "not_for_sale");
            }

            if (stock != null && stock.GetStock(definition.ShopId, itemId) < quantity)
            {
                return ShopTransactionResult.Fail(itemId, quantity, "out_of_stock");
            }

            ShopTransactionResult result = service.Buy(itemId, quantity, definition.GetItemPrice(itemId));
            if (result.Success)
            {
                stock?.TryConsume(definition.ShopId, itemId, quantity);
            }

            return result;
        }

        public ShopTransactionResult Sell(string itemId, int quantity, int currentHour)
        {
            if (definition == null || service == null)
            {
                return ShopTransactionResult.Fail(itemId, quantity, "shop_unconfigured");
            }

            if (!definition.IsOpen(currentHour))
            {
                return ShopTransactionResult.Fail(itemId, quantity, "shop_closed");
            }

            ShopTransactionResult result = service.Sell(itemId, quantity, definition.BuyRate);
            if (result.Success && stock != null && stock.HasItem(definition.ShopId, itemId))
            {
                stock.Restock(definition.ShopId, itemId, quantity);
            }

            return result;
        }
    }
}
