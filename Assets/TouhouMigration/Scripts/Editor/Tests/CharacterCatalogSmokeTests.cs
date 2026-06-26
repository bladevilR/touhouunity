using System;
using TouhouMigration.Runtime.Player;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    // Covers MigrationCharacterCatalog: the playable-character base-stat table (Godot CharacterData.CHARACTERS).
    public static class CharacterCatalogSmokeTests
    {
        private const double Tol = 1e-9;

        [MenuItem("Touhou Migration/Tests/Run Character Catalog Smoke Tests")]
        public static void RunAll()
        {
            TestCatalogLoads();
            TestMokouTankStats();
            TestYumaAndKoishi();
            Debug.Log("Character catalog smoke tests passed.");
        }

        private static void TestCatalogLoads()
        {
            MigrationCharacterCatalog cat = new MigrationCharacterCatalog();
            AssertEqual(6, cat.Count, "There are six playable characters.");
            AssertEqual(true, cat.GetCharacter("reimu") != null, "Reimu is present.");
            AssertEqual("homing_amulet", cat.GetCharacter("reimu").DefaultWeapon, "Reimu's default weapon is the homing amulet.");
            AssertEqual(true, cat.GetCharacter("not_a_character") == null, "An unknown character id returns null.");
        }

        private static void TestMokouTankStats()
        {
            MigrationCharacterDefinition mokou = new MigrationCharacterCatalog().GetCharacter("mokou");
            AssertEqual("藤原妹红", mokou.Name, "Mokou's name.");
            AssertTrue(Math.Abs(120.0 - mokou.MaxHp) < Tol, "Mokou is a 120-HP tank.");
            AssertTrue(Math.Abs(3.2 - mokou.Speed) < Tol, "Mokou's speed is 3.2.");
            AssertEqual(1, mokou.Revivals, "Mokou has one revival.");
            AssertEqual("mokou_kick_light", mokou.DefaultWeapon, "Mokou starts with the light kick.");
        }

        private static void TestYumaAndKoishi()
        {
            MigrationCharacterCatalog cat = new MigrationCharacterCatalog();
            MigrationCharacterDefinition yuma = cat.GetCharacter("yuma");
            AssertTrue(Math.Abs(150.0 - yuma.MaxHp) < Tol, "Yuma is the 150-HP heavy.");
            AssertTrue(Math.Abs(3.0 - yuma.Armor) < Tol, "Yuma has 3 armor.");

            MigrationCharacterDefinition koishi = cat.GetCharacter("koishi");
            AssertTrue(Math.Abs(80.0 - koishi.MaxHp) < Tol, "Koishi is the 80-HP glass cannon.");
            AssertTrue(Math.Abs(1.2 - koishi.Area) < Tol, "Koishi has a 1.2 area multiplier.");
            AssertTrue(Math.Abs(1.5 - koishi.Luck) < Tol, "Koishi has 1.5 luck.");
        }

        private static void AssertTrue(bool condition, string message)
        {
            if (!condition)
            {
                throw new Exception(message);
            }
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
