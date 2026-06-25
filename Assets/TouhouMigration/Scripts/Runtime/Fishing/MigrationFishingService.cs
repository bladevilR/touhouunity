using System;
using System.Collections.Generic;
using TouhouMigration.Runtime.Inventory;

namespace TouhouMigration.Runtime.Fishing
{
    // Weighted fish-catch service (Godot FishingManager.roll_fish): each registered fish contributes a
    // rarity-based weight, and Catch performs a weighted random selection, granting the caught fish's
    // item to the inventory. Free of UnityEngine; catch randomness is injected. Fishing-level boost,
    // spot/season/hour context, and size rolls are later slices.
    public sealed class MigrationFishingService
    {
        // Godot FishDatabase.RARITY_WEIGHTS.
        public const int CommonWeight = 50;
        public const int UncommonWeight = 30;
        public const int RareWeight = 15;
        public const int LegendaryWeight = 5;

        private readonly InventoryService inventory;
        private readonly List<MigrationFishDefinition> fish = new List<MigrationFishDefinition>();

        public MigrationFishingService(InventoryService inventory)
        {
            this.inventory = inventory;
        }

        public static int RarityWeight(MigrationFishRarity rarity)
        {
            switch (rarity)
            {
                case MigrationFishRarity.Common:
                    return CommonWeight;
                case MigrationFishRarity.Uncommon:
                    return UncommonWeight;
                case MigrationFishRarity.Rare:
                    return RareWeight;
                case MigrationFishRarity.Legendary:
                    return LegendaryWeight;
                default:
                    return CommonWeight;
            }
        }

        public void RegisterFish(MigrationFishDefinition definition)
        {
            if (definition != null && !string.IsNullOrWhiteSpace(definition.FishId))
            {
                fish.Add(definition);
            }
        }

        public int TotalWeight()
        {
            int total = 0;
            foreach (MigrationFishDefinition definition in fish)
            {
                total += RarityWeight(definition.Rarity);
            }

            return total;
        }

        // nextInt(maxExclusive) -> a value in [0, maxExclusive), e.g. System.Random.Next(max).
        public MigrationFishCatchResult Catch(Func<int, int> nextInt)
        {
            int total = TotalWeight();
            if (fish.Count == 0 || total <= 0 || nextInt == null)
            {
                return MigrationFishCatchResult.Fail("no_fish");
            }

            int roll = ((nextInt(total) % total) + total) % total;
            int cumulative = 0;
            foreach (MigrationFishDefinition definition in fish)
            {
                cumulative += RarityWeight(definition.Rarity);
                if (roll < cumulative)
                {
                    if (!string.IsNullOrWhiteSpace(definition.ItemId))
                    {
                        inventory?.AddItem(definition.ItemId, 1);
                    }

                    return MigrationFishCatchResult.Ok(definition.FishId, definition.ItemId, definition.Rarity);
                }
            }

            MigrationFishDefinition last = fish[fish.Count - 1];
            return MigrationFishCatchResult.Ok(last.FishId, last.ItemId, last.Rarity);
        }
    }
}
