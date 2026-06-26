using System;
using TouhouMigration.Runtime.CardBuild;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    // Covers MigrationCardBossHp: the CardBuild boss HP pool (Godot CardBuildMvpRunController CIRNO_MAX_HP /
    // _damage_boss / is_boss_defeated): clamped damage and the defeat check.
    public static class CardBossHpSmokeTests
    {
        [MenuItem("Touhou Migration/Tests/Run Card Boss HP Smoke Tests")]
        public static void RunAll()
        {
            TestStartsAtFullCirnoHp();
            TestDamageReducesAndReportsDealt();
            TestDamageClampsAtZeroAndDefeats();
            TestNonPositiveDamageIsNoOp();
            TestCustomMaxHp();
            Debug.Log("Card boss HP smoke tests passed.");
        }

        private static void TestStartsAtFullCirnoHp()
        {
            MigrationCardBossHp boss = new MigrationCardBossHp();
            AssertEqual(540, boss.MaxHp, "The default boss max HP is Cirno's 540.");
            AssertEqual(540, boss.CurrentHp, "The boss starts at full HP.");
            AssertEqual(false, boss.IsDefeated, "A full-HP boss is not defeated.");
        }

        private static void TestDamageReducesAndReportsDealt()
        {
            MigrationCardBossHp boss = new MigrationCardBossHp();
            AssertEqual(40, boss.Damage(40), "Damage returns the HP actually removed.");
            AssertEqual(500, boss.CurrentHp, "Damage reduces current HP.");
            boss.Damage(60);
            AssertEqual(440, boss.CurrentHp, "Damage accumulates.");
            AssertEqual(false, boss.IsDefeated, "The boss survives partial damage.");
        }

        private static void TestDamageClampsAtZeroAndDefeats()
        {
            MigrationCardBossHp boss = new MigrationCardBossHp(100);
            AssertEqual(100, boss.Damage(250), "Over-kill damage only removes the remaining HP.");
            AssertEqual(0, boss.CurrentHp, "HP clamps at 0, never negative.");
            AssertEqual(true, boss.IsDefeated, "A boss at 0 HP is defeated.");
            AssertEqual(0, boss.Damage(50), "Damaging a defeated boss removes nothing.");
        }

        private static void TestNonPositiveDamageIsNoOp()
        {
            MigrationCardBossHp boss = new MigrationCardBossHp();
            AssertEqual(0, boss.Damage(0), "Zero damage removes nothing.");
            AssertEqual(0, boss.Damage(-30), "Negative damage does not heal.");
            AssertEqual(540, boss.CurrentHp, "Current HP is unchanged by non-positive damage.");
        }

        private static void TestCustomMaxHp()
        {
            MigrationCardBossHp boss = new MigrationCardBossHp(320);
            AssertEqual(320, boss.MaxHp, "A custom max HP is honored.");
            AssertEqual(320, boss.CurrentHp, "A custom-max boss starts full.");
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
