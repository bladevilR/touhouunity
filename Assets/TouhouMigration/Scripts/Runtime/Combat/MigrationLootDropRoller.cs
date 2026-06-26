using System;
using System.Collections.Generic;

namespace TouhouMigration.Runtime.Combat
{
    // One rolled loot drop (item id + count).
    public readonly struct LootDrop
    {
        public LootDrop(string itemId, int count)
        {
            ItemId = itemId;
            Count = count;
        }

        public string ItemId { get; }
        public int Count { get; }
    }

    // The rank-evaluation bonus-drop rolls (Godot LootDropManager grant_rank_bonus_drops +
    // RANK_DROP_MULTIPLIERS): S/A grant element crystals, deep floors (3+) roll high-grade fertilizer + a
    // rare seed scaled by the rank multiplier, and S adds dungeon compost. RNG is injected (randFloat for
    // the chance checks, randIndex for the picks) so the rolls are deterministic + unit-testable; granting
    // the items to the inventory is the caller's job.
    public sealed class MigrationLootDropRoller
    {
        private static readonly string[] Crystals =
        {
            "element_crystal_fire", "element_crystal_ice", "element_crystal_earth", "element_crystal_wind",
        };

        private static readonly string[] RareSeeds = { "seed_spirit_bloom", "seed_shadow_root" };

        public double RankMultiplier(string rank)
        {
            switch (rank)
            {
                case "S": return 2.5;
                case "A": return 1.8;
                case "B": return 1.0;
                case "C": return 0.6;
                default: return 1.0;
            }
        }

        public IReadOnlyList<LootDrop> RollRankBonusDrops(
            string rank, int floorLevel, Func<double> randFloat, Func<int, int> randIndex)
        {
            Func<double> rf = randFloat ?? (() => 0.0);
            Func<int, int> ri = randIndex ?? (_ => 0);
            double multiplier = RankMultiplier(rank);
            List<LootDrop> drops = new List<LootDrop>();

            // S/A grant element crystals (S = 2, A = 1).
            if (rank == "S" || rank == "A")
            {
                int crystalCount = rank == "A" ? 1 : 2;
                for (int i = 0; i < crystalCount; i++)
                {
                    drops.Add(new LootDrop(Pick(Crystals, ri), 1));
                }
            }

            // Deep dungeon floors roll high-grade fertilizer + a rare seed, scaled by the rank multiplier.
            if (floorLevel >= 3)
            {
                if (rf() < 0.15 * multiplier)
                {
                    drops.Add(new LootDrop("spirit_soil", 1));
                }

                if (rf() < 0.08 * multiplier)
                {
                    drops.Add(new LootDrop(Pick(RareSeeds, ri), 1));
                }
            }

            // S grade adds dungeon compost.
            if (rank == "S")
            {
                drops.Add(new LootDrop("dungeon_compost", 2));
            }

            return drops;
        }

        private static string Pick(string[] items, Func<int, int> randIndex)
        {
            int index = randIndex(items.Length);
            if (index < 0 || index >= items.Length)
            {
                index = 0;
            }

            return items[index];
        }
    }
}
