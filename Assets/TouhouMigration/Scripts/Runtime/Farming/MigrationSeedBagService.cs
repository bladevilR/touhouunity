using System;
using System.Collections.Generic;
using TouhouMigration.Runtime.Inventory;

namespace TouhouMigration.Runtime.Farming
{
    // The outcome of opening a seed bag.
    public sealed class SeedBagOpenResult
    {
        private SeedBagOpenResult(bool success, string reason, IReadOnlyList<string> seeds)
        {
            Success = success;
            Reason = reason;
            Seeds = seeds ?? Array.Empty<string>();
        }

        public bool Success { get; }
        public string Reason { get; }
        public IReadOnlyList<string> Seeds { get; }

        public static SeedBagOpenResult Ok(IReadOnlyList<string> seeds) => new SeedBagOpenResult(true, string.Empty, seeds);
        public static SeedBagOpenResult Fail(string reason) => new SeedBagOpenResult(false, reason, null);
    }

    // Composes the seed-bag gacha with the inventory into a playable "open the bag" flow: consume the
    // shop-bought bag item, roll its seeds, and grant them. UnityEngine-free + unit-testable.
    public sealed class MigrationSeedBagService
    {
        private readonly MigrationSeedBag bag;
        private readonly InventoryService inventory;

        public MigrationSeedBagService(MigrationSeedBag bag, InventoryService inventory)
        {
            this.bag = bag;
            this.inventory = inventory;
        }

        public SeedBagOpenResult OpenBag(string bagType, MigrationCropSeason season)
        {
            string bagItemId = BagItemId(bagType);
            if (bagItemId == null)
            {
                return SeedBagOpenResult.Fail("unknown_bag");
            }

            if (inventory == null || inventory.GetItemCount(bagItemId) < 1)
            {
                return SeedBagOpenResult.Fail("no_bag");
            }

            inventory.RemoveItem(bagItemId, 1);

            IReadOnlyList<string> seeds = bag != null
                ? bag.Open(bagType, season)
                : Array.Empty<string>();
            foreach (string seedId in seeds)
            {
                inventory.AddItem(seedId, 1);
            }

            return SeedBagOpenResult.Ok(seeds);
        }

        // Map the bag type to its shop item id (Godot seed_bag_<type>).
        private static string BagItemId(string bagType)
        {
            switch (bagType)
            {
                case "bamboo": return "seed_bag_bamboo";
                case "silver": return "seed_bag_silver";
                case "gold": return "seed_bag_gold";
                default: return null;
            }
        }
    }
}
