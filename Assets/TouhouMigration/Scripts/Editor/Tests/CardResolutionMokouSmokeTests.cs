using System;
using System.Collections.Generic;
using TouhouMigration.Runtime.CardBuild;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    // Covers the mokou-archetype per-card resolution (Godot _apply_cirno_card_resolution mokou branch) —
    // the final archetype, completing the Cirno card resolution.
    public static class CardResolutionMokouSmokeTests
    {
        [MenuItem("Touhou Migration/Tests/Run Card Resolution Mokou Smoke Tests")]
        public static void RunAll()
        {
            TestFireBird();
            TestFujiyamaBurst();
            TestFlameFist();
            TestHouraiDollGateThenBurst();
            TestXuFuDimensionAndHonestMansDeath();
            TestMeltTheLakeContestsAndSeals();
            Debug.Log("Card resolution mokou smoke tests passed.");
        }

        private static MigrationCardBuildRunController NewRun()
        {
            return new MigrationCardBuildRunController(
                new List<string> { "a", "b" }, bossMaxHp: 540, bossClauseId: "cirno_domain");
        }

        private static void TestFireBird()
        {
            MigrationCardBuildRunController closed = NewRun();
            closed.ResolveCardEffect("mokou_starter_fire_bird");
            AssertEqual(540, closed.BossHp, "Fire bird does nothing while guarded.");

            MigrationCardBuildRunController open = NewRun();
            open.OpenVulnerability(2.0);
            open.RewrittenRuleCount = 1;
            open.State.AddResource("ember", 2);
            open.ResolveCardEffect("mokou_starter_fire_bird");
            AssertEqual(540 - (20 + 6 + 4), open.BossHp, "Fire bird deals 20 + 6/rewritten + 2/ember vulnerable.");
        }

        private static void TestFujiyamaBurst()
        {
            // burn 2, ember 1, not vulnerable: 28 + 40 + 5 = 73; burn consumed; terrain 2 -> 1.
            MigrationCardBuildRunController run = NewRun();
            run.State.ApplyStatus("enemy", "burn", 2);
            run.State.AddResource("ember", 1);
            run.ResolveCardEffect("mokou_payoff_fujiyama_burst");
            AssertEqual(540 - 73, run.BossHp, "Fujiyama burst = 28 + burn*20 + ember*5.");
            AssertEqual(0, run.State.GetStatus("enemy", "burn"), "Fujiyama consumes burn.");
            AssertEqual(1, run.TerrainPressure, "Fujiyama relieves terrain pressure.");
        }

        private static void TestFlameFist()
        {
            // ember 2, no burn: 24 + 6 = 30; applies burn 1.
            MigrationCardBuildRunController noBurn = NewRun();
            noBurn.State.AddResource("ember", 2);
            noBurn.ResolveCardEffect("mokou_attack_flame_fist");
            AssertEqual(540 - 30, noBurn.BossHp, "Flame fist base = 24 + ember*3.");
            AssertEqual(1, noBurn.State.GetStatus("enemy", "burn"), "Flame fist applies burn.");

            // with existing burn: +18 and opens a vulnerability window.
            MigrationCardBuildRunController withBurn = NewRun();
            withBurn.State.AddResource("ember", 2);
            withBurn.State.ApplyStatus("enemy", "burn", 1);
            withBurn.ResolveCardEffect("mokou_attack_flame_fist");
            AssertEqual(540 - (24 + 6 + 18), withBurn.BossHp, "Flame fist adds 18 against a burning enemy.");
            AssertEqual(true, withBurn.IsVulnerabilityOpen, "A burning flame fist opens vulnerability.");
        }

        private static void TestHouraiDollGateThenBurst()
        {
            MigrationCardBuildRunController gated = NewRun();
            gated.State.AddResource("ember", 1);
            gated.ResolveCardEffect("mokou_terminal_hourai_doll"); // ember < 2 -> waits
            AssertEqual(540, gated.BossHp, "Hourai doll waits below 2 ember.");

            // ember 2, burn 1, not vulnerable: 92 + 60 + 14 + 0 = 166; ember spent.
            MigrationCardBuildRunController burst = NewRun();
            burst.State.AddResource("ember", 2);
            burst.State.ApplyStatus("enemy", "burn", 1);
            burst.ResolveCardEffect("mokou_terminal_hourai_doll");
            AssertEqual(540 - 166, burst.BossHp, "Hourai doll burst = 92 + ember*30 + burn*14.");
            AssertEqual(0, burst.State.GetResource("ember"), "Hourai doll spends all ember.");
        }

        private static void TestXuFuDimensionAndHonestMansDeath()
        {
            MigrationCardBuildRunController xu = NewRun(); // terrain pressure 2
            xu.ResolveCardEffect("mokou_defense_xu_fu_dimension");
            AssertEqual(0, xu.TerrainPressure, "Xu Fu dimension relieves 2 terrain pressure.");
            AssertEqual(true, xu.IsTerrainSuppressed, "Xu Fu dimension suppresses terrain.");

            MigrationCardBuildRunController honest = NewRun(); // terrain pressure 2
            honest.ResolveCardEffect("mokou_risk_honest_mans_death");
            AssertEqual(1, honest.InstalledCards.Count, "Honest man's death installs a card.");
            AssertEqual(3, honest.TerrainPressure, "Honest man's death raises terrain pressure.");
            AssertEqual(true, honest.IsVulnerabilityOpen, "Honest man's death opens vulnerability.");
        }

        private static void TestMeltTheLakeContestsAndSeals()
        {
            MigrationCardBuildRunController run = NewRun();
            // The Cirno domain + clause are installed exposed at run setup; model that here.
            run.Domains.Install("cirno_domain", threshold: 1, answerFamilies: new[] { "field_replace" });
            run.Clauses.Install("cirno_domain", new[] { "field_replace" }, revealed: true, exposed: true);
            run.State.AddResource("ember", 1);
            run.State.ApplyStatus("enemy", "burn", 1);

            run.ResolveCardEffect("mokou_boss_melt_the_lake");

            AssertEqual(0, run.State.GetResource("ember"), "Melt the lake spends an ember.");
            AssertEqual(1, run.RewrittenRuleCount, "Melt the lake rewrites a rule.");
            AssertEqual(true, run.Domains.IsSealed("cirno_domain"), "The domain contest breaks (seals) the domain.");
            AssertEqual(true, run.Clauses.IsSealed("cirno_domain"), "A sealed domain seals the boss clause via the answer.");
            AssertEqual(true, run.IsVulnerabilityOpen, "Melt the lake opens a wide vulnerability window.");

            // Gated: without ember/burn it waits.
            MigrationCardBuildRunController idle = NewRun();
            idle.Domains.Install("cirno_domain", threshold: 1);
            idle.ResolveCardEffect("mokou_boss_melt_the_lake");
            AssertEqual(false, idle.Domains.IsSealed("cirno_domain"), "Melt the lake waits without ember and burn.");
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
