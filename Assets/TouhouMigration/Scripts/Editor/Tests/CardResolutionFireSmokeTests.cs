using System;
using System.Collections.Generic;
using TouhouMigration.Runtime.CardBuild;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    // Covers the fire-archetype per-card resolution (Godot _apply_cirno_card_resolution fire branch) on
    // MigrationCardBuildRunController.ResolveCardEffect.
    public static class CardResolutionFireSmokeTests
    {
        [MenuItem("Touhou Migration/Tests/Run Card Resolution Fire Smoke Tests")]
        public static void RunAll()
        {
            TestEmberShotDamagesOnlyWhenVulnerable();
            TestAshCollectorGainsEmberByBurn();
            TestDetonationPalmConsumesBurnAndScales();
            TestSharedKindlingAndPhoenixGuard();
            TestHouraiPhoenixGatedThenBurst();
            Debug.Log("Card resolution fire smoke tests passed.");
        }

        private static MigrationCardBuildRunController NewRun()
        {
            return new MigrationCardBuildRunController(
                new List<string> { "a", "b", "c", "d" }, bossMaxHp: 540, bossClauseId: "cirno_domain");
        }

        private static void TestEmberShotDamagesOnlyWhenVulnerable()
        {
            MigrationCardBuildRunController closed = NewRun();
            closed.ResolveCardEffect("fire_starter_ember_shot");
            AssertEqual(540, closed.BossHp, "Ember shot does nothing while the boss is guarded.");

            MigrationCardBuildRunController open = NewRun();
            open.OpenVulnerability(2.0);
            open.RewrittenRuleCount = 2;
            open.ResolveCardEffect("fire_starter_ember_shot");
            AssertEqual(540 - (18 + 2 * 6), open.BossHp, "Ember shot deals 18 + 6/rewritten while vulnerable.");
        }

        private static void TestAshCollectorGainsEmberByBurn()
        {
            MigrationCardBuildRunController run = NewRun();
            run.State.ApplyStatus("enemy", "burn", 3);
            run.ResolveCardEffect("fire_resource_ash_collector");
            AssertEqual(3, run.State.GetResource("ember"), "Ash collector grants ember equal to burn (>=1).");

            MigrationCardBuildRunController noBurn = NewRun();
            noBurn.ResolveCardEffect("fire_resource_ash_collector");
            AssertEqual(1, noBurn.State.GetResource("ember"), "Ash collector grants at least 1 ember.");
        }

        private static void TestDetonationPalmConsumesBurnAndScales()
        {
            // burn 3, ember 2, not vulnerable: damage = 22 + 3*18 + 2*4 = 84.
            MigrationCardBuildRunController run = NewRun();
            run.State.ApplyStatus("enemy", "burn", 3);
            run.State.AddResource("ember", 2);
            run.ResolveCardEffect("fire_payoff_detonation_palm");
            AssertEqual(540 - 84, run.BossHp, "Detonation deals 22 + burn*18 + ember*4.");
            AssertEqual(0, run.State.GetStatus("enemy", "burn"), "Detonation consumes all burn.");

            // Same, vulnerable: 84 * 1.35 = 113.4 -> round 113.
            MigrationCardBuildRunController vuln = NewRun();
            vuln.State.ApplyStatus("enemy", "burn", 3);
            vuln.State.AddResource("ember", 2);
            vuln.OpenVulnerability(2.0);
            vuln.ResolveCardEffect("fire_payoff_detonation_palm");
            AssertEqual(540 - 113, vuln.BossHp, "Detonation x1.35 while vulnerable.");

            // No burn: waits, no damage.
            MigrationCardBuildRunController idle = NewRun();
            idle.ResolveCardEffect("fire_payoff_detonation_palm");
            AssertEqual(540, idle.BossHp, "Detonation with no burn deals nothing.");
        }

        private static void TestSharedKindlingAndPhoenixGuard()
        {
            MigrationCardBuildRunController run = NewRun();
            run.ResolveCardEffect("fire_partner_shared_kindling");
            AssertEqual(1, run.State.GetStatus("enemy", "burn"), "Shared kindling applies burn.");
            AssertEqual(1, run.State.GetResource("ember"), "Shared kindling grants ember.");

            MigrationCardBuildRunController guard = NewRun(); // terrain pressure starts at 2
            guard.ResolveCardEffect("fire_defense_phoenix_guard");
            AssertEqual(1, guard.InstalledCards.Count, "Phoenix guard installs a card.");
            AssertEqual(1, guard.TerrainPressure, "Phoenix guard relieves terrain pressure by 1.");
        }

        private static void TestHouraiPhoenixGatedThenBurst()
        {
            MigrationCardBuildRunController gated = NewRun();
            gated.State.AddResource("ember", 2);
            gated.ResolveCardEffect("fire_terminal_hourai_phoenix"); // not vulnerable -> waits
            AssertEqual(540, gated.BossHp, "Hourai phoenix waits without a vulnerability window.");

            // vulnerable, ember 2, burn 1: damage = 80 + 2*34 + 1*16 + 0 = 164; ember spent.
            MigrationCardBuildRunController burst = NewRun();
            burst.OpenVulnerability(3.0);
            burst.State.AddResource("ember", 2);
            burst.State.ApplyStatus("enemy", "burn", 1);
            burst.ResolveCardEffect("fire_terminal_hourai_phoenix");
            AssertEqual(540 - 164, burst.BossHp, "Hourai phoenix burst damage formula.");
            AssertEqual(0, burst.State.GetResource("ember"), "Hourai phoenix spends all ember.");
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
