using System;
using TouhouMigration.Runtime.Inventory;
using TouhouMigration.Runtime.Player;

namespace TouhouMigration.Runtime.Economy
{
    // Unity-native shop economy (Godot ShopData/ShopManager intent): buy items for coins, sell items
    // back at a buy_rate. Bridges InventoryService (items), ItemDatabase (prices), and
    // MigrationPlayerProgressService (coins). Free of UnityEngine so it stays unit-testable.
    public sealed class MigrationShopService
    {
        // Godot ShopData.get_buy_rate default: a shop pays 50% of an item's price to buy it back.
        public const float DefaultBuyRate = 0.5f;

        private readonly InventoryService inventory;
        private readonly ItemDatabase items;
        private readonly MigrationPlayerProgressService progress;

        public MigrationShopService(InventoryService inventory, ItemDatabase items, MigrationPlayerProgressService progress)
        {
            this.inventory = inventory;
            this.items = items;
            this.progress = progress;
        }

        public ShopTransactionResult Buy(string itemId, int quantity)
        {
            if (inventory == null || items == null || progress == null)
            {
                return ShopTransactionResult.Fail(itemId, quantity, "shop_unconfigured");
            }

            if (string.IsNullOrWhiteSpace(itemId) || quantity <= 0)
            {
                return ShopTransactionResult.Fail(itemId, quantity, "invalid_request");
            }

            ItemDefinition definition = items.GetItem(itemId);
            if (definition == null)
            {
                return ShopTransactionResult.Fail(itemId, quantity, "unknown_item");
            }

            int totalCost = Math.Max(0, definition.Price) * quantity;
            if (progress.Coins < totalCost)
            {
                return ShopTransactionResult.Fail(itemId, quantity, "insufficient_funds");
            }

            if (!inventory.AddItem(itemId, quantity))
            {
                return ShopTransactionResult.Fail(itemId, quantity, "inventory_full");
            }

            if (totalCost > 0)
            {
                progress.TrySpendCoins(totalCost);
            }

            return ShopTransactionResult.Ok(itemId, quantity, -totalCost);
        }

        public ShopTransactionResult Sell(string itemId, int quantity)
        {
            return Sell(itemId, quantity, DefaultBuyRate);
        }

        public ShopTransactionResult Sell(string itemId, int quantity, float buyRate)
        {
            if (inventory == null || items == null || progress == null)
            {
                return ShopTransactionResult.Fail(itemId, quantity, "shop_unconfigured");
            }

            if (string.IsNullOrWhiteSpace(itemId) || quantity <= 0)
            {
                return ShopTransactionResult.Fail(itemId, quantity, "invalid_request");
            }

            ItemDefinition definition = items.GetItem(itemId);
            if (definition == null)
            {
                return ShopTransactionResult.Fail(itemId, quantity, "unknown_item");
            }

            if (inventory.GetItemCount(itemId) < quantity)
            {
                return ShopTransactionResult.Fail(itemId, quantity, "insufficient_items");
            }

            int unitPayout = (int)Math.Floor(Math.Max(0, definition.Price) * Math.Max(0f, buyRate));
            int totalPayout = unitPayout * quantity;
            if (!inventory.RemoveItem(itemId, quantity))
            {
                return ShopTransactionResult.Fail(itemId, quantity, "insufficient_items");
            }

            if (totalPayout > 0)
            {
                progress.AddCoins(totalPayout);
            }

            return ShopTransactionResult.Ok(itemId, quantity, totalPayout);
        }
    }
}
