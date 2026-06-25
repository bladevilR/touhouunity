namespace TouhouMigration.Runtime.Economy
{
    // A runtime shop: gates buy/sell on the shop's open hours + catalog (per-shop catalog price for
    // buying, per-shop buy_rate for selling), composing MigrationShopService for the coins/inventory
    // mechanic (no duplication). Godot ShopManager intent.
    public sealed class MigrationShop
    {
        private readonly MigrationShopDefinition definition;
        private readonly MigrationShopService service;

        public MigrationShop(MigrationShopDefinition definition, MigrationShopService service)
        {
            this.definition = definition;
            this.service = service;
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

            return service.Buy(itemId, quantity, definition.GetItemPrice(itemId));
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

            return service.Sell(itemId, quantity, definition.BuyRate);
        }
    }
}
