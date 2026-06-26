using System;
using System.Collections.Generic;

namespace TouhouMigration.Runtime.Weapons
{
    // Weapon ownership + leveling (Godot WeaponSystem add_weapon / upgrade_weapon): owned weapons each hold
    // a level, capped at the weapon's max level. The per-weapon max level is injected (maxLevelOf returns
    // <= 0 for an unknown weapon id, which can't be added). UnityEngine-free; composes with
    // MigrationWeaponFusion (its GetLevel feeds CanFuse / AvailableFusions). The real-time firing /
    // projectile half of WeaponSystem is separate scene work.
    public sealed class MigrationWeaponInventory
    {
        private readonly Func<string, int> maxLevelOf;
        private readonly Dictionary<string, int> levels = new Dictionary<string, int>();

        public MigrationWeaponInventory(Func<string, int> maxLevelOf)
        {
            this.maxLevelOf = maxLevelOf ?? (_ => 0);
        }

        public bool IsOwned(string weaponId)
        {
            return weaponId != null && levels.ContainsKey(weaponId);
        }

        public int GetLevel(string weaponId)
        {
            return weaponId != null && levels.TryGetValue(weaponId, out int level) ? level : 0;
        }

        public IReadOnlyList<string> GetOwnedWeaponIds()
        {
            return new List<string>(levels.Keys);
        }

        // Add a weapon (Godot add_weapon): an already-owned weapon is upgraded instead; an unknown weapon
        // id (max level <= 0) is rejected; otherwise it enters at level 1.
        public void AddWeapon(string weaponId)
        {
            if (string.IsNullOrEmpty(weaponId))
            {
                return;
            }

            if (levels.ContainsKey(weaponId))
            {
                UpgradeWeapon(weaponId);
                return;
            }

            if (maxLevelOf(weaponId) <= 0)
            {
                return;
            }

            levels[weaponId] = 1;
        }

        // Upgrade an owned weapon by one level (Godot upgrade_weapon), capped at its max level. An unowned
        // weapon is a no-op.
        public void UpgradeWeapon(string weaponId)
        {
            if (weaponId == null || !levels.TryGetValue(weaponId, out int level))
            {
                return;
            }

            if (level >= maxLevelOf(weaponId))
            {
                return;
            }

            levels[weaponId] = level + 1;
        }
    }
}
