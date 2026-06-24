using System;
using TouhouMigration.Runtime.Cooking;
using TouhouMigration.Runtime.Player;

namespace TouhouMigration.Runtime.Inventory
{
    public sealed class ItemUseService
    {
        private readonly InventoryService inventoryService;
        private readonly ItemDatabase itemDatabase;
        private readonly CookingBuffService cookingBuffService;
        private readonly MigrationPlayerHealthRuntime healthRuntime;
        private int currentHp = 100;
        private int maxHp = 100;

        public ItemUseService(
            InventoryService inventoryService,
            ItemDatabase itemDatabase,
            CookingBuffService cookingBuffService)
            : this(inventoryService, itemDatabase, cookingBuffService, null)
        {
        }

        public ItemUseService(
            InventoryService inventoryService,
            ItemDatabase itemDatabase,
            CookingBuffService cookingBuffService,
            MigrationPlayerHealthRuntime healthRuntime)
        {
            this.inventoryService = inventoryService;
            this.itemDatabase = itemDatabase;
            this.cookingBuffService = cookingBuffService;
            this.healthRuntime = healthRuntime;
        }

        public int CurrentHp => healthRuntime != null ? (int)Math.Round(healthRuntime.CurrentHp) : currentHp;
        public int MaxHp => healthRuntime != null ? (int)Math.Round(healthRuntime.MaxHp) : maxHp;

        public void SetHealth(int currentHp, int maxHp)
        {
            if (healthRuntime != null)
            {
                healthRuntime.SetHealth(currentHp, maxHp);
                return;
            }

            this.maxHp = Math.Max(1, maxHp);
            this.currentHp = Math.Max(0, Math.Min(this.maxHp, currentHp));
            cookingBuffService?.SetPlayerHpRatio(this.maxHp > 0 ? this.currentHp / (float)this.maxHp : 1f);
        }

        public ItemUseResult UseItem(string itemId)
        {
            return UseItem(itemId, 0);
        }

        public ItemUseResult UseItem(string itemId, int quality)
        {
            string normalizedItemId = NormalizeId(itemId);
            int normalizedQuality = Math.Max(0, quality);
            if (inventoryService == null || itemDatabase == null || string.IsNullOrEmpty(normalizedItemId))
            {
                return Failed(normalizedItemId, normalizedQuality, "service_not_ready");
            }

            ItemDefinition item = itemDatabase.GetItem(normalizedItemId);
            if (item == null)
            {
                return Failed(normalizedItemId, normalizedQuality, "unknown_item");
            }

            string itemType = NormalizeId(item.ItemType);
            bool qualitySpecific = itemType == "dish" || itemType == "drink";
            int count = qualitySpecific
                ? inventoryService.GetItemCount(normalizedItemId, normalizedQuality)
                : inventoryService.GetItemCount(normalizedItemId);
            if (count <= 0)
            {
                return Failed(normalizedItemId, normalizedQuality, "not_in_inventory", itemType);
            }

            return itemType switch
            {
                "consumable" => UseConsumable(item, normalizedQuality),
                "dish" => UseDish(item, normalizedQuality),
                "drink" => UseDrink(item, normalizedQuality),
                "currency" => Failed(normalizedItemId, normalizedQuality, "currency_not_usable", itemType),
                "equipment" => Failed(normalizedItemId, normalizedQuality, "equipment_not_usable", itemType),
                _ => Failed(normalizedItemId, normalizedQuality, "unsupported_item_type", itemType)
            };
        }

        private ItemUseResult UseConsumable(ItemDefinition item, int quality)
        {
            int healAmount = ApplyHealing(item.GetEffectInt("heal_hp"));
            string statusEffect = item.GetEffectString("buff");
            string combatItem = item.GetEffectString("combat_item");
            bool hasUsableEffect = healAmount > 0 ||
                !string.IsNullOrWhiteSpace(statusEffect) ||
                !string.IsNullOrWhiteSpace(combatItem) ||
                item.GetEffectInt("restore_mp") > 0;

            if (!hasUsableEffect)
            {
                return Failed(item.Id, quality, "no_usable_effect", item.ItemType);
            }

            if (!inventoryService.RemoveItem(item.Id, 1))
            {
                return Failed(item.Id, quality, "remove_failed", item.ItemType);
            }

            return new ItemUseResult
            {
                Success = true,
                ItemId = item.Id,
                ItemType = item.ItemType,
                Quality = quality,
                HealAmount = healAmount,
                AppliedStatusEffect = !string.IsNullOrWhiteSpace(statusEffect) ? statusEffect : combatItem
            };
        }

        private ItemUseResult UseDish(ItemDefinition item, int quality)
        {
            int healAmount = ApplyHealing(item.GetEffectInt("heal_hp"));
            bool appliedBuff = cookingBuffService != null && cookingBuffService.ConsumeDish(item.Id, quality);
            if (!appliedBuff && healAmount <= 0)
            {
                return Failed(item.Id, quality, "no_usable_effect", item.ItemType);
            }

            if (!inventoryService.RemoveItem(item.Id, 1, quality))
            {
                return Failed(item.Id, quality, "remove_failed", item.ItemType);
            }

            return new ItemUseResult
            {
                Success = true,
                ItemId = item.Id,
                ItemType = item.ItemType,
                Quality = quality,
                HealAmount = healAmount,
                AppliedCookingBuff = appliedBuff
            };
        }

        private ItemUseResult UseDrink(ItemDefinition item, int quality)
        {
            int healAmount = ApplyHealing(item.GetEffectInt("heal_hp"));
            bool appliedBuff = cookingBuffService != null && cookingBuffService.ConsumeDrink(item.Id);
            if (!appliedBuff && healAmount <= 0)
            {
                return Failed(item.Id, quality, "no_usable_effect", item.ItemType);
            }

            if (!inventoryService.RemoveItem(item.Id, 1, quality))
            {
                return Failed(item.Id, quality, "remove_failed", item.ItemType);
            }

            return new ItemUseResult
            {
                Success = true,
                ItemId = item.Id,
                ItemType = item.ItemType,
                Quality = quality,
                HealAmount = healAmount,
                AppliedCookingBuff = appliedBuff
            };
        }

        private int ApplyHealing(int healAmount)
        {
            int normalizedHeal = Math.Max(0, healAmount);
            if (normalizedHeal <= 0)
            {
                return 0;
            }

            if (healthRuntime != null)
            {
                return (int)Math.Round(healthRuntime.Heal(normalizedHeal));
            }

            int before = currentHp;
            currentHp = Math.Min(maxHp, currentHp + normalizedHeal);
            cookingBuffService?.SetPlayerHpRatio(maxHp > 0 ? currentHp / (float)maxHp : 1f);
            return currentHp - before;
        }

        private static ItemUseResult Failed(string itemId, int quality, string reason, string itemType = "")
        {
            return new ItemUseResult
            {
                Success = false,
                ItemId = itemId ?? string.Empty,
                ItemType = itemType ?? string.Empty,
                FailureReason = reason,
                Quality = quality
            };
        }

        private static string NormalizeId(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToLowerInvariant();
        }
    }
}
