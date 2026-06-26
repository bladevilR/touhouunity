using System;
using System.Collections.Generic;

namespace TouhouMigration.Runtime.Weapons
{
    // A weapon fusion recipe (Godot WeaponData.WeaponRecipe): two MAX-level weapons fuse into a spell-card
    // weapon.
    public sealed class MigrationWeaponRecipe
    {
        public MigrationWeaponRecipe(string id, string name, string requiresA, string requiresB, string resultWeaponId)
        {
            Id = id ?? string.Empty;
            Name = name ?? string.Empty;
            RequiresA = requiresA ?? string.Empty;
            RequiresB = requiresB ?? string.Empty;
            ResultWeaponId = resultWeaponId ?? string.Empty;
        }

        public string Id { get; }
        public string Name { get; }
        public string RequiresA { get; }
        public string RequiresB { get; }
        public string ResultWeaponId { get; }
    }

    // The result of a fusion check (Godot FusionSystem.can_fuse).
    public readonly struct WeaponFusionCheck
    {
        public WeaponFusionCheck(bool canFuse, MigrationWeaponRecipe recipe, string reason)
        {
            CanFuse = canFuse;
            Recipe = recipe;
            Reason = reason;
        }

        public bool CanFuse { get; }
        public MigrationWeaponRecipe Recipe { get; }
        public string Reason { get; }
    }

    // Spell-card weapon fusion (Godot FusionSystem + WeaponData recipes): two owned MAX-level weapons that
    // match a recipe fuse into a spell-card weapon. UnityEngine-free; the owning weapon level is injected
    // (a future weapon system supplies it). Recipe data mirrors WeaponData.WEAPON_RECIPES.
    public sealed class MigrationWeaponFusion
    {
        public const int MaxLevel = 3;

        private static readonly MigrationWeaponRecipe[] RecipeTable =
        {
            new MigrationWeaponRecipe("dream_seal_fusion", "梦想封印", "homing_amulet", "yin_yang_orb", "dream_seal"),
            new MigrationWeaponRecipe("master_spark_fusion", "恋符·Master Spark", "star_dust", "laser", "master_spark"),
            new MigrationWeaponRecipe("sakuyas_world_fusion", "The World - 咲夜之世界", "knives", "time_stop", "sakuyas_world"),
        };

        public IReadOnlyList<MigrationWeaponRecipe> Recipes => RecipeTable;

        // Order-independent recipe lookup (Godot can_fuse_weapons).
        public MigrationWeaponRecipe FindRecipe(string weaponId1, string weaponId2)
        {
            foreach (MigrationWeaponRecipe recipe in RecipeTable)
            {
                if ((recipe.RequiresA == weaponId1 && recipe.RequiresB == weaponId2)
                    || (recipe.RequiresA == weaponId2 && recipe.RequiresB == weaponId1))
                {
                    return recipe;
                }
            }

            return null;
        }

        // Whether two weapons can fuse (Godot FusionSystem.can_fuse): both must be owned (level >= 1) and at
        // MAX level (3), and match a recipe. weaponLevel returns 0 for an unowned weapon.
        public WeaponFusionCheck CanFuse(string weaponId1, string weaponId2, Func<string, int> weaponLevel)
        {
            if (weaponLevel == null)
            {
                return new WeaponFusionCheck(false, null, "武器系统未就绪");
            }

            if (weaponLevel(weaponId1) <= 0)
            {
                return new WeaponFusionCheck(false, null, "武器1未装备");
            }

            if (weaponLevel(weaponId2) <= 0)
            {
                return new WeaponFusionCheck(false, null, "武器2未装备");
            }

            if (weaponLevel(weaponId1) < MaxLevel)
            {
                return new WeaponFusionCheck(false, null, weaponId1 + " 未达到MAX等级");
            }

            if (weaponLevel(weaponId2) < MaxLevel)
            {
                return new WeaponFusionCheck(false, null, weaponId2 + " 未达到MAX等级");
            }

            MigrationWeaponRecipe recipe = FindRecipe(weaponId1, weaponId2);
            if (recipe == null)
            {
                return new WeaponFusionCheck(false, null, "没有匹配的融合配方");
            }

            return new WeaponFusionCheck(true, recipe, string.Empty);
        }

        // Recipes whose both ingredients are owned at MAX level (Godot _check_available_fusions).
        public IReadOnlyList<MigrationWeaponRecipe> AvailableFusions(
            IEnumerable<string> ownedWeaponIds, Func<string, int> weaponLevel)
        {
            HashSet<string> maxLevelWeapons = new HashSet<string>();
            if (ownedWeaponIds != null && weaponLevel != null)
            {
                foreach (string weaponId in ownedWeaponIds)
                {
                    if (weaponId != null && weaponLevel(weaponId) >= MaxLevel)
                    {
                        maxLevelWeapons.Add(weaponId);
                    }
                }
            }

            List<MigrationWeaponRecipe> available = new List<MigrationWeaponRecipe>();
            foreach (MigrationWeaponRecipe recipe in RecipeTable)
            {
                if (maxLevelWeapons.Contains(recipe.RequiresA) && maxLevelWeapons.Contains(recipe.RequiresB))
                {
                    available.Add(recipe);
                }
            }

            return available;
        }
    }
}
