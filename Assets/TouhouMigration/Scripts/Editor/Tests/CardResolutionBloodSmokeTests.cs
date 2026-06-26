using System;
using System.Collections.Generic;
using TouhouMigration.Runtime.CardBuild;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    // Covers the blood-archetype per-card resolution (Godot _apply_cirno_card_resolution blood branch).
    public static class CardResolutionBloodSmokeTests
    {
        [MenuItem("Touhou Migration/Tests/Run Card Resolution Blood Smoke Tests")]
        public static void RunAll()
        {
            TestScarletShot();
            TestFateSpear();
            TestGungnirGatedThenBurst();
            TestNightDashAndBreakDestiny();
            Debug.Log("Card resolution blood smoke tests passed.");
        }

        private static MigrationCardBuildRunController NewRun()
        {
            return new MigrationCardBuildRunController(
                new List<string> { "a", "b" }, bossMaxHp: 540, bossClauseId: "cirno_domain");
        }

        private static void TestScarletShot()
        {
            MigrationCardBuildRunController run = NewRun();
            run.State.AddResource("fate", 2);
            run.ResolveCardEffect("blood_starter_scarlet_shot");
            AssertEqual(1, run.State.GetStatus("enemy", "fate_lock"), "Scarlet shot applies fate_lock.");
            AssertEqual(540 - (14 + 2 * 3), run.BossHp, "Scarlet shot deals 14 + 3/fate.");
        }

        private static void TestFateSpear()
        {
            // fate_lock 1, fate 2: damage = 18 + 34 + 2*8 = 68; fate_lock -> 0; fate -> 1 (spends 1).
            MigrationCardBuildRunController run = NewRun();
            run.State.ApplyStatus("enemy", "fate_lock", 1);
            run.State.AddResource("fate", 2);
            run.ResolveCardEffect("blood_payoff_fate_spear");
            AssertEqual(540 - 68, run.BossHp, "Fate spear deals 18 + 34 + fate*8 with fate_lock.");
            AssertEqual(0, run.State.GetStatus("enemy", "fate_lock"), "Fate spear consumes one fate_lock.");
            AssertEqual(1, run.State.GetResource("fate"), "Fate spear spends one fate.");

            // No fate_lock, no fate: just the base 18.
            MigrationCardBuildRunController bare = NewRun();
            bare.ResolveCardEffect("blood_payoff_fate_spear");
            AssertEqual(540 - 18, bare.BossHp, "Fate spear's base damage is 18.");
        }

        private static void TestGungnirGatedThenBurst()
        {
            MigrationCardBuildRunController gated = NewRun();
            gated.ResolveCardEffect("blood_terminal_spear_the_gungnir"); // no fate / fate_lock -> waits
            AssertEqual(540, gated.BossHp, "Gungnir waits without fate or fate_lock.");

            // fate 2, fate_lock 1: damage = 70 + 2*22 + 1*18 = 132; fate spent, fate_lock cleared.
            MigrationCardBuildRunController burst = NewRun();
            burst.State.AddResource("fate", 2);
            burst.State.ApplyStatus("enemy", "fate_lock", 1);
            burst.ResolveCardEffect("blood_terminal_spear_the_gungnir");
            AssertEqual(540 - 132, burst.BossHp, "Gungnir burst formula.");
            AssertEqual(0, burst.State.GetResource("fate"), "Gungnir spends all fate.");
            AssertEqual(0, burst.State.GetStatus("enemy", "fate_lock"), "Gungnir clears fate_lock.");
        }

        private static void TestNightDashAndBreakDestiny()
        {
            MigrationCardBuildRunController dash = NewRun();
            dash.ResolveCardEffect("blood_movement_night_dash");
            AssertEqual(1, dash.State.GetStatus("enemy", "fate_lock"), "Night dash applies fate_lock.");
            AssertEqual(true, dash.IsTerrainSuppressed, "Night dash suppresses terrain.");

            MigrationCardBuildRunController destiny = NewRun(); // terrain pressure 2
            destiny.ResolveCardEffect("blood_boss_break_destiny");
            AssertEqual(1, destiny.TerrainPressure, "Break destiny relieves terrain pressure.");
            AssertEqual(true, destiny.IsVulnerabilityOpen, "Break destiny opens a vulnerability window.");
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
