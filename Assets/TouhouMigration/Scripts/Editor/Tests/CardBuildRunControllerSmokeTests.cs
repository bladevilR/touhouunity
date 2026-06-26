using System;
using System.Collections.Generic;
using TouhouMigration.Runtime.CardBuild;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    // Covers MigrationCardBuildRunController: the facade that composes the split CardBuild units (deck,
    // resources/statuses, boss HP, vulnerability window, clauses, domains, Mokou chain) and resolves the
    // composite queries — notably is_vulnerability_open = window-open OR boss-clause-sealed, and a
    // player attack resolved through the controller's own terrain/rewritten/vulnerability state.
    public static class CardBuildRunControllerSmokeTests
    {
        [MenuItem("Touhou Migration/Tests/Run Card Build Run Controller Smoke Tests")]
        public static void RunAll()
        {
            TestComposesFreshState();
            TestResourceAndDeckDelegation();
            TestVulnerabilityIsWindowOrClauseSealed();
            TestAttackUsesCompositeVulnerabilityAndTerrain();
            TestTerrainSuppressionAndPressureClamp();
            TestSetupCirnoRunInstallsClauseAndDomain();
            TestCardCooldownDurations();
            TestFullRunSnapshotRoundTrip();
            Debug.Log("Card build run controller smoke tests passed.");
        }

        private static MigrationCardBuildRunController NewRun()
        {
            return new MigrationCardBuildRunController(
                new List<string> { "a", "b", "c", "d", "e" }, bossMaxHp: 540, bossClauseId: "cirno_domain");
        }

        private static void TestComposesFreshState()
        {
            MigrationCardBuildRunController run = NewRun();
            AssertEqual(540, run.BossHp, "Boss starts at full HP.");
            AssertEqual(false, run.IsBossDefeated, "Boss is not defeated at the start.");
            AssertEqual(2, run.TerrainPressure, "Terrain pressure defaults to 2 (Godot init).");
            AssertEqual(0, run.RewrittenRuleCount, "Rewritten-rule count defaults to 0.");
            AssertEqual(false, run.IsVulnerabilityOpen, "Vulnerability starts closed.");
            AssertEqual(true, run.Deck != null, "The deck unit is exposed.");
            AssertEqual(true, run.State != null, "The run-state unit is exposed.");
            AssertEqual(true, run.Mokou != null, "The Mokou chain unit is exposed.");
        }

        private static void TestResourceAndDeckDelegation()
        {
            MigrationCardBuildRunController run = NewRun();
            run.State.AddResource("ember", 3);
            AssertEqual(3, run.State.GetResource("ember"), "Resource ops delegate to the run state.");

            int drawn = run.Deck.Draw(2, _ => 0);
            AssertEqual(2, drawn, "Deck draw delegates to the deck unit.");
            AssertEqual(2, run.Deck.HandCount, "The drawn cards are in hand.");
        }

        private static void TestVulnerabilityIsWindowOrClauseSealed()
        {
            MigrationCardBuildRunController run = NewRun();
            AssertEqual(false, run.IsVulnerabilityOpen, "Closed window + unsealed clause -> not vulnerable.");

            run.OpenVulnerability(2.0);
            AssertEqual(true, run.IsVulnerabilityOpen, "An open window opens vulnerability.");

            run.TickVulnerability(5.0);
            AssertEqual(false, run.IsVulnerabilityOpen, "Draining the window closes vulnerability.");

            // Sealing the boss clause opens vulnerability even with the window closed.
            run.Clauses.Install("cirno_domain", new[] { "field_replace" });
            run.Clauses.Expose("cirno_domain");
            run.Clauses.SealWithAnswer("cirno_domain", "field_replace", 2);
            AssertEqual(true, run.IsVulnerabilityOpen, "A sealed boss clause opens vulnerability (Godot disjunct).");
        }

        private static void TestAttackUsesCompositeVulnerabilityAndTerrain()
        {
            MigrationCardBuildRunController run = NewRun();
            // Not vulnerable, default terrain pressure 2 -> x0.18; round(50*0.18)=9.
            AssertEqual(9, run.ApplyPlayerAttack(50), "A guarded attack uses the x0.18 chip multiplier.");
            AssertEqual(531, run.BossHp, "The resolved damage is applied to boss HP.");

            // Open the window -> vulnerable -> x1.0; round(50*1.0)=50.
            run.OpenVulnerability(2.0);
            AssertEqual(50, run.ApplyPlayerAttack(50), "An open vulnerability deals full damage.");
        }

        private static void TestTerrainSuppressionAndPressureClamp()
        {
            MigrationCardBuildRunController run = NewRun();
            AssertEqual(false, run.IsTerrainSuppressed, "Terrain starts unsuppressed.");

            run.SuppressTerrain(2.0);
            AssertEqual(true, run.IsTerrainSuppressed, "Suppressing terrain reads suppressed.");
            run.TickTerrainSuppression(5.0);
            AssertEqual(false, run.IsTerrainSuppressed, "Draining the suppression window clears it.");

            // A sealed boss clause suppresses terrain even with the window closed (Godot disjunct).
            run.Clauses.Install("cirno_domain", new[] { "field_replace" });
            run.Clauses.Expose("cirno_domain");
            run.Clauses.SealWithAnswer("cirno_domain", "field_replace", 2);
            AssertEqual(true, run.IsTerrainSuppressed, "A sealed clause suppresses terrain.");

            // Pressure clamps within [0, MaxTerrainPressure].
            MigrationCardBuildRunController p = NewRun(); // TerrainPressure 2
            p.AddTerrainPressure(10);
            AssertEqual(MigrationCardBuildRunController.MaxTerrainPressure, p.TerrainPressure, "Pressure clamps at the max.");
            p.AddTerrainPressure(-100);
            AssertEqual(0, p.TerrainPressure, "Pressure clamps at zero.");
        }

        private static void TestSetupCirnoRunInstallsClauseAndDomain()
        {
            // The default boss clause id is the faithful Godot CIRNO_CLAUSE_ID.
            MigrationCardBuildRunController run = new MigrationCardBuildRunController(new List<string> { "a", "b" });
            AssertEqual("terrain_tyranny", run.BossClauseId, "The default clause id is the Godot CIRNO_CLAUSE_ID.");

            run.SetupCirnoRun();
            AssertEqual(true, run.Clauses.IsRevealed("terrain_tyranny"), "Setup reveals the Cirno clause.");
            AssertEqual(true, run.Clauses.IsExposed("terrain_tyranny"), "Setup exposes the Cirno clause.");
            AssertEqual(3, run.Domains.GetThreshold("terrain_tyranny"), "The Cirno domain has a threshold of 3.");

            // With the clause exposed, three melt-the-lake contests seal the domain -> seal the clause.
            run.State.AddResource("ember", 9);
            for (int i = 0; i < 3; i++)
            {
                run.State.ApplyStatus("enemy", "burn", 1);
                run.ResolveCardEffect("mokou_boss_melt_the_lake");
            }

            AssertEqual(true, run.Domains.IsSealed("terrain_tyranny"), "Three contests break the threshold-3 domain.");
            AssertEqual(true, run.Clauses.IsSealed("terrain_tyranny"), "Sealing the domain seals the Cirno clause.");
            AssertEqual(true, run.IsVulnerabilityOpen, "A sealed clause keeps vulnerability open.");
        }

        private static void TestCardCooldownDurations()
        {
            AssertEqual(8.0, MigrationCardBuildRunController.CardCooldownDuration("fire_terminal_hourai_phoenix"),
                "Terminal cards have an 8s replay cooldown.");
            AssertEqual(2.0, MigrationCardBuildRunController.CardCooldownDuration("mokou_starter_fire_bird"),
                "Starter cards cool down fast (2s).");
            AssertEqual(3.0, MigrationCardBuildRunController.CardCooldownDuration("some_unlisted_card"),
                "An unlisted card uses the 3s default.");

            // PlayCard applies the card's CARD_COOLDOWNS duration when no explicit value is given.
            MigrationCardBuildRunController run = new MigrationCardBuildRunController(
                new List<string> { "mokou_starter_fire_bird", "x" });
            run.Deck.Draw(2, _ => 0);
            run.PlayCard("mokou_starter_fire_bird", new List<MigrationCardEffectBlock>());
            AssertEqual(2.0, run.GetCardCooldown("mokou_starter_fire_bird"),
                "Playing a card sets its CARD_COOLDOWNS replay cooldown.");
        }

        private static void TestFullRunSnapshotRoundTrip()
        {
            MigrationCardBuildRunController run = new MigrationCardBuildRunController(
                new List<string> { "a", "b", "c", "d" });
            run.SetupCirnoRun();
            run.Deck.Draw(2, _ => 0);
            run.State.AddResource("ember", 3);
            run.State.ApplyStatus("enemy", "burn", 2);
            run.Boss.Damage(140);                  // boss hp 400
            run.RewrittenRuleCount = 1;
            run.TerrainPressure = 4;
            run.OpenVulnerability(2.5);
            run.SuppressTerrain(3.0);
            run.SetCardCooldown("a", 5.0);

            CardBuildRunSnapshot snapshot = run.CaptureSnapshot();

            MigrationCardBuildRunController restored = new MigrationCardBuildRunController(
                new List<string> { "x" }); // different deck — fully overwritten by the load
            restored.RestoreSnapshot(snapshot);

            AssertEqual(400, restored.BossHp, "Boss HP round-trips.");
            AssertEqual(3, restored.State.GetResource("ember"), "Resources round-trip.");
            AssertEqual(2, restored.State.GetStatus("enemy", "burn"), "Statuses round-trip.");
            AssertEqual(1, restored.RewrittenRuleCount, "Rewritten-rule count round-trips.");
            AssertEqual(4, restored.TerrainPressure, "Terrain pressure round-trips.");
            AssertEqual(2, restored.Deck.HandCount, "The deck (hand) round-trips.");
            AssertEqual(true, restored.IsVulnerabilityOpen, "The vulnerability window round-trips open.");
            AssertEqual(true, restored.IsTerrainSuppressed, "Terrain suppression round-trips.");
            AssertEqual(true, restored.IsCardOnCooldown("a"), "Card cooldowns round-trip.");
            AssertEqual(true, restored.Clauses.IsExposed("terrain_tyranny"), "The boss clause (from setup) round-trips.");
            AssertEqual(3, restored.Domains.GetThreshold("terrain_tyranny"), "The boss domain round-trips.");
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
