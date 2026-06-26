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

        private static void AssertEqual<T>(T expected, T actual, string message)
        {
            if (!Equals(expected, actual))
            {
                throw new Exception($"{message} Expected: {expected}. Actual: {actual}.");
            }
        }
    }
}
