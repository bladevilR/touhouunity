using System;
using TouhouMigration.Runtime.Weapons;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    // Covers MigrationWeaponCatalog: the weapon gameplay config table (Godot WeaponData.WEAPONS pure-data
    // fields) and its use as the weapon inventory's max-level source.
    public static class WeaponCatalogSmokeTests
    {
        [MenuItem("Touhou Migration/Tests/Run Weapon Catalog Smoke Tests")]
        public static void RunAll()
        {
            TestCatalogLoads();
            TestWeaponConfigFields();
            TestMaxLevelOfFeedsInventory();
            Debug.Log("Weapon catalog smoke tests passed.");
        }

        private static void TestCatalogLoads()
        {
            MigrationWeaponCatalog catalog = new MigrationWeaponCatalog();
            AssertEqual(17, catalog.Count, "The catalog holds the 17 active weapons.");
            AssertEqual(true, catalog.GetWeapon("homing_amulet") != null, "homing_amulet is present.");
            AssertEqual(true, catalog.GetWeapon("not_a_weapon") == null, "An unknown weapon id returns null.");
        }

        private static void TestWeaponConfigFields()
        {
            MigrationWeaponCatalog catalog = new MigrationWeaponCatalog();
            MigrationWeaponDefinition homing = catalog.GetWeapon("homing_amulet");
            AssertEqual(8, homing.MaxLevel, "homing_amulet max level is 8.");
            AssertEqual(15.0, homing.BaseDamage, "homing_amulet base damage is 15.");
            AssertEqual(1.0, homing.CooldownMax, "homing_amulet cooldown is 1.0s.");

            AssertEqual(20, catalog.GetWeapon("mokou_kick_heavy").MaxLevel, "mokou_kick_heavy can reach level 20.");
            AssertEqual(500.0, catalog.GetWeapon("phoenix_rebirth").BaseDamage, "phoenix_rebirth hits for 500.");
            AssertEqual(0, catalog.MaxLevelOf("not_a_weapon"), "An unknown weapon has max level 0.");
        }

        private static void TestMaxLevelOfFeedsInventory()
        {
            MigrationWeaponCatalog catalog = new MigrationWeaponCatalog();
            MigrationWeaponInventory inv = new MigrationWeaponInventory(catalog.MaxLevelOf);

            inv.AddWeapon("knives");
            for (int i = 0; i < 20; i++)
            {
                inv.UpgradeWeapon("knives");
            }

            AssertEqual(8, inv.GetLevel("knives"), "The inventory caps knives at its catalog max level (8).");

            inv.AddWeapon("phantom_weapon"); // not in catalog
            AssertEqual(false, inv.IsOwned("phantom_weapon"), "A weapon absent from the catalog cannot be added.");
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
