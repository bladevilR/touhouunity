using System;
using System.Collections.Generic;
using TouhouMigration.Runtime.Weapons;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    // Covers MigrationWeaponInventory: weapon ownership + leveling (Godot WeaponSystem add_weapon /
    // upgrade_weapon), capped at each weapon's max level, and its composition with the fusion system.
    public static class WeaponInventorySmokeTests
    {
        [MenuItem("Touhou Migration/Tests/Run Weapon Inventory Smoke Tests")]
        public static void RunAll()
        {
            TestAddAndUpgrade();
            TestAddExistingUpgrades();
            TestUpgradeCapsAtMaxLevel();
            TestInvalidAndUnownedAreSafe();
            TestComposesWithFusion();
            Debug.Log("Weapon inventory smoke tests passed.");
        }

        private static Func<string, int> MaxLevels(Dictionary<string, int> max)
        {
            return id => id != null && max.TryGetValue(id, out int level) ? level : 0;
        }

        private static MigrationWeaponInventory NewInventory()
        {
            return new MigrationWeaponInventory(MaxLevels(new Dictionary<string, int>
            {
                ["homing_amulet"] = 8,
                ["yin_yang_orb"] = 8,
                ["star_dust"] = 8,
            }));
        }

        private static void TestAddAndUpgrade()
        {
            MigrationWeaponInventory inv = NewInventory();
            inv.AddWeapon("homing_amulet");
            AssertEqual(true, inv.IsOwned("homing_amulet"), "A new weapon is owned.");
            AssertEqual(1, inv.GetLevel("homing_amulet"), "A new weapon starts at level 1.");

            inv.UpgradeWeapon("homing_amulet");
            AssertEqual(2, inv.GetLevel("homing_amulet"), "Upgrade raises the level.");
        }

        private static void TestAddExistingUpgrades()
        {
            MigrationWeaponInventory inv = NewInventory();
            inv.AddWeapon("homing_amulet"); // level 1
            inv.AddWeapon("homing_amulet"); // already owned -> upgrade to 2 (Godot add_weapon)
            AssertEqual(2, inv.GetLevel("homing_amulet"), "Adding an owned weapon upgrades it.");
        }

        private static void TestUpgradeCapsAtMaxLevel()
        {
            MigrationWeaponInventory inv = new MigrationWeaponInventory(
                MaxLevels(new Dictionary<string, int> { ["star_dust"] = 3 }));
            inv.AddWeapon("star_dust");
            for (int i = 0; i < 10; i++)
            {
                inv.UpgradeWeapon("star_dust");
            }

            AssertEqual(3, inv.GetLevel("star_dust"), "Upgrades cap at the weapon's max level.");
        }

        private static void TestInvalidAndUnownedAreSafe()
        {
            MigrationWeaponInventory inv = NewInventory();
            inv.AddWeapon("not_a_weapon"); // max level 0 -> invalid, not added
            AssertEqual(false, inv.IsOwned("not_a_weapon"), "An unknown weapon id cannot be added.");

            inv.UpgradeWeapon("homing_amulet"); // not owned yet -> no-op
            AssertEqual(0, inv.GetLevel("homing_amulet"), "Upgrading an unowned weapon does nothing.");
        }

        private static void TestComposesWithFusion()
        {
            MigrationWeaponInventory inv = NewInventory();
            inv.AddWeapon("homing_amulet");
            inv.AddWeapon("yin_yang_orb");
            for (int i = 0; i < 2; i++)
            {
                inv.UpgradeWeapon("homing_amulet"); // -> level 3
                inv.UpgradeWeapon("yin_yang_orb");  // -> level 3
            }

            MigrationWeaponFusion fusion = new MigrationWeaponFusion();
            WeaponFusionCheck check = fusion.CanFuse("homing_amulet", "yin_yang_orb", inv.GetLevel);
            AssertEqual(true, check.CanFuse, "Two inventory weapons at MAX fuse via the fusion system.");
            AssertEqual("dream_seal", check.Recipe.ResultWeaponId, "The composed fusion yields dream_seal.");

            IReadOnlyList<MigrationWeaponRecipe> available = fusion.AvailableFusions(inv.GetOwnedWeaponIds(), inv.GetLevel);
            AssertEqual(1, available.Count, "The inventory exposes the available fusion.");
        }

        private static void AssertEqual<T>(T expected, T actual, string message)
        {
            if (!Equals(expected, actual))
            {
                throw new Exception($"{message} Expected: {expected}. Actual: {actual}.");
            }
        }
    }
}
